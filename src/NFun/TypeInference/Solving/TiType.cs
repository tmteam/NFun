using System;
using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class GenericType: TiType
    {
        public int GenericId { get; }

        public GenericType(int genericId) : base(TiTypeName.Generic(genericId))
        {
            GenericId = genericId;
        }
    }
    public class TiType
    {
        public TiType(TiTypeName name, params SolvingNode[] arguments)
        {
            
            Name = name;
            Arguments = arguments;
            if(IsPrimitiveGeneric && !(this is GenericType))
                throw new InvalidOperationException("Invalid ftype usage");
        }
        public static TiType Int64 => new TiType(TiTypeName.Int64);
        public static TiType Int32 => new TiType(TiTypeName.Int32);
        public static TiType Int16 => new TiType(TiTypeName.Int16);
        public static TiType UInt64 => new TiType(TiTypeName.Uint64);
        public static TiType UInt32 => new TiType(TiTypeName.Uint32);
        public static TiType UInt16 => new TiType(TiTypeName.Uint16);
        public static TiType UInt8 => new TiType(TiTypeName.Uint8);
        public static TiType Bool => new TiType(TiTypeName.Bool);
        public static TiType Real => new TiType(TiTypeName.Real);
        public static TiType ArrayOf(TiType type) => new TiType(TiTypeName.Array, SolvingNode.CreateStrict(type));
        public static TiType ArrayOf(SolvingNode solvingNode) => new TiType(TiTypeName.Array, solvingNode);
        public static TiType Generic(int id)=> new GenericType(id);
        public static  TiType GenericFun(int argsCount) 
            => new TiType(TiTypeName.Function,  Enumerable.Range(0,argsCount+1).Select(a=> SolvingNode.CreateStrict(Generic(a))).ToArray());
        public static  TiType Fun(TiType output, params TiType[] inputs) => new TiType(
            TiTypeName.Function, 
            new[]{output}.Concat(inputs).Select(SolvingNode.CreateStrict).ToArray());

        public static  TiType Fun(SolvingNode output, params SolvingNode[] inputs) => new TiType(
            TiTypeName.Function, 
            new[]{output}.Concat(inputs).ToArray());
        public static TiType Text => ArrayOf(Char);

        public static TiType Char => new TiType(TiTypeName.Char);

        public static TiType Any => new TiType(TiTypeName.Any);

        public bool IsPrimitive => !Arguments.Any();
        public TiTypeName Name { get; }
        public SolvingNode[] Arguments { get; }

        public bool IsNotConcrete =>
            Name.IsGeneric || Arguments.Any(a => a.MakeType().IsNotConcrete);

        public bool IsPrimitiveGeneric => Name.IsGeneric && !Arguments.Any();


        public override bool Equals(object obj)
        {
            if (!(obj is TiType n))
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
        /// <summary>
        /// Get inherance graph distance to specified parent 
        /// </summary>
        public int GetParentalDistanceTo(TiType parent) => Name.Depth- parent.Name.Depth;

        public bool CanBeSafelyConvertedTo(TiType type2)
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

        /// <summary>
        /// Returns last common ancestor for specified types
        /// </summary>
        public static TiType GetLca(TiType[] fTypes)
        {
            var left = fTypes.Where(c=>c.Name.Start>=0).Min(c => c.Name.Start);
            var right = fTypes.Where(c=>c.Name.Finish>=0).Max(c=>c.Name.Finish);
            
            //Search for GeneralParent
            var parentName = fTypes[0].Name;
            while (true)
            {
                if (parentName.Start <= left && parentName.Finish >= right)
                    return new TiType(parentName);
                
                if(parentName.Parent==null)
                    return TiType.Any;
                parentName = parentName.Parent;
            }
        }

        public static FitResult CanBeConverted(TiType from, TiType to, int maxDepth){
            if (to.IsPrimitiveGeneric)
                return new  FitResult(FitType.Convertable,0);
            if (to.IsPrimitive)
            {
                if (to.Name.Equals(from.Name))
                    return new FitResult(FitType.Strict, 0);
                //special case: Int is most expected type for someInteger
                if(from.Name.Id== TiTypeName.Int32Id && to.Name.Id== TiTypeName.SomeIntegerId)
                    return new FitResult(FitType.Candidate, from.GetParentalDistanceTo(to));
                if (from.CanBeSafelyConvertedTo(to)) 
                    return new FitResult(FitType.Convertable, from.GetParentalDistanceTo(to));
                else
                    return FitResult.Not;
            }
            
            if (!to.Name.Equals(from.Name))
                return FitResult.Not;
            
            if (from.Arguments.Length!= to.Arguments.Length)
                return FitResult.Not;

            var minimalResult = FitType.Strict;
            for (int i = 0; i < from.Arguments.Length ; i++)
            {
                var res =from.Arguments[i]
                    .CanBeConvertedTo(to.Arguments[i].MakeType(maxDepth-1), maxDepth-1);
                if (res.Type < minimalResult)
                    minimalResult = res.Type;
            }
            return new FitResult(minimalResult,0);
        } 

    }
}