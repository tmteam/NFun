using System.Collections.Generic;

namespace Funny.Interpritation
{
    public class IfCaseExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _condition;
        private readonly IExpressionNode _result;

        public IfCaseExpressionNode(IExpressionNode condition, IExpressionNode result)
        {
            _condition = condition;
            _result = result;
        }

        public IEnumerable<IExpressionNode> Children
        {
            get
            {
                yield return _condition;
                yield return _result;
            }
        }

        public bool IsSatisfied() => _condition.Calc() != 0;
        public double Calc() 
            => _result.Calc();
    }
}