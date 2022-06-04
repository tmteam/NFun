using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class VariableExpressionNode : IExpressionNode {
    internal VariableExpressionNode(VariableSource source, Interval interval) {
        Source = source;
        Interval = interval;
    }

    internal VariableSource Source { get; }
    public Interval Interval { get; }
    public FunnyType Type => Source.Type;
    public object Calc() => Source.FunnyValue;
    public override string ToString() => $"{Source.Name}: {Source.FunnyValue}";
}