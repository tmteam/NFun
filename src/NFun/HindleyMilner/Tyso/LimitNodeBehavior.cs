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
            if (otherNode.Behavior == this)
                return this;
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

        public ConvertResults CanBeConvertedTo(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return ConvertResults.Converable;
            if (candidateType.Equals(Limit))
                return ConvertResults.Strict;
            //special case: Int is most expected type for someInteger
            if(Limit.Name.Equals(NTypeName.SomeInteger) && candidateType.Name.Equals(NTypeName.Int32))
                return ConvertResults.Strict;
            
            //We can reduce current limit to candidateType
            if (candidateType.CanBeSafelyConvertedTo(Limit))
                return ConvertResults.Candidate;
            
            if (Limit.CanBeSafelyConvertedTo(candidateType))
                return ConvertResults.Converable;
            

            return ConvertResults.Not;
        }

        public override string ToString() => ToSmartString();
    }
}