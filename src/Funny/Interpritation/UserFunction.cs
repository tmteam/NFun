using System;
using System.Collections.Generic;

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
            : base(name, variables.Length)
        {
            _variables = variables;
            _expression = expression;
        }

        readonly Stack<double[]> recursiveArgsStack  = new Stack<double[]>();       
        public override double Calc(double[] args)
        {
            try
            {
                recursiveArgsStack.Push(args);
                if(args.Length!= _variables.Length)
                    throw new ArgumentException();
                SetVariables(args);
                return  _expression.Calc();
            }
            finally
            {
                //restore variables
                recursiveArgsStack.Pop();
                if(recursiveArgsStack.TryPeek(out var previousArgs))
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