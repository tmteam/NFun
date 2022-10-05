using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class FunOfTwoArgsExpressionNode : IExpressionNode {
    private readonly FunctionWithTwoArgs _fun;
    private readonly IExpressionNode _arg1;
    private readonly IExpressionNode _arg2;

    public FunOfTwoArgsExpressionNode(
        FunctionWithTwoArgs fun, IExpressionNode argNode1, IExpressionNode argNode2, Interval interval) {
        _fun = fun;
        _arg1 = argNode1;
        _arg2 = argNode2;
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public object Calc() => _fun.Calc(_arg1.Calc(), _arg2.Calc());
    public string DebugName => $"FUN-two {_fun}";
    public IEnumerable<IExpressionNode> Children => new[] { _arg1, _arg2 };

    public IExpressionNode Clone(ICloneContext context)
        => new FunOfTwoArgsExpressionNode((FunctionWithTwoArgs)_fun.Clone(context), _arg1.Clone(context), _arg2.Clone(context), Interval);
}