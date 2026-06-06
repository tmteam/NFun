using System.Collections.Generic;
using NFun.Runtime;
using NFun.Runtime.Lists;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal sealed class ForExpressionNode : IExpressionNode {
    private readonly IExpressionNode _collection;
    private readonly IExpressionNode _body;
    private readonly VariableSource _iteratorVar;

    /// <summary>
    /// Iterator VariableSource — exposed so an enclosing lambda's capture
    /// collector can exclude it from snapshotting (BugHunt-stmt #63).
    /// </summary>
    internal VariableSource IteratorVar => _iteratorVar;

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
        // Iterates anything implementing IFunnyEnumerable — `IFunnyArray` and
        // `IFunnyList` are the current concrete shapes. Using the marker
        // interface excludes stray CLR collections that bypassed NFun's input
        // converters from being silently iterated as if they were funny values.
        if (collection is IFunnyEnumerable items) {
            foreach (var item in items) {
                _iteratorVar.SetFunnyValueUnsafe(item);
                var result = _body.Calc();
                if (result is BreakSignal) break;
                if (result is ContinueSignal) continue;
                if (result is ReturnSignal) return result; // propagate return up
            }
        }
        return FunnyNone.Instance;
    }

    public IExpressionNode Clone(ICloneContext context) {
        // Body is cloned in a scope that exposes the iterator variable as-is
        // (no snapshotting). Without this, an enclosing lambda's
        // SnapshotCloneContext snapshots iteratorVar at lambda-emission time
        // (default value 0); the for-loop then sets the ORIGINAL source on
        // each iteration but the body reads the SNAPSHOT — silent wrong
        // value (BugHunt-stmt #63).
        var bodyContext = context.GetScopedContext(new[] { _iteratorVar });
        return new ForExpressionNode(
            _collection.Clone(context), _body.Clone(bodyContext),
            _iteratorVar, Type, Interval);
    }
}
