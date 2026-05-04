using System.Collections.Generic;
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

    public FieldAssignExpressionNode(
        string fieldName, IExpressionNode source, IExpressionNode value,
        Interval interval, FunnyType type) {
        _fieldName = fieldName;
        _source = source;
        _value = value;
        Interval = interval;
        Type = type;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _source, _value };

    public object Calc() {
        var sourceStruct = (FunnyStruct)_source.Calc();
        var newValue = _value.Calc();
        sourceStruct.SetValue(_fieldName, newValue);
        return sourceStruct;
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new FieldAssignExpressionNode(
            _fieldName,
            _source.Clone(context),
            _value.Clone(context),
            Interval, Type);
}
