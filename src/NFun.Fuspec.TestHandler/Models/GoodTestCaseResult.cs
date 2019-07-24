using System;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public sealed class GoodTestCaseResult:BaseFuspecTestCaseResult
    {
        public GoodTestCaseResult(long totalRunning, FuspecTestCase fuspecTestCase) 
            : base(totalRunning, fuspecTestCase)
        {
        }
    }
}