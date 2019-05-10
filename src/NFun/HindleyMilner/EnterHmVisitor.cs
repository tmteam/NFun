using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;

namespace NFun.SyntaxParsing
{
    class EnterHmVisitor: ISyntaxNodeVisitor<VisitorResult>
    {
        public VisitorResult Visit(ArraySyntaxNode node)=> VisitorResult.Skip;
        public VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)=> VisitorResult.Skip;
        public VisitorResult Visit(ProcArrayInit node)=> VisitorResult.Skip;
        
        public VisitorResult Visit(AnonymCallSyntaxNode node) => VisitorResult.Continue;
        public VisitorResult Visit(EquationSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(FunCallSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(IfThenElseSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(IfThenSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(ListOfExpressionsSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(NumberSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(SyntaxTree node)=> VisitorResult.Continue;
        public VisitorResult Visit(TextSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(TypedVarDefSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(VarDefenitionSyntaxNode node)=> VisitorResult.Continue;
        public VisitorResult Visit(VariableSyntaxNode node)=> VisitorResult.Continue;

    }
}