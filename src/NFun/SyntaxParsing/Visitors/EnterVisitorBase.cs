using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class EnterVisitorBase: ISyntaxNodeVisitor<VisitorEnterResult>
    {
        protected virtual VisitorEnterResult DefaultVisit(ISyntaxNode node)
            => VisitorEnterResult.Continue;

        public void SetChildrenNumber(ISyntaxNode parent, int num) { }
        public virtual VisitorEnterResult Visit(AnonymCallSyntaxNode anonymFunNode) => DefaultVisit(anonymFunNode);
        public virtual VisitorEnterResult Visit(ArraySyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(EquationSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(FunCallSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(IfThenElseSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(IfCaseSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(ListOfExpressionsSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(ConstantSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(ProcArrayInit node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(SyntaxTree node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(TypedVarDefSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(UserFunctionDefenitionSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(VarDefenitionSyntaxNode node)=> DefaultVisit(node);
        public virtual VisitorEnterResult Visit(VariableSyntaxNode node)=> DefaultVisit(node);
    }
}