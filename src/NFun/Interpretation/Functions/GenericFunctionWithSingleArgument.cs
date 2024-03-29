using System;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

public abstract class GenericFunctionWithSingleArgument : GenericFunctionBase {
    protected GenericFunctionWithSingleArgument(string name, FunnyType returnType, params FunnyType[] argTypes) : base(
        name, returnType, argTypes) { }

    protected abstract object Calc(object a);

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) =>
        new ConcreteImplementationWithSingleArg(
            calc: Calc,
            name: Name,
            returnType: FunnyType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
            argType: SubstitudeArgTypes(concreteTypesMap)[0]);

    private class ConcreteImplementationWithSingleArg : FunctionWithSingleArg {
        private readonly Func<object, object> _calc;

        public ConcreteImplementationWithSingleArg(
            Func<object, object> calc,
            string name, FunnyType returnType, FunnyType argType) : base(name, returnType, argType) =>
            _calc = calc;

        public override object Calc(object a) => _calc(a);


        public override string ToString()
            => "FUN-Concrete-impl-single " + TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes);
    }
}