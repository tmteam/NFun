using System;
using NFun.ParseErrors;

namespace NFun.HindleyMilner.Tyso
{
    public class ReferenceBehaviour: INodeBehavior
    {
        public SolvingNode Node { get; }

        public ReferenceBehaviour(SolvingNode node)
        {
            if (node.Behavior is ReferenceBehaviour r)
            {
                
            }
            Node = node;
        }

        public FType MakeType(int maxTypeDepth)
        {
            if(maxTypeDepth<-1)
                throw FunParseException.ErrorStubToDo("Recursive defenition");
            return Node.Behavior.MakeType(maxTypeDepth - 1);
        }

        public INodeBehavior SetLimit(FType newLimit) 
            => Node.SetLimit(newLimit) ? this : null;

        public INodeBehavior SetStrict(FType newType)
            => Node.SetStrict(newType) ? this : null;


        public INodeBehavior SetLca(SolvingNode[] otherNodes)
            => Node.SetLca(otherNodes) ? this : null;


        public INodeBehavior SetReference(SolvingNode otherNode)
        {
            if (otherNode.Behavior is ReferenceBehaviour r)
            {
                if (r == this)
                    return this;
                return Node.SetEqualTo(r.Node) ? this : null;
            }
            return Node.SetEqualTo(otherNode) ? this : null;
        }

        public INodeBehavior SetGeneric(SolvingNode otherGeneric)
            =>  Node.SetGeneric(otherGeneric)? this: null;

        public string ToSmartString(int maxDepth = 10)
        {
            if (maxDepth < 0)
                return "...";
            return " => " + Node.ToSmartString(maxDepth - 1);
        }

        public INodeBehavior Optimize(out bool o)
        {
            if (Node.Behavior is ReferenceBehaviour r) {
                
                o = true;
                if (r == this)
                    return new GenericTypeBehaviour();
                return new ReferenceBehaviour(r.Node);
            }
            o = false;
            return this;
        }

        public ConvertResults CanBeConvertedTo(FType candidateType, int maxDepth) 
            => Node.CanBeConvertedTo(candidateType, maxDepth-1);

        // public override string ToString() => ToSmartString(SolvingNode.MaxTypeDepth);
    }
}