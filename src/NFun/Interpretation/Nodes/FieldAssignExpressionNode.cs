using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

/// <summary>
/// Runtime node for field assignment: mutates the struct field in-place.
/// Zero-copy — modifies the existing FunnyStruct dictionary entry.
/// </summary>
internal class FieldAssignExpressionNode : IExpressionNode {
    private readonly string _fieldName;
    private readonly IExpressionNode _source;
    private readonly IExpressionNode _value;
    private readonly string _variableName;

    public FieldAssignExpressionNode(
        string fieldName, IExpressionNode source, IExpressionNode value,
        Interval interval, FunnyType type, string variableName = null) {
        _fieldName = fieldName;
        _source = source;
        _value = value;
        Interval = interval;
        Type = type;
        _variableName = variableName;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _source, _value };

    public object Calc() {
        var sourceObj = _source.Calc();
        if (sourceObj is null)
            throw new FunnyRuntimeException(
                _variableName != null
                    ? $"Cannot assign to '{_variableName}.{_fieldName}' — '{_variableName}' was not initialized (declare it before field assignment)"
                    : $"Cannot assign to field '.{_fieldName}' — target struct is none");
        var sourceStruct = (FunnyStruct)sourceObj;
        var newValue = _value.Calc();
        sourceStruct.SetValue(_fieldName, newValue);
        return sourceStruct;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new FieldAssignExpressionNode(
            _fieldName,
            _source.Clone(context),
            _value.Clone(context),
            Interval, Type, _variableName);
}
