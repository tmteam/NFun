using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors; 

public abstract class ExitVisitorBase : ISyntaxNodeVisitor<bool> {
    public virtual bool Visit(AnonymFunctionSyntaxNode node) => true;
    public virtual bool Visit(ArraySyntaxNode node) => true;
    public virtual bool Visit(EquationSyntaxNode node) => true;
    public virtual bool Visit(FunCallSyntaxNode node) => true;
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
}