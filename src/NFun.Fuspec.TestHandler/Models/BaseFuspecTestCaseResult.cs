using System;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public class BaseFuspecTestCaseResult
    {
        public BaseFuspecTestCaseResult(DateTime? totalRunning, FuspecTestCase fuspecTestCase)
        {
            TotalRunning = totalRunning;
            FuspecTestCase = fuspecTestCase;
        }

        public DateTime? TotalRunning { get; set; }
        public FuspecTestCase FuspecTestCase { get; }
    }
}