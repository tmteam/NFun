using System;
using NFun.SyntaxParsing;

namespace NFun.Types
{
    public readonly struct VarInfo
    {
        private readonly bool _isOutput;
        public readonly FunnyType Type;
        public readonly string Name;
        public readonly VarAttribute[] Attributes;

        public VarInfo(
            bool isOutput,
            FunnyType type,
            string name,
            VarAttribute[] attributes = null)
        {
            _isOutput = isOutput;
            Type = type;
            Name = name;
            Attributes = attributes ?? Array.Empty<VarAttribute>();
        }

        public override bool Equals(object obj)
        {
            if (obj is VarInfo v)
                return Equals(v);
            return false;
        }

        public bool Equals(VarInfo other)
        {
            return _isOutput == other._isOutput
                   && Type.Equals(other.Type)
                   && string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _isOutput.GetHashCode();
                hashCode = (hashCode * 397) ^ _isOutput.GetHashCode();
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => $"({(_isOutput ? "out" : "in")}) {Name}:{Type}";
    }
}