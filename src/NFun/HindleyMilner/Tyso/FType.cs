using System;
using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class GenericType: FType
    {
        public int GenericId { get; }

        public GenericType(int genericId) : base(HmTypeName.Generic(genericId))
        {
            GenericId = genericId;
        }
    }
    public class FType
    {
        
        public FType(HmTypeName name, params SolvingNode[] arguments)
        {
            
            Name = name;
            Arguments = arguments;
            if(IsPrimitiveGeneric && !(this is GenericType))
                throw new InvalidOperationException("Invalid ftype usage");
        }
        public static FType Int64 => new FType(HmTypeName.Int64);
        public static FType Int32 => new FType(HmTypeName.Int32);
        public static FType Int16 => new FType(HmTypeName.Int16);
        public static FType Int8 => new FType(HmTypeName.Int8);

        public static FType UInt64 => new FType(HmTypeName.Uint64);
        public static FType UInt32 => new FType(HmTypeName.Uint32);
        public static FType UInt16 => new FType(HmTypeName.Uint16);
        public static FType UInt8 => new FType(HmTypeName.Uint8);
        
        public static FType Bool => new FType(HmTypeName.Bool);

        public static FType Real => new FType(HmTypeName.Real);
        public static FType ArrayOf(FType type) => new FType(HmTypeName.Array, SolvingNode.CreateStrict(type));
        public static FType ArrayOf(SolvingNode solvingNode) => new FType(HmTypeName.Array, solvingNode);

        public static FType Generic(int id)=> new GenericType(id);
        
        public static  FType GenericFun(int argsCount) 
            => new FType(HmTypeName.Function,  Enumerable.Range(0,argsCount+1).Select(a=> SolvingNode.CreateStrict(Generic(a))).ToArray());

        public static  FType Fun(FType output, params FType[] inputs) => new FType(
            HmTypeName.Function, 
            new[]{output}.Concat(inputs).Select(SolvingNode.CreateStrict).ToArray());

        public static  FType Fun(SolvingNode output, params SolvingNode[] inputs) => new FType(
            HmTypeName.Function, 
            new[]{output}.Concat(inputs).ToArray());
        public static FType Text => ArrayOf(Char);

        public static FType Char => new FType(HmTypeName.Char);

        public static FType Any => new FType(HmTypeName.Any);

        
        public bool IsPrimitive => !Arguments.Any();
        public HmTypeName Name { get; }
        public SolvingNode[] Arguments { get; }

        public bool IsNotConcrete =>
            Name.IsGeneric || Arguments.Any(a => a.MakeType(SolvingNode.MaxTypeDepth).IsNotConcrete);

        public bool IsPrimitiveGeneric => Name.IsGeneric && !Arguments.Any();


        public override bool Equals(object obj)
        {
            if (!(obj is FType n))
                return false;
            if (!Name.Equals(n.Name))
                return false;
            if (Arguments?.Length != n.Arguments?.Length)
                return false;
            if (Arguments?.Any() != true)
                return true;
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (!Arguments[i].MakeType().Equals(n.Arguments[i].MakeType()))
                    return false;
            }

            return true;
        }

        public int GetParentalDistanceTo(FType parent) => Name.Depth- parent.Name.Depth;

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
                if (!Arguments[i].MakeType()
                    .CanBeSafelyConvertedTo(
                            type2.Arguments[i].MakeType()))
                    return false;
            }
            return true;
        }

        public string ToSmartString(int maxDepth)
        {
            if (maxDepth < 0)
                return "...";
            
            if (Arguments.Any())
                return $"{Name}<{string.Join(",", Arguments.Select(a => a.ToSmartString(maxDepth-1)))}>";
            else
                return Name.ToString();

        }

        public override string ToString() => ToSmartString(SolvingNode.MaxTypeDepth);

    }
}