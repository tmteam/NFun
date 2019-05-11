namespace NFun.HindleyMilner.Tyso
{
    public class StrictNodeBehaviour : INodeBehavior
    {
        private readonly FType _type;
        public StrictNodeBehaviour(FType type)
        {
            _type = type;
        }

        public FType MakeType(int maxTypeDepth) => _type;

        public INodeBehavior SetLimit(FType newLimit)
        {
            if(newLimit.Name.Id== NTypeName.AnyId)
                return this;
            if (_type.IsPrimitive)
            {
                //ERROR: If limit is more strict than concrete
                if (newLimit.CanBeConvertedTo(_type))
                    return null;
            }
            else
            {
                if (newLimit.Arguments.Length != _type.Arguments.Length)
                    return null;
                for (int i = 0; i < newLimit.Arguments.Length; i++)
                {
                    var limArg = newLimit.Arguments[i];
                    if (limArg.Behavior is GenericTypeBehaviour)
                    {
                        if (!_type.Arguments[i].SetGeneric(limArg))
                            return null;
                    }
                    else
                    {
                        if (!_type.Arguments[i].SetLimit(limArg.MakeType(SolvingNode.MaxTypeDepth)))
                            return null;
                    }
                }
            }
            return this;
        }

        public INodeBehavior SetStrict(FType newType)
        {
            if (!newType.Name.Equals(_type.Name))
                return null;
            if (_type.Arguments.Length != newType.Arguments.Length)
                return null;
            for (int i = 0; i < _type.Arguments.Length ; i++) {
                if (!_type.Arguments[i].SetStrict(newType.Arguments[i].MakeType(SolvingNode.MaxTypeDepth)))
                    return null;
            }
            return this;
        }

        public INodeBehavior SetLca(SolvingNode[] otherNodes)
        {
            foreach (var solvingNode in otherNodes)
            {
                if (!solvingNode.SetLimit(_type))
                    return null;
            }
            return this;
        }

        public INodeBehavior SetReference(SolvingNode otherNode)
        {
            if (!otherNode.SetStrict(_type))
                return null;
            return new ReferenceBehaviour(otherNode);
        }
        public INodeBehavior SetGeneric(SolvingNode otherGeneric)
            =>  otherGeneric.SetStrict(_type)? this: null;

        public string ToSmartString(int maxDepth = 10)
        {
            return $"{_type}";
        }

        public INodeBehavior Optimize(out bool o)
        {
            o = false;
            return this;
        }

        public FitResults Fits(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return FitResults.Converable;
            if (!candidateType.Name.Equals(_type.Name))
                return FitResults.Not;
            if (candidateType.IsPrimitive)
                return FitResults.Strict;
            if (_type.Arguments.Length!= candidateType.Arguments.Length)
                return FitResults.Not;

            FitResults minimalResult = FitResults.Strict;
            for (int i = 0; i < _type.Arguments.Length ; i++)
            {
                var res =_type.Arguments[i]
                    .Fits(candidateType.Arguments[i].MakeType(maxDepth-1), maxDepth-1);
                if (res < minimalResult)
                    minimalResult = res;
            }
            return minimalResult;
        }

        public override string ToString()
        {
            return $"{_type}";
        }
    }
}