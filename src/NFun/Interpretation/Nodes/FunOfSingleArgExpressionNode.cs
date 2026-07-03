using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunOfSingleArgExpressionNode : IExpressionNode {
    public FunOfSingleArgExpressionNode(FunctionWithSingleArg fun, IExpressionNode argsNode, Interval interval,
        bool lazy = false) {
        _fun = fun;
        _arg1 = argsNode;
        _lazy = lazy;
        Interval = interval;
    }

    private readonly FunctionWithSingleArg _fun;
    private readonly IExpressionNode _arg1;
    private readonly bool _lazy;

    internal IConcreteFunction Fun => _fun;
    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public IEnumerable<IRuntimeNode> Children => new[] { _arg1 };

    public object Calc() {
        try {
            if (_lazy) return _fun.Calc((object)_arg1);
            object a = _arg1.Calc();
            if (a is ReturnSignal or BreakSignal or ContinueSignal) return a;
            return _fun.Calc(a);
        }
        catch (FunnyRuntimeException) { throw; }
        catch (OverflowException e) {
            // Rewrap CLR overflow text (e.g. "Negating the minimum value of a
            // twos complement number is invalid") in a NFun-typed runtime
            // message. Bug hunt round 3 #19.
            throw new FunnyRuntimeException($"{_fun.Name}: integer overflow", e);
        }
        catch (Exception e) { throw new FunnyRuntimeException(e.Message, e); }
    }

    public IExpressionNode Clone(ICloneContext context)
        => new FunOfSingleArgExpressionNode(
            (FunctionWithSingleArg)_fun.Clone(context),
            _arg1.Clone(context), Interval, _lazy);
}
