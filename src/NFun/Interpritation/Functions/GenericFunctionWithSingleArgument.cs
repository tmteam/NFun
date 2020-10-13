using System;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class GenericFunctionWithSingleArgument : GenericFunctionBase
    {
        protected GenericFunctionWithSingleArgument(string name, VarType returnType, params VarType[] argTypes) : base(
            name, returnType, argTypes)
        {
        }

        protected GenericFunctionWithSingleArgument(string name, GenericConstrains[] constrainses,
            VarType returnType, params VarType[] argTypes) : base(name, constrainses, returnType, argTypes)
        {
        }

        protected GenericFunctionWithSingleArgument(string name, GenericConstrains constrains,
            VarType returnType, params VarType[] argTypes) : base(name, constrains, returnType, argTypes)
        {
        }

        protected abstract object Calc(object a);

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap) =>
            new ConcreteImplementationWithSingleArg(
                calc: Calc,
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
                argType: SubstitudeArgTypes(concreteTypesMap)[0]);

        private class ConcreteImplementationWithSingleArg : FunctionWithSingleArg
        {
            private readonly Func<object, object> _calc;

            public ConcreteImplementationWithSingleArg(
                Func<object, object> calc,
                string name, VarType returnType, VarType argType) : base(name, returnType, argType)
            {
                _calc = calc;
            }

            public override object Calc(object a) => _calc(a);
        }
    }
}