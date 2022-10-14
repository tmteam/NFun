using System;
using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class DefaultValueExpressionNode : IExpressionNode {
    public DefaultValueExpressionNode(object value, FunnyType type, Interval interval) {
        _value = value;
        Interval = interval;
        Type = type;
    }
    private readonly object _value;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IExpressionNode> Children => Array.Empty<IExpressionNode>();

    public object Calc() => _value;
    public IExpressionNode Clone(ICloneContext context) => this;
}