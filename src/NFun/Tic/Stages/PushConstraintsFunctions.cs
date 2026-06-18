using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PushConstraintsFunctions : IStateFunction {
    public static PushConstraintsFunctions Singleton { get; } = new();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        => descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        descendant.AddAncestor(ancestor);
        var result = descendant.SimplifyOrNull();
        if (result == null)
            return false;
        descendantNode.State = result;
        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode)
        => true;

    public bool Apply(
        ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode,
        TicNode descendantNode) {
        if (!ancestor.HasAncestor)
            return true;
        return descendant.CanBePessimisticConvertedTo(ancestor.Ancestor);
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode,
        TicNode descendantNode) {
        // Propagate IsComparable downward: if ancestor requires comparability,
        // descendant must also be comparable. Rule: D.cmp := D.cmp ∨ A.cmp
        bool flagChanged = false;
        if (ancestor.IsComparable && !descendant.IsComparable) {
            descendant.IsComparable = true;
            flagChanged = true;
        }
        // Same rule for Clearable typeclass: D.clr := D.clr ∨ A.clr
        if (ancestor.IsClearable && !descendant.IsClearable) {
            descendant.IsClearable = true;
            flagChanged = true;
        }

        // F-bound StructBound merges via Gcd (meet of upper bounds). Symmetric to Pull's
        // Apply(CS,CS). Runs independently of HasAncestor — StructBound is a third dimension,
        // peer to IsComparable (which is also propagated above without HasAncestor gating).
        if (ancestor.HasStructBound) {
            descendant.StructBound = !descendant.HasStructBound
                ? SolvingFunctions.RewireStructBoundOwnership(ancestor.StructBound, ancestorNode, descendantNode)
                : SolvingFunctions.GcdBound(descendant.StructBound, ancestor.StructBound,
                                            descendantNode, ancestorNode);
            if (!descendant.HasStructBound) return false; // conflict
            flagChanged = true;
        }

        // Trans-axis consistency check after typeclass-flag propagation. Without
        // this, an IsComparable-only ancestor can stamp IsComparable onto a
        // descendant whose Descendant is a composite (struct/array/fun) — the
        // resulting CS is algebraically inconsistent and Finalize emits a type
        // that crashes at runtime cast. Gated: only validates when a flag
        // actually changed AND the descendant carries something for it to
        // conflict with.
        if (flagChanged && descendant.HasDescendant) {
            if (descendant.SimplifyOrNull() == null) return false;
        }

        if (!ancestor.HasAncestor)
            return true;

        if (!descendant.TryAddAncestor(ancestor.Ancestor))
            return false;
        var result = descendant.SimplifyOrNull();
        if (result == null)
            return false;
        descendantNode.State = result;
        return true;
    }

    public bool Apply(ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.HasAncestor && ancestor.Ancestor != StatePrimitive.Any)
            return false;
        // Clearable typeclass narrowing: when ancestor CS requires IsClearable
        // and descendant is non-Clearable composite, narrow descendant to
        // StateCollection(List) — symmetric to the IsComparable+composite
        // rule in CS.SimplifyOrNull.
        // Only fire when ancestor is NOT IsOptional. With IsOptional, the
        // wrap interacts with the existing Optional-lift cell below (line
        // 154) creating a self-Optional cycle in implicit-None-return bodies.
        if (ancestor.IsClearable && !ancestor.IsOptional) {
            if (descendant is StateArray narrSa) {
                descendantNode.State = new StateCollection(ConstructorKind.List, narrSa.ElementNode);
                return true;
            }
            if (descendant is StateCollection narrSc && !ConstructorLattice.IsClearable(narrSc.Constructor)) {
                descendantNode.State = new StateCollection(ConstructorKind.List, narrSc.ElementNode);
                return true;
            }
        }

        // F-bound on ancestor projects fields down onto descendant struct (covariant width
        // subtype). When ancestor has S and descendant is a StateStruct, treat S like an
        // additional ancestor-side struct descendant — propagate any S-required field that's
        // missing from desc (open-row extension), and push field-state constraints into shared
        // fields. F-bound vs non-struct composite is a structural conflict — reject.
        if (ancestor.HasStructBound)
        {
            if (descendant is StateStruct descStructForBound)
            {
                foreach (var bf in ancestor.StructBound.Fields)
                {
                    var df = descStructForBound.GetFieldOrNull(bf.Key);
                    if (df == null)
                    {
                        if (descStructForBound.IsFrozen) return false;
                        descStructForBound.AddField(bf.Key, bf.Value);
                        continue;
                    }
                    var bfState = bf.Value.GetNonReference().State;
                    var dfState = df.GetNonReference().State;
                    if (bfState is ITypeState && dfState is ITypeState)
                        SolvingFunctions.PushConstraints(df, bf.Value);
                }
                return true;
            }
            else if (descendant is StateArray || descendant is StateFun)
            {
                return false;
            }
        }

        // Cross-Constructor StateCollection: ancestor.Desc carries a Lower-bound SC
        // constraint, descendant is a concrete SC of a wider-or-equal Constructor
        // (per ConstructorLattice). Accept and propagate element constraints
        // covariantly. Without this case, Push falls through to "return true" with
        // no action — the descendant's incompatible state is then re-checked at
        // Destruction and rejected as IncompatibleNodes (0832 LeetCode regression
        // when LCA at AddDescendant time produced a mixed graph). Bug hunt round 6 #32.
        if (ancestor.HasDescendant
            && ancestor.Descendant is StateCollection ancSc
            && descendant is StateCollection descSc
            && ancSc.Constructor != descSc.Constructor
            && IsArrayBranchKind(ancSc.Constructor)
            && IsArrayBranchKind(descSc.Constructor)
            && IsSubtypeOrEqual(ancSc.Constructor, descSc.Constructor))
        {
            // descSc.Constructor ≥ ancSc.Constructor per lattice — propagate
            // element constraints; descendant Constructor stays as-is (wider).
            SolvingFunctions.PushConstraints(descSc.ElementNode, ancSc.ElementNode);
            return true;
        }

        // If ancestor constrains has a struct descendant, propagate field constraints down.
        // Struct fields are covariant (immutable struct).
        if (ancestor.HasDescendant && ancestor.Descendant is StateStruct ancDescStruct
                                   && descendant is StateStruct descStruct)
        {
            foreach (var ancField in ancDescStruct.Fields)
            {
                var descField = descStruct.GetFieldOrNull(ancField.Key);
                if (descField == null) continue;

                if (ancField.Value.State is StateOptional ancOpt && descField.State is ConstraintsState)
                {
                    // Merged struct field is Optional (LCA with none field).
                    // Don't merge opt(T) into constraint — push inner element constraints instead.
                    SolvingFunctions.PushConstraints(ancOpt.ElementNode, descField);
                }
                else if (descField.State is StateOptional descOpt && ancField.Value.State is ConstraintsState)
                {
                    SolvingFunctions.PushConstraints(descOpt.ElementNode, ancField.Value);
                }
                else if (descField.State is ConstraintsState && ancField.Value.State != StatePrimitive.Any)
                    SolvingFunctions.MergeInplace(descField, ancField.Value);
                else
                    SolvingFunctions.PushConstraints(descField, ancField.Value);
            }
        }

        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateOptional opt)
        {
            // None ≤ opt(T) for any T — no constraint on T
            // T_value ≤ opt(T) — propagate: value ≤ T (element of optional)
            if (descendant.Name != PrimitiveTypeName.None)
            {
                descendantNode.AddAncestor(opt.ElementNode);
                SolvingFunctions.PushConstraints(descendantNode, opt.ElementNode);
            }
            return true;
        }
        return false;
    }

    public bool Apply(
        ICompositeState ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode) {
        // Algebraic rule: if descendant is optional (has None branch) and ancestor is
        // a non-Optional composite, the descendant must become opt(composite).
        // Transform to Optional first, then push the composite constraint into the element.
        // This is uniform for Array, Fun, Struct — not a special case per composite type.
        if (descendant.IsOptional && ancestor is not StateOptional) {
            var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
            innerCs.IsClearable = descendant.IsClearable;
            var innerNode = TicNode.CreateTypeVariableNode(
                "e" + descendantNode.Name + "'",
                innerCs);
            innerNode.IsOptionalElement = true;
            descendantNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            innerNode.AddAncestor(ancestorNode);
            SolvingFunctions.PushConstraints(innerNode, ancestorNode);
            return true;
        }

        switch (ancestor)
        {
            // if ancestor is composite type then descendant HAS to have same composite type
            // y:int[] = a:[..]  # 'a' has to be an array
            case StateArray ancArray:
            {
                // Clearable-typeclass intersection: when descendant CS carries
                // IsClearable=true, the only kind in the Array-branch that
                // satisfies Clearable is List. Route through the unified
                // StateCollection(List) path so the descendant collapses to a
                // List which simultaneously satisfies the StateArray ancestor
                // edge (List ⊆ Array-branch in the cross-kind merge rule).
                if (descendant.IsClearable
                    && !descendant.HasDescendant
                    && !descendant.IsComparable
                    && !descendant.HasStructBound) {
                    var listResult = SolvingFunctions.TransformToCollectionOrNull(
                        ConstructorKind.List, descendantNode.Name, descendant);
                    if (listResult == null) return false;
                    if (listResult.ElementNode != ancArray.ElementNode)
                        listResult.ElementNode.AddAncestor(ancArray.ElementNode);
                    descendantNode.State = listResult;
                    descendantNode.RemoveAncestor(ancestorNode);
                    if (listResult.ElementNode != ancArray.ElementNode)
                        SolvingFunctions.PushConstraints(listResult.ElementNode, ancArray.ElementNode);
                    return true;
                }
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                if (result == null)
                    return false;
                if (result.ElementNode == ancArray.ElementNode)
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }

                result.ElementNode.AddAncestor(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                SolvingFunctions.PushConstraints(result.ElementNode, ancArray.ElementNode);
                return true;
            }
            // Unified single-arg invariant collection (Stage 2.1b).
            case StateCollection ancColl:
            {
                var result = SolvingFunctions.TransformToCollectionOrNull(
                    ancColl.Constructor, descendantNode.Name, descendant);
                if (result == null) return false;
                if (result.ElementNode == ancColl.ElementNode) {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }
                result.ElementNode.AddAncestor(ancColl.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                SolvingFunctions.PushConstraints(result.ElementNode, ancColl.ElementNode);
                return true;
            }
            // StateMap deleted — Map flows through `case StateCollection ancColl`
            // above (with kind = ConstructorKind.Map) via the same path.
            // y:f(x) = a:[..]  # 'a' has to be a functional variable
            case StateFun ancFun:
            {
                var descFun = SolvingFunctions.TransformToFunOrNull(descendantNode.Name, descendant, ancFun);
                if (descFun == null)
                    return false;
                if (!descendantNode.State.Equals(descFun))
                {
                    descendantNode.State = descFun;
                    PushFunTypeArgumentsConstraints(descFun, ancFun);
                }

                return true;
            }
            // y:user = a:[..]  # 'a' has to be a struct, that converts to type of 'user'
            case StateStruct ancStruct:
            {
                var descStruct = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
                if (descStruct == null)
                    return false;
                if (descendantNode.State.Equals(descStruct))
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }

                descendantNode.State = descStruct;
                if (TryMergeStructFields(ancStruct, descStruct))
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }

                return false;
            }
            case StateOptional ancOpt:
            {
                var result = SolvingFunctions.TransformToOptionalOrNull(descendantNode.Name, descendant);
                if (result == null)
                {
                    if (descendant.HasDescendant && descendant.Descendant is StateStruct descStruct
                                                 && descStruct.IsOpen)
                    {
                        // Struct descendant is OPEN (row-poly source — came from another ?.field,
                        // a generic lambda param, or similar inference site). Wrap descendant in
                        // Optional, carrying struct constraints into element. This handles map
                        // lambda params on optional struct arrays where Pull single-pass didn't
                        // propagate Optional to the lambda parameter.
                        //
                        // Guard MUST be IsOpen, not !IsSolved: a literal `{b=1}` has field type
                        // `[U8..Re]I32!` (constraint state, not concrete primitive), so !IsSolved
                        // would falsely trigger wrap on closed concrete literals and infect the
                        // receiver with Optional — symptom: `a={b=1}; y=a?.b; z=a.c` rejects
                        // `a.c` because `a` was widened to `{b,c}?`. Closed (literal) structs
                        // must use implicit lift T ≤ Opt(T) like primitives, not the wrap path.
                        // (MR5Bug5.)
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", descendant.GetCopy());
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        SolvingFunctions.PushConstraints(innerNode, ancOpt.ElementNode);
                        return true;
                    }
                    // When descendant has IsOptional flag (contains None branch),
                    // it represents an Optional value — materialize to opt(inner)
                    // and do element-level Push. Without this, the IsOptional flag
                    // leaks through the direct ancestor edge to the Optional's element,
                    // bypassing the Optional structural layer (e.g., ?? unwrapping).
                    if (descendant.IsOptional)
                    {
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", ConstraintsState.Empty);
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        SolvingFunctions.PushConstraints(innerNode, ancOpt.ElementNode);
                        return true;
                    }
                    // Implicit lift: T ≤ opt(T) for primitive/empty constraints
                    descendantNode.RemoveAncestor(ancestorNode);
                    // If descendant has non-None constraints, propagate to element
                    if (!descendant.HasDescendant
                        || descendant.Descendant != StatePrimitive.None)
                    {
                        descendantNode.AddAncestor(ancOpt.ElementNode);
                        SolvingFunctions.PushConstraints(descendantNode, ancOpt.ElementNode);
                    }
                    return true;
                }
                if (result.ElementNode == ancOpt.ElementNode)
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }

                result.ElementNode.AddAncestor(ancOpt.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                SolvingFunctions.PushConstraints(result.ElementNode, ancOpt.ElementNode);
                return true;
            }
            default: return false;
        }
    }

    private static bool TryMergeStructFields(StateStruct ancStruct, StateStruct descStruct) {
        if (ancStruct.IsOptionalSourced || descStruct.IsOptionalSourced)
            ancStruct.IsOptionalSourced = descStruct.IsOptionalSourced = true;
        foreach (var ancField in ancStruct.Fields)
        {
            var descFieldNode = descStruct.GetFieldOrNull(ancField.Key);
            if (descFieldNode == null) {
                if (descStruct.IsOpen) {
                    descStruct.AddField(ancField.Key, ancField.Value);
                    continue;
                }
                return false;
            }
            var descNr = descFieldNode.GetNonReference();
            var ancNr = ancField.Value.GetNonReference();
            if (descNr == ancNr)
                continue;
            // None desc field: lift ancestor CS field to Optional via IsOptional
            // flag. Mirror of Pull's struct-field handling — needed for shared
            // generic args where one carries int and another carries none.
            if (descNr.State == StatePrimitive.None)
            {
                if (ancNr.State is ConstraintsState ancFieldCs)
                    ancFieldCs.AddDescendant(StatePrimitive.None);
                continue;
            }
            // None anc field: push to propagate None → descendant.
            if (ancNr.State == StatePrimitive.None)
                SolvingFunctions.PushConstraints(descFieldNode, ancField.Value);
            // Optional ancestor field × ConstraintsState descendant field (primitive range).
            // MergeInplace(opt(T), [U8..Re]I32!) fails — opt is composite, CS is primitive
            // range; they are NOT unifiable shapes. The right algebra is Push (subtyping
            // with implicit lift T ≤ opt(T)): propagate the opt's inner element constraint
            // to descField so descField's range narrows to fit opt(T)'s element. Mirrors
            // the inline handling in Apply(StateStruct, StateStruct) lines 107-115. (MR2Bug1.)
            else if (ancNr.State is StateOptional ancOpt && descNr.State is ConstraintsState)
                SolvingFunctions.PushConstraints(ancOpt.ElementNode, descFieldNode);
            // Both solved primitives: Push (subtyping). Struct covariance: {x:I32} ≤ {x:Real}
            // is valid but MergeInplace(I32, Real) requires equality → throws.
            // Composites/CS: MergeInplace needed for node unification (Optional propagation).
            else if (descNr.IsSolved && ancNr.IsSolved && descNr.State is StatePrimitive && ancNr.State is StatePrimitive)
                SolvingFunctions.PushConstraints(descFieldNode, ancField.Value);
            else
                SolvingFunctions.MergeInplace(descFieldNode, ancField.Value);
        }

        return true;
    }

    public bool Apply(StateOptional ancestor, StateOptional descendant, TicNode ancestorNode, TicNode descendantNode) {
        SolvingFunctions.PushConstraints(descendant.ElementNode, ancestor.ElementNode);
        return true;
    }

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        SolvingFunctions.PushConstraints(descendant.ElementNode, ancestor.ElementNode);
        return true;
    }

    /// <summary>
    /// Push for the unified single-arg invariant collection (Stage 2.1b).
    /// Cross-kind pairs reject. Same-kind: push element constraints down.
    /// </summary>
    public bool Apply(StateCollection ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Constructor != descendant.Constructor)
        {
            if (!IsArrayBranchKind(ancestor.Constructor)
                || !IsArrayBranchKind(descendant.Constructor))
                return false;
            if (IsSubtypeOrEqual(descendant.Constructor, ancestor.Constructor)) {
                // descendant already narrower-or-equal — element push only.
            } else if (IsSubtypeOrEqual(ancestor.Constructor, descendant.Constructor)) {
                // ancestor is narrower (e.g. anc=List vs desc=Array). Narrow
                // the descendant to the ancestor's kind — both Array-branch
                // constraints must hold simultaneously, intersection is the
                // narrower kind. Required for Clearable + indexed-write where
                // ancestor edge converged to List and descendant carries Array.
                descendantNode.State = new StateCollection(ancestor.Constructor, descendant.ElementNode);
            } else {
                return false;
            }
        }
        SolvingFunctions.PushConstraints(descendant.ElementNode, ancestor.ElementNode);
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
    /// Cross-family Push: any Array-branch StateCollection (List / Array /
    /// FixedArray) fits into the legacy StateArray slot per Stage 0 hierarchy.
    /// Element is pushed down (covariant). Set is rejected.
    /// </summary>
    public bool Apply(StateArray ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(descendant.Constructor))
            return false;
        SolvingFunctions.PushConstraints(descendant.ElementNode, ancestor.ElementNode);
        return true;
    }

    public bool Apply(StateCollection ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (!IsArrayBranchKind(ancestor.Constructor))
            return false;
        SolvingFunctions.PushConstraints(descendant.ElementNode, ancestor.ElementNode);
        return true;
    }

    private static bool IsArrayBranchKind(ConstructorKind kind) =>
        kind == ConstructorKind.List
        || kind == ConstructorKind.Array
        || kind == ConstructorKind.FixedArray;

    // StateMap deleted — map-vs-map Push handled by StateCollection same-kind cell.

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ArgsCount != ancestor.ArgsCount)
            return false;
        PushFunTypeArgumentsConstraints(descendant, ancestor);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Opt-sourcedness propagates across the merge.
        if (ancestor.IsOptionalSourced || descendant.IsOptionalSourced)
            ancestor.IsOptionalSourced = descendant.IsOptionalSourced = true;
        foreach (var ancField in ancestor.Fields)
        {
            var descField = descendant.GetFieldOrNull(ancField.Key);
            if (descField == null)
            {
                if (descendant.IsFrozen)
                    return false;
                descendant.AddField(ancField.Key, ancField.Value);
                descendantNode.State = descendant;
            }
            else
            {
                // None field: skip push.
                if (descField.GetNonReference().State == StatePrimitive.None)
                    continue;
                SolvingFunctions.PushConstraints(descField, ancField.Value);
            }
        }
        // No width propagation in Push: an open-row ancestor `{x:.. | ρ}` is
        // already row-polymorphic — descendant fields not present in the
        // ancestor are absorbed by the row variable `ρ` for free (Wand '87,
        // Rémy '89). Copying them into the ancestor was over-eager and turned
        // the polymorphic row into a concrete requirement that other
        // descendants couldn't satisfy. Bug hunt round 5 #28:
        // `[{x=1,y=2},{x=3}].first().x` failed because Push widened the
        // lambda's element-type to `{x,y}`, then `{x=3}` couldn't satisfy it.
        return true;
    }

    private static void PushFunTypeArgumentsConstraints(StateFun descFun, StateFun ancFun) {
        for (int i = 0; i < descFun.ArgsCount; i++)
            SolvingFunctions.PushConstraints(descFun.ArgNodes[i], ancFun.ArgNodes[i]);

        SolvingFunctions.PushConstraints(descFun.RetNode, ancFun.RetNode);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Stage C.3 — StateCompositeConstraints Push cells.
    // Per spec §4.1.2: Push is precondition-only on most CompCS cells; element
    // unification is invariance discipline.

    public bool Apply(StateCompositeConstraints ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => CompCsApply.ApplySameClass(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        => false; // composite shape ≠ primitive

    public bool Apply(StatePrimitive ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => ancestor == StatePrimitive.Any;

    public bool Apply(StateCompositeConstraints ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.HasDescendant && descendant.Descendant is StateCollection sc)
            return CompCsApply.ForwardPushCompCsSc(ancestor, sc, ancestorNode, descendantNode);
        if (descendant.HasDescendant && descendant.Descendant is StateArray sa)
            return CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: false);
        // Typeclass propagation analogue of IsComparable rule `D.cmp := D.cmp ∨ A.cmp`
        // (Push CS→CS line 36): if the ancestor's CompCs carries IsClearable=true,
        // mark the descendant CS too. This avoids replacing the CS state (which
        // freezes its shape too early) while still propagating the typeclass
        // requirement so it eventually surfaces at FunnyType conversion.
        if (ancestor.IsClearable && !descendant.IsClearable)
            descendant.IsClearable = true;
        // Empty / primitive-bound CS: defer (mirrors Pull behaviour). Reject when CS positively
        // forbids composites.
        if (descendant.IsComparable) return false;
        if (descendant.HasAncestor && descendant.Ancestor is StatePrimitive prim && prim != StatePrimitive.Any)
            return false;
        // Mirror of the Pull non-None-primitive-Descendant reject. Bug #40.
        if (descendant.HasDescendant && descendant.Descendant is StatePrimitive primDesc && primDesc != StatePrimitive.None)
            return false;
        return true;
    }

    public bool Apply(ConstraintsState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => !ancestor.IsComparable;

    public bool Apply(StateCompositeConstraints ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        return descendant switch {
            // StateMap was deleted — Map flows as StateCollection(Map, pair-struct).
            StateCollection sc => CompCsApply.ForwardPushCompCsSc(ancestor, sc, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: false),
            StateOptional _ => true,
            StateFun _ => false,
            StateStruct _ => false,
            _ => false,
        };
    }

    public bool Apply(ICompositeState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode) {
        return ancestor switch {
            StateCollection sc => CompCsApply.ReversePushScCompCs(sc, descendant, ancestorNode, descendantNode),
            StateArray sa => CompCsApply.ReverseCompCsStateArray(sa, descendant, ancestorNode, descendantNode, isPull: false),
            StateOptional _ => true,
            StateFun _ => false,
            StateStruct _ => false,
            _ => false,
        };
    }
}
