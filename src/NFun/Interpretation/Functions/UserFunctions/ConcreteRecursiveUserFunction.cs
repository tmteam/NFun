using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

internal class ConcreteRecursiveUserFunction : ConcreteUserFunction {
    readonly Stack<object[]> _recursiveArgsStack = new();

    public override object Calc(object[] args) {
        try
        {
            _recursiveArgsStack.Push(args);
            if (_recursiveArgsStack.Count > 400)
                throw new FunnyRuntimeStackoverflowException($"stack overflow on {Name}");

            if (args.Length != ArgumentSources.Length)
                throw new ArgumentException();
            SetVariables(args);

            return Expression.Calc();
        }
        finally
        {
            //restore variables
            _recursiveArgsStack.Pop();
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

        var newUserFunction = new ConcreteRecursiveUserFunction(Name, sourceClones, Expression.Clone(scopeContext), ArgTypes);
        return newUserFunction;
    }
    
    internal ConcreteRecursiveUserFunction(
        string name,
        VariableSource[] argumentSources,
        IExpressionNode expression,
        FunnyType[] argTypes)
        :
        base(name, argumentSources, expression, argTypes) { }
    
    public override string ToString() => $"FUN-req-user {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
    public override FunctionRecursionKind RecursionKind => FunctionRecursionKind.SelfRecursion;
}