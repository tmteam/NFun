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

        descendant.AddAncestor(ancestor.Ancestor);
        var result = descendant.SimplifyOrNull();
        if (result == null)
            return false;
        descendantNode.State = result;
        return true;
    }

    public bool Apply(ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.HasAncestor && !ancestor.Ancestor.Equals(StatePrimitive.Any))
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

                if (descField.State is ConstraintsState && !ancField.Value.State.Equals(StatePrimitive.Any))
                    SolvingFunctions.MergeInplace(descField, ancField.Value);
                else
                    SolvingFunctions.PushConstraints(descField, ancField.Value);
            }
        }

        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __) => false;

    public bool Apply(
        ICompositeState ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode) {
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
            default: return false;
        }
    }

    private static bool TryMergeStructFields(StateStruct ancStruct, StateStruct descStruct) {
        TraceLog.WriteLine($"  Push MergeStructFields: anc={ancStruct.StateDescription} desc={descStruct.StateDescription}");
        foreach (var ancField in ancStruct.Fields)
        {
            var descFieldNode = descStruct.GetFieldOrNull(ancField.Key);
            if (descFieldNode == null)
            {
                TraceLog.WriteLine($"    FAIL: desc missing field '{ancField.Key}'");
                return false;
            }
            TraceLog.WriteLine($"    Merging field '{ancField.Key}': desc={descFieldNode.State} anc={ancField.Value.State}");
            //  i m not sure why - but it is very important to set descFieldNode as main merge node...
            SolvingFunctions.MergeInplace(descFieldNode, ancField.Value);
        }

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
        // Width subtyping: descendant (more specific) must have all fields of ancestor.
        // If descendant is missing a field, extend it (unless frozen).
        foreach (var ancField in ancestor.Fields)
        {
            if (descendant.GetFieldOrNull(ancField.Key) == null)
            {
                if (descendant.IsFrozen)
                    return false;
                descendant = descendant.With(ancField.Key, ancField.Value);
                descendantNode.State = descendant;
            }
        }
        // Covariant field-by-field push.
        foreach (var ancField in ancestor.Fields)
        {
            SolvingFunctions.PushConstraints(descendant.GetFieldOrNull(ancField.Key), ancField.Value);
        }
        return true;
    }

    private static void PushFunTypeArgumentsConstraints(StateFun descFun, StateFun ancFun) {
        for (int i = 0; i < descFun.ArgsCount; i++)
            SolvingFunctions.PushConstraints(descFun.ArgNodes[i], ancFun.ArgNodes[i]);

        SolvingFunctions.PushConstraints(descFun.RetNode, ancFun.RetNode);
    }
}
