using System;

namespace NFun.TypeInference.Solving
{
    public class SolvingNode
    {
        public const int MaxTypeDepth = 100;

        public static SolvingNode CreateStrict(TiType type)
        {
            var ans =  new SolvingNode();
            ans.SetStrict(type);
            return ans;
        }
        public static SolvingNode CreateStrict(TiTypeName name, params SolvingNode[] args)
        {
            var ans =  new SolvingNode();
            ans.SetStrict(new TiType(name, args));
            return ans;
        }
        private static int _lastId = 0;
        public SolvingNode()
        {
            Id = _lastId++;
            Behavior =  new GenericTypeBehaviour();
            
        }
        
        public INodeBehavior Behavior { get; private set; }

        private bool TrySet(Func<INodeBehavior> act) 
        {
            var res = act();
            if (res == null)
                return false;
            Behavior = res;
            return true;
        }
        
        public bool SetEqualTo(SolvingNode node) 
            => TrySet(()=> Behavior.SetReference(node));
    

        public bool SetLca(SolvingNode[] nodes)
            => TrySet(()=> Behavior.SetLca(nodes));

        public bool SetStrict(TiType limit)
            => TrySet(()=> Behavior.SetStrict(limit));

        public bool SetLimit(TiType limit)
            => TrySet(() => Behavior.SetLimit(limit));

        public bool SetGeneric(SolvingNode generic) 
            => TrySet(() => Behavior.SetGeneric(generic));
        public TiType MakeType(int maxTypeDepth = SolvingNode.MaxTypeDepth) => Behavior.MakeType(maxTypeDepth);

        public int Id { get; }

        public override string ToString() => $"{Id}:{Behavior.ToSmartString(10)}";

        public string ToSmartString(int maxDepth)
        {
            if (maxDepth < 0)
                return "...";
            return $"{Id}. {Behavior.ToSmartString(maxDepth)}";

        }
        public bool Optimize(out bool hasChanged)
        {
            
             var beh = Behavior.Optimize(out hasChanged);
             if (beh == null)
                 return false;
             Behavior = beh;
             return true;
        }

        public FitResult CanBeConvertedFrom(TiType candidateType, int maxDepth = SolvingNode.MaxTypeDepth)
        {
            if(maxDepth<0)
                throw new StackOverflowException("Fits too depth");
            return Behavior.CanBeConvertedFrom(candidateType, maxDepth);
        }
        public FitResult CanBeConvertedTo(TiType candidateType, int maxDepth = SolvingNode.MaxTypeDepth)
        {
            if(maxDepth<0)
                throw new StackOverflowException("Fits too depth");
            return Behavior.CanBeConvertedTo(candidateType, maxDepth);
        }

        
        public static SolvingNode CreateRefTo(SolvingNode children)
        {
            var node = new SolvingNode();
            node.SetEqualTo(children);
            return node;
        }
        public static SolvingNode CreateLca(params SolvingNode[] children)
        {
            var node = new SolvingNode();
            node.SetLca(children);
            return node;
        }

        public static SolvingNode CreateLimit(TiType int32)
        {
            var node = new SolvingNode();
            node.SetLimit(int32);
            return node;
        }

        public  SolvingNode GetActualNode(int depth = MaxTypeDepth)
        {
            if(depth<0)
                throw new StackOverflowException("Cycled reference");
            if (Behavior is ReferenceBehaviour r)
            {
                return r.Node.GetActualNode(depth - 1);
            }
            return this;
        }
    }

    public struct FitResult
    {
        public static readonly FitResult Not    = new FitResult(FitType.Not, 0);
        public static readonly FitResult Strict = new FitResult(FitType.Strict,0);
        
        public readonly FitType Type;
        public readonly int Distance;

        public bool IsBetterThan(FitResult result)
        {
            if (Type > result.Type)
                return true;
            if (Type < result.Type)
                return false;
            return Distance < result.Distance;
        }
        public FitResult(FitType type, int distance)
        {
            Type = type;
            Distance = distance;
        }

        public static FitResult Candidate(int distance)
        => new FitResult(FitType.Candidate, distance);
    }
    public enum FitType
    {
        Not = 0, 
        Convertable = 1,
        Candidate = 2,
        Strict = 3,
    }
}