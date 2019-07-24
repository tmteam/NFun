using System;
using System.Diagnostics;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler.Models;
using NFun.ParseErrors;
using NFun.Runtime;

namespace NFun.Fuspec.TestHandler
{
    public static class FuspecTestCasesRunner
    {
        public static BaseFuspecTestCaseResult RunFuspecTest(FuspecTestCase testCase)
        {
            var stopwatch = Stopwatch.StartNew();
            BaseFuspecTestCaseResult testResult;

            try
            {
                //todo parse in and out funResult
                FunRuntime funResult = FunBuilder.With(testCase.Script).Build();
                testResult = TestResultConverter.GoodTestResult(stopwatch.ElapsedMilliseconds, testCase);
            }
            catch (FunParseException parseException)
            {
                testResult = TestResultConverter.BadTestResult(stopwatch.ElapsedMilliseconds, testCase, parseException);
            }
            catch (FunRuntimeException runtimeException)
            {
                testResult = TestResultConverter.BadTestResult(stopwatch.ElapsedMilliseconds, testCase, runtimeException);
            }
            catch (Exception e)
            {
                testResult = TestResultConverter.BadTestResult(stopwatch.ElapsedMilliseconds, testCase, e);
            }

            return testResult;
        }


    }
}