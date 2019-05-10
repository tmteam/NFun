using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;

namespace NFun.SyntaxParsing
{
    public class HmAlgorithmAdapter
    {
        public HmAlgorithmAdapter()
        {
            EnterVisitor = new EnterHmVisitor();
            ExitVisitor = new ExitHmVisitor();
        }
        public ISyntaxNodeVisitor<VisitorResult> EnterVisitor { get; } 
        public ISyntaxNodeVisitor<bool> ExitVisitor { get; }

        public bool Apply(ISyntaxNode tree)
        {
            return tree.ComeOver(EnterVisitor, ExitVisitor);
        }
        
    }
}