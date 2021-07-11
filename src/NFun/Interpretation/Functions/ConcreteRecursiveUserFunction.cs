using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions
{
    public class ConcreteRecursiveUserFunction : ConcreteUserFunction
    {
        readonly Stack<object[]> _recursiveArgsStack = new();

        public override object Calc(object[] args)
        {
            try
            {
                _recursiveArgsStack.Push(args);
                if (_recursiveArgsStack.Count > 400)
                    throw new FunRuntimeStackoverflowException($"stack overflow on {Name}");

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


        internal ConcreteRecursiveUserFunction(
            string name, 
            VariableSource[] argumentSources,
            IExpressionNode expression, 
            FunnyType[] argTypes) 
            : 
            base(name, argumentSources,expression, argTypes)
        {
        }
    }
}