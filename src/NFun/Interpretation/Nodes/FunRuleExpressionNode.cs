using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunRuleExpressionNode : IExpressionNode {
    private readonly ConcreteUserFunction _value;

    public FunRuleExpressionNode(ConcreteUserFunction fun, Interval interval) {
        _value = fun;
        Interval = interval;
        Type = FunnyType.FunOf(_value.ReturnType, _value.ArgTypes);
    }

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public object Calc() => _value;
    public string DebugName => $"FUN-rule {_value}";
    public IEnumerable<IExpressionNode> Children => new []{_value.Expression};

    public IExpressionNode Clone(ICloneContext context) => 
        new FunRuleExpressionNode((ConcreteUserFunction)_value.Clone(context), Interval);
}