namespace NFun.TypeInference.Solving
{
    public class LimitNodeBehavior: INodeBehavior
    {
        private TiType _limit { get; set; }

        public LimitNodeBehavior(TiType limit)
        {
            _limit = limit;
        } 
        public TiType MakeType(int maxTypeDepth) => _limit;

        private bool _isVisited = false;
        public INodeBehavior SetLimit(TiType newLimit)
        {
            _isVisited = true;
            //_limit: any; newLimit: real
            if (!_limit.CanBeSafelyConvertedTo(newLimit))
                _limit = newLimit;
            return this;
        }

        public INodeBehavior SetStrict(TiType newType)
        {
            if (!newType.CanBeSafelyConvertedTo(_limit))
            {
                //Limitation conflict
                //like: _limit: real; type: any
                return null;
            }
            return new StrictNodeBehaviour(newType);
        }

        public INodeBehavior SetLca(SolvingNode[] otherNodes)
        {
            
            foreach (var solvingNode in otherNodes)
            {
                if (!solvingNode.SetLimit(_limit))
                    return null;
            }

            //Resolving circular dependencies
            if (_isVisited)
            {
                _isVisited = false;
                return this;
            }

            return new LcaNodeBehaviour(otherNodes);
        }

        public INodeBehavior SetReference(SolvingNode otherNode)
        {
            var actual = otherNode.GetActualNode();
            
            if (actual.Behavior == this)
                return this;
            actual.SetLimit(_limit);
            return new ReferenceBehaviour(actual);
        }

        public INodeBehavior SetGeneric(SolvingNode otherGeneric)
            => !otherGeneric.SetLimit(_limit)
                ? null : this;

        public string ToSmartString(int maxDepth = 10)
        {
            if (maxDepth < 0)
                return "...";

            return $"Child or {_limit.ToSmartString(maxDepth - 1)}";
        }

        public INodeBehavior Optimize(out bool hasChanged)
        {
            hasChanged = false;
            if (_limit.IsPrimitive)
                return this;

            foreach (var limitArgument in _limit.Arguments)
            {
                if (!limitArgument.Optimize(out _))
                    return null;
            }
            return this;
        }
        
        public  FitResult CanBeConvertedFrom(TiType from, int maxDepth)
        {
            var res =  TiType.CanBeConverted(@from, _limit, maxDepth);
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
            if (candidateType.Equals(_limit))
                return FitResult.Candidate(0);
            //special case: Int is most expected type for someInteger
            if(_limit.Name.Equals(TiTypeName.SomeInteger) && candidateType.Name.Equals(TiTypeName.Int32))
                return FitResult.Candidate(0);
            
            //We can reduce current limit to candidateType
            if (candidateType.CanBeSafelyConvertedTo(_limit))
                return new FitResult(FitType.Candidate, candidateType.GetParentalDistanceTo(_limit));
            
            if (_limit.CanBeSafelyConvertedTo(candidateType))
                return new FitResult(FitType.Convertable, _limit.GetParentalDistanceTo(candidateType));

            return FitResult.Not;
        }

        public override string ToString() => ToSmartString();
    }
}