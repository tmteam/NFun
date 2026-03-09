using System.Runtime.CompilerServices;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PullConstraintsFunctions : IStateFunction {
    public static IStateFunction Singleton { get; } = new PullConstraintsFunctions();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __) =>
        descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ConstraintsState descendant, TicNode _, TicNode __)
        => descendant.CanBeConvertedOptimisticTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
        => ApplyAncestorConstrains(ancestorNode, ancestor, descendant);

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(descendant.Descendant);
        var result = ancestorCopy.SimplifyOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode)
        => ApplyAncestorConstrains(ancestorNode, ancestor, descendant);

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateOptional)
        {
            // None ≤ opt(T) and T ≤ opt(T) via implicit lift
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        return false;
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        switch (ancestor)
        {
            case StateArray ancArray:
            {
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
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
            case StateOptional ancOpt:
            {
                var result = SolvingFunctions.TransformToOptionalOrNull(descendantNode.Name, descendant);
                if (result == null)
                {
                    // Implicit lift: T ≤ opt(T) for any T (primitive/constrains)
                    descendantNode.RemoveAncestor(ancestorNode);
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
        // Decompose struct constraint into field-level ancestor edges.
        // Same pattern as Array (element ≤ element) and Fun (ret ≤ ret, arg ≥ arg).
        // Struct fields are covariant: descField ≤ ancField.
        TraceLog.WriteLine($"  Pull Struct<-Struct: anc={ancestorNode.Name}:{ancestor.StateDescription} desc={descendantNode.Name}:{descendant.StateDescription}");
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
                descendantNode.State = descendant.With(ancField.Key, ancField.Value);
            }
            else if (descField != ancField.Value)
            {
                TraceLog.WriteLine($"    Field '{ancField.Key}': desc ≤ anc ({descField.State} ≤ {ancField.Value.State})");
                descField.AddAncestor(ancField.Value);
            }
        }

        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }


    public bool Apply(StateOptional ancestor, StateOptional descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
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
}
