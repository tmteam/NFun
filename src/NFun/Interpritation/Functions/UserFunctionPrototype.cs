using System;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class UserFunctionPrototype: FunctionBase
    {
        public UserFunctionPrototype(string name, VarType returnType, VarType[] argTypes) : base(name,  returnType, argTypes)
        {
        }

        private FunctionBase _function;
        public void SetActual(FunctionBase function, Interval interval)
        {
            _function = function;

            if (ReturnType != function.ReturnType)
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