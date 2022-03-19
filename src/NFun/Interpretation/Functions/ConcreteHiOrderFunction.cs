using NFun.Exceptions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions {

internal class ConcreteHiOrderFunctionWithSyntaxNode : FunctionWithManyArguments {
    private readonly IExpressionNode _source;

    public static FunctionWithManyArguments Create(IExpressionNode funSource) {
        var signature = funSource.Type.FunTypeSpecification;
        signature.IfNullThrow("[vaa 13] Functional type specification is missed");
        return new ConcreteHiOrderFunctionWithSyntaxNode(
            funSource,
            signature.Output,
            signature.Inputs);
    }

    private ConcreteHiOrderFunctionWithSyntaxNode(
        IExpressionNode source, FunnyType returnType,
        FunnyType[] argTypes)
        : base(source.ToString(), returnType, argTypes) {
        _source = source;
    }


    public override object Calc(object[] args) => ((IConcreteFunction)_source.Calc()).Calc(args);
}

internal class ConcreteHiOrderFunction : FunctionWithManyArguments {
    private readonly VariableSource _source;

    internal static FunctionWithManyArguments Create(VariableSource varSource) {
        return new ConcreteHiOrderFunction(
            varSource,
            varSource.Type.FunTypeSpecification.Output,
            varSource.Type.FunTypeSpecification.Inputs);
    }

    private ConcreteHiOrderFunction(VariableSource source, FunnyType returnType, FunnyType[] argTypes) : base(
        source.Name, returnType, argTypes) {
        _source = source;
    }


    public override object Calc(object[] args) => ((IConcreteFunction)_source.FunnyValue).Calc(args);
}

}