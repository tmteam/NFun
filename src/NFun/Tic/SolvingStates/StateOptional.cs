namespace NFun.Tic.SolvingStates;

using System;
using System.Collections.Generic;
using Algebra;

public class StateOptional : ICompositeState, ITypeState, ITicNodeState {
    public StateOptional(TicNode elementNode) => ElementNode = elementNode;

    public static StateOptional Of(ITicNodeState state) =>
        state switch {
            ITypeState type => Of(type),
            StateRefTo refTo => Of(refTo.Node),
            ConstraintsState c => Of(c),
            _ => throw new InvalidOperationException($"Optional cannot have state {state}")
        };

    private static StateOptional Of(ConstraintsState state) => new(TicNode.CreateInvisibleNode(state));

    public static StateOptional Of(TicNode node) => new(node);

    public static StateOptional Of(ITypeState type) => new(TicNode.CreateTypeVariableNode(type));

    public TicNode ElementNode { get; }
    public bool IsSolved => Element.IsSolved;
    public bool IsMutable => !IsSolved;

    public ITicNodeState Element => ElementNode.State;

    public override string ToString() {
        if (ElementNode.IsSolved)
            return $"opt({ElementNode})";

        return $"opt({ElementNode.Name})";
    }

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) =>
        this.Lca(otherType) as ITypeState;

    public string PrintState(int depth) {
        if (depth > 100)
            return "opt(...REQ...)";
        return $"opt({Element.PrintState(depth + 1)})";
    }

    public bool CanBePessimisticConvertedTo(StatePrimitive type) => type.Name == PrimitiveTypeName.Any;

    public override bool Equals(object obj) {
        if (obj is StateOptional opt)
            return opt.Element.Equals(Element);
        return false;
    }

    public override int GetHashCode() => Element.GetHashCode() * 31 + 7;

    public ICompositeState GetNonReferenced()
        => Of(ElementNode.GetNonReference());

    public bool HasAnyReferenceMember => ElementNode.State is StateRefTo;

    public int MemberCount => 1;
    public TicNode GetMember(int index) => ElementNode;
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

    public string StateDescription => PrintState(0);

    public string Description => "opt(" + ElementNode.Name + ")";
}
