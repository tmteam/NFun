using NFun.SyntaxParsing;

namespace NFun.Types
{
    public readonly struct VarInfo
    {
        public readonly bool IsOutput;
        public readonly FunnyType Type;
        public readonly string Name;
        public readonly VarAttribute[] Attributes;
        public bool IsStrictTyped { get; }

        public VarInfo(
            bool isOutput, 
            FunnyType type,
            string name, 
            bool isStrictTyped,
            VarAttribute[] attributes = null)
        {
            IsStrictTyped = isStrictTyped;
            IsOutput = isOutput;
            Type = type;
            Name = name;
            Attributes = attributes ??new VarAttribute[0];
        }

        public override bool Equals(object obj)
        {
            if(obj is VarInfo v)
                return Equals(v);
            return false;
        }

        public bool Equals(VarInfo other)
        {
            return IsOutput == other.IsOutput 
                   && IsStrictTyped== other.IsStrictTyped
                   && Type.Equals(other.Type) 
                   && string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsOutput.GetHashCode();
                hashCode = (hashCode * 397) ^ IsOutput.GetHashCode();
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"({(IsOutput?"out":"in")}) {Name}:{Type}";
        }
    }
}