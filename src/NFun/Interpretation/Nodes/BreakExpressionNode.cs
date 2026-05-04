using System;
using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal sealed class BreakExpressionNode : IExpressionNode {
    public BreakExpressionNode(FunnyType type, Interval interval) {
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => Array.Empty<IRuntimeNode>();
    public object Calc() => BreakSignal.Instance;
    public IExpressionNode Clone(ICloneContext context) => new BreakExpressionNode(Type, Interval);
}
