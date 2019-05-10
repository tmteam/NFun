using NFun.Parsing;

namespace NFun.SyntaxParsing.Visitors
{
    public class SetNodeNumberVisitor: ISyntaxNodeVisitor<VisitorResult>
    {
        public SetNodeNumberVisitor(int startNum = 0)
        {
            LastNum = startNum;
        }
        public int LastNum { get; private set; }

        private VisitorResult VisitBase(ISyntaxNode node)
        {
            node.NodeNumber = LastNum++;
            return VisitorResult.Continue;
        }

        public VisitorResult Visit(AnonymCallSyntaxNode node) => VisitBase(node);

        public VisitorResult Visit(ArraySyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(EquationSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(FunCallSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(IfThenElseSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(IfThenSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(ListOfExpressionsSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(NumberSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(ProcArrayInit node)=> VisitBase(node);

        public VisitorResult Visit(SyntaxTree node)=> VisitBase(node);

        public VisitorResult Visit(TextSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(TypedVarDefSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(VarDefenitionSyntaxNode node)=> VisitBase(node);

        public VisitorResult Visit(VariableSyntaxNode node)=> VisitBase(node);
    }
}