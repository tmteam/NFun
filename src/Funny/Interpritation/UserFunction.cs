using System;

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

        public override double Calc(double[] args)
        {
            if(args.Length!= _variables.Length)
                throw new ArgumentException();
            
            for (int i = 0; i < args.Length; i++)
                _variables[i].SetValue(args[i]);

            var res =  _expression.Calc();
            return res;    
        }
    }
}