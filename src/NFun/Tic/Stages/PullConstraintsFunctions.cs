using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PullConstraintsFunctions : IStateFunction {
    public static PullConstraintsFunctions Singleton { get; } = new();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __) =>
        descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ConstraintsState descendant, TicNode _, TicNode __)
        => descendant.CanBeConvertedOptimisticTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        return ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        var ancestorCopy = ancestor.GetCopy();
        // Negative-skolem (??/! rigid U): unwrap `Desc is StateOptional` — absorbing
        // it would yield `opt(opt(X))`. Mirrors IntersectIntervalsOrNull's gate.
        var descDesc = descendant.Descendant;
        if (ancestorNode.IsForcedNonOptional && descDesc is StateOptional descOpt)
            descDesc = descOpt.Element;
        ancestorCopy.AddDescendant(descDesc);
        // Bidirectional Preferred propagation. See specs_tic/Algebra/Preferred.md §3.
        if (ancestorCopy.Preferred == null && descendant.Preferred != null)
            ancestorCopy.Preferred = descendant.Preferred;
        if (descendant.Preferred == null && ancestor.Preferred != null)
            descendant.Preferred = ancestor.Preferred;
        // IsOptional OR-propagation via AddDescendant(None). Skip when ancestor is:
        //   - IsOptionalElement (outer Optional already captures it; opt(opt(T)) forbidden)
        //   - IsForcedNonOptional (??/! rigid U rejects the implicit T ≤ opt(T) lift)
        if (descendant.IsOptional && !ancestorNode.IsOptionalElement && !ancestorNode.IsForcedNonOptional)
            ancestorCopy.AddDescendant(StatePrimitive.None);
        // F-bound StructBound is a third axis. Combining upper bounds is meet (Gcd).
        // No Lca on S — upper bounds are never widened by ≤.
        if (descendant.HasStructBound) {
            ancestorCopy.StructBound = !ancestorCopy.HasStructBound
                ? SolvingFunctions.RewireStructBoundOwnership(descendant.StructBound, descendantNode, ancestorNode)
                : SolvingFunctions.GcdBound(ancestorCopy.StructBound, descendant.StructBound,
                                            ancestorNode, descendantNode);
            if (!ancestorCopy.HasStructBound) return false; // conflict
        }
        var result = ancestorCopy.SimplifyOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant is StateOptional descOpt)
        {
            if (ancestorNode.IsOptionalElement)
            {
                descOpt.ElementNode.AddAncestor(ancestorNode);
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            // Reject if ancestor constraints forbid composite (IsComparable, non-Any ancestor cap).
            var probe = ancestor.GetCopy();
            probe.AddDescendant(descendant);
            if (probe.SimplifyOrNull() == null)
                return false;
            // Wrap ancestor in Optional; inner carries the CS minus IsOptional
            // (consumed by the wrapper).
            var innerCs = ConstraintsState.Of(ancestor.Descendant, ancestor.Ancestor, ancestor.IsComparable);
            innerCs.Preferred = ancestor.Preferred;
            innerCs.IsClearable = ancestor.IsClearable;
            var innerNode = TicNode.CreateTypeVariableNode("e" + ancestorNode.Name + "'", innerCs);
            // F-bound migrates to inner on Optional wrap; self-RefTos pointing at the
            // outer must rewire to innerNode or dangle.
            if (ancestor.HasStructBound)
                innerCs.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                    ancestor.StructBound, ancestorNode, innerNode);
            descOpt.ElementNode.AddAncestor(innerNode);
            ancestorNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        // F-bound + StateStruct descendant: merge desc into the bound via GcdBound
        // (desc treated as a CS-with-S = struct itself).
        if (descendant is StateStruct descStruct && ancestor.HasStructBound)
        {
            var copy = ancestor.GetCopy();
            copy.AddDescendant(descStruct);
            var merged = SolvingFunctions.GcdBound(
                copy.StructBound, descStruct, ancestorNode, descendantNode);
            if (merged == null) return false;
            copy.StructBound = merged;
            var simplified = copy.SimplifyOrNull();
            if (simplified == null) return false;
            ancestorNode.State = simplified;
            return true;
        }
        return ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateOptional opt)
        {
            if (descendant.Name == PrimitiveTypeName.None)
            {
                // None ≤ opt(T): detach edge. IsOptional materializes elsewhere via Apply(CS,None).
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            LiftDescendantToOptionalElement(opt, ancestorNode, descendantNode);
            return true;
        }
        return false;
    }

    /// <summary>Materialise `T ≤ Opt(T)` lift: rewire desc→Opt(T) to desc→T and eagerly
    /// re-Invoke. Eager step needed because Pull is single-pass over toposort.
    /// WORKAROUND: rewire-after-toposort breaks single-pass invariant; proper fix is
    /// worklist Pull. See specs_tic/TicTechnicalDebt.md.</summary>
    private void LiftDescendantToOptionalElement(StateOptional opt, TicNode ancestorNode, TicNode descendantNode) {
        descendantNode.RemoveAncestor(ancestorNode);
        descendantNode.AddAncestor(opt.ElementNode);
        StagesExtension.Invoke(this, opt.ElementNode, descendantNode);
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // IsOptional CS descendant ≡ opt(desc_inner); bare composite ancestor cannot
        // accept it (`opt(T) ≤ T` is rejected — see StagesExtension Push direction).
        // The StateOptional ancestor branch handles it via TransformToOptionalOrNull.
        if (descendant.IsOptional && ancestor is not StateOptional)
            return false;
        switch (ancestor)
        {
            case StateArray ancArray:
            {
                // F-bound × StateArray: structural conflict.
                if (descendant.HasStructBound) return false;
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                if (result == null)
                    return false;
                // WORKAROUND: TransformToArrayOrNull/CollectionOrNull/MapOrNull
                // reuse the descendant collection's element-node directly (perf
                // optimisation — SolvingFunctions.cs line 1569 et al). When the
                // descendant's element already aliases the ancestor's element
                // (chained `[]` over lang collections), AddAncestor(self) panics
                // "Circular ancestor 0". Mirrors the guard in
                // Apply(StateArray, StateCollection) line 430. Proper fix:
                // always allocate fresh element nodes in Transform*.
                // See Specs/Tic/TicTechnicalDebt.md #15.
                if (result.ElementNode != ancArray.ElementNode)
                    result.ElementNode.AddAncestor(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            // Unified single-arg invariant collection (Stage 2.1b). Same shape as
            // the StateArray branch above; kind discriminator carried as data.
            case StateCollection ancColl:
            {
                if (descendant.HasStructBound) return false;
                var result = SolvingFunctions.TransformToCollectionOrNull(
                    ancColl.Constructor, descendantNode.Name, descendant);
                if (result == null) return false;
                if (result.ElementNode != ancColl.ElementNode)
                    result.ElementNode.AddAncestor(ancColl.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            case StateFun ancFun:
            {
                if (descendant.HasStructBound) return false;
                var result = SolvingFunctions.TransformToFunOrNull(
                    descendantNode.Name, descendant, ancFun);
                if (result == null)
                    return false;
                result.RetNode.AddAncestor(ancFun.RetNode);
                for (int i = 0; i < result.ArgsCount; i++)
                    ancFun.ArgNodes[i].AddAncestor(result.ArgNodes[i]);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            case StateStruct ancStruct:
            {
                var result = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
                if (result == null)
                    return false;
                // TypeName conflict between named structs: downgrade both to anonymous.
                // Named types are structural — see Specs/NamedTypes.md.
                if (result.TypeName != null && ancStruct.TypeName != null
                    && !result.TypeName.Equals(ancStruct.TypeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    TraceLog.WriteLine($"    Structural merge: TypeName conflict in struct/CS Pull — anc {ancStruct.TypeName} vs desc {result.TypeName} → downgrade to anonymous");
                    result.TypeName = null;
                    ancStruct.TypeName = null;
                }
                // F-bound flowing into concrete struct: merge S via GcdBound (field union).
                if (descendant.HasStructBound)
                {
                    var merged = SolvingFunctions.GcdBound(
                        result, descendant.StructBound, descendantNode, descendantNode);
                    if (merged == null) return false;
                    result = merged;
                }
                descendantNode.State = result;
                // Field-identity unification — see UnifyStructFieldIdentities doc.
                SolvingFunctions.UnifyStructFieldIdentities(result.Fields, ancStruct);
                // Width propagation: add desc-only fields to non-frozen ancestor so
                // generic-T constraint graphs preserve full shape.
                if (!ancStruct.IsFrozen)
                {
                    foreach (var descField in result.Fields)
                    {
                        if (ancStruct.GetFieldOrNull(descField.Key) == null)
                        {
                            TraceLog.WriteLine($"    Width propagation (CS→Struct): adding field '{descField.Key}' to ancestor {ancestorNode.Name}");
                            ancStruct.AddField(descField.Key, descField.Value);
                            ancestorNode.State = ancStruct;
                        }
                    }
                }
                // Propagate IsOptionalSourced — merged identity, cycle-rescue must apply uniformly.
                if (result.IsOptionalSourced) ancStruct.IsOptionalSourced = true;
                return true;
            }
            case StateOptional ancOpt:
            {
                var result = SolvingFunctions.TransformToOptionalOrNull(descendantNode.Name, descendant);
                if (result == null)
                {
                    if (descendant.HasDescendant && descendant.Descendant is StateStruct)
                    {
                        // Struct desc × Optional anc: wrap as opt(inner) carrying the struct constraints.
                        var innerCsCopy = descendant.GetCopy();
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", innerCsCopy);
                        if (innerCsCopy.HasStructBound)
                            innerCsCopy.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                                innerCsCopy.StructBound, descendantNode, innerNode);
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        return true;
                    }
                    // IsOptional descendant: materialize as opt(inner) so IsOptional doesn't
                    // leak through the implicit lift to a rigid carrier (e.g. SetCoalesce U).
                    if (descendant.IsOptional)
                    {
                        var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
                        innerCs.Preferred = descendant.Preferred;
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", innerCs);
                        if (descendant.HasStructBound)
                            innerCs.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                                descendant.StructBound, descendantNode, innerNode);
                        innerNode.IsOptionalElement = true;
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        return true;
                    }
                    if (!descendant.HasDescendant || descendant.Descendant is StatePrimitive)
                    {
                        if (!descendant.HasDescendant
                            || descendant.Descendant != StatePrimitive.None)
                        {
                            // Non-None descendant: rewire+propagate via helper.
                            LiftDescendantToOptionalElement(ancOpt, ancestorNode, descendantNode);
                        }
                        else
                        {
                            // None descendant: detach; IsOptional materializes via Apply(CS,None).
                            descendantNode.RemoveAncestor(ancestorNode);
                        }
                    }
                    return true;
                }
                result.ElementNode.AddAncestor(ancOpt.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            default: return true;
        }
    }

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ElementNode != ancestor.ElementNode)
        {
            // Descendant element Optional, ancestor not: wrap ancestor element too.
            // Triggered by re-pull after Phase 2 wraps inner elements.
            if (descendant.ElementNode.State is StateOptional descOpt
                && ancestor.ElementNode.State is ConstraintsState ancCon)
            {
                // Unwrap nested opt in ancestor's Descendant — outer wrapper consumes it.
                var innerCopy = ancCon.GetCopy();
                if (innerCopy.HasDescendant && innerCopy.Descendant is StateOptional innerOptDesc) {
                    var pref = innerCopy.Preferred;
                    innerCopy = ConstraintsState.Of(innerOptDesc.Element, innerCopy.Ancestor, innerCopy.IsComparable);
                    innerCopy.Preferred = pref;
                }
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestor.ElementNode.Name + "'", innerCopy);
                descOpt.ElementNode.AddAncestor(innerNode);
                ancestor.ElementNode.State = new StateOptional(innerNode);
                ancestor.ElementNode.IsOptionalElement = true;
            }
            else
            {
                descendant.ElementNode.AddAncestor(ancestor.ElementNode);
            }
        }
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>Pull for unified single-arg invariant collections.
    /// Invariance is enforced later in Destruction via MergeInplace.</summary>
    public bool Apply(StateCollection ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Constructor != descendant.Constructor)
        {
            // Array-branch: List ⊂ Array ⊂ FixedArray — desc must be at-or-below anc.
            if (!IsArrayBranchKind(ancestor.Constructor)
                || !IsArrayBranchKind(descendant.Constructor)
                || !IsSubtypeOrEqual(descendant.Constructor, ancestor.Constructor))
                return false;
        }
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    private static bool IsSubtypeOrEqual(ConstructorKind sub, ConstructorKind sup) {
        if (sub == sup) return true;
        if (sup == ConstructorKind.Array && sub == ConstructorKind.List) return true;
        if (sup == ConstructorKind.FixedArray
            && (sub == ConstructorKind.Array || sub == ConstructorKind.List))
            return true;
        return false;
    }

    /// <summary>Cross-family Pull: Array-branch StateCollection ≤ StateArray.
    /// Set rejected (different branch).</summary>
    public bool Apply(StateArray ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(descendant.Constructor))
            return false;
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(StateCollection ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(ancestor.Constructor))
            return false;
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    private static bool IsArrayBranchKind(ConstructorKind kind) =>
        kind == ConstructorKind.List
        || kind == ConstructorKind.Array
        || kind == ConstructorKind.FixedArray;

    // StateMap deleted — Map flows as StateCollection(Map, pair-struct) and
    // map-vs-map Pull is handled by the StateCollection same-kind cell above.

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ArgsCount != ancestor.ArgsCount)
            return false;
        // Bug hunt round 10 #52. Identity-share between ancestor and descendant
        // StateFun (e.g. through Bug #46's IsLiveSnapshotableFun preserving the
        // exact StateFun instance in a CS Descendant, then a downstream edge
        // re-meeting it) reaches this cell with `descendant.RetNode ==
        // ancestor.RetNode` and/or `descendant.ArgNodes[i] == ancestor.ArgNodes[i]`.
        // AddAncestor(self) panics "Circular ancestor 0" — `T ≤ T` is the identity
        // ordering element and must be elided structurally, not emitted as an edge.
        // Mirrors the existing guards in Apply(StateArray,…) :206 / :220 /
        // Apply(StateArray, StateCollection) :378, :426.
        if (descendant.RetNode != ancestor.RetNode)
            descendant.RetNode.AddAncestor(ancestor.RetNode);
        for (int i = 0; i < descendant.ArgsCount; i++)
            if (ancestor.ArgNodes[i] != descendant.ArgNodes[i])
                ancestor.ArgNodes[i].AddAncestor(descendant.ArgNodes[i]);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        TraceLog.WriteLine($"  Pull Struct<-Struct: anc={ancestorNode.Name}:{ancestor.StateDescription} desc={descendantNode.Name}:{descendant.StateDescription}");
        // Merge identity bidirectionally across the Pull struct≤struct edge. After width
        // propagation makes field sets equal, both sides represent the same merged value — they
        // must agree on TypeName and IsOptionalSourced. The merge rule lives in
        // StateStruct.MergedTypeName. Named types are structural per Specs/NamedTypes.md
        // (L3 + L124) — on TypeName conflict downgrade to anonymous rather than rejecting;
        // the field Pull recursion below catches actual shape mismatches. (Bug HH.)
        var mergedName = StateStruct.MergedTypeName(ancestor.TypeName, descendant.TypeName);
        if (mergedName == null && ancestor.TypeName != null && descendant.TypeName != null) {
            TraceLog.WriteLine($"    Structural merge: TypeName conflict {ancestor.TypeName} vs {descendant.TypeName} → downgrade to anonymous");
        }
        ancestor.TypeName = descendant.TypeName = mergedName;
        var mergedOptSourced = StateStruct.MergedIsOptionalSourced(ancestor.IsOptionalSourced, descendant.IsOptionalSourced);
        ancestor.IsOptionalSourced = descendant.IsOptionalSourced = mergedOptSourced;
        foreach (var ancField in ancestor.Fields)
        {
            var descField = descendant.GetFieldOrNull(ancField.Key);
            if (descField == null)
            {
                if (descendant.IsFrozen)
                {
                    TraceLog.WriteLine($"    BLOCKED: desc is frozen, cannot add field '{ancField.Key}'");
                    return false;
                }

                TraceLog.WriteLine($"    Adding field '{ancField.Key}' to desc");
                descendant.AddField(ancField.Key, ancField.Value);
                descendantNode.State = descendant;
            }
            else if (descField != ancField.Value)
            {
                // Tautology guard: shared RefTo target ⇒ already tied. Self-loop
                // would collapse to Any via cycle resolver (recursive named μ-positions).
                if (descField.GetNonReference() == ancField.Value.GetNonReference())
                {
                    TraceLog.WriteLine($"    Field '{ancField.Key}': desc and anc share non-ref target → skip (μ-cycle tautology)");
                    continue;
                }
                // Nominal μ-recursion: same-TypeName μ-positions are equal by nominal
                // definition; per-field reduction would drop recursive identity and resolve to Any.
                if (mergedName != null
                    && FieldReachesNamedType(ancField.Value, mergedName)
                    && FieldReachesNamedType(descField, mergedName))
                {
                    TraceLog.WriteLine($"    Field '{ancField.Key}': nominal μ-position of '{mergedName}' → skip per-field merge");
                    continue;
                }
                // None field: lift ancestor CS to IsOptional via AddDescendant(None).
                // Bare composite/primitive ancestor has no slot — rely on outer Optional.
                if (descField.GetNonReference().State == StatePrimitive.None)
                {
                    var ancFieldNr = ancField.Value.GetNonReference();
                    if (ancFieldNr.State is ConstraintsState ancFieldCs)
                    {
                        ancFieldCs.AddDescendant(StatePrimitive.None);
                        TraceLog.WriteLine($"    Field '{ancField.Key}': None desc → anc CS IsOptional lift");
                    }
                    else
                    {
                        TraceLog.WriteLine($"    Field '{ancField.Key}': None desc → skip (handled by outer Optional)");
                    }
                    continue;
                }
                TraceLog.WriteLine($"    Field '{ancField.Key}': desc ≤ anc ({descField.State} ≤ {ancField.Value.State})");
                descField.AddAncestor(ancField.Value);
            }
        }

        // Width propagation to non-frozen ancestor: desc ≤ anc with anc inferred ⇒
        // anc absorbs all desc fields to stay as narrow as possible.
        if (!ancestor.IsFrozen)
        {
            foreach (var descField in descendant.Fields)
            {
                if (ancestor.GetFieldOrNull(descField.Key) == null)
                {
                    TraceLog.WriteLine($"    Width propagation: adding field '{descField.Key}' to ancestor");
                    ancestor.AddField(descField.Key, descField.Value);
                    ancestorNode.State = ancestor;
                }
            }
        }

        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }


    public bool Apply(StateOptional ancestor, StateOptional descendant, TicNode ancestorNode, TicNode descendantNode) {
        // GetNonReference: recognise RefTo-chain tautology at μ-cycle.
        if (descendant.ElementNode.GetNonReference() != ancestor.ElementNode.GetNonReference())
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>True iff field reaches a struct with the given TypeName via
    /// Optional/Array unwrapping (μ-position of the named recursive type).</summary>
    private static bool FieldReachesNamedType(TicNode field, string typeName) {
        if (string.IsNullOrEmpty(typeName)) return false;
        return ReachesNamedTypeRec(field, typeName, new HashSet<TicNode>());
    }

    private static bool ReachesNamedTypeRec(TicNode n, string typeName, HashSet<TicNode> visited) {
        var nr = n.GetNonReference();
        if (!visited.Add(nr)) return false;
        if (nr.State is StateStruct s && string.Equals(s.TypeName, typeName, StringComparison.OrdinalIgnoreCase))
            return true;
        if (nr.State is StateOptional opt) return ReachesNamedTypeRec(opt.ElementNode, typeName, visited);
        if (nr.State is StateArray arr) return ReachesNamedTypeRec(arr.ElementNode, typeName, visited);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ApplyAncestorConstrains(TicNode ancestorNode, ConstraintsState ancestor, ITypeState typeDesc) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(typeDesc);
        var result = ancestorCopy.SimplifyOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }

    // StateCompositeConstraints Pull cells.
    // See specs_tic/Algebra/CompositeConstraints.md §4.

    public bool Apply(StateCompositeConstraints ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => CompCsApply.ApplySameClass(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        // None desc → set IsOptional and detach. Non-None primitive → reject.
        if (descendant.Name == PrimitiveTypeName.None) {
            var newState = ancestor.WithIsOptional(true);
            if (newState == null) return false;
            ancestorNode.State = newState;
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        return false;
    }

    public bool Apply(StatePrimitive ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Any anc → no-op; non-Any → reject.
        return ancestor == StatePrimitive.Any;
    }

    public bool Apply(StateCompositeConstraints ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.HasDescendant && descendant.Descendant is StateCollection sc)
            return CompCsApply.ForwardPullCompCsSc(ancestor, sc, ancestorNode, descendantNode);
        if (descendant.HasDescendant && descendant.Descendant is StateArray sa)
            return CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: true);
        // Propagate IsClearable downward (mirror of the Push CompCs→CS rule).
        // Pull fires before Push and downstream Pull cells consult the flag.
        if (ancestor.IsClearable && !descendant.IsClearable)
            descendant.IsClearable = true;
        // Defer-on-empty: accept unless CS positively forbids composites
        // (IsComparable / non-Any primitive Ancestor / non-None primitive Descendant).
        if (descendant.IsComparable) return false;
        if (descendant.HasAncestor && descendant.Ancestor is StatePrimitive prim && prim != StatePrimitive.Any)
            return false;
        // Primitive Descendant forbids composite Enumerable/Clearable ancestor —
        // otherwise an early-toposort literal bound (e.g. `[1,2,3]`) leaks past
        // deferred-accept and surfaces as a runtime cast.
        if (descendant.HasDescendant && descendant.Descendant is StatePrimitive primDesc && primDesc != StatePrimitive.None)
            return false;
        return true;
    }

    public bool Apply(ConstraintsState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        // CS without IsComparable blocker: install desc's Concretest into CS (CS.Desc widens).
        if (ancestor.IsComparable) return false;
        var concretest = descendant.ConcretestCompCs();
        if (concretest is StateCompositeConstraints) {
            // Still abstract — defer.
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        if (concretest is ITypeState concreteType)
            return ApplyAncestorConstrains(ancestorNode, ancestor, concreteType);
        return false;
    }

    public bool Apply(StateCompositeConstraints ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        return descendant switch {
            StateCollection sc => CompCsApply.ForwardPullCompCsSc(ancestor, sc, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: true),
            StateOptional opt => ApplyForwardOptional(ancestor, opt, ancestorNode, descendantNode),
            StateFun _ => false,
            StateStruct _ => false,
            _ => false,
        };
    }

    public bool Apply(ICompositeState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        return ancestor switch {
            StateCollection sc => CompCsApply.ReversePullScCompCs(sc, descendant, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ReverseCompCsStateArray(sa, descendant, ancestorNode, descendantNode, isPull: true),
            StateOptional opt => ApplyReverseOptional(opt, descendant, ancestorNode, descendantNode),
            StateFun _ => false,
            StateStruct _ => false,
            _ => false,
        };
    }

    private bool ApplyForwardOptional(StateCompositeConstraints ancestor, StateOptional opt, TicNode ancestorNode, TicNode descendantNode) {
        // Unwrap: set IsOptional on CompCS, route the opt.Element as the descendant.
        var optedState = ancestor.WithIsOptional(true);
        if (optedState == null) return false;
        ancestorNode.State = optedState;
        descendantNode.RemoveAncestor(ancestorNode);
        opt.ElementNode.AddAncestor(ancestorNode);
        return true;
    }

    private bool ApplyReverseOptional(StateOptional opt, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.IsOptional) {
            descendantNode.RemoveAncestor(ancestorNode);
            descendantNode.AddAncestor(opt.ElementNode);
            return true;
        }
        descendantNode.RemoveAncestor(ancestorNode);
        descendantNode.AddAncestor(opt.ElementNode);
        return true;
    }
}
