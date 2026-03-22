namespace NFun.Tic.SolvingStates;

public class StatePrimitive : ITypeState, ITicNodeState {
    private static readonly StatePrimitive[,] LcaMap;
    private static readonly StatePrimitive[,] GcdMap;

    static StatePrimitive() {
        LcaMap = new StatePrimitive [19, 19];
        GcdMap = new StatePrimitive [19, 19];

        FillLcaGcdMaps();
    }


    public StatePrimitive(PrimitiveTypeName name) => Name = name;

    public PrimitiveTypeName Name { get; }
    public bool IsSolved => true;
    public bool IsMutable => false;

    public bool IsNumeric => Name.HasFlag(PrimitiveTypeName._isNumber);
    public int Order => (int)Name >> 6;

    public override string ToString() =>
        Name switch {
            PrimitiveTypeName.Char => "Ch",
            PrimitiveTypeName.Bool => "Bo",
            PrimitiveTypeName.Real => "Re",
            PrimitiveTypeName.None => "None",
            _                      => Name.ToString()
        };

    public static StatePrimitive Any { get; } = new(PrimitiveTypeName.Any);
    public static StatePrimitive Bool { get; } = new(PrimitiveTypeName.Bool);
    public static StatePrimitive Char { get; } = new(PrimitiveTypeName.Char);
    public static StatePrimitive Ip { get; } = new(PrimitiveTypeName.Ip);
    public static StatePrimitive Real { get; } = new(PrimitiveTypeName.Real);
    public static StatePrimitive I96 { get; } = new(PrimitiveTypeName.I96);
    public static StatePrimitive I64 { get; } = new(PrimitiveTypeName.I64);
    public static StatePrimitive I48 { get; } = new(PrimitiveTypeName.I48);
    public static StatePrimitive I32 { get; } = new(PrimitiveTypeName.I32);
    public static StatePrimitive I24 { get; } = new(PrimitiveTypeName.I24);
    public static StatePrimitive I16 { get; } = new(PrimitiveTypeName.I16);
    public static StatePrimitive U64 { get; } = new(PrimitiveTypeName.U64);
    public static StatePrimitive U48 { get; } = new(PrimitiveTypeName.U48);
    public static StatePrimitive U32 { get; } = new(PrimitiveTypeName.U32);
    public static StatePrimitive U24 { get; } = new(PrimitiveTypeName.U24);
    public static StatePrimitive U16 { get; } = new(PrimitiveTypeName.U16);
    public static StatePrimitive U12 { get; } = new(PrimitiveTypeName.U12);
    public static StatePrimitive U8 { get; } = new(PrimitiveTypeName.U8);
    public static StatePrimitive None { get; } = new(PrimitiveTypeName.None);
    public bool IsComparable => IsNumeric || Name == PrimitiveTypeName.Char;
    public string StateDescription => PrintState(0);

    public string PrintState(int depth) => ToString();

    public virtual bool CanBePessimisticConvertedTo(StatePrimitive type) {
        // None ≤ Any (none is a value with toString/equals), but None is not ≤ any other primitive
        if (Name == PrimitiveTypeName.None)
            return type.Name == PrimitiveTypeName.None || type.Name == PrimitiveTypeName.Any;
        if (type.Name == PrimitiveTypeName.None)
            return false;
        return Equals(LcaMap[Order, type.Order], type);
    }

    public virtual StatePrimitive GetFirstCommonDescendantOrNull(StatePrimitive other)
        => GcdMap[Order, other.Order];

    public virtual ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) =>
        otherType is StatePrimitive primitive
            ? GetLastCommonPrimitiveAncestor(primitive)
            : Any;

    public virtual StatePrimitive GetLastCommonPrimitiveAncestor(StatePrimitive other) => LcaMap[Order, other.Order];

    public override bool Equals(object obj) => (obj as StatePrimitive)?.Name == Name;
    public override int GetHashCode() => (int)Name;
    public string Description => Name.ToString();

    private static void FillLcaGcdMaps() {
        int maxVal = 19;
        var numberToTypeMap = new[] {
            Any, //0
            Char, //1
            Bool, //2
            Ip,   //3
            Real, //4
            I96, //5
            I64, //6
            I48, //7
            I32, //8
            I24, //9
            I16, //10
            U64, //11
            U48, //12
            U32, //13
            U24, //14
            U16, //15
            U12, //16
            U8, //17
            None, //18
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
            GcdMap[i, i] = numberToTypeMap[i];
            //x _ any = x
            GcdMap[i, Any.Order] = numberToTypeMap[i];
        }

        //real (only numeric types: Real..U8, orders 4..17)
        for (int i = Real.Order; i <= U8.Order; i++)
        {
            //number ^ real = real
            LcaMap[i, Real.Order] = Real;
            //number _ real = number
            GcdMap[i, Real.Order] = numberToTypeMap[i];
        }

        //i96 (only numeric types: I96..U8, orders 5..17)
        for (int i = I96.Order; i <= U8.Order; i++)
        {
            //i96 ^ iXX = i96,   i96 ^ uXX = i96
            LcaMap[i, I96.Order] = I96;
            //i96 _ iXX = iXX,   i96 _ uXX = uXX
            GcdMap[i, I96.Order] = numberToTypeMap[i];
        }

        //all ints
        for (int anc = I64.Order; anc <= I16.Order; anc++)
        {
            for (int desc = anc + 1; desc <= I16.Order; desc++)
            {
                //I64 ^ i32 = I64
                LcaMap[desc, anc] = numberToTypeMap[anc];
                //I64 _ i32 = i32
                GcdMap[desc, anc] = numberToTypeMap[desc];
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
                GcdMap[desc, anc] = numberToTypeMap[desc];
            }
        }

        //iXX to u12,u8
        for (int i = I64.Order; i <= I16.Order; i++)
        {
            //iXX ^ u8  = iXX
            LcaMap[U8.Order, i] = numberToTypeMap[i];
            //iXX _ u8  = u8
            GcdMap[U8.Order, i] = U8;

            //iXX ^ u12  = iXX
            LcaMap[U12.Order, i] = numberToTypeMap[i];
            //iXX _ u12  = u12
            GcdMap[U12.Order, i] = U12;
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
        GcdMap[U16.Order, I16.Order] = U12;
        GcdMap[U24.Order, I16.Order] = U12;
        GcdMap[U32.Order, I16.Order] = U12;
        GcdMap[U48.Order, I16.Order] = U12;
        GcdMap[U64.Order, I16.Order] = U12;

        GcdMap[U16.Order, I24.Order] = U16;
        GcdMap[U24.Order, I24.Order] = U16;
        GcdMap[U32.Order, I24.Order] = U16;
        GcdMap[U48.Order, I24.Order] = U16;
        GcdMap[U64.Order, I24.Order] = U16;

        GcdMap[U16.Order, I32.Order] = U16;
        GcdMap[U24.Order, I32.Order] = U24;
        GcdMap[U32.Order, I32.Order] = U24;
        GcdMap[U48.Order, I32.Order] = U24;
        GcdMap[U64.Order, I32.Order] = U24;

        GcdMap[U16.Order, I48.Order] = U16;
        GcdMap[U24.Order, I48.Order] = U24;
        GcdMap[U32.Order, I48.Order] = U32;
        GcdMap[U48.Order, I48.Order] = U32;
        GcdMap[U64.Order, I48.Order] = U32;

        GcdMap[U16.Order, I64.Order] = U16;
        GcdMap[U24.Order, I64.Order] = U24;
        GcdMap[U32.Order, I64.Order] = U32;
        GcdMap[U48.Order, I64.Order] = U48;
        GcdMap[U64.Order, I64.Order] = U48;

        // None ≤ Any: GCD(None, Any) = None
        GcdMap[None.Order, Any.Order] = None;

        //a ^ b = b ^ a
        //a _ b = b _ a

        //reflect maps by diagonals
        for (int col = 0; col < maxVal; col++)
        {
            for (int row = col; row < maxVal; row++)
            {
                LcaMap[col, row] = LcaMap[row, col];
                GcdMap[col, row] = GcdMap[row, col];
            }
        }
    }
}
