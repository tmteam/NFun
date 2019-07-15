using System;

namespace Nfun.Fuspec.Parser.FuspecParserErrors
{
    public class FuspecParserException : ArgumentException
    {
        public FuspecParserError[] Errors { get; }
        public FuspecParserException( FuspecParserError[] errors)
            : base("One or more errors occured...")
        {
            Errors = errors;
        }
    }
}