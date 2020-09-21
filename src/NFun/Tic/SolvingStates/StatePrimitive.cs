using System;

namespace NFun.Tic.SolvingStates
{
    public enum PrimitiveTypeName
    {
        Any = _IsPrimitive,

        _IsPrimitive = 1<<0,
        _IsNumber    = 1<<1,
        _IsUint      = 1<<2,
        _isAbstract  = 1<<3,
        
        Char = _IsPrimitive | 1<<5 | 1<<9,
        Bool = _IsPrimitive | 1<<5 | 2<<9,

        Real = _IsPrimitive | _IsNumber | 1 << 5,
        I96  = _IsPrimitive | _IsNumber | 2 << 5 | _isAbstract,
        I64  = _IsPrimitive | _IsNumber | 3 << 5,
        I48  = _IsPrimitive | _IsNumber | 4 << 5 | _isAbstract,
        I32  = _IsPrimitive | _IsNumber | 5 << 5,
        I24  = _IsPrimitive | _IsNumber | 6 << 5 | _isAbstract,
        I16  = _IsPrimitive | _IsNumber | 7 << 5,

        U64  = _IsPrimitive | _IsNumber | _IsUint | 3 << 5,
        U48  = _IsPrimitive | _IsNumber | _IsUint | 4 << 5 | _isAbstract,
        U32  = _IsPrimitive | _IsNumber | _IsUint | 5 << 5,
        U24  = _IsPrimitive | _IsNumber | _IsUint | 6 << 5 | _isAbstract,
        U16  = _IsPrimitive | _IsNumber | _IsUint | 7 << 5,
        U12  = _IsPrimitive | _IsNumber | _IsUint | 8 << 5 | _isAbstract,
        U8   = _IsPrimitive | _IsNumber | _IsUint | 9 << 5,
    }

  
    public class StatePrimitive: ITypeState, ITicNodeState
    {
        private static StatePrimitive[] _integer;
        private static StatePrimitive[] _uint;

        static StatePrimitive()
        {
            _uint = new[]
            {

                U64,
                U48,
                U32,
                U24,
                U16,
                U12,
                U8
            };
            _integer = new[]
            {
                Real,
                I96,
                I64,
                I48,
                I32,
                I24,
                I16
            };
        }

        public StatePrimitive(PrimitiveTypeName name)
        {
            Name = name;
        }

        public PrimitiveTypeName Name { get; }

        public bool IsSolved => true;
        public bool IsNumeric => Name.HasFlag(PrimitiveTypeName._IsNumber);
        
        private int Layer => (int)((int)Name >>5 & 0b1111);

        public override string ToString()
        {
            switch (Name)
            {
                case PrimitiveTypeName.Char: return "Ch";
                case PrimitiveTypeName.Bool: return "Bo";
                case PrimitiveTypeName.Real: return "Re";
                default: return Name.ToString();
            }
        }

        public static StatePrimitive Any { get; } = new StatePrimitive(PrimitiveTypeName.Any);
        public static StatePrimitive Bool { get; } = new StatePrimitive(PrimitiveTypeName.Bool);
        public static StatePrimitive Char { get; } = new StatePrimitive(PrimitiveTypeName.Char);
        public static StatePrimitive Real { get; } = new StatePrimitive(PrimitiveTypeName.Real);
        public static StatePrimitive I96 { get; } = new StatePrimitive(PrimitiveTypeName.I96);
        public static StatePrimitive I64 { get; } = new StatePrimitive(PrimitiveTypeName.I64);
        public static StatePrimitive I48 { get; } = new StatePrimitive(PrimitiveTypeName.I48);
        public static StatePrimitive I32 { get; } = new StatePrimitive(PrimitiveTypeName.I32);
        public static StatePrimitive I24 { get; } = new StatePrimitive(PrimitiveTypeName.I24);
        public static StatePrimitive I16 { get; } = new StatePrimitive(PrimitiveTypeName.I16);
        public static StatePrimitive U64 { get; } = new StatePrimitive(PrimitiveTypeName.U64);
        public static StatePrimitive U48 { get; } = new StatePrimitive(PrimitiveTypeName.U48);
        public static StatePrimitive U32 { get; } = new StatePrimitive(PrimitiveTypeName.U32);
        public static StatePrimitive U24 { get; } = new StatePrimitive(PrimitiveTypeName.U24);
        public static StatePrimitive U16 { get; } = new StatePrimitive(PrimitiveTypeName.U16);
        public static StatePrimitive U12 { get; } = new StatePrimitive(PrimitiveTypeName.U12);
        public static StatePrimitive U8 { get; } = new StatePrimitive(PrimitiveTypeName.U8);
        public bool IsComparable => IsNumeric || Name == PrimitiveTypeName.Char;

        public bool CanBeImplicitlyConvertedTo(StatePrimitive type)
        {
            if (type.Name == PrimitiveTypeName.Any)
                return true;
            if (this.Equals(type))
                return true;
            if (!this.IsNumeric || !type.IsNumeric)
                return false;
            //So both are numbers
            if (type.Name == PrimitiveTypeName.Real)
                return true;
            if (this.Layer <= type.Layer)
                return false;
            if (type.Name.HasFlag(PrimitiveTypeName._IsUint))
                return this.Name.HasFlag(PrimitiveTypeName._IsUint);
            return true;
        }

        public StatePrimitive GetFirstCommonDescendantOrNull(StatePrimitive other)
        {
            if (this.Equals(other))
                return this;

            if (other.CanBeImplicitlyConvertedTo(this))
                return other;
            if (this.CanBeImplicitlyConvertedTo(other))
                return this;
            
            if (!other.IsNumeric || !this.IsNumeric)
                return null;

            var intType = other;

            if (other.Name.HasFlag(PrimitiveTypeName._IsUint))
                intType = this;

            var layer = intType.Layer + 1;
            return _uint[layer-3];
        }
        public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType)
        {
            var primitive = otherType as StatePrimitive;
            if (primitive == null)
                return Any;
            return GetLastCommonPrimitiveAncestor(primitive);
        }

        public StatePrimitive GetLastCommonPrimitiveAncestor(StatePrimitive other)
        {
            if (this.Equals(other))
                return this;
            
            if (!other.IsNumeric || !this.IsNumeric)
                return Any;
            if (other.CanBeImplicitlyConvertedTo(this))
                return this;
            if (this.CanBeImplicitlyConvertedTo(other))
                return other;

            var uintType = this;
            if (other.Name.HasFlag(PrimitiveTypeName._IsUint))
                uintType = other;

            for (int i = uintType.Layer; i >= 1; i--)
            {
                if (uintType.CanBeImplicitlyConvertedTo(_integer[i]))
                    return _integer[i];
            }

            throw new InvalidOperationException();
        }

        public override bool Equals(object obj) => (obj as StatePrimitive)?.Name == Name;
        public string Description => Name.ToString();
    }
}
