using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

using System;

public class DestructionFunctions : IStateFunction {
    public static DestructionFunctions Singleton { get; } = new();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        StatePrimitive ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.FitsInto(descendant)) {
            var resolved = descendant.Preferred != null
                           && descendantNode.IsOptionalElement
                           && descendant.CanBeConvertedTo(descendant.Preferred)
                ? (ITicNodeState)descendant.Preferred
                : ancestor;
            if (descendant.IsOptional && resolved != StatePrimitive.None) {
                descendantNode.State = StateOptional.Of(resolved);
                // State changed to composite — drop edge before it re-fires as Prim→Composite.
                descendantNode.RemoveAncestor(ancestorNode);
            }
            else
                descendantNode.State = resolved;
        }

        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // None ≤ opt(T) lift — surfaces here through StateFun ret-node positional inversion.
        if (ancestor == StatePrimitive.None && descendant is StateOptional)
            return true;
        // IsOptionalElement bypass: Optional element nodes may carry Optional state mid-resolve.
        if (!descendant.CanBePessimisticConvertedTo(ancestor) && !descendantNode.IsOptionalElement)
            throw Errors.TicErrors.IncompatibleNodes(ancestorNode, descendantNode);
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        // None ≤ opt(T): no-op. MaterializeOptionalFlags resolves later.
        if (descendant == StatePrimitive.None && ancestor.IsOptional)
            return true;
        if (ancestor.CanBeConvertedTo(descendant)) {
            if (ancestor.IsOptional) {
                ancestorNode.State = StateOptional.Of(descendant);
                descendantNode.RemoveAncestor(ancestorNode);
            } else {
                ancestorNode.State = descendant;
            }
        }
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Wrap as opt(descendant) instead of flat-merging; otherwise a later None
        // descendant would collapse the merged CS, losing Optional.
        if (ancestor.IsOptional && !descendant.IsOptional)
        {
            // Contractivity (Amadio-Cardelli '93 §3): T ≤ opt(T) is trivial when
            // descendant alias-equals the node we'd wrap — wrapping yields μX.opt(X).
            // Mirrors StagesExtension.WrapAncestorInOptional.
            if (ReferenceEquals(descendantNode.GetNonReference(), ancestorNode))
            {
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            ancestorNode.State = new StateOptional(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        // Pass ancestorNode so IntersectIntervalsOrNull's IsOptional OR-gate fires for
        // negative-skolem and IsOptionalElement carriers.
        var result = ancestor.MergeOrNull(descendant, ancestorNode);
        if (result == null)
            return false;

        if (result is StatePrimitive)
        {
            descendantNode.State = ancestorNode.State = result;
            return true;
        }

        // Split T_inner = ancestor, T_outer = opt(ancestor) when OR-gate suppressed
        // descendant's Optional — a plain RefTo would erase it.
        // See specs_tic/TicTechnicalDebt.md #17.
        bool descendantHadSuppressedOpt =
            ancestorNode.IsForcedNonOptional
            && (descendant.IsOptional || descendant.Descendant is StateOptional)
            && result is ConstraintsState rcs
            && !rcs.IsOptional;

        if (ancestorNode.Type == TicNodeType.TypeVariable ||
            descendantNode.Type != TicNodeType.TypeVariable)
        {
            ancestorNode.State = result;
            descendantNode.State = descendantHadSuppressedOpt
                ? (ITicNodeState)new StateOptional(ancestorNode)
                : new StateRefTo(ancestorNode);
        }
        else
        {
            descendantNode.State = result;
            ancestorNode.State = new StateRefTo(descendantNode);
        }

        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Rigidity for `??` element: when ancestor is marked IsSignatureParam
        // (set by SetCoalesce on U) and descendant is Optional, redirect
        // ancestor to the descendant's INNER element instead of the descendant
        // itself. This implements the algebraic rule "?? unwraps Optional" at
        // the destruction layer: U must not absorb the descendant's Optional
        // outer (BugHunt-stmt #50). Pure Pull-time rigidity is insufficient
        // because the descendant only becomes Optional during Destruction.
        if (ancestorNode.IsSignatureParam && descendant is StateOptional descOpt) {
            ancestorNode.State = new StateRefTo(descOpt.ElementNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        if (descendant.FitsInto(ancestor))
        {
            if (ancestor.IsOptional) {
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestorNode.Name + "'", new StateRefTo(descendantNode));
                ancestorNode.State = new StateOptional(innerNode);
            } else {
                ancestorNode.State = new StateRefTo(descendantNode);
            }
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        TraceLog.WriteLine($"{descendant} does not fit into {ancestor}");

        // WORKAROUND (specs_tic/TicTechnicalDebt.md #5 — stale Pull snapshots):
        // Phase 1 snapshot pre-dates Phase 2 Optional wrapping of elements.
        // Clean fix: single-pass Pull or snapshot refresh.
        if (DescendantHasOptionalLift(ancestor.Descendant, descendant))
        {
            ancestorNode.State = new StateRefTo(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        // WORKAROUND (specs_tic/TicTechnicalDebt.md #5 — reverse direction):
        // snapshot has Optional elements actual lacks. Destruct via the snapshot.
        if (ancestor.Descendant is StateArray ancArr && descendant is StateArray descArr
            && ancArr.Element is StateOptional && descArr.Element is not StateOptional)
        {
            TraceLog.WriteLine($"  Reverse Optional lift: snapshot={ancestor.Descendant}, actual={descendant}");
            ancestorNode.State = ancestor.Descendant;
            descendantNode.RemoveAncestor(ancestorNode);
            return Apply(ancArr, descArr, ancestorNode, descendantNode);
        }

        if (descendant.GetType() == ancestor.Descendant?.GetType())
        {
            // Recurse via snapshot. innerNode (not ancestorNode) preserves the Optional wrapper.
            TicNode destructTarget;
            if (ancestor.IsOptional)
            {
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestorNode.Name + "'", ancestor.Descendant);
                ancestorNode.State = new StateOptional(innerNode);
                destructTarget = innerNode;
            }
            else
            {
                ancestorNode.State = ancestor.Descendant;
                destructTarget = ancestorNode;
            }
            descendantNode.RemoveAncestor(ancestorNode);
            return descendant switch {
                StateArray array =>
                    Apply((StateArray)ancestor.Descendant, array, destructTarget, descendantNode),
                StateFun fun =>
                    Apply((StateFun)ancestor.Descendant, fun, destructTarget, descendantNode),
                StateStruct @struct =>
                    Apply((StateStruct)ancestor.Descendant, @struct, destructTarget, descendantNode),
                StateOptional opt =>
                    Apply((StateOptional)ancestor.Descendant, opt, destructTarget, descendantNode),
                StateCollection coll =>
                    Apply((StateCollection)ancestor.Descendant, coll, destructTarget, descendantNode),
                _ => throw new NotSupportedException($"type {descendant} is not supported for destruction")
            };
        }

        TraceLog.WriteLine($"{descendant} completely not fit into {ancestor}");
        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateOptional opt)
        {
            if (descendant.Name != PrimitiveTypeName.None)
            {
                SolvingFunctions.Destruction(descendantNode, opt.ElementNode);
                descendantNode.RemoveAncestor(ancestorNode);
            }

            return true;
        }

        return false;
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Materialize IsOptional descendant to opt(inner), then element-level destruct.
        // Cycle guard: if ancestor's element chains back to descendant, use the ancestor edge.
        if (ancestor is StateOptional ancOptEarly && descendant.IsOptional)
        {
            var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
            innerCs.Preferred = descendant.Preferred;
            var innerNode = TicNode.CreateTypeVariableNode(
                "e" + descendantNode.Name + "'", innerCs);
            descendantNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            var ancElem = ancOptEarly.ElementNode.GetNonReference();
            if (ancElem == descendantNode || ancElem == innerNode) {
                if (ancElem.IsSolved)
                    innerNode.State = ancElem.State;
                else
                    innerNode.AddAncestor(ancElem);
            } else {
                Apply(ancOptEarly, (StateOptional)descendantNode.State, ancestorNode, descendantNode);
            }
            return true;
        }

        if (ancestor.FitsInto(descendant))
        {
            descendantNode.State = descendant.IsOptional
                ? new StateOptional(TicNode.CreateTypeVariableNode(
                    "e" + descendantNode.Name + "'",
                    new StateRefTo(ancestorNode)))
                : new StateRefTo(ancestorNode);
            descendantNode.RemoveAncestor(ancestorNode);
        }
        else if (ancestor is StateStruct ancStruct
                 && descendant.HasDescendant && descendant.Descendant is StateStruct)
        {
            // Transform to struct; preserve outer Optional when descendant.IsOptional.
            var descStruct = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
            if (descStruct != null)
            {
                if (descendant.IsOptional) {
                    var innerNode = TicNode.CreateTypeVariableNode(
                        "e" + descendantNode.Name + "'", descStruct);
                    var optState = new StateOptional(innerNode);
                    descendantNode.State = optState;
                    descendantNode.RemoveAncestor(ancestorNode);
                    SolvingFunctions.Destruction(innerNode, ancestorNode);
                } else {
                    descendantNode.State = descStruct;
                    descendantNode.RemoveAncestor(ancestorNode);
                    Apply(ancStruct, descStruct, ancestorNode, descendantNode);
                }
            }
            else
            {
                TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
            }
        }
        else if (ancestor is StateOptional ancOpt)
        {
            if (descendant.HasDescendant && descendant.Descendant is StateOptional)
            {
                var descOpt = SolvingFunctions.TransformToOptionalOrNull(descendantNode.Name, descendant);
                if (descOpt != null)
                {
                    descendantNode.State = descOpt;
                    descendantNode.RemoveAncestor(ancestorNode);
                    Apply(ancOpt, descOpt, ancestorNode, descendantNode);
                }
                else
                {
                    TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
                }
            }
            else if (descendant.IsOptional)
            {
                // IsOptional descendant: materialize to opt(inner), then opt-vs-opt destruct.
                var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
                innerCs.Preferred = descendant.Preferred;
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + descendantNode.Name + "'", innerCs);
                descendantNode.State = new StateOptional(innerNode);
                descendantNode.RemoveAncestor(ancestorNode);
                Apply(ancOpt, (StateOptional)descendantNode.State, ancestorNode, descendantNode);
            }
            else
            {
                SolvingFunctions.Destruction(descendantNode, ancOpt.ElementNode);
                descendantNode.RemoveAncestor(ancestorNode);
            }
        }
        else
        {
            TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
            // Reject only when descendant CS has a concrete Ancestor cap that
            // excludes the composite. DestructionRec ignores bool return —
            // a silent `return true` here lets TIC commit a bogus type that
            // VarTypeConverter then coerces at runtime, so throw explicitly.
            // Unconstrained-descendant cases stay silent-true for back-compat.
            if (descendant.HasAncestor && descendant.Ancestor != null
                && !ancestor.FitsInto(descendant.Ancestor))
                throw Errors.TicErrors.IncompatibleNodes(ancestorNode, descendantNode);
        }

        return true;
    }

    public bool Apply(StateOptional ancestor, StateOptional descendant, TicNode ancestorNode, TicNode descendantNode) =>
        SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) =>
        SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);

    /// <summary>Destruction for unified single-arg invariant collections.
    /// Element MergeInplace is the invariance rejection channel — Pull/Push
    /// (covariant-style) cannot deliver it alone.</summary>
    public bool Apply(StateCollection ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Constructor != descendant.Constructor)
        {
            if (!IsArrayBranchKind(ancestor.Constructor)
                || !IsArrayBranchKind(descendant.Constructor)
                || !IsSubtypeOrEqual(descendant.Constructor, ancestor.Constructor))
                return false;
            // Cross-kind at Destruction: MergeInplace requires invariance and crashes
            // when ElementNodes hold cross-kind solved collections. Recursive
            // Destruction routes same-kind through MergeInplace, cross-kind recurses.
            return SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);
        }
        if (descendant.ElementNode != ancestor.ElementNode)
            SolvingFunctions.MergeInplace(descendant.ElementNode, ancestor.ElementNode);
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

    /// <summary>
    /// Cross-family Destruction: Array-branch StateCollection (List / Array /
    /// FixedArray) ≤ legacy StateArray. Covariant element fit. Runtime gap
    /// closed by <see cref="VarTypeConverter"/> at the call-site cast.
    /// </summary>
    public bool Apply(StateArray ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(descendant.Constructor))
            return false;
        return SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);
    }

    public bool Apply(StateCollection ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(ancestor.Constructor))
            return false;
        return SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);
    }

    private static bool IsArrayBranchKind(ConstructorKind kind) =>
        kind == ConstructorKind.List
        || kind == ConstructorKind.Array
        || kind == ConstructorKind.FixedArray;

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Function subtyping for `desc ≤ anc`:
        //   args contravariant: Destruction(anc.arg, desc.arg)
        //   ret  covariant:     Destruction(desc.ret, anc.ret)
        // Signature is Destruction(subtype, supertype).
        if (ancestor.ArgsCount == descendant.ArgsCount)
            for (int i = 0; i < ancestor.ArgsCount; i++)
                SolvingFunctions.Destruction(ancestor.ArgNodes[i], descendant.ArgNodes[i]);
        SolvingFunctions.Destruction(descendant.RetNode, ancestor.RetNode);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        // MutStruct × MutStruct: invariant (MergeInplace).
        // MutStruct × Struct: Mut ≤ Frozen (Liskov read-only view) — handled by
        // the covariant field-by-field branch below.
        bool invariant = ancestor is StateMutableStruct && descendant is StateMutableStruct;

        var sameFieldCount = 0;
        foreach (var (key, ancFieldNode) in ancestor.Fields)
        {
            var descFieldNode = descendant.GetFieldOrNull(key);
            if (descFieldNode == null)
                continue; // width subtyping: descendant may have fewer fields

            if (invariant)
            {
                // Stale-snapshot None ≤ opt(T) lift at invariant fields: Pull Phase 1
                // snapshots `none` defaults; Phase 2 lifts the other side to opt(T).
                // Raw MergeInplace would assert "already solved" on the unequal pair.
                // RefTo redirect preserves the lift. See specs_tic/TicTechnicalDebt.md #5.
                var ancNr = ancFieldNode.GetNonReference();
                var descNr = descFieldNode.GetNonReference();
                if (ancNr.State is StatePrimitive { Name: PrimitiveTypeName.None }
                    && descNr.State is StateOptional)
                {
                    ancNr.State = new StateRefTo(descNr);
                    sameFieldCount++;
                    continue;
                }
                if (descNr.State is StatePrimitive { Name: PrimitiveTypeName.None }
                    && ancNr.State is StateOptional)
                {
                    descNr.State = new StateRefTo(ancNr);
                    sameFieldCount++;
                    continue;
                }
                SolvingFunctions.MergeInplace(descFieldNode, ancFieldNode);
                sameFieldCount++;
                continue;
            }

            // Stale-snapshot None ≤ opt(T) lift (covariant branch).
            // See specs_tic/TicTechnicalDebt.md #5.
            var ancFieldNr = ancFieldNode.GetNonReference();
            var descFieldNr = descFieldNode.GetNonReference();
            if (ancFieldNr.State is StatePrimitive { Name: PrimitiveTypeName.None }
                && descFieldNr.State is StateOptional)
            {
                ancFieldNr.State = new StateRefTo(descFieldNr);
                sameFieldCount++;
                continue;
            }

            if (SolvingFunctions.Destruction(descFieldNode, ancFieldNode))
                sameFieldCount++;
        }

        // Redirect only when field nodes resolved to identical targets — prevents
        // overwriting an LCA result when types are compatible but unequal.
        if (sameFieldCount == ancestor.FieldsCount && sameFieldCount == descendant.FieldsCount)
        {
            bool fieldsEquivalent = true;
            foreach (var (key, ancFieldNode) in ancestor.Fields)
            {
                var descFieldNode = descendant.GetFieldOrNull(key);
                if (ancFieldNode.GetNonReference() != descFieldNode.GetNonReference())
                {
                    fieldsEquivalent = false;
                    break;
                }
            }
            if (fieldsEquivalent)
                ancestorNode.State = new StateRefTo(descendantNode);
        }

        return true;
    }

    /// <summary>Detects stale-snapshot Optional lift in nested composite layers.
    /// See specs_tic/TicTechnicalDebt.md #5.</summary>
    private static bool DescendantHasOptionalLift(ITicNodeState staleDescendant, ICompositeState actual) =>
        (staleDescendant, actual) switch {
            (StateArray staleArr, StateArray actualArr) =>
                IsOptionalLiftBetween(staleArr.Element, actualArr.Element),
            (StateStruct staleStr, StateStruct actualStr) =>
                HasAnyOptionalLiftedField(staleStr, actualStr),
            _ => false
        };

    /// <summary>Direct or deeper-position Optional-lift divergence.</summary>
    private static bool IsOptionalLiftBetween(ITicNodeState stale, ITicNodeState actual) {
        if (actual is StateOptional && stale is not StateOptional)
            return true;
        return (stale, actual) switch {
            (StateArray sa, StateArray aa) => IsOptionalLiftBetween(sa.Element, aa.Element),
            (StateStruct ss, StateStruct sas) => HasAnyOptionalLiftedField(ss, sas),
            _ => false
        };
    }

    private static bool HasAnyOptionalLiftedField(StateStruct stale, StateStruct actual) {
        foreach (var (key, actualFieldNode) in actual.Fields) {
            var staleFieldNode = stale.GetFieldOrNull(key);
            if (staleFieldNode == null) continue;
            if (IsOptionalLiftBetween(staleFieldNode.State, actualFieldNode.State))
                return true;
        }
        return false;
    }

    // StateCompositeConstraints Destruction cells: Concretest then re-dispatch.

    public bool Apply(StateCompositeConstraints ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => CompCsApply.ApplySameClass(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        => descendant == StatePrimitive.Any
           || ConcretiseAndRetryForward(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StatePrimitive ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => ancestor == StatePrimitive.Any
           || ConcretiseAndRetryReverse(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.HasDescendant && descendant.Descendant is StateCollection sc)
            return CompCsApply.ForwardPullCompCsSc(ancestor, sc, ancestorNode, descendantNode);
        if (descendant.HasDescendant && descendant.Descendant is StateArray sa)
            return CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: true);
        return false;
    }

    public bool Apply(ConstraintsState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        var concretest = descendant.ConcretestCompCs();
        if (concretest is StateCompositeConstraints) {
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        descendantNode.State = concretest;
        return true;
    }

    public bool Apply(StateCompositeConstraints ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        return descendant switch {
            StateCollection sc => CompCsApply.ForwardPullCompCsSc(ancestor, sc, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: true),
            StateOptional _ => true,
            StateFun _ => false,
            StateStruct _ => false,
            _ => false,
        };
    }

    public bool Apply(ICompositeState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        return ancestor switch {
            StateCollection sc => CompCsApply.ReversePullScCompCs(sc, descendant, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ReverseCompCsStateArray(sa, descendant, ancestorNode, descendantNode, isPull: true),
            StateOptional _ => true,
            StateFun _ => false,
            StateStruct _ => false,
            _ => false,
        };
    }

    private bool ConcretiseAndRetryForward(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        var concretest = ancestor.ConcretestCompCs();
        if (concretest is StateCompositeConstraints) return false;
        ancestorNode.State = concretest;
        return descendant == StatePrimitive.Any;
    }

    private bool ConcretiseAndRetryReverse(StatePrimitive ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        var concretest = descendant.ConcretestCompCs();
        if (concretest is StateCompositeConstraints) return false;
        descendantNode.State = concretest;
        return ancestor == StatePrimitive.Any;
    }
}
