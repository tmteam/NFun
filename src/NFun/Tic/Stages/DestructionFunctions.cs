using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

using System;

public class DestructionFunctions : IStateFunction {
    public static DestructionFunctions Singleton { get; } = new();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        StatePrimitive ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.FitsInto(descendant))
            descendantNode.State = ancestor;

        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        ConstrainsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.CanBeConvertedTo(descendant))
            ancestorNode.State = descendant;
        return true;
    }

    public bool Apply(
        ConstrainsState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
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
        ConstrainsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.FitsInto(ancestor))
        {
            ancestorNode.State = new StateRefTo(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
        }
        else if (!descendantNode.IsSolved)
        {
            if (descendant is StateArray array)
            {
                if (ancestor.Descendant is StateArray ancestorArray)
                {
                    ancestorNode.State = ancestorArray;
                    descendantNode.RemoveAncestor(ancestorNode);
                    return Apply(ancestorArray, array, ancestorNode, descendantNode);
                }

            }
           /* else if (descendant is StateFun fun)
            {
                if (ancestor.Descendant is StateFun ancestorFun)
                {
                    ancestorNode.State = ancestorFun;
                    descendantNode.RemoveAncestor(ancestorNode);
                    return Apply(ancestorFun, fun, ancestorNode, descendantNode);
                }
            }
            else if (descendant is StateStruct stateStruct)
            {
                if (ancestor.Descendant is StateStruct ancestorStruct)
                {
                    ancestorNode.State = ancestorStruct;
                    descendantNode.RemoveAncestor(ancestorNode);
                    return Apply(ancestorStruct, stateStruct, ancestorNode, descendantNode);
                }
            }*/
            //else
            throw new NotImplementedException($"{descendant} does not fit into {ancestor}");
            TraceLog.WriteLine($"{descendant} does not fit into {ancestor}");
        }

        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __)
        => false;

    public bool Apply(
        ICompositeState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.FitsInto(descendant))
        {
            descendantNode.State = new StateRefTo(ancestorNode);
            descendantNode.RemoveAncestor(ancestorNode);
        }
        else
        {
            TraceLog.WriteLine($"{descendant} does not fit into {ancestor}");
        }

        return true;
    }

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) =>
        SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.ArgsCount == descendant.ArgsCount)
            for (int i = 0; i < ancestor.ArgsCount; i++)
                SolvingFunctions.Destruction(descendant.ArgNodes[i], ancestor.ArgNodes[i]);
        SolvingFunctions.Destruction(ancestor.RetNode, descendant.RetNode);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        foreach (var (key, value) in ancestor.Fields)
        {
            var descFieldNode = descendant.GetFieldOrNull(key);
            if (descFieldNode == null)
            {
                //todo!!
                //throw new ImpossibleException(
                //    $"Struct descendant '{descendantNode.Name}:{descendant}' of node '{ancestorNode.Name}:{ancestor}' miss field '{ancField.Key}'");
                descendantNode.State = descendant.With(key, value);
            }
            else
            {
                SolvingFunctions.Destruction(descFieldNode, value);
            }
        }

        ancestorNode.State = new StateRefTo(descendantNode);
        return true;
    }
}
