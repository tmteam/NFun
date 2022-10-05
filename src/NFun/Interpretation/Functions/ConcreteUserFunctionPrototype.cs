using System;
using NFun.Exceptions;
using NFun.Interpretation.Nodes;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

internal class ConcreteUserFunctionPrototype : FunctionWithManyArguments {
    public ConcreteUserFunctionPrototype(string name, FunnyType returnType, FunnyType[] argTypes) : base(
        name,
        returnType, argTypes) { }

    private ConcreteUserFunction _function;

    public void SetActual(ConcreteUserFunction function) {
        if(ReturnType != function.ReturnType)
            AssertChecks.Panic($"'{function.ReturnType}' is not supported as return type of {function.Name}()");
        _function = function;
    }

    public override object Calc(object[] args) {
        if (_function == null)
            throw new InvalidOperationException("Function prototype cannot be called");
        return _function.Calc(args);
    }

    public override IConcreteFunction Clone(ICloneContext context)
    {
        var userFunction = context.GetUserFunctionClone(this);
        if (userFunction != null)
            return userFunction;
        var clone = new ConcreteUserFunctionPrototype(Name, ReturnType, ArgTypes);
        context.AddUserFunctionClone(this, clone);
        var originClone = _function.Clone(context);
        clone.SetActual((ConcreteUserFunction)originClone);
        return clone;
    }
    
    public override string ToString() => $"FUN-user-prototype {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";

}