using System.Collections.Generic;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public class IfCaseExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _condition;
        private readonly IExpressionNode _result;

        public IfCaseExpressionNode(IExpressionNode condition, IExpressionNode result)
        {
            if(condition.Type!= VarType.BoolType)
                throw new OutpuCastParseException("if Condition has to be boolean but was "+ condition.Type);
            
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

        public bool IsSatisfied() => (bool)_condition.Calc();
        public object Calc() 
            => _result.Calc();
        public VarType Type => _result.Type;

    }
}