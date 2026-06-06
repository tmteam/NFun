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
                // Remove stale edge: state changed to composite (StateOptional),
                // re-processing would hit Apply(Prim, Composite) → IncompatibleNodes.
                descendantNode.RemoveAncestor(ancestorNode);
            }
            else
                descendantNode.State = resolved;
        }

        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // StateFun's Destruction calls `Destruction(ancestor.RetNode, descendant.RetNode)`
        // with positional args (descendantNode=ancestor.RetNode, ancestorNode=descendant.RetNode)
        // — semantically inverted from intuitive (anc,desc) order. When the actual return-type
        // relation is `None ≤ opt(T)` (e.g. `rule none` body fed into `rule()->T?` field with
        // the body wrapped in a struct under an Optional declaration), this swap surfaces here
        // as `ancestor=None, descendant=opt(T)`. The lift `None ≤ opt(T)` is algebraically
        // valid; accept silently to honor the inversion.
        if (ancestor == StatePrimitive.None && descendant is StateOptional)
            return true;
        // Composite can only fit into Any. However, during Optional solving, Optional element
        // nodes may temporarily have Optional state before Destruction resolves them.
        if (!descendant.CanBePessimisticConvertedTo(ancestor) && !descendantNode.IsOptionalElement)
            throw Errors.TicErrors.IncompatibleNodes(ancestorNode, descendantNode);
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        // None ≤ opt(T): no-op. None adds no information to an Optional constraint.
        // MaterializeOptionalFlags resolves the final type after all branches are processed.
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
        // When ancestor is Optional and descendant is not, wrap as opt(descendant)
        // instead of flat-merging. This mirrors Apply(CS, StatePrimitive) which already
        // does StateOptional.Of(descendant) for non-None primitives.
        // Without this, a subsequent None descendant would collapse the flat-merged
        // ConstraintsState to None, losing the Optional semantic.
        if (ancestor.IsOptional && !descendant.IsOptional)
        {
            ancestorNode.State = new StateOptional(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        var result = ancestor.MergeOrNull(descendant);
        if (result == null)
            return false;

        if (result is StatePrimitive)
        {
            descendantNode.State = ancestorNode.State = result;
            return true;
        }

        if (ancestorNode.Type == TicNodeType.TypeVariable ||
            descendantNode.Type != TicNodeType.TypeVariable)
        {
            ancestorNode.State = result;
            descendantNode.State = new StateRefTo(ancestorNode);
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
                // Wrap in Optional: ancestor becomes opt(descendant)
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

        // WORKAROUND: stale Pull snapshot. Desc-snapshot created in Phase 1, but Phase 2
        // wraps elements in Optional. Snapshot diverges from actual descendant.
        // Root cause: temporal gap in two-phase Pull. Clean fix: single-pass Pull or snapshot refresh.
        // When actual descendant has Optional-wrapped elements that snapshot doesn't know about:
        if (DescendantHasOptionalLift(ancestor.Descendant, descendant))
        {
            ancestorNode.State = new StateRefTo(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        // WORKAROUND: reverse stale snapshot. Snapshot has Optional elements that actual doesn't
        // (e.g., snapshot arr(opt(U8)) from IsOptional LCA, actual arr(U8)).
        // Same root cause as above. Use snapshot and destruct element-by-element.
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
            // Use the snapshot type and recursively destruct element-by-element.
            // Preserve Optional wrapper when ancestor has IsOptional flag.
            // Pass innerNode (not ancestorNode) to recursive Apply so it doesn't overwrite the Optional.
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
                StateMap map =>
                    Apply((StateMap)ancestor.Descendant, map, destructTarget, descendantNode),
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
                // T ≤ opt(T): constrain the Optional's element with T
                SolvingFunctions.Destruction(descendantNode, opt.ElementNode);
                descendantNode.RemoveAncestor(ancestorNode);
            }

            // None ≤ opt(T) is always valid (no-op)
            return true;
        }

        return false;
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // When ancestor is Optional and descendant has IsOptional, materialize descendant
        // to Optional and do element-level destruction. Guard against cycles: if ancestor's
        // element chain leads back to descendantNode, use ancestor edge instead of recursive Apply.
        if (ancestor is StateOptional ancOptEarly && descendant.IsOptional)
        {
            var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
            innerCs.Preferred = descendant.Preferred;
            var innerNode = TicNode.CreateTypeVariableNode(
                "e" + descendantNode.Name + "'", innerCs);
            descendantNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            // Check for cycle: does ancestor element resolve to descendantNode?
            var ancElem = ancOptEarly.ElementNode.GetNonReference();
            if (ancElem == descendantNode || ancElem == innerNode) {
                // Cycle — connect via ancestor edge instead of recursive Apply
                if (ancElem.IsSolved)
                    innerNode.State = ancElem.State;
                else
                    innerNode.AddAncestor(ancElem);
            } else {
                // No cycle — safe to do element-level destruction
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
            // Transform descendant constrains to struct, then destruct field-by-field.
            // Preserve IsOptional: if the constraint was Optional, wrap result in Optional.
            var descStruct = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
            if (descStruct != null)
            {
                if (descendant.IsOptional) {
                    var innerNode = TicNode.CreateTypeVariableNode(
                        "e" + descendantNode.Name + "'", descStruct);
                    var optState = new StateOptional(innerNode);
                    descendantNode.State = optState;
                    descendantNode.RemoveAncestor(ancestorNode);
                    // Destruct inner struct with ancestor
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
                // Descendant has IsOptional flag — materialize to Optional, then
                // do element-level destruction (opt vs opt).
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
                // Implicit lift: T ≤ opt(T). Destruct descendant with Optional's element.
                SolvingFunctions.Destruction(descendantNode, ancOpt.ElementNode);
                descendantNode.RemoveAncestor(ancestorNode);
            }
        }
        else
        {
            TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
            // Genuine incompatibility: ancestor composite (e.g. arr(Ch))
            // cannot fit into descendant CS's interval, and no
            // optional/struct/snapshot rescue branch matched. Per professor's
            // analysis (BugHunt #75): silent `return true` here let TIC
            // commit a bogus type (function inferred `(int)->arr(Ch)` from
            // `f(x) = if(x==0) 'a' else x`) which then silently coerces
            // values via `VarTypeConverter.ToText` at the call site.
            // DestructionRec ignores the bool return — must throw explicitly.
            // Reject only when the descendant CS has a concrete upper bound
            // (`Ancestor`) that genuinely excludes the composite — that's the
            // case where Destruction has no algebraic solution. Other cases
            // (unconstrained descendant) stay silent-true for back-compat.
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

    /// <summary>
    /// Destruction for the unified single-arg invariant collection (Stage 2.1b).
    /// Cross-kind pairs reject (different ConstructorKind cannot share an instance).
    /// Same-kind: MergeInplace on element nodes enforces invariance — unequal
    /// concrete elements throw a TIC error here, which is the rejection channel
    /// that Pull/Push (one-directional, covariant-style) cannot deliver alone.
    /// Mirrors StateMutableStruct's invariant field merge.
    /// </summary>
    public bool Apply(StateCollection ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Constructor != descendant.Constructor)
        {
            if (!IsArrayBranchKind(ancestor.Constructor)
                || !IsArrayBranchKind(descendant.Constructor)
                || !IsSubtypeOrEqual(descendant.Constructor, ancestor.Constructor))
                return false;
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

    /// <summary>
    /// Destruction for <see cref="StateMap"/> — two invariant arguments
    /// (key + value). Cross-state instance: this fires when both sides are
    /// concrete maps. Merge both pairs of nodes; unequal element types raise
    /// a clean TIC error via MergeInplace (same rejection channel as
    /// StateCollection same-kind invariance).
    /// </summary>
    public bool Apply(StateMap ancestor, StateMap descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.KeyNode != ancestor.KeyNode)
            SolvingFunctions.MergeInplace(descendant.KeyNode, ancestor.KeyNode);
        if (descendant.ValueNode != ancestor.ValueNode)
            SolvingFunctions.MergeInplace(descendant.ValueNode, ancestor.ValueNode);
        return true;
    }

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Variance: for `desc ≤ anc` on StateFun (function subtyping):
        //   args contravariant — anc.arg ≤ desc.arg, so call `Destruction(anc.arg, desc.arg)`
        //   ret  covariant     — desc.ret ≤ anc.ret, so call `Destruction(desc.ret, anc.ret)`
        // Signature `Destruction(descendantNode, ancestorNode)`: first positional is the
        // subtype, second is the supertype. The previous wiring inverted both directions
        // : args called as `Destruction(desc.arg, anc.arg)` (covariant —
        // wrong), ret called as `Destruction(anc.ret, desc.ret)` (contravariant — wrong).
        // It usually still worked because in most cases arg/ret nodes are reference-equal after
        // merge (Destruction line 1438 returns true at identity). But when args/rets have a
        // genuine T ≤ opt(T) lift on either side (e.g. SafeAccess unwrap+wrap setup wiring),
        // the inversion surfaced StatePrimitive vs StateOptional mismatch on the wrong side and
        // threw IncompatibleNodes.
        if (ancestor.ArgsCount == descendant.ArgsCount)
            for (int i = 0; i < ancestor.ArgsCount; i++)
                SolvingFunctions.Destruction(ancestor.ArgNodes[i], descendant.ArgNodes[i]);
        SolvingFunctions.Destruction(descendant.RetNode, ancestor.RetNode);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        // MutStruct x MutStruct: invariant fields — use MergeInplace (enforces equality).
        bool invariant = ancestor is StateMutableStruct && descendant is StateMutableStruct;

        // Struct cannot fit into MutStruct (immutable cannot be upgraded to mutable)
        if (ancestor is StateMutableStruct && descendant is not StateMutableStruct)
            throw Errors.TicErrors.IncompatibleNodes(ancestorNode, descendantNode);

        // Destruct field-by-field: for each ancestor field, find matching descendant field.
        var sameFieldCount = 0;
        foreach (var (key, ancFieldNode) in ancestor.Fields)
        {
            var descFieldNode = descendant.GetFieldOrNull(key);
            if (descFieldNode == null)
                continue; // descendant may have fewer fields (struct width subtyping)

            if (invariant)
            {
                // Apply the same stale-None-vs-Optional lift that the covariant branch
                // below honors. In a recursive named struct whose optional field defaults
                // to `none` (`l: tree? = none`), Pull Phase 1 snapshots one literal's `l`
                // as `None` while another literal's `l` (assigned via `t!.l`) reaches
                // `opt(tree)`. The two MutableStruct literals then meet in invariant
                // destruction (mut↔mut), and a raw MergeInplace asserts "Node is already
                // solved" because None and opt(tree) are both solved but unequal
                // (BugHunt-stmt #48). Redirecting the None side to RefTo the Optional
                // side preserves the algebraic lift `None ≤ opt(T)`.
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
                // Invariant: unify fields (must be same type)
                SolvingFunctions.MergeInplace(descFieldNode, ancFieldNode);
                sameFieldCount++;
                continue;
            }

            // Stale snapshot lift: ancestor field came from a Pull-Phase-1 snapshot that captured
            // a `none` default (StatePrimitive.None) before Phase 2 wrapped the corresponding
            // descendant field in Optional. None ≤ opt(T) is a valid implicit lift; rejecting it
            // would error on the algebraically-trivial case. Mirrors Pull's "Field 'X': None desc
            // → skip (handled by outer Optional)" rule. We refresh ancestor's field to RefTo the
            // descendant's (correctly-inferred) field — otherwise the inferred output type would
            // still render the stale `k:none` instead of `k:opt(T)`. Same root as
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

        // Only redirect if all fields match AND field nodes actually resolved to the
        // same node. This prevents overwriting an LCA result when field types differ
        // (e.g. I32 ≤ Re both destruct successfully but aren't equal).
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

    /// <summary>
    /// Checks if the actual descendant composite contains Optional-wrapped elements
    /// that the stale constraint descendant doesn't (e.g. arr(opt(T)) vs arr(U8)).
    /// This happens when Phase 2 wraps array/struct elements in Optional after
    /// the constraint's Descendant was already set. Recurses transitively through
    /// composite layers — μ-identity preservation
    /// keeps deeper nesting intact so the lift detection must walk past one level.
    /// </summary>
    private static bool DescendantHasOptionalLift(ITicNodeState staleDescendant, ICompositeState actual) =>
        (staleDescendant, actual) switch {
            (StateArray staleArr, StateArray actualArr) =>
                IsOptionalLiftBetween(staleArr.Element, actualArr.Element),
            (StateStruct staleStr, StateStruct actualStr) =>
                HasAnyOptionalLiftedField(staleStr, actualStr),
            _ => false
        };

    /// <summary>
    /// Two states differ by Optional-lift if either:
    /// - actual is Optional and stale is not (the direct case);
    /// - both are composite of the same kind, and recursively a deeper position
    ///   shows the same divergence (μ-recursion case: arr(struct{k:opt}) vs arr(struct{k:None})).
    /// </summary>
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

    // ─────────────────────────────────────────────────────────────────────
    // Stage C.3 — StateCompositeConstraints Destruction cells.
    // Destruction Concretest's CompCS to a concrete StateCollection, then
    // re-dispatches via the resolved state.

    public bool Apply(StateCompositeConstraints ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => CompCsApply.ApplySameClass(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        // CompCS resolves to concrete; merge with primitive.
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
        // Concretest descendant, then re-dispatch.
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
            StateMap sm => CompCsApply.ForwardPullCompCsStateMap(ancestor, sm, ancestorNode, descendantNode),
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
            StateMap sm => CompCsApply.ReversePullStateMapCompCs(sm, descendant, ancestorNode, descendantNode),
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
