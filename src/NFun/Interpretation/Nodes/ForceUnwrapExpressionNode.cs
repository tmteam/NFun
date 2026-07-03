using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Runtime node for the `!` (force unwrap) operator — handled as a TIC special
/// form (mirrors SafeGetElement / NullCoalesce). Strips one Optional layer from
/// the source value; throws <see cref="FunnyRuntimeException"/> on None.
/// Bug hunt round 5 #10 family.
/// </summary>
internal class ForceUnwrapExpressionNode : IExpressionNode {
    private readonly IExpressionNode _source;
    private readonly Func<object, object> _converter;

    public ForceUnwrapExpressionNode(IExpressionNode source, FunnyType type,
        Func<object, object> converter, Interval interval) {
        _source = source;
        _converter = converter;
        Type = type;
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new[] { _source };

    public object Calc() {
        var value = _source.Calc();
        if (value is FunnyNone)
            throw new FunnyRuntimeException("Force unwrap of none value");
        return _converter == null ? value : _converter(value);
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new ForceUnwrapExpressionNode(_source.Clone(context), Type, _converter, Interval);
}
