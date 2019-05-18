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
                if (!_type.CanBeSafelyConvertedTo(newLimit))
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
            for (int i = 0; i < _type.Arguments.Length ; i++)
            {
                var thisArgument = _type.Arguments[i];
                var newArgument = newType.Arguments[i];
                if (newArgument.Behavior is GenericTypeBehaviour)
                {
                    if (!newArgument.SetStrict(thisArgument.MakeType()))
                        return null;
                }   
                else if (!thisArgument.SetStrict(newArgument.MakeType()))
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
            if (otherNode.Behavior == this)
                return this;
            if (!otherNode.SetStrict(_type))
                return null;
            return this;
        }
        public INodeBehavior SetGeneric(SolvingNode otherGeneric)
            =>  otherGeneric.SetStrict(_type)? this: null;

        public string ToSmartString(int maxDepth = 10) => $"{_type}";

        public INodeBehavior Optimize(out bool o)
        {
            o = false;
            return this;
        }

        public ConvertResults CanBeConvertedTo(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return ConvertResults.Converable;
            if (candidateType.IsPrimitive)
            {
                if (candidateType.Name.Equals(_type.Name))
                    return ConvertResults.Strict;
                //special case: Int is most expected type for someInteger
                if(_type.Name.Id== NTypeName.Int32Id && candidateType.Name.Id== NTypeName.SomeIntegerId)
                    return ConvertResults.Candidate;
                if (_type.CanBeSafelyConvertedTo(candidateType)) 
                    return ConvertResults.Converable;
                else
                    return ConvertResults.Not;
            }
            
            if (!candidateType.Name.Equals(_type.Name))
                return ConvertResults.Not;

            
            if (_type.Arguments.Length!= candidateType.Arguments.Length)
                return ConvertResults.Not;

            ConvertResults minimalResult = ConvertResults.Strict;
            for (int i = 0; i < _type.Arguments.Length ; i++)
            {
                var res =_type.Arguments[i]
                    .CanBeConvertedTo(candidateType.Arguments[i].MakeType(maxDepth-1), maxDepth-1);
                if (res < minimalResult)
                    minimalResult = res;
            }
            return minimalResult;
        }

        public override string ToString() => $"{_type}";
    }
}