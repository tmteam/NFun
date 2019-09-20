namespace NFun.TypeInference.Solving
{
    public class TiTypeName
    {
        public TiTypeName(string id, int start, int finish, int depth, TiTypeName parent)
        {
            Id = id;
            Start = start;
            Finish = finish;
            Depth = depth;
            Parent = parent;
        }

        public const string AnyId = "any";
        public const string RealId = "real";
        public const string SomeIntegerId = "[someInteger]";
        public const string Int64Id = "int64";
        public const string Int32Id = "int32";
        public const string Int16Id = "int16";
        public const string Int8Id = "int8";
        public const string UInt64Id = "uint64";
        public const string UInt32Id = "uint32";
        public const string UInt16Id = "uint16";
        public const string UInt8Id = "uint8";
        public const string BoolId = "bool";
        public const string TextId = "text";
        public const string ArrayId = "array";
        public const string FunId = "fun";
        public const string CharId = "char";
        
        public static TiTypeName Any => new TiTypeName(AnyId,0,35,0, null);
        public static TiTypeName Real => new TiTypeName(RealId,1,20,1, Any);
        public static TiTypeName SomeInteger => new TiTypeName(SomeIntegerId,2,19,2, Real);
        public static TiTypeName Int64 => new TiTypeName(Int64Id,3,16,3, SomeInteger);
        public static TiTypeName Int32 => new TiTypeName(Int32Id,4,13,4, Int64);
        public static TiTypeName Int16 => new TiTypeName(Int16Id,5,10,5, Int32);
        public static TiTypeName Int8 => new TiTypeName(Int8Id,6,7,6, Int16);
        public static TiTypeName Uint8 => new TiTypeName(UInt8Id,8,9,10, Uint16);
        public static TiTypeName Uint16 => new TiTypeName(UInt16Id,11,12,9, Uint32);
        public static TiTypeName Uint32 => new TiTypeName(UInt32Id,14,15,8, Uint64);
        public static TiTypeName Uint64 => new TiTypeName(UInt64Id,17,18,7, SomeInteger);
        public static TiTypeName Char => new TiTypeName(CharId,21,22,1, Any);
        public static TiTypeName Bool => new TiTypeName(BoolId,23,24,1, Any);
        public static TiTypeName Complex => new TiTypeName("[someComplex]",25,34,1, Any);
        public static TiTypeName Array => new TiTypeName(ArrayId,26,29,2, Any);
        public static TiTypeName Function => new TiTypeName(FunId,30,31,2, Any);
        public static TiTypeName Generic(int num) => new TiTypeName("T" + num, -2, -1, -1, null);
        public bool IsGeneric => Start == -2 && Finish == -1;
        public string Id { get; }
        public int Start { get; }
        public int Depth { get; }
        public TiTypeName Parent { get; }
        public int Finish { get; }
        public override string ToString() => Id;
        
        public bool CanBeConvertedTo(TiTypeName baseType)
        {
            if (Start >= baseType.Start && Finish <= baseType.Finish)
                return true;

            switch (Id)
            {
                //Special uint convertion
                case UInt8Id:
                    return baseType.Id == UInt16Id || baseType.Id == UInt32Id || baseType.Id == UInt64Id;
                case UInt16Id:
                    return  baseType.Id == UInt32Id || baseType.Id == UInt64Id;
                case UInt32Id:
                    return  baseType.Id == UInt64Id;
                default:
                    return false;
            }
        } 
        public override bool Equals(object obj)
        {
            return (obj is TiTypeName type) && type.Start == Start && type.Finish == Finish && type.Id== Id;
        }

        protected bool Equals(TiTypeName other)
        {
            return string.Equals(Id, other.Id) && Start == other.Start && Finish == other.Finish;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Start;
                hashCode = (hashCode * 397) ^ Finish;
                return hashCode;
            }
        }
    }
}