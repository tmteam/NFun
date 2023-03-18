using System;
using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;

namespace NFun.SyntaxParsing;

public static class SyntaxNodeExtensions {
    public static string ToShortText(this ISyntaxNode node) =>
        node.Accept(new ShortDescriptionVisitor());

    public static Queue<ISyntaxNode> FindSyntaxNodePath(this ISyntaxNode root, object nodeId) {
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
                || (root is EquationSyntaxNode en && en.Id == named)
                || (root is NamedIdSyntaxNode n && n.Id == named))
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
            if (FindNodePathReq(child, nodeId, path))
            {
                path.Enqueue(child);
                return true;
            }
        }

        return false;
    }

    public static ISyntaxNode Dfs(this ISyntaxNode root, Func<ISyntaxNode, bool> condition) {
        if (condition(root))
            return root;

        foreach (var child in root.Children)
        {
            var result = Dfs(child, condition);
            if (result != null)
                return result;
        }

        return null;
    }

    public static bool ComeOver(
        this ISyntaxNode root,
        ISyntaxNodeVisitor<DfsEnterResult> enterVisitor,
        ISyntaxNodeVisitor<bool> exitVisitor) {
        var enterResult = root.Accept(enterVisitor);

        if (enterResult == DfsEnterResult.Stop)
            return false;
        if (enterResult == DfsEnterResult.Skip)
            return true;

        foreach (var child in root.Children)
        {
            if (!child.ComeOver(enterVisitor, exitVisitor))
                return false;
        }

        return root.Accept(exitVisitor);
    }

    public static bool ComeOver(this ISyntaxNode root, ISyntaxNodeVisitor<DfsEnterResult> enterVisitor) {
        var enterResult = root.Accept(enterVisitor);
        if (enterResult == DfsEnterResult.Stop)
            return false;
        if (enterResult == DfsEnterResult.Skip)
            return true;

        foreach (var child in root.Children)
        {
            if (!child.ComeOver(enterVisitor))
                return false;
        }

        return true;
    }

    public static ISyntaxNode Find(
        this ISyntaxNode root,
        Func<ISyntaxNode, bool> predicate,
        Func<ISyntaxNode, bool> enterCondition) {
        ISyntaxNode result = null;
        root.ComeOver(visiting => {
            if (!enterCondition(visiting))
                return DfsEnterResult.Skip;
            if (!predicate(visiting))
                return DfsEnterResult.Continue;

            result = visiting;
            return DfsEnterResult.Stop;
        });
        return result;
    }

    public static bool ComeOver(this ISyntaxNode root, Func<ISyntaxNode, DfsEnterResult> runner) {
        var result = runner(root);

        return result switch {
            DfsEnterResult.Stop => false,
            DfsEnterResult.Skip => true,
            DfsEnterResult.Continue => root.Children.All(child => ComeOver(child, runner)),
            _ => throw new NotSupportedException($"Value {result} is not supported")
        };
    }
}
