using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;

namespace NFun.SyntaxParsing
{
    class ExitHmVisitor: ISyntaxNodeVisitor<bool>
    {
        public bool Visit(ArraySyntaxNode node)=> false;
        public bool Visit(UserFunctionDefenitionSyntaxNode node)=> false;
        public bool Visit(ProcArrayInit node)=> false;
       
        public bool Visit(AnonymCallSyntaxNode node) => false;
        public bool Visit(EquationSyntaxNode node)=> true;
        public bool Visit(FunCallSyntaxNode node)=> true;
        public bool Visit(IfThenElseSyntaxNode node)=> true;
        public bool Visit(IfThenSyntaxNode node)=> true;
        public bool Visit(ListOfExpressionsSyntaxNode node)=> true;
        public bool Visit(NumberSyntaxNode node)=> true;
        public bool Visit(SyntaxTree node)=> true;
        public bool Visit(TextSyntaxNode node)=> true;
        public bool Visit(TypedVarDefSyntaxNode node)=> true;
        public bool Visit(VarDefenitionSyntaxNode node)=> true;
        public bool Visit(VariableSyntaxNode node)=> true;

    }
}