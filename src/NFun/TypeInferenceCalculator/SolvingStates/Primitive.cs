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

  
    public class Primitive: IType, IState
    {
        private static Primitive[] _integer;
        private static Primitive[] _uint;

        static Primitive()
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

        public Primitive(PrimitiveTypeName name)
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
                case PrimitiveTypeName.Any:  return "A";
                case PrimitiveTypeName.Char: return "C";
                case PrimitiveTypeName.Bool: return "B";
                case PrimitiveTypeName.Real: return "R";
                default: return Name.ToString();
            }
        }

        public static Primitive Any { get; } = new Primitive(PrimitiveTypeName.Any);
        public static Primitive Bool { get; } = new Primitive(PrimitiveTypeName.Bool);
        public static Primitive Char { get; } = new Primitive(PrimitiveTypeName.Char);
        public static Primitive Real { get; } = new Primitive(PrimitiveTypeName.Real);
        public static Primitive I96 { get; } = new Primitive(PrimitiveTypeName.I96);
        public static Primitive I64 { get; } = new Primitive(PrimitiveTypeName.I64);
        public static Primitive I48 { get; } = new Primitive(PrimitiveTypeName.I48);
        public static Primitive I32 { get; } = new Primitive(PrimitiveTypeName.I32);
        public static Primitive I24 { get; } = new Primitive(PrimitiveTypeName.I24);
        public static Primitive I16 { get; } = new Primitive(PrimitiveTypeName.I16);
        public static Primitive U64 { get; } = new Primitive(PrimitiveTypeName.U64);
        public static Primitive U48 { get; } = new Primitive(PrimitiveTypeName.U48);
        public static Primitive U32 { get; } = new Primitive(PrimitiveTypeName.U32);
        public static Primitive U24 { get; } = new Primitive(PrimitiveTypeName.U24);
        public static Primitive U16 { get; } = new Primitive(PrimitiveTypeName.U16);
        public static Primitive U12 { get; } = new Primitive(PrimitiveTypeName.U12);
        public static Primitive U8 { get; } = new Primitive(PrimitiveTypeName.U8);
        public bool IsComparable => IsNumeric || Name == PrimitiveTypeName.Char;

        public bool CanBeImplicitlyConvertedTo(Primitive type)
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

        public Primitive GetFirstCommonDescendantOrNull(Primitive other)
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
        public IType GetLastCommonAncestorOrNull(IType otherType)
        {
            var primitive = otherType as Primitive;
            if (primitive == null)
                return Any;
            return GetLastCommonPrimitiveAncestor(primitive);
        }

        public Primitive GetLastCommonPrimitiveAncestor(Primitive other)
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

        public override bool Equals(object obj) => (obj as Primitive)?.Name == Name;
        public string Description => Name.ToString();
    }
}
