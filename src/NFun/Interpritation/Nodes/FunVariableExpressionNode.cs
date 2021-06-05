using NFun.Interpritation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunVariableExpressionNode : IExpressionNode
    {
        private readonly IConcreteFunction _value;

        public FunVariableExpressionNode(IConcreteFunction fun, Interval interval)
        {
            _value = fun;
            Interval = interval;
            Type = FunnyType.Fun(_value.ReturnType, _value.ArgTypes);
        }
        public Interval Interval { get; }
        public FunnyType Type { get; }
        public object Calc() => _value;
    }
}