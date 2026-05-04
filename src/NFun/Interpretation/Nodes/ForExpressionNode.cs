using System.Collections.Generic;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal sealed class ForExpressionNode : IExpressionNode {
    private readonly IExpressionNode _collection;
    private readonly IExpressionNode _body;
    private readonly VariableSource _iteratorVar;

    public ForExpressionNode(
        IExpressionNode collection, IExpressionNode body,
        VariableSource iteratorVar, FunnyType type, Interval interval) {
        _collection = collection;
        _body = body;
        _iteratorVar = iteratorVar;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _collection, _body };

    public object Calc() {
        var collection = _collection.Calc();
        if (collection is IFunnyArray arr) {
            foreach (var item in arr) {
                _iteratorVar.SetFunnyValueUnsafe(item);
                var result = _body.Calc();
                if (result is BreakSignal) break;
                if (result is ContinueSignal) continue;
                if (result is ReturnSignal) return result; // propagate return up
            }
        }
        return FunnyNone.Instance;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new ForExpressionNode(
            _collection.Clone(context), _body.Clone(context),
            _iteratorVar, Type, Interval);
}
