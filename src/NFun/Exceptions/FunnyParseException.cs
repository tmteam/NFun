using System;
using NFun.Tokenization;

namespace NFun.Exceptions {

public class FunnyParseException : Exception {
    public int Code { get; }
    public Interval Interval { get; }
    public int Start => Interval.Start;
    public int End => Interval.Finish;

    internal FunnyParseException(int code, string message, Interval interval) : base(message) {
        Code = code;
        Interval = interval;
    }

    internal FunnyParseException(int code, string message, int start, int end) : base(message) {
        Code = code;
        Interval = new Interval(start, end);
    }

    public override string ToString()
        => $"[FU{Code}] {base.ToString()}";

    //todo the error is not implemented yet, but it should -)
    internal static FunnyParseException ErrorStubToDo(string message) => new(-1, message, 0, 0);
}

}