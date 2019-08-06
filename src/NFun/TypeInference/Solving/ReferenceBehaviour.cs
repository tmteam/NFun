using NFun.ParseErrors;

namespace NFun.TypeInference.Solving
{
    public class ReferenceBehaviour: INodeBehavior
    {
        public SolvingNode Node { get; }

        public ReferenceBehaviour(SolvingNode node)
        {
            Node = node;
        }

        public TiType MakeType(int maxTypeDepth)
        {
            if(maxTypeDepth<-1)
                throw FunParseException.ErrorStubToDo("Recursive defenition");
            return Node.Behavior.MakeType(maxTypeDepth - 1);
        }

        public INodeBehavior SetLimit(TiType newLimit) 
            => Node.SetLimit(newLimit) ? this : null;

        public INodeBehavior SetStrict(TiType newType)
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

        public INodeBehavior Optimize(out bool hasChanged)
        {
            if (Node.Behavior is ReferenceBehaviour r) {
                hasChanged = true;
                if (r == this)
                    return new GenericTypeBehaviour();
                return new ReferenceBehaviour(r.Node);
            }

            if (!Node.Optimize(out hasChanged)) {
                hasChanged = false;
            }

            return this;
        }

        public FitResult CanBeConvertedTo(TiType candidateType, int maxDepth) 
            => Node.CanBeConvertedTo(candidateType, maxDepth-1);
        public FitResult CanBeConvertedFrom(TiType candidateType, int maxDepth) 
            => Node.CanBeConvertedFrom(candidateType, maxDepth-1);

        // public override string ToString() => ToSmartString(SolvingNode.MaxTypeDepth);
    }
}