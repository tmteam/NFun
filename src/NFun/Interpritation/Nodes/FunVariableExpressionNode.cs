using NFun.Interpritation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunVariableExpressionNode : IExpressionNode
    {
        public FunVariableExpressionNode(FunctionBase fun, Interval interval)
        {
            _value = fun;
            Interval = interval;
            Type = VarType.Fun(_value.OutputType, _value.ArgTypes);
        }
        public Interval Interval { get; }
        public VarType Type { get; }
        private readonly FunctionBase _value;
        public object Calc() => _value;
    }
}