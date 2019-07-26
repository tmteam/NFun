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

        private bool isVisited = false;
        public INodeBehavior SetLimit(FType newLimit)
        {
            isVisited = true;
            //_limit: any; newLimit: real
            if (!Limit.CanBeSafelyConvertedTo(newLimit))
                Limit = newLimit;
            return this;
        }

        public INodeBehavior SetStrict(FType newType)
        {
            //Limitation conflict
            //like: _limit: real; type: any
            if (Limit.IsPrimitive)
            {
                //Downcast
                if (!Limit.Name.Equals(newType.Name)
                    &&  Limit.CanBeSafelyConvertedTo(newType))
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

        public INodeBehavior SetReference(SolvingNode otherNode)
        {
            var actual = otherNode.GetActualNode();
            
            if (actual.Behavior == this)
                return this;
            actual.SetLimit(Limit);
            return new ReferenceBehaviour(actual);
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

        public FitResult CanBeConvertedTo(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return new FitResult(FitType.Converable,0);
            if (candidateType.Equals(Limit))
                return FitResult.Strict;
            //special case: Int is most expected type for someInteger
            if(Limit.Name.Equals(HmTypeName.SomeInteger) && candidateType.Name.Equals(HmTypeName.Int32))
                return FitResult.Strict;
            
            //We can reduce current limit to candidateType
            if (candidateType.CanBeSafelyConvertedTo(Limit))
                return new FitResult(FitType.Candidate, candidateType.GetParentalDistanceTo(Limit));
            
            if (Limit.CanBeSafelyConvertedTo(candidateType))
                return new FitResult(FitType.Converable, Limit.GetParentalDistanceTo(candidateType));

            return FitResult.Not;
        }

        public override string ToString() => ToSmartString();
    }
}