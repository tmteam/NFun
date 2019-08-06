using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class GenericTypeBehaviour: INodeBehavior
    {
        private static int lastId = 0;
        private readonly int id;
        public GenericTypeBehaviour()
        {
            id = lastId;
            lastId++;
        }
        public TiType MakeType(int maxTypeDepth) => new GenericType(0);

        public INodeBehavior SetLimit(TiType newLimit) => new LimitNodeBehavior(newLimit);

        public INodeBehavior SetStrict(TiType newType) => new StrictNodeBehaviour(newType);

        public INodeBehavior SetLca(SolvingNode[] otherNodes)
        {
            var filtered = otherNodes.Where(o => o.Behavior != this).ToArray();
            if (!filtered.Any())
                return this;
            var lca = new LcaNodeBehaviour(new SolvingNode[0]);
            return lca.SetLca(filtered);
        }

        public INodeBehavior SetReference(SolvingNode otherNode)
        {
            if (otherNode.Behavior == this)
                return this;
            if (otherNode.Behavior is ReferenceBehaviour r)
            {
                if (r.Node.Behavior == this)
                    return this;
            }
            return new ReferenceBehaviour(otherNode);
        }

        public INodeBehavior SetGeneric(SolvingNode otherGeneric)
        {
            if (otherGeneric.Behavior == this)
                return this;
            return new ReferenceBehaviour(otherGeneric);
        }

        public string ToSmartString(int maxDepth = 10) => ToString();
        public INodeBehavior Optimize(out bool hasChanged)
        {
            hasChanged = false;
            return this;
        }
        public FitResult CanBeConvertedFrom(TiType from, int maxDepth) 
            => new FitResult(FitType.Convertable, 0);

        public FitResult CanBeConvertedTo(TiType candidateType, int maxDepth) 
            => new FitResult(FitType.Convertable, 0);

        public override string ToString() => $"T{id}";
    }
}