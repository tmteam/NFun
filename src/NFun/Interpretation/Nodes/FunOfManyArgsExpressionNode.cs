using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunOfManyArgsExpressionNode : IExpressionNode {
    public FunOfManyArgsExpressionNode(
        IConcreteFunction fun, IExpressionNode[] argsNodes,
        Interval interval, bool[] lazyArgs = null) {
        _fun = fun;
        _argsNodes = argsNodes;
        Interval = interval;
        _argsCount = argsNodes.Length;
        _lazyArgs = lazyArgs;
    }

    private readonly int _argsCount;
    private readonly IConcreteFunction _fun;
    private readonly IExpressionNode[] _argsNodes;
    private readonly bool[] _lazyArgs;

    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public IEnumerable<IRuntimeNode> Children => _argsNodes;

    public object Calc() {
        var args = new object[_argsCount];
        if (_lazyArgs != null)
            for (int i = 0; i < _argsCount; i++)
                args[i] = _lazyArgs[i] ? (object)_argsNodes[i] : _argsNodes[i].Calc();
        else
            for (int i = 0; i < _argsCount; i++)
                args[i] = _argsNodes[i].Calc();
        try { return _fun.Calc(args); }
        catch (FunnyRuntimeException) { throw; }
        catch (Exception e) { throw new FunnyRuntimeException(e.Message, e); }
    }

    public IExpressionNode Clone(ICloneContext context) {
        var funCopy =  _fun.Clone(context);
        var argNodesCopy = _argsNodes.SelectToArray(a => a.Clone(context));
        return new FunOfManyArgsExpressionNode(funCopy, argNodesCopy, Interval, _lazyArgs);
    }
}
