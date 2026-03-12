using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class SafeArrayAccessExpressionNode : IExpressionNode {
    private readonly IExpressionNode _source;
    private readonly IExpressionNode _index;
    private readonly Func<object, object> _elementConverter;

    public SafeArrayAccessExpressionNode(
        IExpressionNode source, IExpressionNode index, FunnyType resultType,
        Func<object, object> elementConverter, Interval interval) {
        _source = source;
        _index = index;
        _elementConverter = elementConverter;
        Type = resultType;
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new[] { _source, _index };

    public object Calc() {
        var source = _source.Calc();
        if (source is FunnyNone)
            return FunnyNone.Instance;
        var arr = (IFunnyArray)source;
        var index = (int)_index.Calc();
        if (index < 0 || index >= arr.Count)
            throw new FunnyRuntimeException("Argument out of range");
        var element = arr.GetElementOrNull(index) ?? throw new FunnyRuntimeException("Argument out of range");
        return _elementConverter != null ? _elementConverter(element) : element;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new SafeArrayAccessExpressionNode(_source.Clone(context), _index.Clone(context), Type, _elementConverter, Interval);
}
