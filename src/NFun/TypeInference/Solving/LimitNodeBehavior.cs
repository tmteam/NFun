namespace NFun.TypeInference.Solving
{
    public class LimitNodeBehavior: INodeBehavior
    {
        public TiType Limit { get; private set; }

        public LimitNodeBehavior(TiType limit)
        {
            Limit = limit;
        } 
        public TiType MakeType(int maxTypeDepth) => Limit;

        private bool isVisited = false;
        public INodeBehavior SetLimit(TiType newLimit)
        {
            isVisited = true;
            //_limit: any; newLimit: real
            if (!Limit.CanBeSafelyConvertedTo(newLimit))
                Limit = newLimit;
            return this;
        }

        public INodeBehavior SetStrict(TiType newType)
        {
            //Limitation conflict
            //like: _limit: real; type: any
            if (Limit.IsPrimitive && !Limit.Name.Equals(newType.Name))
            {
                //Downcast
                if (!newType.CanBeSafelyConvertedTo(Limit))
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
        
        public  FitResult CanBeConvertedFrom(TiType from, int maxDepth)
        {
            var res =  TiType.CanBeConverted(@from, Limit, maxDepth);
            if (res.Type == FitType.Strict)
            {
                return FitResult.Candidate(res.Distance);
            }
            else
            {
                return res;
            }
        }

        public FitResult CanBeConvertedTo(TiType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return new FitResult(FitType.Convertable,0);
            if (candidateType.Equals(Limit))
                return FitResult.Candidate(0);
            //special case: Int is most expected type for someInteger
            if(Limit.Name.Equals(TiTypeName.SomeInteger) && candidateType.Name.Equals(TiTypeName.Int32))
                return FitResult.Candidate(0);
            
            //We can reduce current limit to candidateType
            if (candidateType.CanBeSafelyConvertedTo(Limit))
                return new FitResult(FitType.Candidate, candidateType.GetParentalDistanceTo(Limit));
            
            if (Limit.CanBeSafelyConvertedTo(candidateType))
                return new FitResult(FitType.Convertable, Limit.GetParentalDistanceTo(candidateType));

            return FitResult.Not;
        }

        public override string ToString() => ToSmartString();
    }
}