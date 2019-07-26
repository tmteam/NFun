using System;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public sealed class GoodTestCaseResult:BaseFuspecTestCaseResult
    {
        public GoodTestCaseResult(TimeSpan totalRunning, FuspecTestCase fuspecTestCase) 
            : base(totalRunning, fuspecTestCase)
        {
        }
    }
}