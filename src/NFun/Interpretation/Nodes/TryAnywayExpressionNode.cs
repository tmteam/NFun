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
        try {
            return _body.Calc();
        }
        finally {
            _anyway.Calc();
        }
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new TryAnywayExpressionNode(
            _body.Clone(context), _anyway.Clone(context),
            Type, Interval);
}
