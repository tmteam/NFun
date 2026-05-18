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
        var inner = _expression != null ? _expression.Calc() : NFun.Types.FunnyNone.Instance;
        // If the inner expression already produced a control-flow signal (e.g.
        // `return return X`, or `return try-catch` where the catch body itself
        // executed `return X`), propagate it as-is. Re-wrapping a sentinel inside
        // another ReturnSignal would leak the inner sentinel to the caller as a
        // value (BugHunt-stmt #54/#56).
        if (inner is ReturnSignal or BreakSignal or ContinueSignal)
            return inner;
        var signal = ReturnSignal.Instance;
        // Bare `return` (no expression) yields `none` per Statements.md. Use the
        // FunnyNone sentinel so callers that test for none (e.g. `??`) see it
        // correctly; raw null would silently miss the check (BugHunt-stmt #29).
        signal.Value = inner;
        return signal;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new ReturnExpressionNode(_expression?.Clone(context), Type, Interval);
}
