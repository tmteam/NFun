using System;
using NFun.Tokenization;

namespace NFun.ParseErrors
{
    public class FunParseException : Exception
    {
        public int Code { get; }
        public Interval Interval { get; }
        public int Start => Interval.Start;
        public int End => Interval.Finish;

        public FunParseException(int code, string message, Interval interval): base(message)
        {
            Code = code;
            Interval = interval;
        }
        public FunParseException(int code, string message, int start, int end): base(message)
        {
            Code = code;
            Interval = new Interval(start,end);
        }

        public override string ToString()
        {
            return $"[FU{Code}] {base.ToString()}";
        }
        //todo the error is not implemented yet, but it should -)
        public static FunParseException ErrorStubToDo(string varAlreadyDeclared)
        {
            return new FunParseException(-1, varAlreadyDeclared, 0,0);
        }
    }
}