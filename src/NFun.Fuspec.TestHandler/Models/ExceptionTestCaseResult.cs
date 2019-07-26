using System;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public sealed class ExceptionTestCaseResult:BaseFuspecTestCaseResult
    {
        public Exception Exception { get; }

        public ExceptionTestCaseResult(TimeSpan totalRunning, FuspecTestCase fuspecTestCase, Exception exception) 
            : base(totalRunning, fuspecTestCase)
        {
            Exception = exception;
        }
    }
}