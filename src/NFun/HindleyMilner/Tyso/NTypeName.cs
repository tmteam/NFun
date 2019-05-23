namespace NFun.HindleyMilner.Tyso
{
    public class NTypeName
    {
        public NTypeName(string id, int start, int finish)
        {
            Id = id;
            Start = start;
            Finish = finish;
        }

        public const string AnyId = "any";
        public const string RealId = "real";
        public const string SomeIntegerId = "[someInteger]";
        public const string Int64Id = "int64";
        public const string Int32Id = "int32";
        public const string Int16Id = "int16";
        public const string UInt64Id = "uint64";
        public const string UInt32Id = "uint32";
        public const string UInt16Id = "uint16";
        public const string UInt8Id = "uint8";
        public const string BoolId = "bool";
        public const string TextId = "text";
        public const string ArrayId = "array";
        public const string FunId = "fun";

        public static NTypeName Any => new NTypeName(AnyId,0,35);
        public static NTypeName Real => new NTypeName(RealId,1,20);
        public static NTypeName SomeInteger => new NTypeName(SomeIntegerId,2,19);
        public static NTypeName Int64 => new NTypeName(Int64Id,3,16);
        public static NTypeName Int32 => new NTypeName(Int32Id,4,13);
        public static NTypeName Int16 => new NTypeName(Int16Id,5,10);
        public static NTypeName Int8 => new NTypeName("int8",6,7);
        public static NTypeName Uint8 => new NTypeName(UInt8Id,8,9);
        public static NTypeName Uint16 => new NTypeName(UInt16Id,11,12);
        public static NTypeName Uint32 => new NTypeName(UInt32Id,14,15);
        public static NTypeName Uint64 => new NTypeName(UInt64Id,17,18);
        public static NTypeName Char => new NTypeName("char",21,22);
        public static NTypeName Bool => new NTypeName(BoolId,23,24);
        public static NTypeName Complex => new NTypeName("[someComplex]",25,34);
        public static NTypeName Array => new NTypeName(ArrayId,26,29);
        public static NTypeName Text => new NTypeName(TextId,27,28);
        public static NTypeName Function => new NTypeName(FunId,30,31);
        public static NTypeName Generic(int num) => new NTypeName("T"+num,  -2,-1);
        public bool IsGeneric => Start == -2 && Finish == -1;
        public string Id { get; }
        public int Start { get; }
        public int Finish { get; }
        public override string ToString() => Id;

        public bool CanBeConvertedTo(NTypeName baseType)
        {
            return Start >= baseType.Start && Finish <= baseType.Finish;
        } 
        public override bool Equals(object obj)
        {
            return (obj is NTypeName type) && type.Start == Start && type.Finish == Finish && type.Id== Id;
        }

        protected bool Equals(NTypeName other)
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