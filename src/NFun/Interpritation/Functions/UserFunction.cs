using System;
using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Exceptions;
using NFun.Interpritation.Nodes;
using NFun.Runtime;

namespace NFun.Interpritation.Functions
{
    public class UserFunction : FunctionBase
    {
        public VariableSource[] Variables { get; }
        public bool IsReturnTypeStrictlyTyped { get; }
        private readonly IExpressionNode _expression;
            
        public UserFunction(
            string name, 
            VariableSource[] variables,
            bool isReturnTypeStrictlyTyped,
            IExpressionNode expression) 
            : base(
                name, 
                expression.Type,
                variables.Select(v=>v.Type).ToArray())
        {
            Variables = variables;
            IsReturnTypeStrictlyTyped = isReturnTypeStrictlyTyped;
            _expression = expression;
        }

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

                return _expression.Calc();
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

        private void SetVariables(object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                Variables[i].Value=  args[i];
        }

        public override string ToString() 
            => $"{Name}({string.Join(",",ArgTypes.Select(a=>a.ToString()))}):{ReturnType}";
    }
}