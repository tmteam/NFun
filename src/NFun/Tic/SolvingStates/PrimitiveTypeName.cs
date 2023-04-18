namespace NFun.Tic.SolvingStates; 

public enum PrimitiveTypeName {
    Any = 0,
    Char = 1 << 6,
    Bool = 2 << 6,
    Ip   = 3 << 6,

    Real = 4 << 6 | _isNumber,
    I96 = 5 << 6 | _isNumber | _isAbstract,
    I64 = 6 << 6 | _isNumber,
    I48 = 7 << 6 | _isNumber | _isAbstract,
    I32 = 8 << 6 | _isNumber,
    I24 = 9 << 6 | _isNumber | _isAbstract,
    I16 = 10 << 6 | _isNumber,
    U64 = 11 << 6 | _isNumber,
    U48 = 12 << 6 | _isNumber | _isAbstract,
    U32 = 13 << 6 | _isNumber,
    U24 = 14 << 6 | _isNumber | _isAbstract,
    U16 = 15 << 6 | _isNumber,
    U12 = 16 << 6 | _isNumber | _isAbstract,
    U8 = 17 << 6 | _isNumber,

    // ReSharper disable once InconsistentNaming
    _isAbstract = 1 << 3,
    // ReSharper disable once InconsistentNaming
    _isNumber = 1 << 2
}