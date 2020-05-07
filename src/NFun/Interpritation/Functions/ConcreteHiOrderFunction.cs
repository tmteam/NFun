using System;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class ConcreteHiOrderFunction: FunctionBase
    {
        private readonly VariableSource _source;

        public static FunctionBase Create(VariableSource varSource)
        {
            return new ConcreteHiOrderFunction(
                varSource, 
                varSource.Type.FunTypeSpecification.Output, 
                varSource.Type.FunTypeSpecification.Inputs);
        }
        private ConcreteHiOrderFunction(VariableSource source, VarType returnType, VarType[] argTypes) : base(source.Name,  returnType, argTypes)
        {
            _source = source;
        }

       

        public override object Calc(object[] args) => ((FunctionBase) _source.Value).Calc(args);
    }
}