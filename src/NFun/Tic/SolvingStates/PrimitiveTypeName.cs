namespace NFun.Tic.SolvingStates {

public enum PrimitiveTypeName {
    Any = 0,
    Char = 1 << 6,
    Bool = 2 << 6,
    Real = 3 << 6 | _isNumber,
    I96 = 4 << 6 | _isNumber | _isAbstract,
    I64 = 5 << 6 | _isNumber,
    I48 = 6 << 6 | _isNumber | _isAbstract,
    I32 = 7 << 6 | _isNumber,
    I24 = 8 << 6 | _isNumber | _isAbstract,
    I16 = 9 << 6 | _isNumber,
    U64 = 10 << 6 | _isNumber,
    U48 = 11 << 6 | _isNumber | _isAbstract,
    U32 = 12 << 6 | _isNumber,
    U24 = 13 << 6 | _isNumber | _isAbstract,
    U16 = 14 << 6 | _isNumber,
    U12 = 15 << 6 | _isNumber | _isAbstract,
    U8 = 16 << 6 | _isNumber,

    // ReSharper disable once InconsistentNaming
    _isAbstract = 1 << 3,
    // ReSharper disable once InconsistentNaming
    _isNumber = 1 << 2
}

}