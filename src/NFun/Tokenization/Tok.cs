namespace NFun.Tokenization
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
        public int FinishInString => Type == TokType.Eof ? Finish - 1 : Finish;
        
        public int Start => Finish - Value?.Length??0;
        public int StartInString => Type == TokType.Eof ? Start - 1 : Start;

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
}