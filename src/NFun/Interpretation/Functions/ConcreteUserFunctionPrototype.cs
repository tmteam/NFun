using System;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Functions {

internal class ConcreteUserFunctionPrototype : FunctionWithManyArguments {
    public ConcreteUserFunctionPrototype(string name, FunnyType returnType, FunnyType[] argTypes) : base(
        name,
        returnType, argTypes) { }

    private ConcreteUserFunction _function;

    public void SetActual(ConcreteUserFunction function, Interval interval) {
        _function = function;

        if (ReturnType != function.ReturnType)
            throw ErrorFactory.InvalidOutputType(function, interval);
    }

    public override object Calc(object[] args) {
        if (_function == null)
            throw new InvalidOperationException("Function prototype cannot be called");
        return _function.Calc(args);
    }
}

}