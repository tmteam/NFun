using System;
using System.Linq;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class GenericUserFunctionPrototype : GenericFunctionBase
    {
        private UserFunction _function;

        public GenericUserFunctionPrototype(
            string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {
            
        }
        public void SetActual(UserFunction function, Interval interval)
        {
            _function = function;
        }
        /*
        public override FunctionBase CreateConcreteOrNull(params VarType[] concreteArgTypes)
        {
            var solvingParams = new VarType[_maxGenericId+1];

            for (int i = 0; i < ArgTypes.Length; i++)
            {
                if (!VarType.TrySolveGenericTypes(solvingParams, ArgTypes[i], concreteArgTypes[i]))
                    return null;
            }

            return new ConcreteGenericFunction(
                calc: Calc, 
                name: Name,
                returnType:  VarType.SubstituteConcreteTypes(SpecifiedType, solvingParams), 
                argTypes: concreteArgTypes);
        }*/

        public override object Calc(object[] args) => _function.Calc(args);
    }
}