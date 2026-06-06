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
        // None is handled by AddDescendant setting IsOptional flag — no special wrapping needed.
        return ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(descendant.Descendant);
        // Propagate Preferred bidirectionally:
        // Upward (desc→anc): integer constants push I32 to array element types.
        // Downward (anc→desc): struct field chains push I32 to generic function results.
        if (ancestorCopy.Preferred == null && descendant.Preferred != null)
            ancestorCopy.Preferred = descendant.Preferred;
        if (descendant.Preferred == null && ancestor.Preferred != null)
            descendant.Preferred = ancestor.Preferred;
        // Propagate IsOptional flag (OR semantics): if descendant is optional,
        // the ancestor must be optional too. Uses AddDescendant(None) which sets the flag.
        if (descendant.IsOptional)
            ancestorCopy.AddDescendant(StatePrimitive.None);
        // F-bound StructBound is a third independent constraint dimension. Combining two upper
        // bounds is meet (Gcd = field union, narrower predicate). Both Pull and Push merge S via
        // Gcd — there is no Lca on S because upper bounds are never widened by ≤. Contractivity
        // preserved when both inputs are contractive (no wrapper structure changed).
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
            // Verify ancestor constraints are compatible with Optional.
            // If ancestor has constraints that reject composite types (e.g., IsComparable
            // for arithmetic, or HasAncestor with a non-Any bound), reject.
            var probe = ancestor.GetCopy();
            probe.AddDescendant(descendant);
            if (probe.SimplifyOrNull() == null)
                return false;
            // Wrap ancestor in Optional, connecting element nodes for covariant propagation.
            // The inner element gets a copy of ancestor constraints WITHOUT IsOptional —
            // the Optional wrapper consumes the IsOptional flag.
            var innerCs = ConstraintsState.Of(ancestor.Descendant, ancestor.Ancestor, ancestor.IsComparable);
            innerCs.Preferred = ancestor.Preferred;
            var innerNode = TicNode.CreateTypeVariableNode("e" + ancestorNode.Name + "'", innerCs);
            // F-bound migrates to inner CS on Optional wrap. The bound lives on the recursive
            // variable (the inner element); self-RefTos in the bound that pointed at ancestorNode
            // (the outer) must be rewired to point at innerNode, otherwise opt(CS{S}) with
            // back-edges to outer-ancestor leaves dangling refs.
            if (ancestor.HasStructBound)
                innerCs.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                    ancestor.StructBound, ancestorNode, innerNode);
            descOpt.ElementNode.AddAncestor(innerNode);
            ancestorNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        // Descendant is StateStruct and ancestor has F-bound. The descendant struct is the
        // candidate value; its fields contribute to the bound's required field set. GcdBound
        // merges desc as another bound contributor (treats StateStruct as "CS-with-S = struct itself").
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
                // None ≤ Opt(T) — None doesn't propagate to the element; the IsOptional
                // flag is set by Apply(CS,None) elsewhere when None reaches a CS-typed
                // result. Just remove the opt-edge.
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            LiftDescendantToOptionalElement(opt, ancestorNode, descendantNode);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Lifts a non-None descendant from `Opt(T)` ancestor down to the element node `T`.
    /// Implements the implicit-lift rule `T ≤ Opt(T)` materialisation: when a descendant
    /// edge `desc → Opt(T)` is processed during Pull, rewire it to `desc → T` so the
    /// descendant's state flows into the element, and EAGERLY propagate state — Pull is
    /// single-pass over toposort and won't revisit the rewired ancestor's outgoing edges.
    ///
    /// Used at both primitive-descendant (`Apply(ICompositeState, StatePrimitive)`) and
    /// CS-descendant (`Apply(CS, ICompositeState)` for descendants resolving to primitive)
    /// sites. Without the eager propagation, `true ?? 1.5` would resolve to `Real = true`
    /// (Bool never reaches the LCA target — MR6Bug3 family) and `1 ?? 'hello'` would
    /// resolve to `arr(Ch) = 1` (MR3Bug1, originally).
    ///
    /// WORKAROUND-of-debt: the rewire+propagate pattern violates Pull's single-pass
    /// toposort invariant — new edges should not appear AFTER toposort fixes node order.
    /// The principled fix is a worklist Pull that re-fires propagation on rewires
    /// automatically. Tracked in Specs/Tic/TicTechnicalDebt.md. For now, both call
    /// sites use THIS helper to ensure the eager-propagation step cannot be forgotten.
    /// </summary>
    private void LiftDescendantToOptionalElement(StateOptional opt, TicNode ancestorNode, TicNode descendantNode) {
        descendantNode.RemoveAncestor(ancestorNode);
        descendantNode.AddAncestor(opt.ElementNode);
        StagesExtension.Invoke(this, opt.ElementNode, descendantNode);
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Symmetric to Apply(CS, StateOptional) below: an IsOptional CS descendant
        // logically represents opt(desc_inner). Per Pull's documented invariant
        // (StagesExtension.cs lines 40-62) `opt(T) ≤ T` is rejected — a bare
        // composite ancestor cannot accept it. Without this guard, the per-shape
        // cases below TransformToArray/Fun/Struct strip the IsOptional flag
        // silently and the chain `arr?.method()[idx]` compiles into an unsound
        // bare-index that crashes at runtime when the source is none. The
        // StateOptional ancestor case below handles IsOptional descendants
        // correctly via TransformToOptionalOrNull. (MR7Bug3.)
        if (descendant.IsOptional && ancestor is not StateOptional)
            return false;
        switch (ancestor)
        {
            case StateArray ancArray:
            {
                // Descendant has F-bound (struct shape) but ancestor is StateArray —
                // structural conflict (struct ≠ array).
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
            // Stage 5 / Map.2 — two-arg invariant map. Mirror of StateCollection
            // but propagate both KeyNode and ValueNode.
            case StateMap ancMap:
            {
                if (descendant.HasStructBound) return false;
                var result = SolvingFunctions.TransformToMapOrNull(descendantNode.Name, descendant);
                if (result == null) return false;
                if (result.KeyNode != ancMap.KeyNode)
                    result.KeyNode.AddAncestor(ancMap.KeyNode);
                if (result.ValueNode != ancMap.ValueNode)
                    result.ValueNode.AddAncestor(ancMap.ValueNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            case StateFun ancFun:
            {
                // F-bound vs Fun is a structural conflict.
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
                // Per Specs/NamedTypes.md (L3 "transparent alias — fully interchangeable",
                // L124 "named types are structural — no runtime distinction") two distinct
                // named structs with identical shape ARE compatible. On TypeName conflict
                // downgrade both sides to anonymous instead of rejecting; the field-level
                // Pull below still catches genuine shape mismatches. (Bug HH.)
                if (result.TypeName != null && ancStruct.TypeName != null
                    && !result.TypeName.Equals(ancStruct.TypeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    TraceLog.WriteLine($"    Structural merge: TypeName conflict in struct/CS Pull — anc {ancStruct.TypeName} vs desc {result.TypeName} → downgrade to anonymous");
                    result.TypeName = null;
                    ancStruct.TypeName = null;
                }
                // F-bound flowing INTO concrete struct. Descendant carries S = upper-bound shape;
                // merge S into the produced struct so width-required fields become explicit
                // constraints. GcdBound treats result + S as two parallel descendants, returning
                // their meet (field union).
                if (descendant.HasStructBound)
                {
                    var merged = SolvingFunctions.GcdBound(
                        result, descendant.StructBound, descendantNode, descendantNode);
                    if (merged == null) return false;
                    result = merged;
                }
                descendantNode.State = result;
                // Field-identity unification for shared fields whose snapshot side
                // is a solved primitive and ancestor side is an empty CS placeholder
                // (typical SetFieldAccess setup). Without this step, the field-access
                // result node stays disconnected from the source's concrete primitive
                // and the cascade through `+`/arithmetic picks the wrong type at
                // Destruction. Safe-merge predicate lives in
                // <see cref="SolvingFunctions.UnifyStructFieldIdentities"/> — see
                // its doc for the algebraic justification.
                SolvingFunctions.UnifyStructFieldIdentities(result.Fields, ancStruct);
                // Width propagation: descendant struct may have more fields than
                // ancestor (e.g., generic T constrained to {a} via SetFieldAccess,
                // but input provides {a,b}). Add extra fields to non-frozen ancestor
                // to preserve type info through generic constraint graph.
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
                // If descendant's struct is opt-sourced, propagate the marker to the ancestor —
                // both share the same merged identity and the cycle-rescue rule must apply uniformly.
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
                        // Struct descendant with explicit Optional ancestor
                        // (e.g., ?.field sets opt(struct) ancestor on lambda param that already
                        // has struct descendant from generic function binding).
                        // Transform to opt(inner) carrying the struct constraints.
                        var innerCsCopy = descendant.GetCopy();
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", innerCsCopy);
                        // Rewire StructBound self-refs to new inner owner.
                        if (innerCsCopy.HasStructBound)
                            innerCsCopy.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                                innerCsCopy.StructBound, descendantNode, innerNode);
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        return true;
                    }
                    // When descendant is already marked Optional (from None in another branch),
                    // materialize as opt(inner) and connect inner to ancestor's element.
                    // This prevents IsOptional from leaking through the implicit lift redirect
                    // (e.g., SetCoalesce's opt(U) ancestor — U must NOT inherit IsOptional).
                    if (descendant.IsOptional)
                    {
                        var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
                        innerCs.Preferred = descendant.Preferred;
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", innerCs);
                        // F-bound migrates to inner CS on Optional materialization.
                        if (descendant.HasStructBound)
                            innerCs.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                                descendant.StructBound, descendantNode, innerNode);
                        innerNode.IsOptionalElement = true;
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        return true;
                    }
                    // Implicit lift: T ≤ opt(T).
                    // Only redirect to element for primitive/empty constraints.
                    if (!descendant.HasDescendant || descendant.Descendant is StatePrimitive)
                    {
                        if (!descendant.HasDescendant
                            || descendant.Descendant != StatePrimitive.None)
                        {
                            // Non-None descendant: shared rewire+propagate helper
                            // (originally MR3Bug1's fix; now also fixes MR6Bug3).
                            LiftDescendantToOptionalElement(ancOpt, ancestorNode, descendantNode);
                        }
                        else
                        {
                            // None descendant: detach edge, no propagation needed
                            // (IsOptional is materialized elsewhere via Apply(CS,None)).
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
            // When descendant element is Optional but ancestor element is not,
            // the ancestor element needs to be wrapped in Optional too.
            // This happens during re-pull after PullNoneNode Phase 2 wraps inner elements.
            if (descendant.ElementNode.State is StateOptional descOpt
                && ancestor.ElementNode.State is ConstraintsState ancCon)
            {
                // Create inner CS for the Optional element. When the ancestor's descendant
                // is itself Optional (e.g., opt(I32) from a named type snapshot), unwrap it:
                // the outer Optional wrapper consumes the Optional layer.
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

    /// <summary>
    /// Pull for the unified single-arg invariant collection (Stage 2.1b).
    /// Cross-kind pairs reject (uniform invariance). Same-kind: propagate
    /// element subtype edge — covariant-style. Invariance is enforced LATER
    /// in Destruction via MergeInplace (mirrors StateMutableStruct's pattern).
    /// </summary>
    public bool Apply(StateCollection ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Constructor != descendant.Constructor)
        {
            // Cross-kind among the Array-branch: per Stage 0 lattice,
            // descendant must be at-or-below ancestor (List ⊂ Array ⊂ FixedArray).
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
        // Lattice subtype: List ⊆ Array ⊆ FixedArray (Array-branch chain).
        if (sup == ConstructorKind.Array && sub == ConstructorKind.List) return true;
        if (sup == ConstructorKind.FixedArray
            && (sub == ConstructorKind.Array || sub == ConstructorKind.List))
            return true;
        return false;
    }

    /// <summary>
    /// Cross-family Pull: any Array-branch StateCollection (List / Array /
    /// FixedArray) fits into the legacy StateArray slot per Stage 0 hierarchy
    /// <c>List ⊆ Array ⊆ FixedArray</c>. Set is rejected — different branch.
    /// Element propagation is covariant-style (same as
    /// <c>Apply(StateArray, StateArray)</c>).
    /// </summary>
    public bool Apply(StateArray ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(descendant.Constructor))
            return false;
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(StateCollection ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Reverse direction: legacy T[] flows into lang collection slot.
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

    /// <summary>
    /// Stage 5 / Map.2 — Pull for <c>map&lt;K, V&gt; ≤ map&lt;K, V&gt;</c>.
    /// Both key and value invariant: route both pairs through Pull (each side
    /// gets its descendant-node as an ancestor edge of the other). Cross-map
    /// kind dispatch (Map vs Set, Map vs Collection) rejected at
    /// <see cref="StagesExtension"/> dispatch table.
    /// </summary>
    public bool Apply(StateMap ancestor, StateMap descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.KeyNode != ancestor.KeyNode)
            descendant.KeyNode.AddAncestor(ancestor.KeyNode);
        if (descendant.ValueNode != ancestor.ValueNode)
            descendant.ValueNode.AddAncestor(ancestor.ValueNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ArgsCount != ancestor.ArgsCount)
            return false;
        descendant.RetNode.AddAncestor(ancestor.RetNode);
        for (int i = 0; i < descendant.ArgsCount; i++)
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
                // Tautology guard: when descField and ancField resolve (via RefTo)
                // to the same target node, they are ALREADY tied — adding an
                // ancestor edge creates a self-loop that the cycle resolver
                // collapses to Any. This occurs at recursive μ-positions of named
                // types (#10): body literal's field is RefTo to function return,
                // ancestor's field cycles back to same node.
                if (descField.GetNonReference() == ancField.Value.GetNonReference())
                {
                    TraceLog.WriteLine($"    Field '{ancField.Key}': desc and anc share non-ref target → skip (μ-cycle tautology)");
                    continue;
                }
                // Nominal μ-recursion (#10): when both sides are at a μ-position of
                // the same named type, they are equal by nominal definition (named
                // types are nominally identified by TypeName). Per-field reduction
                // is not just unnecessary — it produces fresh anonymous nodes that
                // drop the recursive identity and resolve to Any.
                if (mergedName != null
                    && FieldReachesNamedType(ancField.Value, mergedName)
                    && FieldReachesNamedType(descField, mergedName))
                {
                    TraceLog.WriteLine($"    Field '{ancField.Key}': nominal μ-position of '{mergedName}' → skip per-field merge");
                    continue;
                }
                // None field: None ≤ opt(T) is valid for any T, but None cannot be
                // directly connected to a bare struct/array/fun ancestor. When the
                // ancestor field is a CS, lift it to Optional by setting the
                // IsOptional flag (via AddDescendant(None)) — this handles the
                // function-arg case where two signature args share the same generic
                // and one push constrains the field while another carries `none`.
                // Without this, e.g. `factory({v=1}, {v=none})` with shared V silently
                // drops the None and resolves V to int instead of int?. When the
                // ancestor field is a bare composite/primitive there's no slot to
                // lift into — rely on the outer Optional layer (array LCA, etc).
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

        // Width propagation: when descendant has extra fields and ancestor is mutable
        // (inferred/generic, not frozen), add them to ancestor. This preserves type
        // information through generic constraints (e.g., sort(T[],rule(T)->R):T[]
        // where T must carry all input struct fields, not just those accessed in rule).
        // Algebraic basis: desc ≤ anc, anc is being inferred → anc should be as
        // narrow (specific) as possible → absorb all descendant fields.
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
        // Compare through GetNonReference so RefTo chains pointing at the same
        // node are recognised as tautology (#10 μ-cycle case).
        if (descendant.ElementNode.GetNonReference() != ancestor.ElementNode.GetNonReference())
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>
    /// True iff <paramref name="field"/> reaches a struct with TypeName == <paramref name="typeName"/>
    /// via Optional / Array unwrapping (μ-position of the named recursive type).
    /// Used by Pull Struct←Struct to recognise that ancField and descField sit at the same
    /// nominal recursion site, where per-field merge would lose recursive identity.
    /// </summary>
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

    // ─────────────────────────────────────────────────────────────────────
    // Stage C.3 — StateCompositeConstraints Pull cells (Specs/Tic/Algebra_CompositeConstraints.md §4).

    public bool Apply(StateCompositeConstraints ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => CompCsApply.ApplySameClass(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        // §4.0.1: Any descendant → no-op; non-Any → reject (composite anc cannot accept prim).
        // None descendant → set IsOptional flag, detach edge (mirrors CS pattern).
        if (descendant.Name == PrimitiveTypeName.None) {
            var newState = ancestor.WithIsOptional(true);
            if (newState == null) return false;
            ancestorNode.State = newState;
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        // Composite ancestor cannot accept a non-None primitive descendant.
        return false;
    }

    public bool Apply(StatePrimitive ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        // §4.0.2 reverse: Any anc → no-op; non-Any → reject.
        return ancestor == StatePrimitive.Any;
    }

    public bool Apply(StateCompositeConstraints ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // §4.0.3 coerce: route through descendant's composite Descendant if any.
        if (descendant.HasDescendant && descendant.Descendant is StateCollection sc)
            return CompCsApply.ForwardPullCompCsSc(ancestor, sc, ancestorNode, descendantNode);
        if (descendant.HasDescendant && descendant.Descendant is StateArray sa)
            return CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: true);
        // Empty / primitive-bound CS without composite Desc: the constraint hasn't materialised
        // a shape yet. Reject only when CS positively forbids composites (IsComparable, primitive
        // Ancestor cap). Otherwise accept — Pull will fire again when descendant later resolves
        // to a collection (e.g. varargs binds x to StateArray after the count call sets the
        // CompCS ancestor edge). Mirrors the Pull(CS, CS) defer-on-empty pattern.
        if (descendant.IsComparable) return false;
        if (descendant.HasAncestor && descendant.Ancestor is StatePrimitive prim && prim != StatePrimitive.Any)
            return false;
        return true;
    }

    public bool Apply(ConstraintsState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        // §4.0.2 reverse with CS: if CS can accept composite (no IsComparable blocker),
        // route as if CS were a CompCS-view. Pull semantics: CS.Desc widens to admit
        // descendant's CompCS. Simplest sound move: install descendant's Concretest into CS.
        if (ancestor.IsComparable) return false;
        var concretest = descendant.ConcretestCompCs();
        if (concretest is StateCompositeConstraints) {
            // Still abstract — no concrete state to install; defer to later phases.
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
            StateMap sm => CompCsApply.ForwardPullCompCsStateMap(ancestor, sm, ancestorNode, descendantNode),
            StateOptional opt => ApplyForwardOptional(ancestor, opt, ancestorNode, descendantNode),
            StateFun _ => false,    // §4.0.1 explicit reject
            StateStruct _ => false, // §4.0.1 explicit reject
            _ => false,
        };
    }

    public bool Apply(ICompositeState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        return ancestor switch {
            StateCollection sc => CompCsApply.ReversePullScCompCs(sc, descendant, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ReverseCompCsStateArray(sa, descendant, ancestorNode, descendantNode, isPull: true),
            StateMap sm => CompCsApply.ReversePullStateMapCompCs(sm, descendant, ancestorNode, descendantNode),
            StateOptional opt => ApplyReverseOptional(opt, descendant, ancestorNode, descendantNode),
            StateFun _ => false,    // §4.0.2 explicit reject
            StateStruct _ => false, // §4.0.2 explicit reject
            _ => false,
        };
    }

    private bool ApplyForwardOptional(StateCompositeConstraints ancestor, StateOptional opt, TicNode ancestorNode, TicNode descendantNode) {
        // §4.0.1: unwrap, recurse, set IsOptional=true on CompCS.
        var optedState = ancestor.WithIsOptional(true);
        if (optedState == null) return false;
        // Pass-through to the inner element by setting IsOpt-true on the CompCS
        // and treating the opt.Element as the actual descendant.
        ancestorNode.State = optedState;
        descendantNode.RemoveAncestor(ancestorNode);
        opt.ElementNode.AddAncestor(ancestorNode);
        return true;
    }

    private bool ApplyReverseOptional(StateOptional opt, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        // §4.0.2: wrap or unwrap depending on descendant's IsOpt.
        if (descendant.IsOptional) {
            // Both Optional — recurse on inner element.
            descendantNode.RemoveAncestor(ancestorNode);
            descendantNode.AddAncestor(opt.ElementNode);
            return true;
        }
        // Descendant non-Optional, ancestor Optional → implicit lift, route to inner.
        descendantNode.RemoveAncestor(ancestorNode);
        descendantNode.AddAncestor(opt.ElementNode);
        return true;
    }
}
