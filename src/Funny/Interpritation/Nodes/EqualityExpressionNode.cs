using Funny.Types;

namespace Funny.Interpritation.Nodes
{
    public class EqualityExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _a;
        private readonly IExpressionNode _b;
        private readonly bool _equal;

        public EqualityExpressionNode(
            IExpressionNode a, 
            IExpressionNode b, bool equal)
        {
            _a = a;
            _b = b;
            _equal = equal;
        }
        public VarType Type=> VarType.BoolType;
        public object Calc() => _equal == TypeHelper.AreEqual(_a.Calc(), _b.Calc());
    }
}