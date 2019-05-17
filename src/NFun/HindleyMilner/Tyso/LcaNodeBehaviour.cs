using System;
using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class LcaNodeBehaviour : INodeBehavior
    {
        public SolvingNode[] OtherNodes { get; }

        public LcaNodeBehaviour(SolvingNode[] otherNodes)
        {
            OtherNodes = otherNodes;
        }
        public FType MakeType(int maxTypeDepth)
        {
            if(maxTypeDepth<0)
                throw new StackOverflowException("Make type depth SO");
            
            if (OtherNodes.Length == 1)
                return OtherNodes.First().MakeType(maxTypeDepth-1);
            
            //todo find last common ancestor
            return OtherNodes.Select(o => o.MakeType(maxTypeDepth-1)).OrderBy(a => a.Name.Start).First();
        }

        public INodeBehavior SetLimit(FType newLimit)
        {
            foreach (var solvingNode in OtherNodes)
            {
                if (!solvingNode.SetLimit(newLimit))
                    return null;
            }
            return this;
        }

        public INodeBehavior SetStrict(FType newType)
        {
            foreach (var solvingNode in OtherNodes)
            {
                if (!solvingNode.SetLimit(newType))
                    return null;
            }
            return new StrictNodeBehaviour(newType);        
        }

        public INodeBehavior SetLca(SolvingNode[] otherNodes)
        {
            return new LcaNodeBehaviour(otherNodes.Concat(OtherNodes).Distinct().ToArray());
        }

        public INodeBehavior SetReference(SolvingNode otherNode)
        {
            if (!otherNode.SetLca(OtherNodes))
                return null;
            return new ReferenceBehaviour(otherNode);
        }

        public INodeBehavior SetGeneric(SolvingNode otherGeneric) 
            => !otherGeneric.SetLca(OtherNodes)
                ? null : this;

        public string ToSmartString(int maxDepth = 10)
        {
            if (maxDepth < 0)
                return "...";
            return "Parent or  (" + string.Join(",", OtherNodes.Select(s=>s.ToSmartString(maxDepth-1)))+")";

        }

        public INodeBehavior Optimize(out bool changed)
        {
            changed = false;
            int i = 0;
            if (OtherNodes.Length == 0)
            {
                changed = true;
                return new GenericTypeBehaviour();
            }

            if (OtherNodes.Length == 1)
            {
                changed = true;
                if (OtherNodes[0].Behavior == this)
                {
                    //Generic is up there
                    return new GenericTypeBehaviour();
                }
                return new ReferenceBehaviour(OtherNodes[0]);
            }
                
            foreach (var node in OtherNodes)
            {
                
                if (node.Behavior == this)
                {
                    changed = true;
                    var newNodes = OtherNodes.Where(o => o.Behavior != this).ToArray();
                    return new LcaNodeBehaviour(newNodes);
                }
                if (node.Behavior is GenericTypeBehaviour g)
                {
                    changed = true;
                    var nodes = OtherNodes.Where(o => o != node).ToArray();
                    node.SetLca(nodes);
                    return new ReferenceBehaviour(node);
                }

                if (node.Behavior is ReferenceBehaviour r)
                {
                    OtherNodes[i] = r.Node;
                    var unitedNodes = OtherNodes
                        .Where(o=>o.Behavior!= this) 
                        .Where(o=>o!= node)
                        .Distinct();
                    
                    changed = true;
                    return new LcaNodeBehaviour(unitedNodes.ToArray());
                }
                
                if (node.Behavior is LcaNodeBehaviour l)
                {
                    //a = b = c 
                    var unitedNodes 
                        = OtherNodes
                            .Concat(l.OtherNodes)
                            .Where(o=>o.Behavior!= this) 
                            .Where(o=>o!= node)
                            .Distinct();
                    changed = true;
                    return new LcaNodeBehaviour(unitedNodes.ToArray());
                }
                i++;
            }
            return this;
        }

        public ConvertResults CanBeConvertedTo(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return ConvertResults.Converable;
            return OtherNodes.Min(o => o.CanBeConvertedTo(candidateType, maxDepth - 1));
        }
    }
}