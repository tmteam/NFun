using System;
using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

/// <summary>Wraps an expression with an 'anyway' (finally) block that always executes.</summary>
internal class TryAnywayExpressionNode : IExpressionNode {
    private readonly IExpressionNode _body;
    private readonly IExpressionNode _anyway;

    public TryAnywayExpressionNode(
        IExpressionNode body, IExpressionNode anyway,
        FunnyType type, Interval interval) {
        _body = body;
        _anyway = anyway;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _body, _anyway };

    public object Calc() {
        // C# try/finally lets the finally-exception replace the try-exception.
        // Per Statements.md §Error handling: "If `anyway` throws, the original
        // error propagates." (BugHunt-stmt #71). Capture the original; if
        // anyway also throws, suppress the anyway error and rethrow original.
        Exception original = null;
        object result = null;
        try {
            result = _body.Calc();
        }
        catch (Exception e) {
            original = e;
        }
        try {
            _anyway.Calc();
        }
        catch when (original != null) {
            // swallow anyway's error so original propagates
        }
        if (original != null)
            throw original;
        return result;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new TryAnywayExpressionNode(
            _body.Clone(context), _anyway.Clone(context),
            Type, Interval);
}
