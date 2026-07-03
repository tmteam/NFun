using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;

internal class FunOfTwoArgsExpressionNode : IExpressionNode {
    public FunOfTwoArgsExpressionNode(
        FunctionWithTwoArgs fun, IExpressionNode argNode1, IExpressionNode argNode2, Interval interval,
        bool lazy1 = false, bool lazy2 = false) {
        _fun = fun;
        _arg1 = argNode1;
        _arg2 = argNode2;
        _lazy1 = lazy1;
        _lazy2 = lazy2;
        Interval = interval;
    }

    private readonly FunctionWithTwoArgs _fun;
    private readonly IExpressionNode _arg1;
    private readonly IExpressionNode _arg2;
    private readonly bool _lazy1;
    private readonly bool _lazy2;

    internal IConcreteFunction Fun => _fun;
    public Interval Interval { get; }
    public FunnyType Type => _fun.ReturnType;
    public IEnumerable<IRuntimeNode> Children => new[] { _arg1, _arg2 };

    public object Calc() {
        try {
            // Control-flow signals (return/break/continue) short-circuit the call:
            // any arg that produces a sentinel propagates up without invoking the
            // function (BugHunt-stmt #57: `5 + return 3` must not feed a sentinel
            // into the binary op).
            object a1 = _lazy1 ? (object)_arg1 : _arg1.Calc();
            if (a1 is ReturnSignal or BreakSignal or ContinueSignal) return a1;
            object a2 = _lazy2 ? (object)_arg2 : _arg2.Calc();
            if (a2 is ReturnSignal or BreakSignal or ContinueSignal) return a2;
            return _fun.Calc(a1, a2);
        }
        catch (FunnyRuntimeException) { throw; }
        catch (OverflowException e) {
            throw new FunnyRuntimeException($"{_fun.Name}: integer overflow", e);
        }
        catch (DivideByZeroException e) {
            throw new FunnyRuntimeException($"{_fun.Name}: division by zero", e);
        }
        catch (Exception e) { throw new FunnyRuntimeException(e.Message, e); }
    }

    public IExpressionNode Clone(ICloneContext context)
        => new FunOfTwoArgsExpressionNode(
            (FunctionWithTwoArgs)_fun.Clone(context),
            _arg1.Clone(context), _arg2.Clone(context),
            Interval, _lazy1, _lazy2);
}
