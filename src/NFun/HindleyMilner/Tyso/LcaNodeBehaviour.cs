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

            var concretes = OtherNodes.Select(o => o.MakeType(maxTypeDepth - 1)).ToArray();
            var left = concretes.Where(c=>c.Name.Start>=0).Min(c => c.Name.Start);
            var right = concretes.Where(c=>c.Name.Finish>=0).Max(c=>c.Name.Finish);
            
            //Search for GeneralParent

            var parentName = concretes[0].Name;

            while (true)
            {
                if (parentName.Start <= left && parentName.Finish >= right)
                    return new FType(parentName);
                
                if(parentName.Parent==null)
                    return FType.Any;
                parentName = parentName.Parent;
            }
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

        public FitResult CanBeConvertedFrom(FType from, int maxDepth)
        {
            if (from.IsPrimitiveGeneric)
                return new FitResult(FitType.Converable,0);

            FType sameTypeForEveryone = null;
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
                return FType.CanBeConverted(
                    from: sameTypeForEveryone, 
                    to:from, 
                    maxDepth: maxDepth-1);
            }
            
            var res =  FType.CanBeConverted(from, MakeType(maxDepth-1), maxDepth-1);
            if (res.Type != FitType.Strict)
                return res;
            else
            {
                return FitResult.Candidate(res.Distance);
            }
        }
        public FitResult CanBeConvertedTo(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return new FitResult(FitType.Converable,0);

            var concreteType = MakeType(maxDepth-1);
            
            var res =  FType.CanBeConverted(concreteType, candidateType,maxDepth-1);
            if(res.Type== FitType.Strict)
                return FitResult.Candidate(res.Distance);
            return res;
            
            var worstResult = new FitResult(FitType.Strict, 0);
            
            foreach (var child in OtherNodes)
            {
                
                var childResult =  child.CanBeConvertedTo(candidateType, maxDepth - 1);
                if (worstResult.IsBetterThan(childResult))
                    worstResult = childResult;
            }
            return worstResult;
        }
    }
}