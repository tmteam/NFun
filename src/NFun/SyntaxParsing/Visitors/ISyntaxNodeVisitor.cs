using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public interface ISyntaxNodeVisitor<T>
    {
        T Visit(AnonymCallSyntaxNode node);
        T Visit(ArraySyntaxNode node);
        T Visit(EquationSyntaxNode node);
        T Visit(FunCallSyntaxNode node);
        T Visit(IfThenElseSyntaxNode node);
        T Visit(IfCaseSyntaxNode node);
        T Visit(ListOfExpressionsSyntaxNode node);
        T Visit(NumberSyntaxNode node);
        T Visit(ProcArrayInit node);
        T Visit(SyntaxTree node);
        T Visit(TextSyntaxNode node);
        T Visit(TypedVarDefSyntaxNode node);
        T Visit(UserFunctionDefenitionSyntaxNode node);
        T Visit(VarDefenitionSyntaxNode node);
        T Visit(VariableSyntaxNode node);
    }
}