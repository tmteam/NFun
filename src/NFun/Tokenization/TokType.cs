namespace NFun.Tokenization;

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
    ParenthObr,
    /// <summary>
    /// )
    /// </summary>
    ParenthCbr,
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
    /// <summary>Format specifier after ':' in interpolation: '{x:0.00}'</summary>
    FormatSpec,
    /// <summary>Alignment direction in interpolation: > (right), &lt; (left), ^ (center)</summary>
    AlignLeft,
    AlignRight,
    AlignCenter,
    /// <summary>
    /// 192.168.0.1
    /// </summary>
    IpAddress,
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
    CharType,
    AnythingType,
    /// <summary>
    /// .
    /// </summary>
    Dot,
    /// <summary>
    /// 'rule'
    /// </summary>
    Rule,
    /// <summary>
    /// 'default'
    /// </summary>
    Default,
    Reserved,
    /// <summary>
    /// /'x'
    /// </summary>
    CharLiteral,
    /// <summary>
    /// none literal
    /// </summary>
    None,
    /// <summary>
    /// '?'
    /// </summary>
    Question,
    /// <summary>
    /// ??
    /// </summary>
    NullCoalesce,
    /// <summary>
    /// ?.
    /// </summary>
    SafeAccess,
    /// <summary>
    /// ! (postfix)
    /// </summary>
    ForceUnwrap,
    /// <summary>
    /// -> (return type arrow)
    /// </summary>
    Arrow,
    /// <summary>
    /// Superscript digit ²³⁴⁵⁶⁷⁸⁹ (postfix power)
    /// </summary>
    Superscript,
    /// <summary>
    /// ... (spread/params)
    /// </summary>
    Spread,
    /// <summary>
    /// 'type' keyword for named type definitions
    /// </summary>
    TypeKeyword,

}
