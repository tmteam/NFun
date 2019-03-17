using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Types;

namespace Funny.Interpritation.Nodes
{
    public class UniteArraysExpressionNode: IExpressionNode
    {
        private readonly IExpressionNode _nodeA;
        private readonly IExpressionNode _nodeB;

        public UniteArraysExpressionNode(IExpressionNode nodeA, IExpressionNode nodeB)
        {
            if(nodeA.Type.BaseType!= PrimitiveVarType.ArrayOf)
                throw new ParseException("left node is not an array");
            if(nodeB.Type.BaseType!= PrimitiveVarType.ArrayOf)
                throw new ParseException("right node is not an array");
            _nodeA = nodeA;
            _nodeB = nodeB;
            var subtype = _nodeA.Type.ArrayTypeSpecification.VarType;
            if(_nodeB.Type.ArrayTypeSpecification.VarType!= subtype)
                throw new ParseException("array types should be same");
            Type = VarType.ArrayOf(subtype);
            
        }
        public VarType Type { get; }
        public object Calc()
        {
            var a = _nodeA.Calc() as IEnumerable;
            var b = _nodeB.Calc() as IEnumerable;
            return a.Cast<object>().Concat(b.Cast<object>()).ToArray();
        }
    }
}