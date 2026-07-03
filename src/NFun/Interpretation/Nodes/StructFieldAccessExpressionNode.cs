using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class StructFieldAccessExpressionNode : IExpressionNode {
    private readonly string _fieldName;
    private readonly IExpressionNode _source;
    private readonly System.Func<object, object> _converter;

    public StructFieldAccessExpressionNode(string fieldName, IExpressionNode source, Interval interval) {
        _fieldName = fieldName;
        _source = source;
        Type = source.Type.StructTypeSpecification[fieldName];
        Interval = interval;
    }

    public StructFieldAccessExpressionNode(string fieldName, IExpressionNode source, Interval interval,
        FunnyType overrideType, System.Func<object, object> converter = null) {
        _fieldName = fieldName;
        _source = source;
        _converter = converter;
        Type = overrideType;
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new[] { _source };

    public object Calc() {
        var sourceObj = _source.Calc();
        if (sourceObj is null)
            throw new FunnyRuntimeException(
                $"Cannot read field '.{_fieldName}' — target struct is none (use `?.` for safe access on optional types)");
        var val = ((FunnyStruct)sourceObj).GetValue(_fieldName);
        return _converter != null ? _converter(val) : val;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new StructFieldAccessExpressionNode(_fieldName, _source.Clone(context), Interval, Type, _converter);
}
