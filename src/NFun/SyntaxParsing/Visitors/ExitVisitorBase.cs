using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class ExitVisitorBase: ISyntaxNodeVisitor<bool>
    {
        public virtual bool Visit(AnonymCallSyntaxNode anonymFunNode) => true;
        public virtual bool Visit(ArraySyntaxNode node) => true;
        public virtual bool Visit(EquationSyntaxNode node) => true;
        public virtual bool Visit(FunCallSyntaxNode node) => true;
        public virtual bool Visit(IfThenElseSyntaxNode node) => true;
        public virtual  bool Visit(IfCaseSyntaxNode node) => true;
        public virtual bool Visit(ListOfExpressionsSyntaxNode node) => true;
        public virtual bool Visit(ConstantSyntaxNode node) => true;
        public virtual bool Visit(ProcArrayInit node) => true;
        public virtual bool Visit(SyntaxTree node) => true;
        public virtual bool Visit(TypedVarDefSyntaxNode node) => true;
        public virtual bool Visit(UserFunctionDefenitionSyntaxNode node) => true;
        public virtual bool Visit(VarDefenitionSyntaxNode node) => true;
        public virtual bool Visit(VariableSyntaxNode node) => true;
    }
}