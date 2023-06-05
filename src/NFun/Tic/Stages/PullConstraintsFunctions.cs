using System;
using System.Runtime.CompilerServices;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PullConstraintsFunctions : IStateFunction {
    public static IStateFunction Singleton { get; } = new PullConstraintsFunctions();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __) =>
        descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ConstrainsState descendant, TicNode _, TicNode __)
    //todo - should we add ancestor to constrains?!
        => descendant.CanBeConvertedOptimisticTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(ConstrainsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        => ApplyAncestorConstrains(ancestorNode, ancestor, descendant);

    public bool Apply(
        ConstrainsState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(descendant.Descendant);
        var result = ancestorCopy.GetOptimizedOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }

    public bool Apply(
        ConstrainsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode)
        => ApplyAncestorConstrains(ancestorNode, ancestor, descendant);

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __) => false;

    public bool Apply(
        ICompositeState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        switch (ancestor)
        {
            case StateArray ancArray:
            {
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                //todo - dont we need to check, if result is solved? What can we decide in the case?
                //todo  Should we put resulting constrains on descendant (relaunch pull constrains with new anc-array)?
                if (result == null)
                    return false;
                result.ElementNode.AddAncestor(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            case StateFun ancFun:
            {
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
                descendantNode.State = result;
                return true;
            }
            default: return true;
        }
    }

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
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
        // desc node has to have all ancestors fields that has exast same type as desc type
        // (implicit field convertion is not allowed)
        foreach (var ancField in ancestor.Fields)
        {
            var descField = descendant.GetFieldOrNull(ancField.Key);
            if (descField == null)
            {
                if (descendant.IsFrozen)
                    return false;
                else
                    descendantNode.State = descendant.With(ancField.Key, ancField.Value);
            }
            else
            {
                SolvingFunctions.MergeInplace(ancField.Value, descField);
                if (ancField.Value.State is StateRefTo)
                    ancestorNode.State = ancestor.GetNonReferenced();
                if (descField.State is StateRefTo)
                    descendantNode.State = descendant.GetNonReferenced();
            }
        }

        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ApplyAncestorConstrains(TicNode ancestorNode, ConstrainsState ancestor, ITypeState typeDesc) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(typeDesc);
        var result = ancestorCopy.GetOptimizedOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }
}
