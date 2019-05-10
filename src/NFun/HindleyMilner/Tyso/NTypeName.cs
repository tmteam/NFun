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
        public static NTypeName Any => new NTypeName("any",0,35);
        public static NTypeName Real => new NTypeName("real",1,20);
        public static NTypeName SomeInteger => new NTypeName("[someInteger]",2,19);
        public static NTypeName Int64 => new NTypeName("int64",3,16);
        public static NTypeName Int32 => new NTypeName("int32",4,13);
        public static NTypeName Int16 => new NTypeName("int16",5,10);
        public static NTypeName Int8 => new NTypeName("int8",6,7);
        public static NTypeName Uint8 => new NTypeName("uint8",8,9);
        public static NTypeName Uint16 => new NTypeName("uint16",11,12);
        public static NTypeName Uint32 => new NTypeName("uint32",14,15);
        public static NTypeName Uint64 => new NTypeName("uint64",17,18);
        public static NTypeName Char => new NTypeName("char",21,22);
        public static NTypeName Bool => new NTypeName("bool",23,24);
        public static NTypeName Complex => new NTypeName("[someComplex]",25,34);
        public static NTypeName Array => new NTypeName("array",26,29);
        public static NTypeName Text => new NTypeName("text",27,28);
        public static NTypeName Function => new NTypeName("fun",30,31);
        public static NTypeName Generic(int num) => new NTypeName("T"+num,  -2,-1);
        public bool IsGeneric => Start == -2 && Finish == -1;
        public string Id { get; }
        public int Start { get; }
        public int Finish { get; }
        public override string ToString() => Id;

        public override bool Equals(object obj)
        {
            return (obj is NTypeName type) && type.Start == Start && type.Finish == Finish && type.Id== Id;
        }
    }
}