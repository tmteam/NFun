namespace NFun.Tic.SolvingStates;

public class StatePrimitive : ITypeState, ITicNodeState {
    private const int LatticeSize = 23;

    private static readonly StatePrimitive[,] LcaMap;
    private static readonly StatePrimitive[,] GcdMap;

    static StatePrimitive() {
        LcaMap = new StatePrimitive [LatticeSize, LatticeSize];
        GcdMap = new StatePrimitive [LatticeSize, LatticeSize];

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
    public static StatePrimitive F32 { get; } = new(PrimitiveTypeName.F32);
    public static StatePrimitive I96 { get; } = new(PrimitiveTypeName.I96);
    public static StatePrimitive I64 { get; } = new(PrimitiveTypeName.I64);
    public static StatePrimitive I48 { get; } = new(PrimitiveTypeName.I48);
    public static StatePrimitive I32 { get; } = new(PrimitiveTypeName.I32);
    public static StatePrimitive I24 { get; } = new(PrimitiveTypeName.I24);
    public static StatePrimitive I16 { get; } = new(PrimitiveTypeName.I16);
    public static StatePrimitive I12 { get; } = new(PrimitiveTypeName.I12);
    public static StatePrimitive I8 { get; } = new(PrimitiveTypeName.I8);
    public static StatePrimitive U64 { get; } = new(PrimitiveTypeName.U64);
    public static StatePrimitive U48 { get; } = new(PrimitiveTypeName.U48);
    public static StatePrimitive U32 { get; } = new(PrimitiveTypeName.U32);
    public static StatePrimitive U24 { get; } = new(PrimitiveTypeName.U24);
    public static StatePrimitive U16 { get; } = new(PrimitiveTypeName.U16);
    public static StatePrimitive U12 { get; } = new(PrimitiveTypeName.U12);
    public static StatePrimitive U8 { get; } = new(PrimitiveTypeName.U8);
    public static StatePrimitive U4 { get; } = new(PrimitiveTypeName.U4);
    public static StatePrimitive None { get; } = new(PrimitiveTypeName.None);

    /// <summary>True for TIC-internal types that have no runtime representation
    /// (I96, I48, I24, I12, U48, U24, U12, U4).</summary>
    public bool IsAbstract => Name.HasFlag(PrimitiveTypeName._isAbstract);

    /// <summary>
    /// Narrowest concrete type that can hold all values of this abstract type.
    /// I96→Real, I48→I64, I24→I32, I12→I16, U48→U64, U24→U32, U12→U16, U4→U8.
    /// </summary>
    public StatePrimitive ConcreteAncestor => Name switch {
        PrimitiveTypeName.I96 => Real,
        PrimitiveTypeName.I48 => I64,
        PrimitiveTypeName.I24 => I32,
        PrimitiveTypeName.I12 => I16,
        PrimitiveTypeName.U48 => U64,
        PrimitiveTypeName.U24 => U32,
        PrimitiveTypeName.U12 => U16,
        PrimitiveTypeName.U4  => U8,
        _ => this, // already concrete
    };

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
        var numberToTypeMap = new[] {
            Any,  //0
            Char, //1
            Bool, //2
            Ip,   //3
            Real, //4
            F32,  //5
            I96,  //6
            I64,  //7
            I48,  //8
            I32,  //9
            I24,  //10
            I16,  //11
            I12,  //12
            I8,   //13
            U64,  //14
            U48,  //15
            U32,  //16
            U24,  //17
            U16,  //18
            U12,  //19
            U8,   //20
            U4,   //21
            None, //22
        };

        //by default - any lca returns any
        for (int i = 0; i < LatticeSize; i++)
            for (int j = 0; j < LatticeSize; j++)
                LcaMap[i, j] = Any;

        //any,char,bool and self
        for (int i = 0; i < LatticeSize; i++)
        {
            //x ^ x = x
            LcaMap[i, i] = numberToTypeMap[i];
            //x _ x = x
            GcdMap[i, i] = numberToTypeMap[i];
            //x _ any = x
            GcdMap[i, Any.Order] = numberToTypeMap[i];
        }

        //real (only numeric types: Real..U4, orders 4..20)
        for (int i = Real.Order; i <= U4.Order; i++)
        {
            //number ^ real = real
            LcaMap[i, Real.Order] = Real;
            //number _ real = number
            GcdMap[i, Real.Order] = numberToTypeMap[i];
        }

        //F32 vs integers: LCA=F32, GCD=int.  (F32 vs Real handled by the Real loop above.)
        for (int i = I96.Order; i <= U4.Order; i++)
        {
            LcaMap[i, F32.Order] = F32;
            GcdMap[i, F32.Order] = numberToTypeMap[i];
        }

        //i96 (only integer types: I96..U4, orders 6..21)
        for (int i = I96.Order; i <= U4.Order; i++)
        {
            //i96 ^ iXX = i96,   i96 ^ uXX = i96
            LcaMap[i, I96.Order] = I96;
            //i96 _ iXX = iXX,   i96 _ uXX = uXX
            GcdMap[i, I96.Order] = numberToTypeMap[i];
        }

        //all signed ints (I64..I8): wider ^ narrower = wider; wider _ narrower = narrower
        for (int anc = I64.Order; anc <= I8.Order; anc++)
        {
            for (int desc = anc + 1; desc <= I8.Order; desc++)
            {
                LcaMap[desc, anc] = numberToTypeMap[anc];
                GcdMap[desc, anc] = numberToTypeMap[desc];
            }
        }

        //all unsigned ints (U64..U4)
        for (int anc = U64.Order; anc <= U4.Order; anc++)
        {
            for (int desc = anc + 1; desc <= U4.Order; desc++)
            {
                LcaMap[desc, anc] = numberToTypeMap[anc];
                GcdMap[desc, anc] = numberToTypeMap[desc];
            }
        }

        // Signed × unsigned cross-table — programmatic from bit widths.
        // LCA(U_m, I_n): smallest signed I_k with k >= max(n, m+1) (need extra bit for sign).
        // GCD(U_m, I_n): largest unsigned U_j with j <= min(m, n-1)  (common safe subset).
        //
        // U4 is the lattice bottom: nominal range 0..127 (= common subset of I8 and U8).
        // Width=7 in the formula encodes "fits in I8 and U8" — anything narrower would be
        // strictly weaker without representational benefit. Naming kept "U4" per spec.
        var signed = new[] {
            (I64, 64), (I48, 48), (I32, 32), (I24, 24), (I16, 16), (I12, 12), (I8, 8)
        };
        var unsigned = new[] {
            (U64, 64), (U48, 48), (U32, 32), (U24, 24), (U16, 16), (U12, 12), (U8, 8), (U4, 7)
        };

        foreach (var (uType, uWidth) in unsigned)
            foreach (var (iType, iWidth) in signed)
            {
                // LCA: smallest signed type k with k >= max(iWidth, uWidth + 1)
                var needed = System.Math.Max(iWidth, uWidth + 1);
                var lca = needed <= 8  ? I8
                        : needed <= 12 ? I12
                        : needed <= 16 ? I16
                        : needed <= 24 ? I24
                        : needed <= 32 ? I32
                        : needed <= 48 ? I48
                        : needed <= 64 ? I64
                        :                I96;
                LcaMap[uType.Order, iType.Order] = lca;

                // GCD: largest unsigned type j with j <= min(uWidth, iWidth - 1)
                var allowed = System.Math.Min(uWidth, iWidth - 1);
                var gcd = allowed >= 64 ? U64
                        : allowed >= 48 ? U48
                        : allowed >= 32 ? U32
                        : allowed >= 24 ? U24
                        : allowed >= 16 ? U16
                        : allowed >= 12 ? U12
                        : allowed >= 8  ? U8
                        :                 U4;
                GcdMap[uType.Order, iType.Order] = gcd;
            }

        // None ≤ Any: GCD(None, Any) = None
        GcdMap[None.Order, Any.Order] = None;

        //a ^ b = b ^ a, a _ b = b _ a — reflect maps by diagonal
        for (int col = 0; col < LatticeSize; col++)
        {
            for (int row = col; row < LatticeSize; row++)
            {
                LcaMap[col, row] = LcaMap[row, col];
                GcdMap[col, row] = GcdMap[row, col];
            }
        }
    }
}
