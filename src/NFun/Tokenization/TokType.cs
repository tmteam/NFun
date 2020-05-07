namespace NFun.Tokenization
{
    public enum TokType
    {
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
        /// <summary>
        /// 1L
        /// </summary>
        LongNumber,
        Plus,
        Minus,
        Div,
        /// <summary>
        /// Division reminder "%"
        /// </summary>
        Rema,
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
        /// not used
        /// </summary>
        ArrConcat,
        In,
        BitOr,
        BitAnd,
        BitXor,
        BitShiftLeft,
        BitShiftRight,
        BitInverse,
        Id,
        /// <summary>
        /// =
        /// </summary>
        Def,
        
        Equal,
        NotEqual,
        And,
        Or,
        Xor,
        Not,
        Less,
        More,
        LessOrEqual,
        MoreOrEqual,
        
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
        /// '--'
        /// </summary>
        Attribute,
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
        PipeForward,
        /// <summary>
        /// ->
        /// </summary>
        AnonymFun,
        
        Reserved
    }
}