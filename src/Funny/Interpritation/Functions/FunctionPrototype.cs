using System;
using Funny.Runtime;

namespace Funny.Interpritation.Functions
{
    public class FunctionPrototype: FunctionBase
    {
        public FunctionPrototype(string name, int argsCount) : base(name, argsCount, VarType.RealType)
        {
        }

        private FunctionBase _function;
        public void SetActual(FunctionBase function)
        {
            _function = function;
            
            if(Type!= function.Type)
                throw new OutpuCastParseException($"{_function.Type} is not supported as output fun parameter");
        }

        public override object Calc(double[] args)
        {
            if(_function== null)
            throw new InvalidOperationException("Function prototype cannot be called");
            return _function.Calc(args);
        }
    }
}