using System;
using System.Collections.Generic;
using Funny.Interpritation.Functions;
using Funny.Interpritation.Nodes;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public class UserFunction : FunctionBase
    {
        private readonly VariableExpressionNode[] _variables;
        private readonly IExpressionNode _expression;

        public UserFunction(
            string name, 
            VariableExpressionNode[] variables, 
            IExpressionNode expression) 
            : base(name, variables.Length, expression.Type)
        {
            _variables = variables;
            _expression = expression;
        }

        readonly Stack<double[]> _recursiveArgsStack  
            = new Stack<double[]>();


        public override object Calc(double[] args)
        {
            try
            {
                _recursiveArgsStack.Push(args);
                if(_recursiveArgsStack.Count>400)
                    throw new FunStackoverflowException($"stack overflow on {base.Name}");

                if (args.Length != _variables.Length)
                    throw new ArgumentException();
                SetVariables(args);
                
                return _expression.Calc();
            }
            finally
            {
                //restore variables
                _recursiveArgsStack.Pop();
                if (_recursiveArgsStack.TryPeek(out var previousArgs))
                    SetVariables(previousArgs);
            }
        }

        private void SetVariables(double[] args)
        {
            for (int i = 0; i < args.Length; i++)
                _variables[i].SetValue(args[i]);
        }
    }
}