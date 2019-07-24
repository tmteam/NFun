using System;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public class BaseFuspecTestCaseResult
    {
        public BaseFuspecTestCaseResult(long totalRunning, FuspecTestCase fuspecTestCase)
        {
            TotalRunning = totalRunning;
            FuspecTestCase = fuspecTestCase;
        }

        public long TotalRunning { get; set; }
        public FuspecTestCase FuspecTestCase { get; }
    }
}