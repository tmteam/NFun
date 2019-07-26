namespace NFun.HindleyMilner.Tyso
{
    public class StrictNodeBehaviour : INodeBehavior
    {
        private readonly FType _type;
        public StrictNodeBehaviour(FType type)
        {
            _type = type;
        }

        public FType MakeType(int maxTypeDepth = SolvingNode.MaxTypeDepth) => _type;

        public INodeBehavior SetLimit(FType newLimit)
        {
            if(newLimit.Name.Id== HmTypeName.AnyId)
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
                    var thisArg = _type.Arguments[i];
                    if (limArg.Behavior is GenericTypeBehaviour)
                    {
                        if (!thisArg.SetGeneric(limArg))
                            return null;
                    }
                    else if (_type.Name.Id == HmTypeName.Function.Id && i > 0)
                    {
                        //hi order function arguments got reversed rules

                        //firstly try to setStrict (for more concrete hi order function solving)
                        if (!thisArg.SetStrict(limArg.MakeType()))
                        {
                            //if it fails - then set limit
                            if (!limArg.SetLimit(thisArg.MakeType()))
                                return null;
                        }
                    }
                    else 
                    {
                        if (!thisArg.SetLimit(limArg.MakeType()))
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
                var thisArgument = _type.Arguments[i].GetActualNode();
                var newArgument = newType.Arguments[i].GetActualNode();
                if (!MergeArguments(newArgument, thisArgument)) 
                    return null;
            }
            return this;
        }

        private static bool MergeArguments(SolvingNode newArgument, SolvingNode thisArgument)
        {
            if (newArgument.Behavior is GenericTypeBehaviour)
            {
                if (thisArgument.Behavior is GenericTypeBehaviour)
                {
                    //both are generics
                    if (!thisArgument.SetEqualTo(newArgument))
                        return false;
                }
                else
                {
                    if (!newArgument.SetStrict(thisArgument.MakeType()))
                        return false;
                }
            }
            else if (thisArgument.Behavior is GenericTypeBehaviour)
            {
                if (!thisArgument.SetStrict(newArgument.MakeType()))
                    return false;
            }
            else if (!thisArgument.SetStrict(newArgument.MakeType()))
                return false;

            return true;
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

        public FitResult CanBeConvertedTo(FType candidateType, int maxDepth)
        {
            if (candidateType.IsPrimitiveGeneric)
                return new  FitResult(FitType.Converable,0);
            if (candidateType.IsPrimitive)
            {
                if (candidateType.Name.Equals(_type.Name))
                    return new FitResult(FitType.Strict, 0);
                //special case: Int is most expected type for someInteger
                if(_type.Name.Id== HmTypeName.Int32Id && candidateType.Name.Id== HmTypeName.SomeIntegerId)
                    return new FitResult(FitType.Candidate, _type.GetParentalDistanceTo(candidateType));
                if (_type.CanBeSafelyConvertedTo(candidateType)) 
                    return new FitResult(FitType.Converable, _type.GetParentalDistanceTo(candidateType));
                else
                    return FitResult.Not;
            }
            
            if (!candidateType.Name.Equals(_type.Name))
                return FitResult.Not;

            
            if (_type.Arguments.Length!= candidateType.Arguments.Length)
                return FitResult.Not;

            FitType minimalResult = FitType.Strict;
            for (int i = 0; i < _type.Arguments.Length ; i++)
            {
                var res =_type.Arguments[i]
                    .CanBeConvertedTo(candidateType.Arguments[i].MakeType(maxDepth-1), maxDepth-1);
                if (res.Type < minimalResult)
                    minimalResult = res.Type;
            }
            return new FitResult(minimalResult,0);
        }

        public override string ToString() => $"{_type}";
    }
}