using System;

namespace Funny.Interpritation
{
    public class FunctionPrototype: FunctionBase
    {
        public FunctionPrototype(string name, int argsCount) : base(name, argsCount)
        {
        }

        private FunctionBase _function;
        public void SetActual(FunctionBase function)
        {
            _function = function;
        }
        public override double Calc(double[] args)
        {
            if(_function== null)
            throw new InvalidOperationException("Function prototype cannot be called");
            return _function.Calc(args);
        }
    }
}