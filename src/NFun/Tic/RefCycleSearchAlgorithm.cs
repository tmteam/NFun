using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    class RefCycleSearchAlgorithm
    {
        private readonly int _nodeInListMark;

        private const int RefVisitingMark = 6782341;
        private const int RefVisitedMark = 672901236;

        public TicNode GetNonReferenceMergedOrNull(TicNode node)
        {
            if (node.VisitMark == RefVisitedMark)
                return null;
            
            _refRoute = new Stack<TicNode>();
            var nonReference = GetNonReferenceNodeOrNull(node);
            if (nonReference != null) 
                return nonReference;
            
            // ref cycle found!
            // the node becomes one non reference node with no constrains
            node.State = new ConstrainsState();
            foreach (var refNode in _refRoute)
            {
                if (refNode == node)
                    continue;
                if(refNode.VisitMark== RefVisitedMark)
                    continue;
                node.AddAncestors(refNode.Ancestors);
                refNode.ClearAncestors();
                refNode.VisitMark = RefVisitedMark;
                if (refNode.IsMemberOfAnything)
                    refNode.IsMemberOfAnything = false;
            }
            return node;
        }

        private Stack<TicNode> _refRoute = null;

        public RefCycleSearchAlgorithm(int nodeInListMark)
        {
            _nodeInListMark = nodeInListMark;
        }

        private TicNode GetNonReferenceNodeOrNull(TicNode node)
        {
            if (node.VisitMark == _nodeInListMark)
                return node;
            if (node.State is not StateRefTo refTo) 
                return node;
            if (node.VisitMark == RefVisitingMark)
                return null;
            node.VisitMark = RefVisitingMark;
            var res =  GetNonReferenceNodeOrNull(refTo.Node);
            if(res==null)
                _refRoute.Push(node);
            else
            {
                node.VisitMark = -1;
                //merge
                if (node.Ancestors.Any())
                {
                    res.AddAncestors(node.Ancestors);
                    node.ClearAncestors();
                }
            }
            return res;
        }

    }
}