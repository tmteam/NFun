using System;
using NFun.Tokenization;

namespace NFun.Exceptions;

public class FunnyParseException : Exception {
    public int ErrorCode { get; }
    public Interval Interval { get; }
    public int Start => Interval.Start;
    public int End => Interval.Finish;

    internal FunnyParseException(int code, string message, Interval interval) : base(message) {
        ErrorCode = code;
        Interval = interval;
    }

    internal FunnyParseException(int code, string message, int start, int end) : base(message) {
        ErrorCode = code;
        Interval = new Interval(start, end);
    }

    public override string ToString() => $"[FU{ErrorCode}] {Message}";
}
