using NFun.Parsing;

namespace NFun.SyntaxParsing.Visitors
{
    public static class SyntaxDFS
    {
        public static bool ComeOver(this ISyntaxNode root, ISyntaxNodeVisitor<VisitorResult> enterVisitor,
            ISyntaxNodeVisitor<bool> exitVisitor)
        {
            var enterResult = root.Visit(enterVisitor);
            if (enterResult == VisitorResult.Failed)
                return false;
            if (enterResult == VisitorResult.Skip)
                return true;
            
            foreach (var child in root.Children)
                if (!child.ComeOver(enterVisitor, exitVisitor))
                    return false;

            return root.Visit(exitVisitor);
        }
        
        public static bool ComeOver(this ISyntaxNode root, ISyntaxNodeVisitor<VisitorResult> enterVisitor)
        {
            var enterResult = root.Visit(enterVisitor);
            if (enterResult == VisitorResult.Failed)
                return false;
            if (enterResult == VisitorResult.Skip)
                return true;
            
            foreach (var child in root.Children)
                if (!child.ComeOver(enterVisitor))
                    return false;

            return true;
        }
    }

    
}