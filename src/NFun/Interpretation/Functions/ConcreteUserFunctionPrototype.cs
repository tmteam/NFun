using System;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun.Interpretation.Functions {

internal class ConcreteUserFunctionPrototype : FunctionWithManyArguments {
    public ConcreteUserFunctionPrototype(string name, FunnyType returnType, FunnyType[] argTypes) : base(
        name,
        returnType, argTypes) { }

    private ConcreteUserFunction _function;

    public void SetActual(ConcreteUserFunction function) {
        if (ReturnType != function.ReturnType)
            //todo - assert
            throw new NFunImpossibleException($"'{function.ReturnType}' is not supported as return type of {function.Name}()");
        _function = function;
    }

    public override object Calc(object[] args) {
        if (_function == null)
            throw new InvalidOperationException("Function prototype cannot be called");
        return _function.Calc(args);
    }
}

}