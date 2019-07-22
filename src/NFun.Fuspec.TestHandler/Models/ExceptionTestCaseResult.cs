using System;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public sealed class ExceptionTestCaseResult:BaseFuspecTestCaseResult
    {
        public Exception Exception { get; }
        public FuspecParserError FuspecParserError { get; }

        public ExceptionTestCaseResult(DateTime? totalRunning, FuspecTestCase fuspecTestCase, Exception exception, FuspecParserError fuspecParserError) 
            : base(totalRunning, fuspecTestCase)
        {
            Exception = exception;
            FuspecParserError = fuspecParserError;
        }
    }
}