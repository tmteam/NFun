using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;
using NFun.Fuspec.TestHandler;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.ConsoleTestHandler
{
    public class RunFuspecTestsFromDirectory
    {
        private const string FileExtension = "*.fuspec";
        private const string BaseResultName = "LastRunResult.txt";

        public void Run()
        {
            var inputString = GetJoinedStringFromFiles();
            var testsResult = RunTests(inputString, out long elapsedMilliseconds);
            var exceptionsNumber = GetTestsResult(testsResult, out var testResult);

            DisplayAndWriteTestsResult(elapsedMilliseconds, testResult, exceptionsNumber);
        }

        private static List<BaseFuspecTestCaseResult> RunTests(string inputString, out long elapsedMilliseconds)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            FuspecTestCase[] testCases = GenerateFuspecTestCases(inputString);
            List<BaseFuspecTestCaseResult> testsResult = FuspecTestBase.RunTests(testCases).ToList();
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            return testsResult;
        }

        private static List<BaseFuspecTestCaseResult> GetTestsResult(List<BaseFuspecTestCaseResult> testsResult, out TestResults testResult)
        {
            List<BaseFuspecTestCaseResult> testsWithErrors = testsResult.Where(t => t is ExceptionTestCaseResult).ToList();
            List<BaseFuspecTestCaseResult> goodTests = testsResult.Where(t => t is GoodTestCaseResult).ToList();
            double averageTime = testsResult.Average(t => t.TotalRunning.TotalMilliseconds);

            testResult = new TestResults(testsResult.Count(), testsWithErrors.Count(), goodTests.Count(), averageTime);

            return testsWithErrors;
        }

        private void DisplayAndWriteTestsResult(long ms, TestResults testResult, List<BaseFuspecTestCaseResult> exceptionsNumber)
        {
            WriteMessage($"Total time, ms:\t{ms}\r\n" +
                         $"Average time, ms:\t{testResult.AverageTime}\r\n " +
                         $"Total tests:\t{testResult.TotalTestsCount}\r\n " +
                         $"Total good tests:\t{testResult.TotalGoodResult}\r\n " +
                         $"Total bad tests:\t{testResult.TotalExceptionsCount}" +
                         $"\r\n\r\n");

            foreach (var baseFuspecTestCaseResult in exceptionsNumber)
            {
                var testCase = (ExceptionTestCaseResult) baseFuspecTestCaseResult;
                WriteMessage($"[FAILED] {testCase.FuspecTestCase.Name} ({testCase.TotalRunning.TotalMilliseconds} ms)\r\n" +
                             $"{testCase.Exception.Message}" +
                             $"\r\n\r\n");
            }
        }

        private static string GetJoinedStringFromFiles()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            IEnumerable<string> files = Directory.EnumerateFiles(currentDirectory, FileExtension, SearchOption.AllDirectories);

            List<string> tests = files.Select(File.ReadAllText).ToList();
            return string.Join(string.Empty, tests);
        }

        private void WriteMessage(string message)
        {
            Console.WriteLine(message);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), BaseResultName);
            File.AppendAllText(filePath, message);
        }

        private static Stream GenerateStreamFromString(string inputString) 
            => new MemoryStream(Encoding.UTF8.GetBytes(inputString));

        private static FuspecTestCase[] GenerateFuspecTestCases(string inputString) 
            => new TestCasesReader(new StreamReader(GenerateStreamFromString(inputString))).Read().TestCases;
    }
}