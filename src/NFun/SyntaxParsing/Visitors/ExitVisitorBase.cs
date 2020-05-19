using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class ExitVisitorBase: ISyntaxNodeVisitor<bool>
    {
        protected int CurrentChildNumber = -1;
        
        protected  readonly Stack<ISyntaxNode> Route = new Stack<ISyntaxNode>();

        public void OnEnterNode(ISyntaxNode parent, int childNum)
        {
            Route.Push(parent);
            CurrentChildNumber = childNum;
        }

        public void OnExitNode() => Route.Pop();


        public virtual bool Visit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode) => true;
        public virtual bool Visit(ArraySyntaxNode node) => true;
        public virtual bool Visit(EquationSyntaxNode node) => true;
        public virtual bool Visit(FunCallSyntaxNode node) => true;
        public virtual bool Visit(ResultFunCallSyntaxNode node) => true;
        public virtual bool Visit(MetaInfoSyntaxNode metaInfoNode)=> true;
        public virtual bool Visit(SuperAnonymFunctionSyntaxNode arrowAnonymFunNode) => true;
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