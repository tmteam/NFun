using System.Collections.Generic;
using System.Linq;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.ConsoleTestHandler
{
    public static class RunFuspecTests
    {

        public static void Run()
        {
            var inputString = FuspecFromDirectoriesGenerator.TryGetJoinedString();
            var testCases = FuspecCasesGenerator.Generate(inputString).RunTests(out long elapsedMilliseconds);
            var testsResult = GetTestsResult(testCases, out List<BaseFuspecTestCaseResult> testWithErrors);

            FileAndConsoleLogger.Write(elapsedMilliseconds, testsResult, testWithErrors);
        }

        private static TestResults GetTestsResult(List<BaseFuspecTestCaseResult> testsResult, out List<BaseFuspecTestCaseResult> testWithErrors)
        {
            testWithErrors = testsResult.Where(t => t is ExceptionTestCaseResult).ToList();
            List<BaseFuspecTestCaseResult> goodTests = testsResult.Where(t => t is GoodTestCaseResult).ToList();
            double averageTime = testsResult.Average(t => t.TotalRunning.TotalMilliseconds);

            return new TestResults(testsResult.Count(), testWithErrors.Count(), goodTests.Count(), averageTime);
        }
    }
}