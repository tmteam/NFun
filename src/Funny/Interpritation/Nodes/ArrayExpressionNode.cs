using System;
using System.Linq;

namespace Funny.Interpritation.Nodes
{
    public class ArrayExpressionNode : IExpressionNode
    {
        private IExpressionNode[] _elements;

        public ArrayExpressionNode(IExpressionNode[] elements)
        {
            _elements = elements;
            foreach (var expressionNode in elements)
            {
                if(expressionNode.Type!= VarType.RealType)
                    throw new NotImplementedException("Only real type supported");
            }
        }

        public VarType Type 
            => VarType.ArrayOf(VarType.RealType);
        public object Calc() 
            => _elements.Select(e => (double) e.Calc()).ToArray();
    }
}