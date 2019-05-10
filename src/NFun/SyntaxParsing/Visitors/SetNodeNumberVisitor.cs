using NFun.Parsing;

namespace NFun.SyntaxParsing.Visitors
{
    public class MarkIdVisitor: ISyntaxNodeVisitor<bool>
    {
        public int Id { get; }
        public bool Visit(AnonymCallSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(ArraySyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(EquationSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(FunCallSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(IfThenElseSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(IfThenSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(ListOfExpressionsSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(NumberSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(ProcArrayInit node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(SyntaxTree node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(TextSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(TypedVarDefSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(UserFunctionDefenitionSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(VarDefenitionSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }

        public bool Visit(VariableSyntaxNode node)
        {
            throw new System.NotImplementedException();
        }
    }
}