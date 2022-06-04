using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors; 

public static class SyntaxTreeDeepFieldSearch {
    public static Queue<ISyntaxNode> FindNodePath(this ISyntaxNode root, object nodeId) {
        var stack = new Queue<ISyntaxNode>();
        if (root == null)
            return stack;
        if (FindNodePathReq(root, nodeId, stack))
            stack.Enqueue(root);
        return stack;
    }

    private static bool FindNodePathReq(ISyntaxNode root, object nodeId, Queue<ISyntaxNode> path) {
        if (nodeId is int num)
        {
            if (root.OrderNumber == num)
                return true;
        }
        else if (nodeId is string named)
        {
            if ((root is TypedVarDefSyntaxNode v && v.Id == named) 
                || (root is VarDefinitionSyntaxNode vd && vd.Id == named)
                || (root is EquationSyntaxNode en && en.Id == named))
            {
                return true;
            }
        }
        else
        {
            return false;
        }

        foreach (var child in root.Children)
        {
            if (FindNodePathReq(child,nodeId, path))
            {
                path.Enqueue(child);
                return true;
            }
        }

        return false;
    }

    public static bool ComeOver(
        this ISyntaxNode root, ISyntaxNodeVisitor<VisitorEnterResult> enterVisitor,
        ISyntaxNodeVisitor<bool> exitVisitor) {
        var enterResult = root.Accept(enterVisitor);

        if (enterResult == VisitorEnterResult.Failed)
            return false;
        if (enterResult == VisitorEnterResult.Skip)
            return true;

        foreach (var child in root.Children)
        {
            if (!child.ComeOver(enterVisitor, exitVisitor))
                return false;
        }

        return root.Accept(exitVisitor);
    }

    public static bool ComeOver(this ISyntaxNode root, ISyntaxNodeVisitor<VisitorEnterResult> enterVisitor) {
        var enterResult = root.Accept(enterVisitor);
        if (enterResult == VisitorEnterResult.Failed)
            return false;
        if (enterResult == VisitorEnterResult.Skip)
            return true;

        foreach (var child in root.Children)
            if (!child.ComeOver(enterVisitor))
                return false;

        return true;
    }
}