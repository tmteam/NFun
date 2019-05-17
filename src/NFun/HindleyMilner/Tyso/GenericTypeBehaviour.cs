using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class GenericTypeBehaviour: INodeBehavior
    {
        public FType MakeType(int maxTypeDepth) {
            return new FType(NTypeName.Generic(0));
        }

        public INodeBehavior SetLimit(FType newLimit) => new LimitNodeBehavior(newLimit);

        public INodeBehavior SetStrict(FType newType) => new StrictNodeBehaviour(newType);

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
        public INodeBehavior Optimize(out bool changed)
        {
            changed = false;
            return this;
        }

        public ConvertResults CanBeConvertedTo(FType candidateType, int maxDepth) 
            => ConvertResults.Converable;

        public override string ToString() => "T";
    }
}