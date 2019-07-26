using System;
using Nfun.Fuspec.Parser.Model;

namespace NFun.Fuspec.TestHandler.Models
{
    public class BaseFuspecTestCaseResult
    {
        public BaseFuspecTestCaseResult(TimeSpan totalRunning, FuspecTestCase fuspecTestCase)
        {
            TotalRunning = totalRunning;
            FuspecTestCase = fuspecTestCase;
        }

        public TimeSpan TotalRunning { get; }
        public FuspecTestCase FuspecTestCase { get; }
    }
}