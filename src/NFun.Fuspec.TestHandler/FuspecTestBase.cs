using System;
using System.Collections.Generic;
using System.Linq;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler.Models;


namespace NFun.Fuspec.TestHandler
{
    public static class FuspecTestBase
    {
        public static IEnumerable<BaseFuspecTestCaseResult> RunTests(FuspecTestCase[] fuspecTestCases)
        {
            if (fuspecTestCases == null)
                throw new ArgumentNullException($"{nameof(fuspecTestCases)} is null");

            if (!fuspecTestCases.Any())
                throw new ArgumentException($"{nameof(fuspecTestCases)} contains no elements");

            var testCaseResults = new List<BaseFuspecTestCaseResult>();
            var testCasesRunner = new FuspecTestCasesRunner();
            testCaseResults.AddRange(fuspecTestCases.AsParallel().Select(testCasesRunner.RunFuspecTest));
            return testCaseResults;
        }
    }
}
