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
        // IsComparable downward: D.cmp := D.cmp ∨ A.cmp.
        bool flagChanged = false;
        if (ancestor.IsComparable && !descendant.IsComparable) {
            descendant.IsComparable = true;
            flagChanged = true;
        }
        // IsClearable downward: D.clr := D.clr ∨ A.clr.
        if (ancestor.IsClearable && !descendant.IsClearable) {
            descendant.IsClearable = true;
            flagChanged = true;
        }

        // F-bound StructBound merges via Gcd (meet of upper bounds).
        // Independent of HasAncestor — third dimension, peer to IsComparable.
        if (ancestor.HasStructBound) {
            descendant.StructBound = !descendant.HasStructBound
                ? SolvingFunctions.RewireStructBoundOwnership(ancestor.StructBound, ancestorNode, descendantNode)
                : SolvingFunctions.GcdBound(descendant.StructBound, ancestor.StructBound,
                                            descendantNode, ancestorNode);
            if (!descendant.HasStructBound) return false; // conflict
            flagChanged = true;
        }

        // Trans-axis consistency after typeclass-flag propagation.
        // Gated so empty descendants aren't validated needlessly.
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
        // Clearable narrowing: non-Clearable composite descendant ⇒ List.
        // Skip when ancestor IsOptional — wrap interacts with Optional-lift below
        // and creates a self-Optional cycle in implicit-None-return bodies.
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

        // F-bound projects fields onto descendant struct (covariant width subtype):
        // S-required missing fields extend desc (open row), shared fields receive Push.
        // F-bound × non-struct composite: structural conflict — reject.
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

        // Cross-Constructor SC: ancestor.Desc lower-bound + wider-or-equal descendant
        // ⇒ propagate element constraints covariantly. Without this, fall-through
        // to silent-true leaves descendant rejected later at Destruction.
        if (ancestor.HasDescendant
            && ancestor.Descendant is StateCollection ancSc
            && descendant is StateCollection descSc
            && ancSc.Constructor != descSc.Constructor
            && IsArrayBranchKind(ancSc.Constructor)
            && IsArrayBranchKind(descSc.Constructor)
            && IsSubtypeOrEqual(ancSc.Constructor, descSc.Constructor))
        {
            SolvingFunctions.PushConstraints(descSc.ElementNode, ancSc.ElementNode);
            return true;
        }

        // CS.Desc=struct + StateStruct desc: propagate field constraints (covariant).
        if (ancestor.HasDescendant && ancestor.Descendant is StateStruct ancDescStruct
                                   && descendant is StateStruct descStruct)
        {
            foreach (var ancField in ancDescStruct.Fields)
            {
                var descField = descStruct.GetFieldOrNull(ancField.Key);
                if (descField == null) continue;

                if (ancField.Value.State is StateOptional ancOpt && descField.State is ConstraintsState)
                {
                    // opt(T) anc × CS desc field: push inner element instead of merging shapes.
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
            // None ≤ opt(T): no constraint. Otherwise value ≤ opt.element.
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
        // IsOptional desc × non-Optional composite anc: materialize desc as opt(inner)
        // and push the composite into the element. Uniform across Array/Fun/Struct.
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
            // Composite anc demands same-shape desc.
            case StateArray ancArray:
            {
                // IsClearable on desc ⇒ List (sole Clearable kind in Array-branch).
                // Route through StateCollection(List); satisfies StateArray edge by lattice.
                if (descendant.IsClearable
                    && !descendant.HasDescendant
                    && !descendant.IsComparable
                    && !descendant.HasStructBound) {
                    var listResult = SolvingFunctions.TransformToCollectionOrNull(
                        ConstructorKind.List, descendantNode, descendantNode.Name, descendant);
                    if (listResult == null) return false;
                    if (listResult.ElementNode != ancArray.ElementNode)
                        listResult.ElementNode.AddAncestor(ancArray.ElementNode);
                    descendantNode.State = listResult;
                    descendantNode.RemoveAncestor(ancestorNode);
                    if (listResult.ElementNode != ancArray.ElementNode)
                        SolvingFunctions.PushConstraints(listResult.ElementNode, ancArray.ElementNode);
                    return true;
                }
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode, descendantNode.Name, descendant);
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
                    ancColl.Constructor, descendantNode, descendantNode.Name, descendant);
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
                var result = SolvingFunctions.TransformToOptionalOrNull(descendantNode, descendantNode.Name, descendant);
                if (result == null)
                {
                    if (descendant.HasDescendant && descendant.Descendant is StateStruct descStruct
                                                 && descStruct.IsOpen)
                    {
                        // Open-row struct desc + Optional anc: wrap desc as opt(inner) so
                        // struct constraints flow into the element.
                        // Guard MUST be IsOpen, not !IsSolved — closed literal structs
                        // carry CS-typed fields and would falsely match !IsSolved,
                        // widening their receiver to opt(struct).
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", descendant.GetCopy());
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        SolvingFunctions.PushConstraints(innerNode, ancOpt.ElementNode);
                        return true;
                    }
                    // IsOptional desc: materialize as opt(inner). Otherwise the flag
                    // leaks through to the Optional's element and bypasses the wrap.
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
                    // Implicit lift T ≤ opt(T) for primitive/empty desc.
                    descendantNode.RemoveAncestor(ancestorNode);
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
            // None desc field: lift ancestor CS field via IsOptional (mirror of Pull).
            if (descNr.State == StatePrimitive.None)
            {
                if (ancNr.State is ConstraintsState ancFieldCs)
                    ancFieldCs.AddDescendant(StatePrimitive.None);
                continue;
            }
            // None anc: propagate to desc via Push.
            if (ancNr.State == StatePrimitive.None)
                SolvingFunctions.PushConstraints(descFieldNode, ancField.Value);
            // opt(T) anc × CS desc field: Push inner element (T ≤ opt(T) lift).
            // MergeInplace would fail on the shape mismatch.
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

    /// <summary>Push for unified single-arg invariant collections.
    /// Cross-kind in Array-branch narrows desc; non-branch rejects.</summary>
    public bool Apply(StateCollection ancestor, StateCollection descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Constructor != descendant.Constructor)
        {
            if (!IsArrayBranchKind(ancestor.Constructor)
                || !IsArrayBranchKind(descendant.Constructor))
                return false;
            if (IsSubtypeOrEqual(descendant.Constructor, ancestor.Constructor)) {
                // desc already narrower-or-equal — element push only.
            } else if (IsSubtypeOrEqual(ancestor.Constructor, descendant.Constructor)) {
                // anc narrower: intersect by narrowing desc's kind.
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

    /// <summary>Cross-family Push: Array-branch StateCollection ≤ StateArray.
    /// Set rejected.</summary>
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

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ArgsCount != ancestor.ArgsCount)
            return false;
        PushFunTypeArgumentsConstraints(descendant, ancestor);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Propagate IsOptionalSourced across the merge.
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
                if (descField.GetNonReference().State == StatePrimitive.None)
                    continue;
                SolvingFunctions.PushConstraints(descField, ancField.Value);
            }
        }
        // No width propagation in Push: open-row ancestor absorbs missing desc fields
        // via row variable ρ (Wand '87, Rémy '89). Copying them into ancestor would
        // turn the polymorphic row into a concrete requirement.
        return true;
    }

    private static void PushFunTypeArgumentsConstraints(StateFun descFun, StateFun ancFun) {
        for (int i = 0; i < descFun.ArgsCount; i++)
            SolvingFunctions.PushConstraints(descFun.ArgNodes[i], ancFun.ArgNodes[i]);

        SolvingFunctions.PushConstraints(descFun.RetNode, ancFun.RetNode);
    }

    // StateCompositeConstraints Push cells.
    // See specs_tic/Algebra/CompositeConstraints.md §4.1.2.

    public bool Apply(StateCompositeConstraints ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => CompCsApply.ApplySameClass(ancestor, descendant, ancestorNode, descendantNode);

    public bool Apply(StateCompositeConstraints ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        => false;

    public bool Apply(StatePrimitive ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => ancestor == StatePrimitive.Any;

    public bool Apply(StateCompositeConstraints ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.HasDescendant && descendant.Descendant is StateCollection sc)
            return CompCsApply.ForwardPushCompCsSc(ancestor, sc, ancestorNode, descendantNode);
        if (descendant.HasDescendant && descendant.Descendant is StateArray sa)
            return CompCsApply.ForwardCompCsStateArray(ancestor, sa, ancestorNode, descendantNode, isPull: false);
        // IsClearable downward (analogue of D.cmp := D.cmp ∨ A.cmp).
        // Mark-only avoids freezing CS shape too early.
        if (ancestor.IsClearable && !descendant.IsClearable)
            descendant.IsClearable = true;
        // Defer-on-empty; reject when CS positively forbids composites.
        if (descendant.IsComparable) return false;
        if (descendant.HasAncestor && descendant.Ancestor is StatePrimitive prim && prim != StatePrimitive.Any)
            return false;
        // Mirror of the Pull non-None-primitive-Descendant reject.
        if (descendant.HasDescendant && descendant.Descendant is StatePrimitive primDesc && primDesc != StatePrimitive.None)
            return false;
        return true;
    }

    public bool Apply(ConstraintsState ancestor, StateCompositeConstraints descendant, TicNode ancestorNode, TicNode descendantNode)
        => !ancestor.IsComparable;

    public bool Apply(StateCompositeConstraints ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        return descendant switch {
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
