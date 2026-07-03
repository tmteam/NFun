using System;
using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class CastExpressionNode : IExpressionNode {
    public static IExpressionNode GetConvertedOrOriginOrThrow(IExpressionNode origin, FunnyType to,
        TypeBehaviour typeBehaviour) {
        if (origin.Type == to)
            return origin;
        var converter = VarTypeConverter.GetConverterOrThrow(typeBehaviour, origin.Type, to, origin.Interval);
        return new CastExpressionNode(origin, to, converter, origin.Interval);
    }

    public CastExpressionNode(
        IExpressionNode origin,
        FunnyType targetType,
        Func<object, object> converter,
        Interval interval) {
        _origin = origin;
        Type = targetType;
        _converter = converter;
        Interval = interval;
    }

    private readonly IExpressionNode _origin;
    private readonly Func<object, object> _converter;

    internal IExpressionNode Origin => _origin;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new[] { _origin };

    public object Calc() {
        var res = _origin.Calc();
        // Control-flow sentinels (ReturnSignal/BreakSignal/ContinueSignal)
        // are not values — they carry the early-exit decision up through the
        // expression tree until BlockExpressionNode or ConcreteUserFunction.Calc
        // catches them. They have no type and must not flow into _converter,
        // which is typed for value→value (e.g. FunnyStruct→FunnyStruct) and
        // would throw `InvalidCastException` on the sentinel. Pass through.
        if (res is ReturnSignal or BreakSignal or ContinueSignal)
            return res;
        return _converter(res);
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new CastExpressionNode(_origin.Clone(context), Type, _converter, Interval);
}
