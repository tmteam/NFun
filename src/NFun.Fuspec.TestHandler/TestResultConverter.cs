using System;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.TestHandler
{
    public static class TestResultConverter
    {
        public static ExceptionTestCaseResult BadTestResult(long ms, FuspecTestCase testCase, Exception e) 
            => new ExceptionTestCaseResult(ms, testCase, e);

        public static GoodTestCaseResult GoodTestResult(long ms, FuspecTestCase testCase) 
            => new GoodTestCaseResult(ms, testCase);
    }
}