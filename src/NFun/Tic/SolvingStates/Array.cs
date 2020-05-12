using System;
using System.Collections.Generic;

namespace NFun.Tic.SolvingStates
{
    public class Array: ICompositeType, IType, IState
    {
        public Array(SolvingNode elementNode)
        {
            ElementNode = elementNode;
        }

        public static Array Of(IState state)
        {
            if (state is IType type)
                return Of(type);
            if (state is RefTo refTo)
                return Of(refTo.Node);
            throw new InvalidOperationException();
        }
            
        public static Array Of(SolvingNode node) 
            => new Array(node);

        public static Array Of(IType type) 
            => new Array(SolvingNode.CreateTypeNode(type));

        public SolvingNode ElementNode { get; }
        public bool IsSolved
        {
            get
            {
                if (Element is Array arr)
                {
                    if (arr.ElementNode == ElementNode)
                        throw new InvalidOperationException("Imposible reqursive defenition");
                }
                return (Element as IType)?.IsSolved == true;
            }
        }

        public IState Element => ElementNode.State;


        public override string ToString()
        {
            if(ElementNode.IsSolved)
                return $"arr({ElementNode})";

            return $"arr({ElementNode.Name})";
        }

        public IType GetLastCommonAncestorOrNull(IType otherType)
        {
            var arrayType = otherType as Array;
            if (arrayType == null)
                return Primitive.Any;
            var elementTypeA = Element as IType;
            if (elementTypeA == null)
                return null;
            var elementTypeB = arrayType.Element as IType;
            if (elementTypeB == null)
                return null;
            var ancestor = elementTypeA.GetLastCommonAncestorOrNull(elementTypeB);
            if (ancestor == null)
                return null;
            return Array.Of(ancestor);
        }

        public bool CanBeImplicitlyConvertedTo(Primitive type) 
            => type.Equals(Primitive.Any);

        public override bool Equals(object obj)
        {
            if (obj is Array arr)
            {
                return arr.Element.Equals(this.Element);
            }
            return false;
        }

        public ICompositeType GetNonReferenced() 
            => Array.Of(ElementNode.GetNonReference());

        public IEnumerable<SolvingNode> Members => new[] {ElementNode};

        public IEnumerable<SolvingNode> AllLeafTypes
        {
            get
            {
                if (ElementNode.State is ICompositeType composite)
                    return composite.AllLeafTypes;
                return new[] {ElementNode};
            }
        }

        public string Description => "arr(" + ElementNode.Name + ")";
    }
}