using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class IfElseExpressionNode: IExpressionNode
    {
        private readonly IExpressionNode[] _ifExpressionNodes;
        private readonly IExpressionNode[] _conditionNodes;
        private readonly IExpressionNode _elseNode;
        public IfElseExpressionNode(
            IExpressionNode[] ifExpressionNodes, 
            IExpressionNode[] conditionNodes, 
            IExpressionNode elseNode, 
            Interval interval, 
            VarType type)
        {
            _ifExpressionNodes = ifExpressionNodes;
            _conditionNodes = conditionNodes;
            _elseNode = elseNode;

            Type = type;
            Interval = interval;
        }

        public object Calc()
        {
            for (var index = 0; index < _ifExpressionNodes.Length; index++)
            {
                if (_conditionNodes[index].Calc().To<bool>())
                    return _ifExpressionNodes[index].Calc(); 
            }
            return _elseNode.Calc();
        }
        public VarType Type { get; }
        public Interval Interval { get; }

        public void Apply(IExpressionNodeVisitor visitor)
        {
            visitor.Visit(this, _ifExpressionNodes.Length);
            for (int i = 0; i < _ifExpressionNodes.Length; i++)
            {
                _conditionNodes[i].Apply(visitor);
                _ifExpressionNodes[i].Apply(visitor);

            }
            _elseNode.Apply(visitor);
        }
    }
}