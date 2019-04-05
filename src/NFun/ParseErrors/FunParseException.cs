using System;
using System.Linq;

namespace NFun.ParseErrors
{
    public class FunParseException : Exception
    {
        public int Code { get; }
        public int Start { get; }
        public int End { get; }

        public FunParseException(int code, string message, int start, int end): base(message)
        {
            Code = code;
            Start = start;
            End = end;
        }   
        public FunParseException(string message):base(message)
        {
            Start = -1;
            End = -1;
        }
    }
}