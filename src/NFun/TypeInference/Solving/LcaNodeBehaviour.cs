using System;
using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class LcaNodeBehaviour : INodeBehavior
    {
        public SolvingNode[] OtherNodes { get; }

        public LcaNodeBehaviour(SolvingNode[] otherNodes)
        {
            OtherNodes = otherNodes;
        }
        public TiType MakeType(int maxTypeDepth)
        {
            if(maxTypeDepth<0)
                throw new StackOverflowException("Make type depth SO");
            
            if (OtherNodes.Length == 1)
                return OtherNodes.First().MakeType(maxTypeDepth-1);

            var concretes = OtherNodes.Select(o => o.MakeType(maxTypeDepth - 1)).ToArray();
            return TiType.GetLca(concretes);
        }

        public INodeBehavior SetLimit(TiType newLimit)
        {
            foreach (var solvingNode in OtherNodes)
            {
                if (!solvingNode.SetLimit(newLimit))
                    return null;
            }
            return this;
        }

        public INodeBehavior SetStrict(TiType newType)
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

        public INodeBehavior Optimize(out bool hasChanged)
        {
            hasChanged = false;
            int i = 0;
            if (OtherNodes.Length == 0)
            {
                hasChanged = true;
                return new GenericTypeBehaviour();
            }

            if (OtherNodes.Length == 1)
            {
                hasChanged = true;
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
                    hasChanged = true;
                    var newNodes = OtherNodes.Where(o => o.Behavior != this).ToArray();
                    return new LcaNodeBehaviour(newNodes);
                }
            
                if (node.Behavior is StrictNodeBehaviour s)
                {
                    
                    var type = s.MakeType();
                    //Lca for complex type is equality
                    //todo How to unite complex type arguments?
                    if (!type.IsPrimitive)
                    {
                        var nodes = OtherNodes.Where(o => o != node).ToArray();
                        foreach (var solvingNode in nodes)
                        {
                            if (!solvingNode.SetEqualTo(node)) 
                                return null;
                        }

                        return new StrictNodeBehaviour(type);
                    }
                }

                if (node.Behavior is GenericTypeBehaviour g)
                {
                    hasChanged = true;
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
                    
                    hasChanged = true;
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
                    hasChanged = true;
                    return new LcaNodeBehaviour(unitedNodes.ToArray());
                }
                i++;
            }
            return this;
        }
        public FitResult CanBeConvertedFrom(TiType from, int maxDepth)
        {
            if (from.IsPrimitiveGeneric)
                return new FitResult(FitType.Convertable,0);

            TiType sameTypeForEveryone = null;
            bool hasSameType = true;
            foreach (var otherNode in OtherNodes)
            {
                if (sameTypeForEveryone == null)
                {
                    sameTypeForEveryone = otherNode.MakeType();
                }
                else
                {
                    var concrete = otherNode.MakeType();
                    if(!concrete.Equals(sameTypeForEveryone))
                    {
                        hasSameType = false;
                        break;
                    }
                }
            }
            if(hasSameType)
            {
                return TiType.CanBeConverted(
                    @from: sameTypeForEveryone, 
                    to:from, 
                    maxDepth: maxDepth-1);
            }
            
            var res =  TiType.CanBeConverted(from, MakeType(maxDepth-1), maxDepth-1);
            if (res.Type != FitType.Strict)
                return res;
            else
            {
                return FitResult.Candidate(res.Distance);
            }
        }
        public FitResult CanBeConvertedTo(TiType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return new FitResult(FitType.Convertable,0);

            var concreteType = MakeType(maxDepth-1);
            
            var res =  TiType.CanBeConverted(concreteType, candidateType,maxDepth-1);
            if(res.Type== FitType.Strict)
                return FitResult.Candidate(res.Distance);
            return res;
        }
    }
}