using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class IfCaseExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _condition;
        public IExpressionNode Body { get; }

        public IfCaseExpressionNode(IExpressionNode condition, IExpressionNode body, Interval interval)
        {
            if (condition.Type != VarType.Bool)
                throw ErrorFactory.IfConditionIsNotBool(condition);
            
            _condition = condition;
            Body = body;
            Interval = interval;
        }

        public IEnumerable<IExpressionNode> Children
        {
            get
            {
                yield return _condition;
                yield return Body;
            }
        }

        public bool IsSatisfied() => (bool)_condition.Calc();
        public object Calc() 
            => Body.Calc();
        public VarType Type => Body.Type;
        public Interval Interval { get; }
    }
}