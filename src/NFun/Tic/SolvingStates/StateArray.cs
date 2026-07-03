namespace NFun.Tic.SolvingStates;

using System;
using System.Collections.Generic;

public class StateArray : ICompositeState, ITypeState, ITicNodeState {
    public StateArray(TicNode elementNode) => ElementNode = elementNode;

    public static StateArray Of(ITicNodeState state) =>
        state switch {
            ITypeState type => Of(type),
            StateRefTo refTo => Of(refTo.Node),
            ConstraintsState c => Of(c),
            _ =>  throw new InvalidOperationException($"Array cannot have state {state}")
        };

    private static StateArray Of(ConstraintsState state) => new(TicNode.CreateInvisibleNode(state));

    public static StateArray Of(TicNode node) => new(node);

    public static StateArray Of(ITypeState type) => new(TicNode.CreateTypeVariableNode(type));

    public TicNode ElementNode { get; }
    public bool IsSolved => Element.IsSolved;
    public bool IsMutable => !IsSolved;

    public ITicNodeState Element => ElementNode.State;

    // Delegate to the depth-guarded printer — μ-recursive element states would
    // recurse forever through TicNode.ToString.
    public override string ToString() =>
        ElementNode.IsSolved ? PrintState(0) : $"arr({ElementNode.Name})";

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        // Cross-family: StateArray vs Array-branch StateCollection widens to T[]
        // per the lattice (specs_tic/TicTypeSystem.md §ConstructorLattice).
        if (otherType is StateCollection collOther
            && (collOther.Constructor == ConstructorKind.List
             || collOther.Constructor == ConstructorKind.Array
             || collOther.Constructor == ConstructorKind.FixedArray))
        {
            if (Element is not ITypeState elemA || collOther.Element is not ITypeState elemB)
                return null;
            var elemLca = elemA.GetLastCommonAncestorOrNull(elemB);
            return elemLca == null ? null : Of(elemLca);
        }
        if (otherType is not StateArray arrayType)
            return StatePrimitive.Any;
        if (Element is not ITypeState elementTypeA)
            return null;
        if (arrayType.Element is not ITypeState elementTypeB)
            return null;
        var ancestor = elementTypeA.GetLastCommonAncestorOrNull(elementTypeB);
        if (ancestor == null)
            return null;
        return Of(ancestor);
    }

    public string PrintState(int depth) {
        if (depth > 100)
            return "arr(...REQ...)";
        return $"arr({Element.PrintState(depth + 1)})";
    }

    public bool CanBePessimisticConvertedTo(StatePrimitive type)
        => type== StatePrimitive.Any;

    private const int ArrayCycleGuard = -57500;

    public override bool Equals(object obj) {
        if (obj is not StateArray arr) return false;
        // Coinductive cycle guard for recursive types (Amadio–Cardelli '93).
        var elem = ElementNode;
        if (elem.VisitMark == ArrayCycleGuard) return true;
        var prev = elem.VisitMark;
        elem.VisitMark = ArrayCycleGuard;
        var result = arr.Element.Equals(Element);
        elem.VisitMark = prev;
        return result;
    }

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
            if (ElementNode.State is ICompositeState composite) {
                if (ElementNode.VisitMark == Tic.TicVisitMarks.StateLeaf) yield break;
                var prev = ElementNode.VisitMark;
                ElementNode.VisitMark = Tic.TicVisitMarks.StateLeaf;
                foreach (var leaf in composite.AllLeafTypes)
                    yield return leaf;
                ElementNode.VisitMark = prev;
            } else {
                yield return ElementNode;
            }
        }
    }

    public string StateDescription => PrintState(0);

    public string Description => "arr(" + ElementNode.Name + ")";
}
