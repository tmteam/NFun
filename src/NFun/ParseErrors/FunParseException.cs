using System;

namespace NFun.ParseErrors
{
    public class FunParseException : Exception
    {
        public int Start { get; }
        public int End { get; }

        public FunParseException(string message, int start, int end): base(message)
        {
            Start = start;
            End = end;
        }   
        public FunParseException(string message):base(message)
        {
            
        }
    }
}