using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

internal class FunOfSingleArgExpressionNode : IExpressionNode {
    private readonly FunctionWithSingleArg _fun;
    private readonly IExpressionNode _arg1;

    public FunOfSingleArgExpressionNode(FunctionWithSingleArg fun, IExpressionNode argsNode, Interval interval) {
        _fun = fun;
        _arg1 = argsNode;
        Interval = interval;
    }

    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public object Calc() => _fun.Calc(_arg1.Calc());
    public string DebugName => $"FUN-single {_fun}";
    public IEnumerable<IExpressionNode> Children => new[] { _arg1 };

    public IExpressionNode Clone(ICloneContext context) 
        => new FunOfSingleArgExpressionNode((FunctionWithSingleArg)_fun.Clone(context), _arg1.Clone(context), Interval);
}