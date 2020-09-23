using System;
using System.Collections.Generic;

namespace NFun.Tic.SolvingStates
{
    public class StateArray: ICompositeTypeState, ITypeState, ITicNodeState
    {
        public StateArray(TicNode elementNode)
        {
            ElementNode = elementNode;
        }

        public static StateArray Of(ITicNodeState state)
        {
            if (state is ITypeState type)
                return Of(type);
            if (state is StateRefTo refTo)
                return Of(refTo.Node);
            throw new InvalidOperationException();
        }
            
        public static StateArray Of(TicNode node) 
            => new StateArray(node);

        public static StateArray Of(ITypeState type) 
            => new StateArray(TicNode.CreateTypeVariableNode(type));

        public TicNode ElementNode { get; }
        public bool IsSolved
        {
            get
            {
                if (Element is StateArray arr)
                {
                    if (arr.ElementNode == ElementNode)
                        throw new InvalidOperationException("Imposible reqursive defenition");
                }
                return (Element as ITypeState)?.IsSolved == true;
            }
        }

        public ITicNodeState Element => ElementNode.State;

        public override string ToString()
        {
            if(ElementNode.IsSolved)
                return $"arr({ElementNode})";

            return $"arr({ElementNode.Name})";
        }

        public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType)
        {
            var arrayType = otherType as StateArray;
            if (arrayType == null)
                return StatePrimitive.Any;
            var elementTypeA = Element as ITypeState;
            if (elementTypeA == null)
                return null;
            var elementTypeB = arrayType.Element as ITypeState;
            if (elementTypeB == null)
                return null;
            var ancestor = elementTypeA.GetLastCommonAncestorOrNull(elementTypeB);
            if (ancestor == null)
                return null;
            return StateArray.Of(ancestor);
        }

        public bool CanBeImplicitlyConvertedTo(StatePrimitive type) 
            => type.Equals(StatePrimitive.Any);

        public override bool Equals(object obj)
        {
            if (obj is StateArray arr)
                return arr.Element.Equals(this.Element);
            return false;
        }

        public ICompositeTypeState GetNonReferenced() 
            => StateArray.Of(ElementNode.GetNonReference());

        public bool HasAnyReferenceMember => ElementNode.State is StateRefTo;

        public IEnumerable<TicNode> Members => new[] {ElementNode};

        public IEnumerable<TicNode> AllLeafTypes
        {
            get
            {
                if (ElementNode.State is ICompositeTypeState composite)
                    return composite.AllLeafTypes;
                return new[] {ElementNode};
            }
        }

        public string Description => "arr(" + ElementNode.Name + ")";
    }
}