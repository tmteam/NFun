using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunVariableExpressionNode : IExpressionNode {
    public FunVariableExpressionNode(IConcreteFunction fun, Interval interval) {
        _value = fun;
        Interval = interval;
        Type = FunnyType.FunOf(_value.ReturnType, _value.ArgTypes);
    }

    private readonly IConcreteFunction _value;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => Array.Empty<IExpressionNode>();

    public object Calc() => _value;

    public IExpressionNode Clone(ICloneContext context) =>
        new FunVariableExpressionNode(_value.Clone( context), Interval);
}
