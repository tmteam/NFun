using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunOfManyArgsExpressionNode : IExpressionNode {
    public FunOfManyArgsExpressionNode(
        IConcreteFunction fun, IExpressionNode[] argsNodes,
        Interval interval) {
        _fun = fun;
        _argsNodes = argsNodes;
        Interval = interval;
        _argsCount = argsNodes.Length;
    }

    private readonly int _argsCount;
    private readonly IConcreteFunction _fun;
    private readonly IExpressionNode[] _argsNodes;

    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public IEnumerable<IRuntimeNode> Children => _argsNodes;

    public object Calc() {
        var args = new object[_argsCount];
        for (int i = 0; i < _argsCount; i++)
            args[i] = _argsNodes[i].Calc();
        return _fun.Calc(args);
    }

    public IExpressionNode Clone(ICloneContext context) {
        var funCopy =  _fun.Clone(context);
        var argNodesCopy = _argsNodes.SelectToArray(a => a.Clone(context));
        return new FunOfManyArgsExpressionNode(funCopy, argNodesCopy, Interval);
    }
}
