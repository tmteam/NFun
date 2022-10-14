using System;
using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes; 

internal class CastExpressionNode : IExpressionNode {
    public static IExpressionNode GetConvertedOrOriginOrThrow(IExpressionNode origin, FunnyType to, TypeBehaviour typeBehaviour) {
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

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IExpressionNode> Children => new[] { _origin };

    public object Calc() {
        var res = _origin.Calc();
        return _converter(res);
    }

    public IExpressionNode Clone(ICloneContext context) => 
        new CastExpressionNode(_origin.Clone(context), Type, _converter, Interval);
}