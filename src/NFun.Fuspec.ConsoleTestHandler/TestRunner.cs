using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.ConsoleTestHandler
{
    public static class TestRunner
    {
        public static List<BaseFuspecTestCaseResult> RunTests(this FuspecTestCase[] testCases, out long elapsedTestRunningMs)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
           
            List<BaseFuspecTestCaseResult> testsResult = FuspecTestBase.RunTests(testCases).ToList();
            elapsedTestRunningMs = stopwatch.ElapsedMilliseconds;

            return testsResult;
        }
    }
}