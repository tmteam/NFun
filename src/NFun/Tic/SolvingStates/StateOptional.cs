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

    /// <summary>Sentinel for cycle detection (generic functions with if..else none create cyclic Optional).</summary>
    private const int OptionalCycleGuard = -55001;

    public bool IsSolved {
        get {
            var elem = ElementNode;
            if (elem.VisitMark == OptionalCycleGuard)
                return false; // cycle → not resolved yet
            var prev = elem.VisitMark;
            elem.VisitMark = OptionalCycleGuard;
            var result = elem.State.IsSolved;
            elem.VisitMark = prev;
            return result;
        }
    }
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
        if (obj is not StateOptional opt) return false;
        var elem = ElementNode;
        if (elem.VisitMark == OptionalCycleGuard) return true; // cycle → treat as equal
        var prev = elem.VisitMark;
        elem.VisitMark = OptionalCycleGuard;
        var result = opt.Element.Equals(Element);
        elem.VisitMark = prev;
        return result;
    }

    public override int GetHashCode() => 7; // stable for cyclic Optionals

    public ICompositeState GetNonReferenced()
        => Of(ElementNode.GetNonReference());

    public bool HasAnyReferenceMember => ElementNode.State is StateRefTo;

    public int MemberCount => 1;
    public TicNode GetMember(int index) => ElementNode;
    public IEnumerable<TicNode> Members => new[] { ElementNode };

    private const int LeafMark = -56000;

    public IEnumerable<TicNode> AllLeafTypes
    {
        get
        {
            if (ElementNode.State is ICompositeState composite) {
                if (ElementNode.VisitMark == LeafMark) yield break;
                var prev = ElementNode.VisitMark;
                ElementNode.VisitMark = LeafMark;
                foreach (var leaf in composite.AllLeafTypes)
                    yield return leaf;
                ElementNode.VisitMark = prev;
            } else {
                yield return ElementNode;
            }
        }
    }

    public string StateDescription => PrintState(0);

    public string Description => "opt(" + ElementNode.Name + ")";
}
