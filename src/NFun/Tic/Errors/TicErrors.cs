using System;
using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors
{
    public static class TicErrors
    {
        private static bool TryFindSyntaxNode(TicNode ancestor, TicNode descendant, out int syntaxNode)
        {
            syntaxNode = -1;
            if (descendant.Type == TicNodeType.SyntaxNode)
            {
                syntaxNode= (int)descendant.Name;
                return true;
            }
            if (ancestor.Type == TicNodeType.SyntaxNode)
            {
                syntaxNode = (int)ancestor.Name;
                return true;
            }

            return false;
        }
        private static bool TryFindNamedNode(TicNode ancestor, TicNode descendant, out object nodeName)
        {
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
        public static Exception IncompatibleNodes(TicNode ancestor, TicNode descendant)
        {
            if (TryFindSyntaxNode(ancestor, descendant, out int id))
                return new ImcompatibleAncestorSyntaxNodeException(id, ancestor.State, descendant.State);
            if (TryFindNamedNode(ancestor, descendant, out var named))
                return new ImcompatibleAncestorNamedNodeException(named.ToString(), ancestor.State, descendant.State);
            return new TicNoDetailsException();
        }

        public static Exception IncompatibleTypes(TicNode ancestor, TicNode descendant)
            => IncompatibleNodes(ancestor, descendant);
        public static Exception CanntoBecomeFunction(TicNode ancestor, TicNode target)
            => IncompatibleNodes(ancestor, target);
        public static Exception CanntoBecomeArray(TicNode ancestor, TicNode target)
            => IncompatibleNodes(ancestor, target);
        public static Exception IncompatibleFunSignatures(TicNode ancestor, TicNode descendant)
            => IncompatibleNodes(ancestor, descendant);

        public static Exception InvalidFunctionalVarableSignature(TicNode targetNode)
        {
            return new TicNoDetailsException();
        }
        public static Exception CannotMerge(TicNode a, TicNode b)
            => IncompatibleNodes(a, b);
        public static Exception CannotMergeGroup(TicNode[] group, TicNode a, TicNode b)
            => IncompatibleNodes(a, b);
        public static Exception RecursiveTypeDefenition(TicNode[] group)
        {
            List<string> listOfNames = new List<string>();
            List<int> listOfNodes = new List<int>();

            foreach (var node in group)
            {
                if (node.Type == TicNodeType.Named) 
                    listOfNames.Add(node.Name.ToString());
                else if(node.Type== TicNodeType.SyntaxNode)
                    listOfNodes.Add((int)node.Name);
            }
            return new RecursiveTypeDefenitionException(listOfNames.ToArray(), listOfNodes.ToArray());
        }
        public static Exception CannotSetState(TicNode node, ITicNodeState b)
        {
            if(node.Type== TicNodeType.SyntaxNode)
                return new ImcompatibleAncestorSyntaxNodeException((int)node.Name, node.State, b);
            if (node.Type == TicNodeType.Named)
                return new ImcompatibleAncestorNamedNodeException(node.Name.ToString(), node.State, b);
            return new TicNoDetailsException();
        }
    }
}
