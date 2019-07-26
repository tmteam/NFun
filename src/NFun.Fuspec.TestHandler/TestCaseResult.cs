using System;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.TestHandler
{
    public static class TestCaseResult
    {
        public static ExceptionTestCaseResult BadTestResult(TimeSpan ms, FuspecTestCase testCase, Exception e) 
            => new ExceptionTestCaseResult(ms, testCase, e);

        public static GoodTestCaseResult GoodTestResult(TimeSpan ms, FuspecTestCase testCase) 
            => new GoodTestCaseResult(ms, testCase);
    }
}