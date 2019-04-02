using System;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class FunctionPrototype: FunctionBase
    {
        public FunctionPrototype(string name, VarType outputType, VarType[] argTypes) : base(name,  outputType, argTypes)
        {
        }

        private FunctionBase _function;
        public void SetActual(FunctionBase function)
        {
            _function = function;
            
            if(OutputType!= function.OutputType)
                throw new OutpuCastFunParseException($"{_function.OutputType} is not supported as output fun parameter");
        }

        public override object Calc(object[] args)
        {
            if(_function== null)
                throw new InvalidOperationException("Function prototype cannot be called");
            return _function.Calc(args);
        }
    }
}