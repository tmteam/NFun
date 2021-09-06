using NFun.Interpretation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes {

internal class FunVariableExpressionNode : IExpressionNode {
    private readonly IConcreteFunction _value;

    public FunVariableExpressionNode(IConcreteFunction fun, Interval interval) {
        _value = fun;
        Interval = interval;
        Type = FunnyType.Fun(_value.ReturnType, _value.ArgTypes);
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public object Calc() => _value;
}

}