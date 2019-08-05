using System;
using System.Collections.Generic;
using System.IO;
using NFun.Fuspec.TestHandler.Models;

namespace NFun.Fuspec.ConsoleTestHandler
{
    public static class FileAndConsoleLogger
    {
        private const string BaseResultName = "LastRunResult.txt";

        public static void Write(long ms, TestResults testResult, List<BaseFuspecTestCaseResult> exceptionsNumber)
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

        private static void WriteMessage(string message)
        {
            Console.WriteLine(message);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), BaseResultName);
            File.AppendAllText(filePath, message);
        }
    }
}