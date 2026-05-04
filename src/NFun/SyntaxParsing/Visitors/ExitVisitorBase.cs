using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors;

public abstract class ExitVisitorBase : ISyntaxNodeVisitor<bool> {
    public virtual bool Visit(AnonymFunctionSyntaxNode node) => true;
    public virtual bool Visit(ArraySyntaxNode node) => true;
    public virtual bool Visit(EquationSyntaxNode node) => true;
    public virtual bool Visit(FunCallSyntaxNode node) => true;
    public virtual bool Visit(ComparisonChainSyntaxNode node) => true;
    public virtual bool Visit(ResultFunCallSyntaxNode node) => true;
    public virtual bool Visit(SuperAnonymFunctionSyntaxNode node) => true;
    public virtual bool Visit(StructFieldAccessSyntaxNode node) => true;
    public virtual bool Visit(StructInitSyntaxNode node) => true;
    public virtual bool Visit(DefaultValueSyntaxNode node) => true;
    public virtual bool Visit(IfThenElseSyntaxNode node) => true;
    public virtual bool Visit(IfCaseSyntaxNode node) => true;
    public virtual bool Visit(ListOfExpressionsSyntaxNode node) => true;
    public virtual bool Visit(ConstantSyntaxNode node) => true;
    public virtual bool Visit(IpAddressConstantSyntaxNode node) => true;
    public virtual bool Visit(GenericIntSyntaxNode node) => true;
    public virtual bool Visit(SyntaxTree node) => true;
    public virtual bool Visit(TypedVarDefSyntaxNode node) => true;
    public virtual bool Visit(UserFunctionDefinitionSyntaxNode node) => true;
    public virtual bool Visit(VarDefinitionSyntaxNode node) => true;
    public virtual bool Visit(NamedIdSyntaxNode node) => true;
    public virtual bool Visit(BinOperatorSyntaxNode node) => true;
    public virtual bool Visit(UnaryOperatorSyntaxNode node) => true;
    public virtual bool Visit(TypeDeclarationSyntaxNode node) => true;
    public virtual bool Visit(NamedTypeConstructorSyntaxNode node) => true;
    public virtual bool Visit(TryCatchSyntaxNode node) => true;
    public virtual bool Visit(BlockSyntaxNode node) => true;
    public virtual bool Visit(ReturnSyntaxNode node) => true;
    public virtual bool Visit(ForSyntaxNode node) => true;
    public virtual bool Visit(WhileSyntaxNode node) => true;
    public virtual bool Visit(WhenSyntaxNode node) => true;
    public virtual bool Visit(WhenArmSyntaxNode node) => true;
    public virtual bool Visit(BreakSyntaxNode node) => true;
    public virtual bool Visit(ContinueSyntaxNode node) => true;
    public virtual bool Visit(PrintSyntaxNode node) => true;
    public virtual bool Visit(TryBlockSyntaxNode node) => true;
    public virtual bool Visit(IfBlockSyntaxNode node) => true;
    public virtual bool Visit(FieldAssignmentSyntaxNode node) => true;
}
