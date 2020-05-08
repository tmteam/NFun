using System;
using NFun.Exceptions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class ConcreteHiOrderFunctionWithSyntaxNode : FunctionBase
    {
        private readonly IExpressionNode _source;

        public static FunctionBase Create(IExpressionNode funSource)
        {
            var signature = funSource.Type.FunTypeSpecification;
            if(signature==null)
                throw new ImpossibleException("[vaa 13] Functional type specification is missed");
            return new ConcreteHiOrderFunctionWithSyntaxNode(
                funSource,
                signature.Output,
                signature.Inputs);
        }
        private ConcreteHiOrderFunctionWithSyntaxNode(IExpressionNode source, VarType returnType, VarType[] argTypes) 
            : base(source.ToString(), returnType, argTypes)
        {
            _source = source;
        }



        public override object Calc(object[] args) => ((FunctionBase)_source.Calc()).Calc(args);
    }
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