using System;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class GenericFunctionWithTwoArguments : GenericFunctionBase
    {
        protected GenericFunctionWithTwoArguments(string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {
        }

        protected GenericFunctionWithTwoArguments(string name, GenericConstrains[] constrainses, VarType returnType, params VarType[] argTypes) : base(name, constrainses, returnType, argTypes)
        {
        }

        protected GenericFunctionWithTwoArguments(string name, GenericConstrains constrains, VarType returnType, params VarType[] argTypes) : base(name, constrains, returnType, argTypes)
        {
        }

        protected override object Calc(object[] args) => Calc(args[0], args[1]);

        protected abstract object Calc(object a, object b);
        
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap) =>
            new ConcreteImplementationWithTwoArgs(
                calc: Calc,
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
                argTypes:   SubstitudeArgTypes(concreteTypesMap));

        private class ConcreteImplementationWithTwoArgs : FunctionWithTwoArgs
        {
            private readonly Func<object, object, object> _calc;

            public ConcreteImplementationWithTwoArgs( 
                Func<object,object,object> calc,
                string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
            {
                _calc = calc;
            }

            public override object Calc(object a, object b) => _calc(a, b);
        }
    }
}