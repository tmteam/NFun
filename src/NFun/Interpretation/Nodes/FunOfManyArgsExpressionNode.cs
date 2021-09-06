using NFun.Interpretation.Functions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes {

internal class FunOfManyArgsExpressionNode : IExpressionNode {
    private readonly FunctionWithManyArguments _fun;
    private readonly IExpressionNode[] _argsNodes;

    public FunOfManyArgsExpressionNode(
        FunctionWithManyArguments fun, IExpressionNode[] argsNodes,
        Interval interval) {
        _fun = fun;
        _argsNodes = argsNodes;
        Interval = interval;
        _argsCount = argsNodes.Length;
    }

    private readonly int _argsCount;
    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;

    public object Calc() {
        var args = new object[_argsCount];
        for (int i = 0; i < _argsCount; i++)
            args[i] = _argsNodes[i].Calc();
        return _fun.Calc(args);
    }
}

}