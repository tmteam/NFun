using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunOfSingleArgExpressionNode : IExpressionNode {
    public FunOfSingleArgExpressionNode(FunctionWithSingleArg fun, IExpressionNode argsNode, Interval interval) {
        _fun = fun;
        _arg1 = argsNode;
        Interval = interval;
    }

    private readonly FunctionWithSingleArg _fun;
    private readonly IExpressionNode _arg1;

    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public IEnumerable<IRuntimeNode> Children => new[] { _arg1 };

    public object Calc() {
        try { return _fun.Calc(_arg1.Calc()); }
        catch (FunnyRuntimeException) { throw; }
        catch (Exception e) { throw new FunnyRuntimeException(e.Message, e); }
    }

    public IExpressionNode Clone(ICloneContext context)
        => new FunOfSingleArgExpressionNode((FunctionWithSingleArg)_fun.Clone(context), _arg1.Clone(context), Interval);
}
