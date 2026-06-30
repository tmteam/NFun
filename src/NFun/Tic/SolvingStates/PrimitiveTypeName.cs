namespace NFun.Tic.SolvingStates;

public enum PrimitiveTypeName {
    Any = 0,
    Char = 1 << 6,
    Bool = 2 << 6,
    Ip   = 3 << 6,

    // Numeric lattice — top to bottom:
    //   Real (=float64) → F32 → { I96 → I64 → I48 → I32 → I24 → I16 → I12 → I8
    //                             U64 → U48 → U32 → U24 → U16 → U12 → U8 → U4 }
    //
    // Abstract types (TIC-internal, no runtime representation):
    //   I96 / I48 / I24 / I12 — signed mid-points
    //   U48 / U24 / U12 / U4  — unsigned mid-points (U4 is lattice bottom)
    Real = 4 << 6 | _isNumber,
    F32  = 5 << 6 | _isNumber,
    I96  = 6 << 6 | _isNumber | _isAbstract,
    I64  = 7 << 6 | _isNumber,
    I48  = 8 << 6 | _isNumber | _isAbstract,
    I32  = 9 << 6 | _isNumber,
    I24  = 10 << 6 | _isNumber | _isAbstract,
    I16  = 11 << 6 | _isNumber,
    I12  = 12 << 6 | _isNumber | _isAbstract,
    I8   = 13 << 6 | _isNumber,
    U64  = 14 << 6 | _isNumber,
    U48  = 15 << 6 | _isNumber | _isAbstract,
    U32  = 16 << 6 | _isNumber,
    U24  = 17 << 6 | _isNumber | _isAbstract,
    U16  = 18 << 6 | _isNumber,
    U12  = 19 << 6 | _isNumber | _isAbstract,
    U8   = 20 << 6 | _isNumber,
    U4   = 21 << 6 | _isNumber | _isAbstract,

    None = 22 << 6,

    // ReSharper disable once InconsistentNaming
    _isAbstract = 1 << 3,
    // ReSharper disable once InconsistentNaming
    _isNumber = 1 << 2
}
