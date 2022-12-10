using NFun.Exceptions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

internal class ConcreteHiOrderFunctionWithSyntaxNode : FunctionWithManyArguments {
    private readonly IExpressionNode _source;

    public static FunctionWithManyArguments Create(IExpressionNode funSource) {
        var signature = funSource.Type.FunTypeSpecification;
        if(signature==null)
            AssertChecks.Panic("[vaa 13] Functional type specification is missed");
        return new ConcreteHiOrderFunctionWithSyntaxNode(
            funSource,
            signature.Output,
            signature.Inputs);
    }

    private ConcreteHiOrderFunctionWithSyntaxNode(
        IExpressionNode source, FunnyType returnType,
        FunnyType[] argTypes)
        : base(source.ToString(), returnType, argTypes) =>
        _source = source;

    public override object Calc(object[] args) => ((IConcreteFunction)_source.Calc()).Calc(args);
    
    /// <summary>
    /// Create deep copy of function body, that can be used in parallel
    /// </summary>
    public override IConcreteFunction Clone(ICloneContext context) 
        => new ConcreteHiOrderFunctionWithSyntaxNode(_source.Clone(context), ReturnType, ArgTypes);
    
    public override string ToString()
        => $"FUN-hi-order-syntax {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
}

internal class ConcreteHiOrderFunction : FunctionWithManyArguments {
    private readonly VariableSource _source;

    internal static FunctionWithManyArguments Create(VariableSource varSource) =>
        new ConcreteHiOrderFunction(
            varSource,
            varSource.Type.FunTypeSpecification.Output,
            varSource.Type.FunTypeSpecification.Inputs);

    private ConcreteHiOrderFunction(VariableSource source, FunnyType returnType, FunnyType[] argTypes) 
        : base(source.Name, returnType, argTypes) =>
        _source = source;

    public override object Calc(object[] args) => 
        ((IConcreteFunction)_source.FunnyValue).Calc(args);
    
    public override IConcreteFunction Clone(ICloneContext context) 
        => new ConcreteHiOrderFunction(context.GetVariableSourceClone(_source), ReturnType, ArgTypes);
    
    public override string ToString()
        => $"FUN-hi-order {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
}