using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

internal class ConcreteRecursiveUserFunction : ConcreteUserFunction {
    readonly Stack<object[]> _recursiveArgsStack = new();
    /// <summary>
    /// Shared depth counter for mutual recursion. All ConcreteRecursiveUserFunction
    /// instances in the same SCC share a single int[1] cell so combined recursion
    /// depth across f→g→f→g→… alternation is bounded (otherwise each per-instance
    /// stack stays below the cap while native call stack overflows).
    /// null for self-recursion (only one function — per-instance stack suffices).
    /// </summary>
    readonly int[] _sharedDepth;
    private const int MaxDepth = 400;

    public override object Calc(object[] args) {
        try
        {
            _recursiveArgsStack.Push(args);
            int depth = _sharedDepth != null ? ++_sharedDepth[0] : _recursiveArgsStack.Count;
            if (depth > MaxDepth)
                throw new FunnyRuntimeStackoverflowException(
                    _sharedDepth != null
                        ? $"stack overflow in mutual recursion (involves {Name})"
                        : $"stack overflow on {Name}");

            if (args.Length != ArgumentSources.Length)
                throw new ArgumentException();
            SetVariables(args);

            var result = Expression.Calc();
            if (result is Nodes.ReturnSignal signal)
                return signal.Value;
            return result;
        }
        finally
        {
            //restore variables
            _recursiveArgsStack.Pop();
            if (_sharedDepth != null) _sharedDepth[0]--;
            if (_recursiveArgsStack.Count > 0)
            {
                var previousArgs = _recursiveArgsStack.Peek();
                SetVariables(previousArgs);
            }
        }
    }

    public override IConcreteFunction Clone(ICloneContext context)
    {
        var sourceClones = ArgumentSources.SelectToArray(s => s.Clone());
        var scopeContext = context.GetScopedContext(sourceClones);

        // Clones get a fresh per-instance shared counter (or null) — depth tracking
        // is per-evaluation, fresh clone = fresh evaluation context.
        var newUserFunction = new ConcreteRecursiveUserFunction(
            Name, sourceClones, Expression.Clone(scopeContext), ArgTypes,
            _sharedDepth != null ? new int[1] : null);
        return newUserFunction;
    }

    internal ConcreteRecursiveUserFunction(
        string name,
        VariableSource[] argumentSources,
        IExpressionNode expression,
        FunnyType[] argTypes,
        int[] sharedDepth = null)
        :
        base(name, argumentSources, expression, argTypes) {
        _sharedDepth = sharedDepth;
    }

    public override string ToString() => $"FUN-req-user {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
    public override FunctionRecursionKind RecursionKind =>
        _sharedDepth != null ? FunctionRecursionKind.MutualRecursion : FunctionRecursionKind.SelfRecursion;
}