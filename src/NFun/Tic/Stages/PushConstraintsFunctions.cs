using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PushConstraintsFunctions : IStateFunction {
    public static IStateFunction Singleton { get; } = new PushConstraintsFunctions();

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
            var innerNode = TicNode.CreateTypeVariableNode(
                "e" + descendantNode.Name.ToString().ToLower() + "'",
                ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable));
            innerNode.IsOptionalElement = true;
            descendantNode.State = new SolvingStates.StateOptional(innerNode);
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
                                                 && !descStruct.IsSolved)
                    {
                        // Struct descendant with unsolved fields (from generic/inferred type, not literal).
                        // Wrap descendant in Optional, carrying struct constraints into element.
                        // This handles map lambda params on optional struct arrays where Pull
                        // single-pass didn't propagate Optional to the lambda parameter.
                        // Guard: only wrap unsolved structs — solved structs come from concrete values
                        // and wrapping them would incorrectly make ?. work on non-optional structs.
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name.ToString().ToLower() + "'",
                            descendant.GetCopy());
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
                            "e" + descendantNode.Name.ToString().ToLower() + "'",
                            ConstraintsState.Empty);
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
        foreach (var ancField in ancStruct.Fields)
        {
            var descFieldNode = descStruct.GetFieldOrNull(ancField.Key);
            if (descFieldNode == null)
                return false;
            var descNr = descFieldNode.GetNonReference();
            var ancNr = ancField.Value.GetNonReference();
            if (descNr == ancNr)
                continue;
            // None desc field: skip — None ≤ opt(T) handled by outer Optional layer.
            if (descNr.State == StatePrimitive.None)
                continue;
            // None anc field: push constraints to propagate None → descendant.
            if (ancNr.State == StatePrimitive.None)
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

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ArgsCount != ancestor.ArgsCount)
            return false;
        PushFunTypeArgumentsConstraints(descendant, ancestor);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        foreach (var ancField in ancestor.Fields)
        {
            var descField = descendant.GetFieldOrNull(ancField.Key);
            if (descField == null)
            {
                if (descendant.IsFrozen)
                    return false;
                // AddField adds the exact ancestor field node, so no push needed for new fields.
                descendant.AddField(ancField.Key, ancField.Value);
                descendantNode.State = descendant;
            }
            else
            {
                // None field: skip push. None ≤ opt(T) is valid for any T,
                // but None ≰ T directly when T is composite.
                // The None compatibility is handled by the outer Optional layer
                // (StagesExtension unwraps opt before reaching struct-struct Push).
                if (descField.GetNonReference().State == StatePrimitive.None)
                    continue;
                SolvingFunctions.PushConstraints(descField, ancField.Value);
            }
        }
        return true;
    }

    private static void PushFunTypeArgumentsConstraints(StateFun descFun, StateFun ancFun) {
        for (int i = 0; i < descFun.ArgsCount; i++)
            SolvingFunctions.PushConstraints(descFun.ArgNodes[i], ancFun.ArgNodes[i]);

        SolvingFunctions.PushConstraints(descFun.RetNode, ancFun.RetNode);
    }
}
