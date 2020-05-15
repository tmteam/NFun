using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public interface ISyntaxNodeVisitor<T>
    {
        void SetChildrenNumber(ISyntaxNode parent, int num);
        T Visit(ArrowAnonymCallSyntaxNode arrowAnonymFunNode);
        T Visit(ArraySyntaxNode node);
        T Visit(EquationSyntaxNode node);
        T Visit(FunCallSyntaxNode node);
        T Visit(IfThenElseSyntaxNode node);
        T Visit(IfCaseSyntaxNode node);
        T Visit(ListOfExpressionsSyntaxNode node);
        T Visit(ConstantSyntaxNode node);
        T Visit(GenericIntSyntaxNode node);
        T Visit(SyntaxTree node);
        T Visit(TypedVarDefSyntaxNode node);
        T Visit(UserFunctionDefenitionSyntaxNode node);
        T Visit(VarDefenitionSyntaxNode node);
        T Visit(VariableSyntaxNode node);
        T Visit(ResultFunCallSyntaxNode node);
        T Visit(MetaInfoSyntaxNode anonymFunNode);
        T Visit(SuperAnonymCallSyntaxNode arrowAnonymFunNode);
    }
}