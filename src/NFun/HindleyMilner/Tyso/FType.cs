using System;
using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class GenericType: FType
    {
        public int GenericId { get; }

        public GenericType(int genericId) : base(NTypeName.Generic(genericId))
        {
            GenericId = genericId;
        }
    }
    public class FType
    {
        
        public FType(NTypeName name, params SolvingNode[] arguments)
        {
            
            Name = name;
            Arguments = arguments;
         //   if(IsPrimitiveGeneric && !(this is GenericType))
         //       throw new InvalidOperationException("Invalid ftype usage");
        }
        public static FType Int64 => new FType(NTypeName.Int64);
        public static FType Int32 => new FType(NTypeName.Int32);
        public static FType Bool => new FType(NTypeName.Bool);

        public static FType Real => new FType(NTypeName.Real);
        public static FType ArrayOf(FType type) => new FType(NTypeName.Array, SolvingNode.CreateStrict(type));
        public static FType ArrayOf(SolvingNode solvingNode) => new FType(NTypeName.Array, solvingNode);

        public static FType Generic(int id)=> new GenericType(id);
        
        public static  FType GenericFun(int argsCount) 
            => new FType(NTypeName.Function,  Enumerable.Range(0,argsCount+1).Select(a=> SolvingNode.CreateStrict(Generic(a))).ToArray());

        public static  FType Fun(FType output, params FType[] inputs) => new FType(
            NTypeName.Function, 
            new[]{output}.Concat(inputs).Select(SolvingNode.CreateStrict).ToArray());

        public static  FType Fun(SolvingNode output, params SolvingNode[] inputs) => new FType(
            NTypeName.Function, 
            new[]{output}.Concat(inputs).ToArray());
        public static FType Text => new FType(NTypeName.Text);

        
        public static FType Any => new FType(NTypeName.Any);

        
        public bool IsPrimitive => !Arguments.Any();
        public NTypeName Name { get; }
        public SolvingNode[] Arguments { get; }

        public bool IsNotConcrete =>
            Name.IsGeneric || Arguments.Any(a => a.MakeType(SolvingNode.MaxTypeDepth).IsNotConcrete);

        public bool IsPrimitiveGeneric => Name.IsGeneric && !Arguments.Any();


        public override bool Equals(object obj)
        {
            if (!(obj is FType n))
                return false;
            return n.ToString() == ToString();
        }
        
        public bool CanBeSafelyConvertedTo(FType type2)
        {
            if (IsPrimitive)
                return Name.CanBeConvertedTo(type2.Name);
            if (!type2.Name.Equals(Name))
                return false;
            if (Arguments.Length != type2.Arguments.Length)
                return false;
            for (int i = 0; i < Arguments.Length ; i++)
            {
                if (!Arguments[i].MakeType(SolvingNode.MaxTypeDepth)
                    .CanBeSafelyConvertedTo(
                            type2.Arguments[i].MakeType(SolvingNode.MaxTypeDepth)))
                    return false;
            }
            return true;
        }

        public string ToSmartString(int maxDepth)
        {
            if (maxDepth < 0)
                return "...";
            
            if (Arguments.Any())
                return $"{Name}<{string.Join(",", Arguments.Select(a => a.MakeType(maxDepth-1).ToSmartString(maxDepth-1)))}>";
            else
                return Name.ToString();

        }

        public override string ToString() => ToSmartString(SolvingNode.MaxTypeDepth);

    }
}