using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class ExitVisitorBase: ISyntaxNodeVisitor<bool>
    {
        protected int CurrentChildNumber = -1;
        protected ISyntaxNode Parent = null;
        public void SetChildrenNumber(ISyntaxNode parent, int num)
        {
            Parent = parent;
            CurrentChildNumber = num;
        }

        public virtual bool Visit(ArrowAnonymCallSyntaxNode arrowAnonymFunNode) => true;
        public virtual bool Visit(ArraySyntaxNode node) => true;
        public virtual bool Visit(EquationSyntaxNode node) => true;
        public virtual bool Visit(FunCallSyntaxNode node) => true;
        public virtual bool Visit(ResultFunCallSyntaxNode node) => true;
        public virtual bool Visit(MetaInfoSyntaxNode metaInfoNode)=> true;
        public virtual bool Visit(SuperAnonymCallSyntaxNode arrowAnonymFunNode) => true;

        public virtual bool Visit(IfThenElseSyntaxNode node) => true;
        public virtual  bool Visit(IfCaseSyntaxNode node) => true;
        public virtual bool Visit(ListOfExpressionsSyntaxNode node) => true;
        public virtual bool Visit(ConstantSyntaxNode node) => true;
        public virtual bool Visit(GenericIntSyntaxNode node) => true;
        public virtual bool Visit(SyntaxTree node) => true;
        public virtual bool Visit(TypedVarDefSyntaxNode node) => true;
        public virtual bool Visit(UserFunctionDefenitionSyntaxNode node) => true;
        public virtual bool Visit(VarDefenitionSyntaxNode node) => true;
        public virtual bool Visit(VariableSyntaxNode node) => true;
        
    }
}