using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class StructFieldAccessExpressionNode : IExpressionNode {
    public StructFieldAccessExpressionNode(string fieldName, IExpressionNode source, Interval interval) {
        _fieldName = fieldName;
        _source = source;
        Type = source.Type.StructTypeSpecification[fieldName];
        Interval = interval;
    }

    private readonly string _fieldName;
    private readonly IExpressionNode _source;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IExpressionNode> Children => new[] { _source };

    public object Calc() => 
        ((FunnyStruct)_source.Calc()).GetValue(_fieldName);
    
    public IExpressionNode Clone(ICloneContext context) => 
        new StructFieldAccessExpressionNode(_fieldName, _source.Clone(context), Interval);
}