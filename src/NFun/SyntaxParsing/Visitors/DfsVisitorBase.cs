using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public abstract class DfsVisitorBase : ISyntaxNodeVisitor<VisitorEnterResult>, ISyntaxNodeVisitor<bool>
    {
        protected virtual VisitorEnterResult DefaultVisitEnter(ISyntaxNode node)
            => VisitorEnterResult.Continue;

        protected virtual bool DefaultVisitExit(ISyntaxNode node) 
            => true;
            
        #region Enter visits
        protected virtual VisitorEnterResult EnterVisit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode) => DefaultVisitEnter(arrowAnonymFunNode);
        protected virtual VisitorEnterResult EnterVisit(ArraySyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(EquationSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(FunCallSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(ResultFunCallSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(MetaInfoSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(SuperAnonymFunctionSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(IfThenElseSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(IfCaseSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(ListOfExpressionsSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(ConstantSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(GenericIntSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(SyntaxTree node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(TypedVarDefSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(UserFunctionDefenitionSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(VarDefenitionSyntaxNode node) => DefaultVisitEnter(node);
        protected virtual VisitorEnterResult EnterVisit(VariableSyntaxNode node) => DefaultVisitEnter(node);
        #endregion

        #region Enter visit mapping
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(ArrowAnonymFunctionSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(ArraySyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(EquationSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(FunCallSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(IfThenElseSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(IfCaseSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(ListOfExpressionsSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(ConstantSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(GenericIntSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(SyntaxTree node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(TypedVarDefSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(UserFunctionDefenitionSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(VarDefenitionSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(VariableSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(ResultFunCallSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(MetaInfoSyntaxNode node)
            => EnterVisit(node);
        VisitorEnterResult ISyntaxNodeVisitor<VisitorEnterResult>.Visit(SuperAnonymFunctionSyntaxNode node)
            => EnterVisit(node);
        #endregion

        #region Exit visits
        protected virtual bool ExitVisit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode) => DefaultVisitExit(arrowAnonymFunNode);
        protected virtual bool ExitVisit(ArraySyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(EquationSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(FunCallSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(ResultFunCallSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(MetaInfoSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(SuperAnonymFunctionSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(IfThenElseSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(IfCaseSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(ListOfExpressionsSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(ConstantSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(GenericIntSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(SyntaxTree node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(TypedVarDefSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(UserFunctionDefenitionSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(VarDefenitionSyntaxNode node) => DefaultVisitExit(node);
        protected virtual bool ExitVisit(VariableSyntaxNode node) => DefaultVisitExit(node);
        #endregion

        #region Exit visit mapping
        bool ISyntaxNodeVisitor<bool>.Visit(ArraySyntaxNode node) => ExitVisit(node);

        bool ISyntaxNodeVisitor<bool>.Visit(EquationSyntaxNode node) => ExitVisit(node);

        bool ISyntaxNodeVisitor<bool>.Visit(FunCallSyntaxNode node) => ExitVisit(node);

        bool ISyntaxNodeVisitor<bool>.Visit(IfThenElseSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(IfCaseSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(ListOfExpressionsSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(ConstantSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(GenericIntSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(SyntaxTree node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(TypedVarDefSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(UserFunctionDefenitionSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(VarDefenitionSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(VariableSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(ResultFunCallSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(MetaInfoSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(SuperAnonymFunctionSyntaxNode node) => ExitVisit(node);
        bool ISyntaxNodeVisitor<bool>.Visit(ArrowAnonymFunctionSyntaxNode node) => ExitVisit(node);
        
        #endregion
    }
}