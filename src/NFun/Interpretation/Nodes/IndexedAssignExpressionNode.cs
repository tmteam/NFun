using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Runtime.Lists;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Runtime node for indexed assignment: <c>a[i] = v</c>. Mutates the list
/// in place (matches `MutableFunnyList`'s reference-type semantics — aliases
/// see the change). Index out of range surfaces as a clean runtime exception
/// per spec §Indexed write.
/// </summary>
internal class IndexedAssignExpressionNode : IExpressionNode {
    private readonly IExpressionNode _target;
    private readonly IExpressionNode _index;
    private readonly IExpressionNode _value;

    public IndexedAssignExpressionNode(
        IExpressionNode target, IExpressionNode index, IExpressionNode value,
        Interval interval, FunnyType type) {
        _target = target;
        _index = index;
        _value = value;
        Interval = interval;
        Type = type;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _target, _index, _value };

    public object Calc() {
        var targetObj = _target.Calc();
        if (targetObj is not IFunnyMutableArray container)
            throw new FunnyRuntimeException(
                "Indexed assignment requires a mutable list or array; got "
                + (targetObj?.GetType().Name ?? "null"));
        var idx = (int)_index.Calc();
        var newValue = _value.Calc();
        if (!container.SetAt(idx, newValue))
            throw new FunnyRuntimeException(
                $"Index {idx} out of range (container count = {container.Count})");
        return container;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new IndexedAssignExpressionNode(
            _target.Clone(context),
            _index.Clone(context),
            _value.Clone(context),
            Interval, Type);
}
