using System;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class FunctionPrototype: FunctionBase
    {
        public FunctionPrototype(string name, VarType outputType, VarType[] argTypes) : base(name,  outputType, argTypes)
        {
        }

        private FunctionBase _function;
        public void SetActual(FunctionBase function, Interval interval)
        {
            _function = function;

            if (OutputType != function.OutputType)
                throw ErrorFactory.InvalidOutputType(function, interval);
        }

        public override object Calc(object[] args)
        {
            if(_function== null)
                throw new InvalidOperationException("Function prototype cannot be called");
            return _function.Calc(args);
        }
    }
}