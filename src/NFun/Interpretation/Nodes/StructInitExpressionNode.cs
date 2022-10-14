using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class StructInitExpressionNode : IExpressionNode {
    public StructInitExpressionNode(
        string[] fieldNames, IExpressionNode[] elements, Interval interval, FunnyType type) {
        Type = type;
        _fieldNames = fieldNames;
        _elements = elements;
        Interval = interval;
    }
    
    private readonly string[] _fieldNames;
    private readonly IExpressionNode[] _elements;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IExpressionNode> Children => _elements;

    public object Calc() {
        var fields = new FunnyStruct.FieldsDictionary(_fieldNames.Length);
        for (var i = 0; i < _fieldNames.Length; i++)
            fields.Add(_fieldNames[i], _elements[i].Calc());

        return new FunnyStruct(fields);
    }

    public IExpressionNode Clone(ICloneContext context) {
        var elementsCopy = _elements.SelectToArray(e => e.Clone(context));
        return new StructInitExpressionNode(_fieldNames, elementsCopy, Interval, Type);
    }
}