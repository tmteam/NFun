using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class ConcreteRecursiveUserFunction : ConcreteUserFunction
    {
        readonly Stack<object[]> _recursiveArgsStack  
            = new Stack<object[]>();

        public override object Calc(object[] args)
        {
            try
            {
                _recursiveArgsStack.Push(args);
                if (_recursiveArgsStack.Count > 400)
                    throw new FunRuntimeStackoverflowException($"stack overflow on {Name}");

                if (args.Length != Variables.Length)
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


        public ConcreteRecursiveUserFunction(string name, VariableSource[] variables, bool isReturnTypeStrictlyTyped, IExpressionNode expression, VarType[] argTypes) : base(name, variables, isReturnTypeStrictlyTyped, expression, argTypes)
        {
        }
    }
}