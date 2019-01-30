using System.Collections.Generic;

namespace Funny.Take2
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
            if (Type == TokType.Uint)
                return $"'{Value}'";
            if (Value == null || Value.Length==1)
                return Type.ToString();
            else 
                return $"{Type}({Value})";
        }

    }

    public enum TokType
    {
        NewLine,
        If,
        Else,
        Then,
        Uint,
        Plus,
        Minus,
        Div,
        Mult,
        Pow,
        Obr,
        Cbr,
        Abc,
        Id,
        Equal,
        Eof,
        NotAToken
    }
    
}