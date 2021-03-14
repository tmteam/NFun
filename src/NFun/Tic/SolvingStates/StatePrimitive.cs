using NFun.Tic.Stages;

namespace NFun.Tic.SolvingStates
{
    public class StatePrimitive: ITypeState, ITicNodeState
    {
        private static readonly StatePrimitive[,] LcaMap;
        private static readonly StatePrimitive[,] FcdMap;
        static StatePrimitive()
        {
            LcaMap = new StatePrimitive [17, 17];
            FcdMap = new StatePrimitive [17, 17];

            FillLcaFcdMaps();
        }

        
        public StatePrimitive(PrimitiveTypeName name)
        {
            Name = name;
        }

        public PrimitiveTypeName Name { get; }
        public bool IsSolved => true;
        public bool IsMutable => false;

        public bool IsNumeric => Name.HasFlag(PrimitiveTypeName._isNumber);
        private int Order => (int)Name>>6;
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
        public override int GetHashCode() => (int) Name;
        public string Description => Name.ToString();

        public bool ApplyDescendant(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode) =>
            descendantNode.State.Apply(visitor, ancestorNode, descendantNode, this);
        public bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, StatePrimitive ancestor)
            => visitor.Apply(ancestor,this,ancestorNode, descendantNode);
        public bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, ConstrainsState ancestor)
            => visitor.Apply( ancestor,this,ancestorNode, descendantNode);
        public bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, ICompositeState ancestor)
            => visitor.Apply(ancestor,this,ancestorNode, descendantNode);

        private static void FillLcaFcdMaps()
        {
            int maxVal = 17;
            var numberToTypeMap = new[]
            {
                Any, //0
                Char, //1
                Bool, //2
                Real, //3
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
                U8, //16
            };

            //by default - any lca returns any
            for (int i = 0; i < maxVal; i++)
            for (int j = 0; j < maxVal; j++)
                LcaMap[i, j] = Any;

            //any,char,bool and self
            for (int i = 0; i < maxVal; i++)
            {
                //x ^ x = x
                LcaMap[i, i] = numberToTypeMap[i];
                //x _ x = x
                FcdMap[i, i] = numberToTypeMap[i];
                //x _ any = x
                FcdMap[i, Any.Order] = numberToTypeMap[i];
            }

            //real
            for (int i = Real.Order; i < maxVal; i++)
            {
                //number ^ real = real
                LcaMap[i, Real.Order] = Real;
                //number _ real = number
                FcdMap[i, Real.Order] = numberToTypeMap[i];
            }

            //i96
            for (int i = I96.Order; i < maxVal; i++)
            {
                //i96 ^ iXX = i96,   i96 ^ uXX = i96 
                LcaMap[i, I96.Order] = I96;
                //i96 _ iXX = iXX,   i96 _ uXX = uXX
                FcdMap[i, I96.Order] = numberToTypeMap[i];
            }

            //all ints
            for (int anc = I64.Order; anc <= I16.Order; anc++)
            {
                for (int desc = anc + 1; desc <= I16.Order; desc++)
                {
                    //I64 ^ i32 = I64
                    LcaMap[desc, anc] = numberToTypeMap[anc];
                    //I64 _ i32 = i32
                    FcdMap[desc, anc] = numberToTypeMap[desc];
                }
            }

            //all uints
            for (int anc = U64.Order; anc <= U8.Order; anc++)
            {
                for (int desc = anc + 1; desc <= U8.Order; desc++)
                {
                    //u64 ^ u32 = u64
                    LcaMap[desc, anc] = numberToTypeMap[anc];
                    //u64 _ u32 = u32
                    FcdMap[desc, anc] = numberToTypeMap[desc];
                }
            }

            //iXX to u12,u8
            for (int i = I64.Order; i <= I16.Order; i++)
            {
                //iXX ^ u8  = iXX
                LcaMap[U8.Order, i] = numberToTypeMap[i];
                //iXX _ u8  = u8
                FcdMap[U8.Order, i] = U8;

                //iXX ^ u12  = iXX
                LcaMap[U12.Order, i] = numberToTypeMap[i];
                //iXX _ u12  = u12
                FcdMap[U12.Order, i] = U12;
            }

            //uXX ^ Ixx 
            //U32 ^ I64 = I64...
            LcaMap[U16.Order, I16.Order] = I24;
            LcaMap[U24.Order, I16.Order] = I32;
            LcaMap[U32.Order, I16.Order] = I48;
            LcaMap[U48.Order, I16.Order] = I64;
            LcaMap[U64.Order, I16.Order] = I96;

            LcaMap[U16.Order, I24.Order] = I24;
            LcaMap[U24.Order, I24.Order] = I32;
            LcaMap[U32.Order, I24.Order] = I48;
            LcaMap[U48.Order, I24.Order] = I64;
            LcaMap[U64.Order, I24.Order] = I96;

            LcaMap[U16.Order, I32.Order] = I32;
            LcaMap[U24.Order, I32.Order] = I32;
            LcaMap[U32.Order, I32.Order] = I48;
            LcaMap[U48.Order, I32.Order] = I64;
            LcaMap[U64.Order, I32.Order] = I96;

            LcaMap[U16.Order, I48.Order] = I48;
            LcaMap[U24.Order, I48.Order] = I48;
            LcaMap[U32.Order, I48.Order] = I48;
            LcaMap[U48.Order, I48.Order] = I64;
            LcaMap[U64.Order, I48.Order] = I96;

            LcaMap[U16.Order, I64.Order] = I64;
            LcaMap[U24.Order, I64.Order] = I64;
            LcaMap[U32.Order, I64.Order] = I64;
            LcaMap[U48.Order, I64.Order] = I64;
            LcaMap[U64.Order, I64.Order] = I96;

            //uXX _ Ixx 
            //U32 _ I64 = U32...
            FcdMap[U16.Order, I16.Order] = U12;
            FcdMap[U24.Order, I16.Order] = U12;
            FcdMap[U32.Order, I16.Order] = U12;
            FcdMap[U48.Order, I16.Order] = U12;
            FcdMap[U64.Order, I16.Order] = U12;

            FcdMap[U16.Order, I24.Order] = U16;
            FcdMap[U24.Order, I24.Order] = U16;
            FcdMap[U32.Order, I24.Order] = U16;
            FcdMap[U48.Order, I24.Order] = U16;
            FcdMap[U64.Order, I24.Order] = U16;

            FcdMap[U16.Order, I32.Order] = U16;
            FcdMap[U24.Order, I32.Order] = U24;
            FcdMap[U32.Order, I32.Order] = U24;
            FcdMap[U48.Order, I32.Order] = U24;
            FcdMap[U64.Order, I32.Order] = U24;

            FcdMap[U16.Order, I48.Order] = U16;
            FcdMap[U24.Order, I48.Order] = U24;
            FcdMap[U32.Order, I48.Order] = U32;
            FcdMap[U48.Order, I48.Order] = U32;
            FcdMap[U64.Order, I48.Order] = U32;

            FcdMap[U16.Order, I64.Order] = U16;
            FcdMap[U24.Order, I64.Order] = U24;
            FcdMap[U32.Order, I64.Order] = U32;
            FcdMap[U48.Order, I64.Order] = U48;
            FcdMap[U64.Order, I64.Order] = U48;

            //a ^ b = b ^ a
            //a _ b = b _ a

            //reflect maps by diagonals
            for (int col = 0; col < maxVal; col++)
            {
                for (int row = col; row < maxVal; row++)
                {
                    LcaMap[col, row] = LcaMap[row, col];
                    FcdMap[col, row] = FcdMap[row, col];
                }
            }
        }

    }
}
