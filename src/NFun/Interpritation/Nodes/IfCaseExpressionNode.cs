using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class IfCaseExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _condition;
        private readonly IExpressionNode _result;

        public IfCaseExpressionNode(IExpressionNode condition, IExpressionNode result, Interval interval)
        {
            if (condition.Type != VarType.Bool)
                throw ErrorFactory.IfConditionIsNotBool(condition);
            
            _condition = condition;
            _result = result;
            Interval = interval;
        }

        public IEnumerable<IExpressionNode> Children
        {
            get
            {
                yield return _condition;
                yield return _result;
            }
        }

        public bool IsSatisfied() => (bool)_condition.Calc();
        public object Calc() 
            => _result.Calc();
        public VarType Type => _result.Type;
        public Interval Interval { get; }
    }
}