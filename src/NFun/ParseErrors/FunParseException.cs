using System;
using System.Linq;
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
        public FunParseException(string message):base(message)
        {
            Interval = new Interval(-1,-1);
        }
    }
}