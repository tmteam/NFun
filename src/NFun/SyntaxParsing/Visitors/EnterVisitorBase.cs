using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class EnterVisitorBase: ISyntaxNodeVisitor<VisitorEnterResult>
    {
        protected virtual VisitorEnterResult DefaultVisitEnter(ISyntaxNode node)
            => VisitorEnterResult.Continue;
        public virtual VisitorEnterResult Visit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode) => DefaultVisitEnter(arrowAnonymFunNode);
        public virtual VisitorEnterResult Visit(ArraySyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(EquationSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(FunCallSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(ResultFunCallSyntaxNode node) => DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(SuperAnonymFunctionSyntaxNode node) => DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(StructFieldAccessSyntaxNode node) => DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(StructInitSyntaxNode node) => DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(IfThenElseSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(IfCaseSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(ListOfExpressionsSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(ConstantSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(GenericIntSyntaxNode node) => DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(SyntaxTree node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(TypedVarDefSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(UserFunctionDefinitionSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(VarDefinitionSyntaxNode node)=> DefaultVisitEnter(node);
        public virtual VisitorEnterResult Visit(NamedIdSyntaxNode node)=> DefaultVisitEnter(node);
    }
}