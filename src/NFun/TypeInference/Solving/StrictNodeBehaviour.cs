namespace NFun.TypeInference.Solving
{
    public class StrictNodeBehaviour : INodeBehavior
    {
        private readonly TiType _type;
        public StrictNodeBehaviour(TiType type)
        {
            _type = type;
        }

        public TiType MakeType(int maxTypeDepth = SolvingNode.MaxTypeDepth) => _type;

        public INodeBehavior SetLimit(TiType newLimit)
        {
            if(newLimit.Name.Id== TiTypeName.AnyId)
                return this;
            if (_type.IsPrimitive)
            {
                if (_type.CanBeSafelyConvertedTo(newLimit))
                    return this;
                else
                    return null;
            }

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
                else if (_type.Name.Id == TiTypeName.Function.Id && i > 0)
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
            return this;
        }

        public INodeBehavior SetStrict(TiType newType)
        {
            if (!newType.Name.Equals(_type.Name))
                return null;
            if (_type.Arguments.Length != newType.Arguments.Length)
                return null;
            for (int i = 0; i < _type.Arguments.Length ; i++)
            {
                var thisArgument  = _type.Arguments[i].GetActualNode();
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
                else if (!newArgument.SetStrict(thisArgument.MakeType()))
                {
                     return false;
                }
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

        public INodeBehavior Optimize(out bool hasChanged) {
            hasChanged = false;
            return this;
        }

        public FitResult CanBeConvertedFrom(TiType candidateType, int maxDepth) 
            => TiType.CanBeConverted(@from: candidateType, to: _type, maxDepth: maxDepth);
        
        public FitResult CanBeConvertedTo(TiType candidateType, int maxDepth)
            => TiType.CanBeConverted(@from: _type, to: candidateType, maxDepth: maxDepth);
        
        public override string ToString() => $"{_type}";
    }
}