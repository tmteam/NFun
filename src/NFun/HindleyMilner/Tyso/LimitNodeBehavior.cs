namespace NFun.HindleyMilner.Tyso
{
    public class LimitNodeBehavior: INodeBehavior
    {
        public FType Limit { get; private set; }

        public LimitNodeBehavior(FType limit)
        {
            Limit = limit;
        } 
        public FType MakeType(int maxTypeDepth) => Limit;
        public INodeBehavior SetLimit(SolvingNode newNodeLimit)
        {
            var newLimit = newNodeLimit.MakeType(SolvingNode.MaxTypeDepth);
            if (Limit.IsPrimitive)
            {
                if (!Limit.CanBeConvertedTo(newLimit))
                    Limit = newLimit;
                return this;
            }
            
            //_limit: any; newLimit: real
            if (!Limit.CanBeConvertedTo(newLimit))
                Limit = newLimit;
            return this;
        }

        private bool isVisited = false;
        public INodeBehavior SetLimit(FType newLimit)
        {
            isVisited = true;
            //_limit: any; newLimit: real
            if (!Limit.CanBeConvertedTo(newLimit))
                Limit = newLimit;
            return this;
        }

        public INodeBehavior SetStrict(FType newType)
        {
            //Limitation conflict
            //like: _limit: real; type: any
            if (Limit.IsPrimitive)
            {
                if (Limit.CanBeConvertedTo(newType))
                    return null;
            }

            return new StrictNodeBehaviour(newType);
            
        }

        public INodeBehavior SetLca(SolvingNode[] otherNodes)
        {
            
            foreach (var solvingNode in otherNodes)
            {
                if (!solvingNode.SetLimit(Limit))
                    return null;
            }

            //Resolving circular dependencies
            if (isVisited)
            {
                isVisited = false;
                return this;
            }

            return new LcaNodeBehaviour(otherNodes);
        }

        public INodeBehavior SetReference(SolvingNode otherNode) {
            otherNode.SetLimit(Limit);
            return new ReferenceBehaviour(otherNode);
        }

        public INodeBehavior SetGeneric(SolvingNode otherGeneric)
            => !otherGeneric.SetLimit(Limit)
                ? null : this;

        public string ToSmartString(int maxDepth = 10)
        {
            if (maxDepth < 0)
                return "...";

            return $"Child or {Limit.ToSmartString(maxDepth - 1)}";
        }

        public INodeBehavior Optimize(out bool changed)
        {
            changed = false;
            if (Limit.IsPrimitive)
            {
                return this;
            }

            foreach (var limitArgument in Limit.Arguments)
            {
                if (!limitArgument.Optimize(out _))
                    return null;
            }
            return this;
        }

        public FitResults Fits(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return FitResults.Converable;
            if (candidateType.Equals(Limit))
                return FitResults.Strict;
            if(this.Limit.Name.Equals(NTypeName.SomeInteger) && candidateType.Name.Equals(NTypeName.Int32))
                return FitResults.Strict;
            
            if (candidateType.CanBeConvertedTo(Limit))
                //if (candidateType.IsPrimitive)
                //    return FitResults.Strict;
                //else    
                    return FitResults.Converable;
            return FitResults.Not;
        }

        public override string ToString() => ToSmartString();
    }
}