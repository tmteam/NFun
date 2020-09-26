using System;
using System.Collections.Generic;
using System.Linq;
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

    public class ConcreteUserFunction : FunctionWithManyArguments
    {
        public VariableSource[] Variables { get; }
        public bool IsReturnTypeStrictlyTyped { get; }
        protected readonly IExpressionNode Expression;

        public static ConcreteUserFunction Create(string name,
            VariableSource[] variables,
            bool isReturnTypeStrictlyTyped,
            IExpressionNode expression,
            bool isRecursive)
        {
            var argTypes = new VarType[variables.Length];
            for (int i = 0; i < variables.Length; i++) 
                argTypes[i] = variables[i].Type;
            if(isRecursive)
                return new ConcreteRecursiveUserFunction(name, variables, isReturnTypeStrictlyTyped, expression, argTypes);
            else
                return new ConcreteUserFunction(name, variables, isReturnTypeStrictlyTyped, expression, argTypes);
        }

        protected ConcreteUserFunction(
            string name, 
            VariableSource[] variables,
            bool isReturnTypeStrictlyTyped,
            IExpressionNode expression, VarType[] argTypes) 
            : base(
                name, 
                expression.Type,
                argTypes)
        {
            Variables = variables;
            IsReturnTypeStrictlyTyped = isReturnTypeStrictlyTyped;
            Expression = expression;
        }

        protected void SetVariables(object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                Variables[i].Value=  args[i];
        }
        public override object Calc(object[] args)
        {
            SetVariables(args);
            return Expression.Calc();
        }

        public override string ToString() 
            => $"{Name}({string.Join(",",ArgTypes.Select(a=>a.ToString()))}):{ReturnType}";
    }
}