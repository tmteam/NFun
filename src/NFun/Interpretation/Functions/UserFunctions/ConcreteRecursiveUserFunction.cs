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
    /// Local variables declared inside the function body (e.g. <c>a = f(n-1)</c>,
    /// <c>b = f(n-1)</c>). The body owns a single <see cref="VariableSource"/>
    /// per local — without a per-frame snapshot, an inner recursive call
    /// overwrites the outer call's local slot, producing silent wrong values
    /// when the outer code reads the local after the recursion returns
    /// (e.g. <c>return a + b</c>). Snapshot-on-entry / restore-on-exit per
    /// recursive call gives proper stack-frame semantics. Empty array if the
    /// body has no locals.
    /// </summary>
    readonly VariableSource[] _localSources;
    readonly Stack<object[]> _localValuesStack = new();
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

            // Snapshot current local values BEFORE setting args / running body.
            // After this call returns, restore so the caller's frame sees its locals.
            if (_localSources.Length > 0)
            {
                var snapshot = new object[_localSources.Length];
                for (int i = 0; i < _localSources.Length; i++)
                    snapshot[i] = _localSources[i].FunnyValue;
                _localValuesStack.Push(snapshot);
            }

            SetVariables(args);

            var result = Expression.Calc();
            if (result is Nodes.ReturnSignal signal)
                return signal.Value;
            // Block-form fun body that falls off the end without `return`
            // returns `none` per Statements.md §Functions. Lambdas keep
            // last-expression-as-return semantics.
            if (!_isLambda && Expression is Nodes.BlockExpressionNode)
                return Types.FunnyNone.Instance;
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
            // Restore locals from snapshot so the caller's frame sees its values.
            if (_localSources.Length > 0 && _localValuesStack.Count > 0)
            {
                var snapshot = _localValuesStack.Pop();
                for (int i = 0; i < _localSources.Length; i++)
                    _localSources[i].SetFunnyValueUnsafe(snapshot[i]);
            }
        }
    }

    public override IConcreteFunction Clone(ICloneContext context)
    {
        var sourceClones = ArgumentSources.SelectToArray(s => s.Clone());
        var scopeContext = context.GetScopedContext(sourceClones);
        // Locals are reachable through the expression tree; we don't have a
        // direct map after cloning. Clone with an empty local array — clones
        // are used for fresh evaluation contexts so per-call save/restore is
        // less critical there. (TODO: thread localSources through clone if a
        // recursion regression surfaces in cloned scopes.)
        var newUserFunction = new ConcreteRecursiveUserFunction(
            Name, sourceClones, Expression.Clone(scopeContext), ArgTypes,
            System.Array.Empty<VariableSource>(),
            _sharedDepth != null ? new int[1] : null);
        newUserFunction._isLambda = _isLambda;
        return newUserFunction;
    }

    internal ConcreteRecursiveUserFunction(
        string name,
        VariableSource[] argumentSources,
        IExpressionNode expression,
        FunnyType[] argTypes,
        VariableSource[] localSources = null,
        int[] sharedDepth = null)
        :
        base(name, argumentSources, expression, argTypes) {
        _localSources = localSources ?? System.Array.Empty<VariableSource>();
        _sharedDepth = sharedDepth;
    }

    public override string ToString() => $"FUN-req-user {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
    public override FunctionRecursionKind RecursionKind =>
        _sharedDepth != null ? FunctionRecursionKind.MutualRecursion : FunctionRecursionKind.SelfRecursion;
}