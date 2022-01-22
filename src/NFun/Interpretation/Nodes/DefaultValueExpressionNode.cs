using NFun.Tokenization;

namespace NFun.Interpretation.Nodes {

internal class DefaultValueExpressionNode : IExpressionNode {
    private readonly object _value;

    public DefaultValueExpressionNode(object value, FunnyType type, Interval interval) {
        _value = value;
        Interval = interval;
        Type = type;

    }
    public Interval Interval { get; }
    public FunnyType Type { get; }
    public object Calc() => _value;
}

}