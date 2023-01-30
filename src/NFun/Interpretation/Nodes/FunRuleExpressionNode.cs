using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunRuleExpressionNode : IExpressionNode {
    public FunRuleExpressionNode(ConcreteUserFunction fun, Interval interval) {
        _value = fun;
        Interval = interval;
        Type = FunnyType.FunOf(_value.ReturnType, _value.ArgTypes);
    }

    private readonly ConcreteUserFunction _value;

    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => new []{_value.Expression};

    public object Calc() => _value;

    public IExpressionNode Clone(ICloneContext context) =>
        new FunRuleExpressionNode((ConcreteUserFunction)_value.Clone(context), Interval);
}
