using System;
using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors {

public static class TicErrors {
    private static bool TryFindSyntaxNode(TicNode ancestor, TicNode descendant, out int syntaxNode) {
        syntaxNode = -1;
        if (descendant.Type == TicNodeType.SyntaxNode)
        {
            syntaxNode = (int)descendant.Name;
            return true;
        }

        if (ancestor.Type == TicNodeType.SyntaxNode)
        {
            syntaxNode = (int)ancestor.Name;
            return true;
        }

        return false;
    }

    private static bool TryFindNamedNode(TicNode ancestor, TicNode descendant, out object nodeName) {
        nodeName = "";
        if (descendant.Type == TicNodeType.Named)
        {
            nodeName = descendant.Name;
            return true;
        }

        if (ancestor.Type == TicNodeType.SyntaxNode)
        {
            nodeName = ancestor.Name;
            return true;
        }

        return false;
    }

    public static Exception IncompatibleNodes(TicNode ancestor, TicNode descendant) {
        //if (TryFindSyntaxNode(ancestor, descendant, out int id))
        return new IncompatibleAncestorSyntaxNodeException(ancestor, descendant);
        //if (TryFindNamedNode(ancestor, descendant, out var named))
        // return new IncompatibleAncestorNamedNodeException(named.ToString(), ancestor.State, descendant.State);
        //return new TicNoDetailsException();
    }

    public static Exception IncompatibleTypes(TicNode ancestor, TicNode descendant)
        => IncompatibleNodes(ancestor, descendant);

    public static Exception CannotBecomeFunction(TicNode ancestor, TicNode target)
        => IncompatibleNodes(ancestor, target);

    public static Exception CannotBecomeArray(TicNode ancestor, TicNode target)
        => IncompatibleNodes(ancestor, target);

    public static Exception IncompatibleFunSignatures(TicNode ancestor, TicNode descendant)
        => IncompatibleNodes(ancestor, descendant);

    public static Exception InvalidFunctionalVarableSignature(TicNode funcNode) 
        => new TicInvalidFunctionalVarableSignature(funcNode, funcNode.State as StateFun);

    public static Exception CannotMerge(TicNode a, TicNode b)
        => IncompatibleNodes(a, b);

    public static Exception CannotMergeGroup(TicNode[] group, TicNode a, TicNode b)
        => IncompatibleNodes(a, b);

    public static Exception RecursiveTypeDefinition(TicNode[] group) => 
        new RecursiveTypeDefinitionException(group);

    public static Exception CannotSetState(TicNode node, ITicNodeState b) => new CannotSetStateSyntaxNodeException(node, b);
}

}