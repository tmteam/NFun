using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors;

public abstract class EnterVisitorBase : ISyntaxNodeVisitor<DfsEnterResult> {
    protected virtual DfsEnterResult DefaultVisitEnter(ISyntaxNode node) =>
        DfsEnterResult.Continue;

    public virtual DfsEnterResult Visit(AnonymFunctionSyntaxNode node) =>
        DefaultVisitEnter(node);

    public virtual DfsEnterResult Visit(ArraySyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(EquationSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(FunCallSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(ComparisonChainSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(ResultFunCallSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(SuperAnonymFunctionSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(StructFieldAccessSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(StructInitSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(DefaultValueSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(IfThenElseSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(IfCaseSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(ListOfExpressionsSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(ConstantSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(IpAddressConstantSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(GenericIntSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(SyntaxTree node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(TypedVarDefSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(UserFunctionDefinitionSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(VarDefinitionSyntaxNode node) => DefaultVisitEnter(node);
    public virtual DfsEnterResult Visit(NamedIdSyntaxNode node) => DefaultVisitEnter(node);
}
