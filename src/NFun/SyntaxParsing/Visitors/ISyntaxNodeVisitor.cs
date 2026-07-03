using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors;

public interface ISyntaxNodeVisitor<out T> {
    T Visit(AnonymFunctionSyntaxNode node);
    T Visit(ArraySyntaxNode node);
    T Visit(EquationSyntaxNode node);
    T Visit(FunCallSyntaxNode node);
    T Visit(ComparisonChainSyntaxNode node);
    T Visit(IfThenElseSyntaxNode node);
    T Visit(IfCaseSyntaxNode node);
    T Visit(ListOfExpressionsSyntaxNode node);
    T Visit(ConstantSyntaxNode node);
    T Visit(GenericIntSyntaxNode node);
    T Visit(IpAddressConstantSyntaxNode node);
    T Visit(SyntaxTree node);
    T Visit(TypedVarDefSyntaxNode node);
    T Visit(UserFunctionDefinitionSyntaxNode node);
    T Visit(VarDefinitionSyntaxNode node);
    T Visit(NamedIdSyntaxNode node);
    T Visit(ResultFunCallSyntaxNode node);
    T Visit(SuperAnonymFunctionSyntaxNode node);
    T Visit(StructFieldAccessSyntaxNode node);
    T Visit(StructInitSyntaxNode node);
    T Visit(DefaultValueSyntaxNode node);
    T Visit(BinOperatorSyntaxNode node);
    T Visit(UnaryOperatorSyntaxNode node);
    T Visit(TypeDeclarationSyntaxNode node);
    T Visit(NamedTypeConstructorSyntaxNode node);
    T Visit(TryCatchSyntaxNode node);
    T Visit(BlockSyntaxNode node);
    T Visit(ReturnSyntaxNode node);
    T Visit(ForSyntaxNode node);
    T Visit(WhileSyntaxNode node);
    T Visit(WhenSyntaxNode node);
    T Visit(WhenArmSyntaxNode node);
    T Visit(BreakSyntaxNode node);
    T Visit(ContinueSyntaxNode node);
    T Visit(PrintSyntaxNode node);
    T Visit(TryBlockSyntaxNode node);
    T Visit(IfBlockSyntaxNode node);
    T Visit(FieldAssignmentSyntaxNode node);
    T Visit(IndexedAssignmentSyntaxNode node);
}
