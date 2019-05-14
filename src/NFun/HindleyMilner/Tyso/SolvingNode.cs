using System;

namespace NFun.HindleyMilner.Tyso
{
    public class SolvingNode
    {
        public const int MaxTypeDepth = 100;

        public static SolvingNode CreateStrict(FType type)
        {
            var ans =  new SolvingNode();
            ans.SetStrict(type);
            return ans;
        }
        public static SolvingNode CreateStrict(NTypeName name, params SolvingNode[] args)
        {
            var ans =  new SolvingNode();
            ans.SetStrict(new FType(name, args));
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


        public bool SetStrict(FType limit)
            => TrySet(()=> Behavior.SetStrict(limit));

        public bool SetLimit(FType limit)
            => TrySet(() => Behavior.SetLimit(limit));

        public bool SetGeneric(SolvingNode generic) 
            => TrySet(() => Behavior.SetGeneric(generic));
        public FType MakeType(int maxTypeDepth) => Behavior.MakeType(maxTypeDepth);

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

        public FitResults Fits(FType candidateType, int maxDepth)
        {
            if(maxDepth<0)
                throw new StackOverflowException("Fits too depth");
            return Behavior.Fits(candidateType, maxDepth);
        }

        public static SolvingNode CreateLca(params SolvingNode[] children)
        {
            var node = new SolvingNode();
            node.SetLca(children);
            return node;
        }
    }

    public enum FitResults
    {
        Not = 0, 
        Converable = 1,
        Strict = 2,
    }
}