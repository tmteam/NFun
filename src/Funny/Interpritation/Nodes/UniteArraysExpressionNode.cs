using System.Linq;

namespace Funny.Interpritation.Nodes
{
    public class UniteArraysExpressionNode: IExpressionNode
    {
        private readonly IExpressionNode _nodeA;
        private readonly IExpressionNode _nodeB;

        public UniteArraysExpressionNode(IExpressionNode nodeA, IExpressionNode nodeB)
        {
            if(nodeA.Type.BaseType!= PrimitiveVarType.Array)
                throw new ParseException("left node is not an array");
            if(nodeB.Type.BaseType!= PrimitiveVarType.Array)
                throw new ParseException("right node is not an array");
            _nodeA = nodeA;
            _nodeB = nodeB;
        }
        public VarType Type => VarType.ArrayOf(VarType.RealType);
        public object Calc()
        {
            var a = (double[])_nodeA.Calc();
            var b = (double[])_nodeB.Calc();
            return a.Concat(b).ToArray();
        }
    }
}