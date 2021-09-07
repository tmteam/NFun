namespace NFun.Tokenization {

public enum TokType {
    NewLine,
    If,
    Else,
    Then,
    /// <summary>
    /// 1,2,3
    /// </summary>
    IntNumber,
    /// <summary>
    /// 0xff, 0bff
    /// </summary>
    HexOrBinaryNumber,
    /// <summary>
    /// 1.0
    /// </summary>
    RealNumber,
    Plus,
    Minus,
    /// <summary>
    /// /
    /// </summary>
    Div,
    /// <summary>
    /// //
    /// </summary>
    DivInt,
    /// <summary>
    /// Division reminder "%"
    /// </summary>
    Rema,
    /// <summary>
    /// *
    /// </summary>
    Mult,
    /// <summary>
    /// Pow "^"
    /// </summary>
    Pow,
    /// <summary>
    /// (
    /// </summary>
    Obr,
    /// <summary>
    /// )
    /// </summary>
    Cbr,
    /// <summary>
    ///  [ 
    /// </summary>
    ArrOBr,
    /// <summary>
    ///  ] 
    /// </summary>
    ArrCBr,
    /// <summary>
    /// {
    /// </summary>
    FiObr,
    /// <summary>
    ///  }
    /// </summary>
    FiCbr,
    /// <summary>
    /// |
    /// </summary>
    BitOr,
    /// <summary>
    /// &
    /// </summary>
    BitAnd,
    /// <summary>
    /// ^
    /// </summary>
    BitXor,
    /// <summary>
    /// <<
    /// </summary>
    BitShiftLeft,
    /// <summary>
    /// >>
    /// </summary>
    BitShiftRight,
    BitInverse,
    /// <summary>
    /// x, y, myFun... etc
    /// </summary>
    Id,
    /// <summary>
    /// =
    /// </summary>
    Def,
    /// <summary>
    /// ==
    /// </summary>
    Equal,
    /// <summary>
    /// !=
    /// </summary>
    NotEqual,
    And,
    Or,
    Xor,
    Not,
    Less,
    More,
    LessOrEqual,
    MoreOrEqual,
    In,
    Eof,
    /// <summary>
    /// ',' symbol
    /// </summary>
    Sep,
    Text,
    TextOpenInterpolation,
    TextMidInterpolation,
    TextCloseInterpolation,
    NotAToken,
    True,
    False,
    /// <summary>
    /// ':'
    /// </summary>
    Colon,
    /// <summary>
    /// '@'
    /// </summary>
    MetaInfo,
    /// <summary>
    /// '..'
    /// </summary>
    TwoDots,
    TextType,
    Int16Type,
    Int32Type,
    Int64Type,
    UInt8Type,
    UInt16Type,
    UInt32Type,
    UInt64Type,
    RealType,
    BoolType,
    AnythingType,
    /// <summary>
    /// .
    /// </summary>
    Dot,
    /// <summary>
    /// ->
    /// </summary>
    Arrow,
    FunRule,
    Reserved
}

}