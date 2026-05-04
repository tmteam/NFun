using System;
using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal sealed class ContinueExpressionNode : IExpressionNode {
    public ContinueExpressionNode(FunnyType type, Interval interval) {
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => Array.Empty<IRuntimeNode>();
    public object Calc() => ContinueSignal.Instance;
    public IExpressionNode Clone(ICloneContext context) => new ContinueExpressionNode(Type, Interval);
}
