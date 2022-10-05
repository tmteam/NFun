using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class StructInitExpressionNode : IExpressionNode {
    private readonly string[] _fieldNames;
    private readonly IExpressionNode[] _elements;

    public StructInitExpressionNode(
        string[] fieldNames, IExpressionNode[] elements, Interval interval, FunnyType type) {
        Type = type;
        _fieldNames = fieldNames;
        _elements = elements;
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }

    public object Calc() {
        var fields = new FunnyStruct.FieldsDictionary(_fieldNames.Length);
        for (var i = 0; i < _fieldNames.Length; i++)
            fields.Add(_fieldNames[i], _elements[i].Calc());

        return new FunnyStruct(fields);
    }

    public string DebugName => "Struct init";
    public IEnumerable<IExpressionNode> Children => _elements;

    public IExpressionNode Clone(ICloneContext context) {
        var elementsCopy = _elements.SelectToArray(e => e.Clone(context));
        return new StructInitExpressionNode(_fieldNames, elementsCopy, Interval, Type);
    }
}