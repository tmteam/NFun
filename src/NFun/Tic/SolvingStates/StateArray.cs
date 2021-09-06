using System;
using System.Collections.Generic;
using NFun.Tic.Stages;

namespace NFun.Tic.SolvingStates {

public class StateArray : ICompositeState, ITypeState, ITicNodeState {
    public StateArray(TicNode elementNode) {
        ElementNode = elementNode;
    }

    public static StateArray Of(ITicNodeState state) =>
        state switch {
            ITypeState type => Of(type),
            StateRefTo refTo => Of(refTo.Node),
            _ => throw new InvalidOperationException()
        };

    public static StateArray Of(TicNode node)
        => new(node);

    public static StateArray Of(ITypeState type)
        => new(TicNode.CreateTypeVariableNode(type));

    public TicNode ElementNode { get; }
    public bool IsSolved => Element.IsSolved;
    public bool IsMutable => !IsSolved;

    public ITicNodeState Element => ElementNode.State;

    public override string ToString() {
        if (ElementNode.IsSolved)
            return $"arr({ElementNode})";

        return $"arr({ElementNode.Name})";
    }

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        if (otherType is not StateArray arrayType)
            return StatePrimitive.Any;
        if (Element is not ITypeState elementTypeA)
            return null;
        if (arrayType.Element is not ITypeState elementTypeB)
            return null;
        var ancestor = elementTypeA.GetLastCommonAncestorOrNull(elementTypeB);
        if (ancestor == null)
            return null;
        return StateArray.Of(ancestor);
    }

    public bool CanBeImplicitlyConvertedTo(StatePrimitive type)
        => type.Equals(StatePrimitive.Any);

    public override bool Equals(object obj) {
        if (obj is StateArray arr)
            return arr.Element.Equals(this.Element);
        return false;
    }

    public ICompositeState GetNonReferenced()
        => StateArray.Of(ElementNode.GetNonReference());

    public bool HasAnyReferenceMember => ElementNode.State is StateRefTo;

    public IEnumerable<TicNode> Members => new[] { ElementNode };

    public IEnumerable<TicNode> AllLeafTypes
    {
        get
        {
            if (ElementNode.State is ICompositeState composite)
                return composite.AllLeafTypes;
            return new[] { ElementNode };
        }
    }

    public string Description => "arr(" + ElementNode.Name + ")";

    public bool ApplyDescendant(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode) =>
        descendantNode.State.Apply(visitor, ancestorNode, descendantNode, this);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        StatePrimitive ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        ConstrainsState ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        ICompositeState ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);
}

}