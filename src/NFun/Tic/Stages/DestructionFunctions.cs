using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

using System;
using System.Security.Cryptography;
using Exceptions;

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
            return true;
        }

        TraceLog.WriteLine($"{descendant} does not fit into {ancestor}");
        if (!descendantNode.IsSolved)
        {
            if (descendant.GetType() == ancestor.Descendant?.GetType())
            {
                ancestorNode.State = ancestor.Descendant;
                descendantNode.RemoveAncestor(ancestorNode);
                return descendant switch {
                    StateArray array =>
                        Apply((StateArray)ancestor.Descendant, array, ancestorNode, descendantNode),
                    StateFun fun =>
                        Apply((StateFun)ancestor.Descendant, fun, ancestorNode, descendantNode),
                    StateStruct @struct =>
                        Apply((StateStruct)ancestor.Descendant, @struct, ancestorNode, descendantNode),
                    _ => throw new NotSupportedException($"type {descendant} is not supported for destruction")
                };
            }

            TraceLog.WriteLine($"{descendant} completely not fit into {ancestor}");
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
        var sameFieldCount = 0;
        foreach (var (key, value) in descendant.Fields)
        {
            var descFieldNode = descendant.GetFieldOrNull(key);
            if (descFieldNode == null)
            {
                //todo!!
                throw new NFunImpossibleException(
                    $"Struct descendant '{descendantNode.Name}:{descendant}' of node '{ancestorNode.Name}:{ancestor}' miss field '{key}'");
                descendantNode.State = descendant.With(key, value);
            }
            else
            {
                if (SolvingFunctions.Destruction(descFieldNode, value))
                    sameFieldCount++;
            }
        }
        if(sameFieldCount == ancestor.FieldsCount && sameFieldCount == descendant.FieldsCount)
            ancestorNode.State = new StateRefTo(descendantNode);
        return true;
    }
}
