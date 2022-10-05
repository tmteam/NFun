using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunVariableExpressionNode : IExpressionNode {
    private readonly IConcreteFunction _value;

    public FunVariableExpressionNode(IConcreteFunction fun, Interval interval) {
        _value = fun;
        Interval = interval;
        Type = FunnyType.FunOf(_value.ReturnType, _value.ArgTypes);
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public object Calc() => _value;
    public string DebugName => $"FUN-variable {_value}";
    public IEnumerable<IExpressionNode> Children => Array.Empty<IExpressionNode>();

    public IExpressionNode Clone(ICloneContext context) => 
        new FunVariableExpressionNode(_value.Clone( context), Interval);
}