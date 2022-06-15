using System;

namespace NFun.Tokenization; 

/// <summary>
/// Nfun lang token
/// </summary>
public class Tok {
    private Tok(TokType type, string value, Interval interval) {
        Value = value;
        Type = type;
        Interval = interval;
    }

    public static Tok New(TokType type, int start, int finish)
        => new(type, String.Empty, new Interval(start, finish));
    
    public static Tok New(TokType type, string value, int start, int finish)
        => new(type, value, new Interval(start, finish));
    public static Tok SubString(string allString, TokType type,  int start, int exclusiveFinish)
        => new(type, allString.Substring(start, exclusiveFinish-start), new Interval(start, exclusiveFinish));

    public bool Is(TokType type) => type == Type;
    public string Value { get; }
    public TokType Type { get; }
    public Interval Interval { get; }
    public int Finish => Interval.Finish;
    public int Start => Interval.Start;

    public override string ToString() {
        if (Type == TokType.Id) return $"\"{Value}\"";
        if (Type == TokType.RealNumber) return $"'{Value}'";
        if (Value == null || Value.Length == 1)
            return Type.ToString();
        else
            return $"{Type}({Value})";
    }
}