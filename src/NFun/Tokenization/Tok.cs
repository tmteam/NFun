namespace NFun.Tokenization
{
    public class Tok
    {
         Tok(TokType type,string value,object content, Interval interval)
         {
             Content = content;
            Value = value;
            Type = type;
            Interval = interval;
        }
         
        public static Tok New(TokType type, int start, int finish)
            => new Tok(type,"",null, new Interval(start,finish));

        public static Tok New(TokType  type,  string value,int start,  int finish) 
            => new Tok(type,value,null, new Interval(start,finish));

        public static Tok New(TokType type, object content, int start, int finish)
            => new Tok(type, content.ToString(), content, new Interval(start, finish));

        public bool Is(TokType type)
            => type == Type;
        public object Content { get; }
        public string Value { get; }
        public TokType Type { get; }
        public Interval Interval { get; }
        public int Finish => Interval.Finish;
 
        public int Start => Interval.Start;

        public override string ToString()
        {
            if(Type== TokType.Id)        return $"\"{Value}\"";
            if (Type == TokType.RealNumber)  return $"'{Value}'";
            if (Value == null || Value.Length==1)
                return Type.ToString();
            else 
                return $"{Type}({Value})";
        }

    }
}