namespace NFun.Tic.SolvingStates;

public enum PrimitiveTypeName {
    Any = 0,
    Char = 1 << 6,
    Bool = 2 << 6,
    Ip   = 3 << 6,

    // Numeric lattice — top to bottom:
    //   Real → I96 → I64 → I48 → I32 → I24 → I16 → I12 → I8
    //                                                       \
    //                  U64 → U48 → U32 → U24 → U16 → U12 → U8 → U4
    //
    // Abstract types (TIC-internal, no runtime representation):
    //   I96 / I48 / I24 / I12 mid-points on signed branch
    //   U48 / U24 / U12 / U4  mid-points on unsigned branch (U4 is the lattice bottom)
    Real = 4 << 6 | _isNumber,
    I96  = 5 << 6 | _isNumber | _isAbstract,
    I64  = 6 << 6 | _isNumber,
    I48  = 7 << 6 | _isNumber | _isAbstract,
    I32  = 8 << 6 | _isNumber,
    I24  = 9 << 6 | _isNumber | _isAbstract,
    I16  = 10 << 6 | _isNumber,
    I12  = 11 << 6 | _isNumber | _isAbstract,
    I8   = 12 << 6 | _isNumber,
    U64  = 13 << 6 | _isNumber,
    U48  = 14 << 6 | _isNumber | _isAbstract,
    U32  = 15 << 6 | _isNumber,
    U24  = 16 << 6 | _isNumber | _isAbstract,
    U16  = 17 << 6 | _isNumber,
    U12  = 18 << 6 | _isNumber | _isAbstract,
    U8   = 19 << 6 | _isNumber,
    U4   = 20 << 6 | _isNumber | _isAbstract,

    None = 21 << 6,

    // ReSharper disable once InconsistentNaming
    _isAbstract = 1 << 3,
    // ReSharper disable once InconsistentNaming
    _isNumber = 1 << 2
}
