using System;
using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors
{
    public static class TicErrors
    {
        private static bool TryFindSyntaxNode(SolvingNode ancestor, SolvingNode descendant, out int syntaxNode)
        {
            syntaxNode = -1;
            if (descendant.Type == SolvingNodeType.SyntaxNode)
            {
                syntaxNode= int.Parse(descendant.Name);
                return true;
            }
            if (ancestor.Type == SolvingNodeType.SyntaxNode)
            {
                syntaxNode = int.Parse(ancestor.Name);
                return true;
            }

            return false;
        }
        private static bool TryFindNamedNode(SolvingNode ancestor, SolvingNode descendant, out string nodeName)
        {
            nodeName = "";
            if (descendant.Type == SolvingNodeType.Named)
            {
                nodeName = descendant.Name;
                return true;
            }
            if (ancestor.Type == SolvingNodeType.SyntaxNode)
            {
                nodeName = ancestor.Name;
                return true;
            }

            return false;
        }
        public static Exception IncompatibleNodes(SolvingNode ancestor, SolvingNode descendant)
        {
            if (TryFindSyntaxNode(ancestor, descendant, out int id))
                return new ImcompatibleAncestorSyntaxNodeException(id, ancestor.State, descendant.State);
            if (TryFindNamedNode(ancestor, descendant, out var named))
                return new ImcompatibleAncestorNamedNodeException(named, ancestor.State, descendant.State);
            return new TicNoDetailsException();
        }

        public static Exception IncompatibleTypes(SolvingNode ancestor, SolvingNode descendant)
            => IncompatibleNodes(ancestor, descendant);
        public static Exception CanntoBecomeFunction(SolvingNode ancestor, SolvingNode target)
            => IncompatibleNodes(ancestor, target);
        public static Exception CanntoBecomeArray(SolvingNode ancestor, SolvingNode target)
            => IncompatibleNodes(ancestor, target);
        public static Exception IncompatibleFunSignatures(SolvingNode ancestor, SolvingNode descendant)
            => IncompatibleNodes(ancestor, descendant);

        public static Exception CannotMerge(SolvingNode a, SolvingNode b)
            => IncompatibleNodes(a, b);
        public static Exception CannotMergeGroup(SolvingNode[] group, SolvingNode a, SolvingNode b)
            => IncompatibleNodes(a, b);
        public static Exception RecursiveTypeDefenition(SolvingNode[] group)
        {
            List<string> listOfNames = new List<string>();
            List<int> listOfNodes = new List<int>();

            foreach (var node in group)
            {
                if (node.Type == SolvingNodeType.Named) 
                    listOfNames.Add(node.Name);
                else if(node.Type== SolvingNodeType.SyntaxNode)
                    listOfNodes.Add(int.Parse(node.Name));
            }
            return new RecursiveTypeDefenitionException(listOfNames.ToArray(), listOfNodes.ToArray());
        }
        public static Exception CannotSetState(SolvingNode node, IState b)
        {
            if(node.Type== SolvingNodeType.SyntaxNode)
                return new ImcompatibleAncestorSyntaxNodeException(Int32.Parse(node.Name), node.State, b);
            if (node.Type == SolvingNodeType.Named)
                return new ImcompatibleAncestorNamedNodeException(node.Name, node.State, b);
            return new TicNoDetailsException();
        }
    }
}
