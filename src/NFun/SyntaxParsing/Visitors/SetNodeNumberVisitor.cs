using NFun.Parsing;

namespace NFun.SyntaxParsing.Visitors
{
    
    public class SetNodeNumberVisitor: EnterVisitorBase
    {
        public SetNodeNumberVisitor(int startNum = 0)
        {
            LastNum = startNum;
        }
        public int LastNum { get; private set; }
        protected override VisitorResult DefaultVisit(ISyntaxNode node)
        {
            node.NodeNumber = LastNum++;
            return VisitorResult.Continue;
        }
    }
}