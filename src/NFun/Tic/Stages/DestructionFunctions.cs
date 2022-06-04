using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages; 

public class DestructionFunctions : IStateCombination2dimensionalVisitor {
    public static DestructionFunctions Singleton { get; } = new();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        StatePrimitive ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.Fits(ancestor))
            descendantNode.State = ancestor;

        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        ConstrainsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.Fits(descendant)) ancestorNode.State = descendant;
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
        //todo Check all nonrefchildren of constrains?
        if (ancestor.Fits(descendant))
        {
            ancestorNode.State = new StateRefTo(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
        }

        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __)
        => false;

    public bool Apply(
        ICompositeState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.Fits(ancestor))
        {
            descendantNode.State = new StateRefTo(ancestorNode);
            descendantNode.RemoveAncestor(ancestorNode);
        }

        return true;
    }

    public bool Apply(
        ICompositeState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateArray ancArray)
        {
            if (descendant is StateArray descArray)
                return SolvingFunctions.Destruction(descArray.ElementNode, ancArray.ElementNode);
            return true;
        }

        if (ancestor is StateFun ancFun)
        {
            if (descendant is StateFun descFun)
            {
                TraceLog.Write("f+f: ");
                if (ancFun.ArgsCount == descFun.ArgsCount)
                {
                    for (int i = 0; i < ancFun.ArgsCount; i++)
                        SolvingFunctions.Destruction(descFun.ArgNodes[i], ancFun.ArgNodes[i]);
                    SolvingFunctions.Destruction(ancFun.RetNode, descFun.RetNode);
                }
            }
        }

        if (ancestor is StateStruct ancStruct)
        {
            if (descendant is StateStruct descStruct)
            {
                foreach (var (key, value) in ancStruct.Fields)
                {
                    var descFieldNode = descStruct.GetFieldOrNull(key);
                    if (descFieldNode == null)
                    {
                        //todo!!
                        //throw new ImpossibleException(
                        //    $"Struct descendant '{descendantNode.Name}:{descendant}' of node '{ancestorNode.Name}:{ancestor}' miss field '{ancField.Key}'");
                        descendantNode.State = descStruct.With(key, value);
                    }
                    else
                    {
                        SolvingFunctions.Destruction(descFieldNode, value);
                    }
                }

                ancestorNode.State = new StateRefTo(descendantNode);
            }
        }

        return true;
    }
}