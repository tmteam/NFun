using System;
using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Sentinel object returned by ReturnExpressionNode.Calc() to signal early return.
/// Replaces the previous ReturnSignalException approach (which cost ~1000ns per throw).
/// The sentinel propagates up through return values and is caught by BlockExpressionNode
/// and ConcreteUserFunction.Calc() via a cheap reference comparison (~0.5ns).
/// </summary>
internal sealed class ReturnSignal {
    /// <summary>The actual value to return from the function.</summary>
    public object Value;

    // Single reusable instance per thread. Safe because:
    // - NFun evaluation is single-threaded per expression
    // - Recursive calls: ConcreteUserFunction.Calc() unwraps the sentinel before
    //   returning to the caller, so nesting is safe even with a single instance
    // - Parallel evaluation: each thread gets its own instance via [ThreadStatic]
    [ThreadStatic] private static ReturnSignal _instance;

    public static ReturnSignal Instance => _instance ??= new ReturnSignal();
}

internal class ReturnExpressionNode : IExpressionNode {
    private readonly IExpressionNode _expression;

    public ReturnExpressionNode(IExpressionNode expression, FunnyType type, Interval interval) {
        _expression = expression;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => _expression != null ? new IRuntimeNode[] { _expression } : Array.Empty<IRuntimeNode>();

    public object Calc() {
        var signal = ReturnSignal.Instance;
        signal.Value = _expression?.Calc();
        return signal;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new ReturnExpressionNode(_expression?.Clone(context), Type, Interval);
}
