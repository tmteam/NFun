using NFun.Parsing;

namespace NFun.SyntaxParsing
{
    public static class SyntaxDFS
    {
        public static void ComeOverTheTree(this ISyntaxNode root, ISyntaxNodeVisitor<bool> enterVisitor,
            ISyntaxNodeVisitor<bool> exitVisitor)
        {
            if (!root.Visit(enterVisitor))
                return;
            
            foreach (var child in root.Children)
                child.ComeOverTheTree(enterVisitor, exitVisitor);

            root.Visit(exitVisitor);
        }
    }

    
}