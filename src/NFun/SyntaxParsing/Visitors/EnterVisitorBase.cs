using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class EnterVisitorBase: ISyntaxNodeVisitor<VisitorResult>
    {
        protected virtual VisitorResult DefaultVisit(ISyntaxNode node)
            => VisitorResult.Continue;

        public virtual VisitorResult Visit(AnonymCallSyntaxNode node) => DefaultVisit(node);
        public virtual VisitorResult Visit(ArraySyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(EquationSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(FunCallSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(IfThenElseSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(IfCaseSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(ListOfExpressionsSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(ConstantSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(ProcArrayInit node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(SyntaxTree node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(TypedVarDefSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(VarDefenitionSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorResult Visit(VariableSyntaxNode node)=> DefaultVisit(node);
    }
}