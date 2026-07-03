using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal sealed class WhileExpressionNode : IExpressionNode {
    private readonly IExpressionNode _condition;
    private readonly IExpressionNode _body;

    public WhileExpressionNode(
        IExpressionNode condition, IExpressionNode body,
        FunnyType type, Interval interval) {
        _condition = condition;
        _body = body;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _condition, _body };

    public object Calc() {
        while ((bool)_condition.Calc()) {
            var result = _body.Calc();
            if (result is BreakSignal) break;
            if (result is ContinueSignal) continue;
            if (result is ReturnSignal) return result; // propagate return up
        }
        return FunnyNone.Instance;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new WhileExpressionNode(
            _condition.Clone(context), _body.Clone(context),
            Type, Interval);
}
