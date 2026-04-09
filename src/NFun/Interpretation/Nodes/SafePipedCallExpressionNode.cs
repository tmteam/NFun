using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Runtime node for ?.method() — safe piped call on Optional.
/// If source is None → returns None. Else → calls inner function, returns result.
/// TIC already types the result as Optional(R).
/// </summary>
internal class SafePipedCallExpressionNode : IExpressionNode {
    private readonly IExpressionNode _source;
    private readonly IExpressionNode _innerCall;

    public SafePipedCallExpressionNode(
        IExpressionNode source, IExpressionNode innerCall,
        FunnyType outputType, Interval interval) {
        _source = source;
        _innerCall = innerCall;
        Type = outputType;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _source, _innerCall };

    public object Calc() {
        var sourceVal = _source.Calc();
        if (sourceVal is FunnyNone)
            return FunnyNone.Instance;
        return _innerCall.Calc();
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new SafePipedCallExpressionNode(
            _source.Clone(context), _innerCall.Clone(context), Type, Interval);
}
