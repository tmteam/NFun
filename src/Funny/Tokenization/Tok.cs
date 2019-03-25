namespace Funny.Tokenization
{
    public class Tok
    {
         Tok(TokType type,string value,int finish)
        {
            Value = value;
            Finish = finish;
            Type = type;
        }
         
        public static Tok New(TokType type, int finish)
            => new Tok(type,"",finish);

        public static Tok New(TokType  type,  string value, int finish) 
            => new Tok(type,value,finish);

        public bool Is(TokType type)
            => type == Type;
        public string Value { get; }
        public TokType Type { get; }
        public int Finish { get;  }
        
        public override string ToString()
        {
            if(Type== TokType.Id)    
                return $"\"{Value}\"";
            if (Type == TokType.Number)
                return $"'{Value}'";
            if (Value == null || Value.Length==1)
                return Type.ToString();
            else 
                return $"{Type}({Value})";
        }

    }

    public enum TokType: int
    {
        NewLine,
        If,
        Else,
        Then,
        Number,
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
        /// ::
        /// </summary>
        ArrUnite,
        BitOr,
        BitAnd,
        BitXor,
        BitShiftLeft,
        BitShiftRight,
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
        NotAToken,

        True,
        False,
        IsTypeOf,
        
        TextType,
        IntType,
        RealType,
        BoolType,
        AnythingType,
        /// <summary>
        /// |>
        /// </summary>
        PipeForward,
        /// <summary>
        /// =>
        /// </summary>
        AnonymFun
    }
    
}