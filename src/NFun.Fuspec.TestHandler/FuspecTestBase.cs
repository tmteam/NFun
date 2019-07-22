using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FuspecTests;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler.Models;
using NFun.ParseErrors;
using NFun.Runtime;
using NUnit.Framework.Internal;

namespace NFun.Fuspec.TestHandler
{
    public class FuspecTestBase
    {
        private readonly FuspecTestCase[] _fuspecTestCases;

        public FuspecTestBase(FuspecTestCase[] fuspecTestCases)
        {
            _fuspecTestCases = fuspecTestCases 
                              ?? throw new ArgumentNullException("No fuspec test cases");
        }

        public IEnumerable<BaseFuspecTestCaseResult> RunTests()
        {
            var result = new List<BaseFuspecTestCaseResult>();
            result.AddRange(_fuspecTestCases.Select(FuspecTestCasesRunner.RunFuspecTest));
            return result;
        }

    }

    public static class FuspecTestCasesRunner
    {
        public static BaseFuspecTestCaseResult RunFuspecTest(FuspecTestCase testCase)
        {
            var stopwatch = Stopwatch.StartNew();
            BaseFuspecTestCaseResult testResult;
            FunRuntime result;

            try
            {
                result = FunBuilder.With(testCase.Script).Build();
                testResult = GoodTestResult(stopwatch);
            }
            catch (FunParseException parseException)
            {
                testResult = BadTestResult();
            }
            catch (FunRuntimeException runtimeException)
            {
                testResult = BadTestResult();
            }
            catch (Exception e)
            {
                testResult = BadTestResult();
            }

            return testResult;
        }

        private static ExceptionTestCaseResult BadTestResult(Stopwatch stopwatch)
        {
            return new ExceptionTestCaseResult(stopwatch.ElapsedMilliseconds, );
        }

        private static GoodTestCaseResult GoodTestResult(Stopwatch stopwatch)
        {
            return new GoodTestCaseResult(stopwatch.ElapsedMilliseconds, );
        }
    }

    public static class ParamsParser
    {
        public static BaseFuspecTestCaseResult WithFuspecParamsParser(this FunRuntime funRuntime)
        {
            
        }
    }

    public static class ParamsInCalculator
    {
        
    }

    public static class ParamsOutCalculator
    {
        
    }
}
