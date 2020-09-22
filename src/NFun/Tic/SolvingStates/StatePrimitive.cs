using System;

namespace NFun.Tic.SolvingStates
{
    public enum PrimitiveTypeName
    {
        _isAbstract  = 1<<3,
        _isNumber  = 1<<2,

        Any = 0,
        Char =   1  << 6,
        Bool =   2  << 6,
        Real =   3  << 6| _isNumber,
        I96  =   4  << 6| _isNumber | _isAbstract,
        I64  =   5  << 6| _isNumber,
        I48  =   6  << 6| _isNumber | _isAbstract,
        I32  =   7  << 6| _isNumber,
        I24  =   8  << 6| _isNumber | _isAbstract,
        I16  =   9  << 6| _isNumber,
        U64  =   10 << 6| _isNumber,
        U48  =   11 << 6| _isNumber | _isAbstract,
        U32  =   12 << 6| _isNumber,
        U24  =   13 << 6| _isNumber | _isAbstract,
        U16  =   14 << 6| _isNumber,
        U12  =   15 << 6| _isNumber | _isAbstract,
        U8   =   16 << 6| _isNumber,
    }

  
    public class StatePrimitive: ITypeState, ITicNodeState
    {
        private static readonly StatePrimitive[] NumberToTypeMap;
        private static readonly StatePrimitive[,] LcaMap;
        private static readonly StatePrimitive[,] FcdMap;

        static StatePrimitive()
        {
            const int maxVal = 17;
            LcaMap = new StatePrimitive [17, 17];
            FcdMap = new StatePrimitive [17, 17];

            NumberToTypeMap = new[] {
                Any, //0
                Char,//1
                Bool,//2
                Real,//3
                I96, //4
                I64, //5
                I48, //6
                I32, //7
                I24, //8
                I16, //9
                U64, //10
                U48, //11
                U32, //12
                U24, //13
                U16, //14
                U12, //15
                U8,  //16
            };
           
            //by default - any lca returns any
            for (int i = 0; i < maxVal; i++)
                for (int j = 0; j < maxVal; j++)
                    LcaMap[i, j] = Any;

            for (int i = 1; i < maxVal; i++) {
                //x ^ x = x
                LcaMap[i, i] = NumberToTypeMap[i];
                //x _ x = x
                FcdMap[i, i] = NumberToTypeMap[i];
                //x _ any = x
                FcdMap[i, Any.Order] = NumberToTypeMap[i];
            }
            for (int i = Real.Order; i < maxVal; i++) {
                //number ^ real = real
                LcaMap[Real.Order, i] = Real;
                //number _ real = number
                FcdMap[Real.Order, i] = NumberToTypeMap[i];
            }
            for (int i = I96.Order; i < maxVal; i++) {
                //i96 ^ int = i96
                LcaMap[I96.Order, i] = I96;
                //i96 _ int = int
                FcdMap[I96.Order, i] = NumberToTypeMap[i];
            }
            //all uints
            for (int anc = U64.Order; anc <= U8.Order; anc++) {
                for (int desc = anc; desc <= U8.Order; desc++)
                {
                    //u64 ^ u32 = u64
                    LcaMap[anc, desc] = NumberToTypeMap[anc];
                    //u64 _ u32 = u32
                    FcdMap[anc, desc] = NumberToTypeMap[desc];
                }
            }
            //all ints
            for (int anc = I64.Order; anc <= I16.Order; anc++) {
                for (int desc = anc; desc <= I16.Order; desc++)
                {
                    //I64 ^ i32 = I64
                    LcaMap[anc, desc] = NumberToTypeMap[anc];
                    //I64 _ i32 = i32
                    FcdMap[anc, desc] = NumberToTypeMap[desc];
                }
            }
            
            for (int i = 5; i < 16; i++)
            {
                //u8 ^ number = number
                LcaMap[U8.Order, i] = NumberToTypeMap[i];
                //u8 _ number = U8
                FcdMap[U8.Order, i] = U8;
            }
            for (int i = 5; i < 15; i++)
            {
                //u12 ^ number = number
                LcaMap[U12.Order, i] = NumberToTypeMap[i];
                //u12 _ number = u12
                FcdMap[U12.Order, i] = U12;
            }
            
            
            //U32 ^ I64 = I64...
            //U32 _ I64 = U32...
            LcaMap[U16.Order, I32.Order] = I32;
            LcaMap[U16.Order, I48.Order] = I48;
            LcaMap[U16.Order, I64.Order] = I64;
            FcdMap[U16.Order, I32.Order] = U16;
            FcdMap[U16.Order, I48.Order] = U16;
            FcdMap[U16.Order, I64.Order] = U16;
            
            LcaMap[U24.Order, I32.Order] = I32;
            LcaMap[U24.Order, I48.Order] = I48;
            LcaMap[U24.Order, I64.Order] = I64;
            FcdMap[U24.Order, I32.Order] = U24;
            FcdMap[U24.Order, I48.Order] = U24;
            FcdMap[U24.Order, I64.Order] = U24;

            LcaMap[U32.Order, I48.Order] = I48;
            LcaMap[U32.Order, I64.Order] = I64;
            FcdMap[U32.Order, I48.Order] = U32;
            FcdMap[U32.Order, I64.Order] = U32;

            LcaMap[U48.Order, I64.Order] = I64;
            FcdMap[U48.Order, I64.Order] = U48;

            for (int row = 1; row < 17; row++)
            {
                for (int col = 0; col < row; col++)
                {
                    LcaMap[col, row] = LcaMap[row, col];
                    FcdMap[col, row] = FcdMap[row, col];
                }
            }
        }

        public StatePrimitive(PrimitiveTypeName name)
        {
            Name = name;
        }

        public PrimitiveTypeName Name { get; }

        public bool IsSolved => true;
        public bool IsNumeric => Name.HasFlag(PrimitiveTypeName._isNumber);
        
        private int Order => (int)((int)Name >>6 & 0b1111);

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
            var a = this.Order;
            var b = type.Order;
            return Equals(LcaMap[a, b], type); 
        }

        public StatePrimitive GetFirstCommonDescendantOrNull(StatePrimitive other) 
            => FcdMap[this.Order, other.Order];

        public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType)
        {
            var primitive = otherType as StatePrimitive;
            if (primitive == null)
                return Any;
            return GetLastCommonPrimitiveAncestor(primitive);
        }

        public StatePrimitive GetLastCommonPrimitiveAncestor(StatePrimitive other)
        {
            var a = this.Order;
            var b = other.Order;
            return LcaMap[a, b];
        }

        public override bool Equals(object obj) => (obj as StatePrimitive)?.Name == Name;
        public string Description => Name.ToString();
    }
}
