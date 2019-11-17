using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;

namespace FuspecHandler
{
    public class Statistic
    {
        private readonly int _numberOfFiles;
        
        private readonly Dictionary<string, FuspecParserException> _fileReadingError;
        private readonly ConsoleWriter _consoleWriter;
        private readonly List<TestCaseResult> _testCaseResults;

        public Statistic(int numberOfFiles)
        {
            _numberOfFiles = numberOfFiles;
            _fileReadingError = new Dictionary<string, FuspecParserException>();
            _consoleWriter=new ConsoleWriter();
            _testCaseResults = new List<TestCaseResult>();
        }

        public void AddTestToStatistic(TestCaseResult testCaseResult)
        {
            _testCaseResults.Add(testCaseResult);
        }

        public void AddFileReadingError(string file, FuspecParserException error)
        {
            _fileReadingError.Add(file, error);
        }

        public void PrintStatistic()
        {
            _consoleWriter.PrintStatisticHeader();
            _consoleWriter.PrintNumberOfFiles(_numberOfFiles);
            _consoleWriter.PrintNumberOfSuccessfulParsedFiles(_numberOfFiles - _fileReadingError.Count); 

            foreach (var error in _fileReadingError)
                _consoleWriter.PrintFuspecParserException(error.Key,error.Value.Errors.FirstOrDefault());

            Console.WriteLine("Number of SuccessfulParsedTests: {0}", _testCaseResults.Count());
            Console.WriteLine("Number of SuccessfulCompleteTests: {0}",
                _testCaseResults.Count(e => e.Error == null && e.Fus.IsTestExecuted));
            Console.WriteLine("Number of Failed tests: {0}",_testCaseResults.Count(e=>e.Error!=null));
            Console.WriteLine("Number of TODO tests: {0}", _testCaseResults.Count(e => !e.Fus.IsTestExecuted));
            Console.WriteLine("_______");
            Console.WriteLine("ERRORS:");
            Console.WriteLine("Number of FunRuntime Errors: {0}",
                _testCaseResults.Count(e => e.Error is FunRuntimeException));
            Console.WriteLine("Number of FunParse Errors: {0}",
                _testCaseResults.Count(e => e.Error is FunParseException));
            Console.WriteLine("Number of In/Out Check Errors: {0}", +
                _testCaseResults.Count(e=> e.Error is OutputInputException));
            Console.WriteLine("Number of Unknown Errors: Can't count now!");
        }

        public void PrintErrorDetails()
        {
            foreach (var test in _testCaseResults.Where(e => e.Error != null))
            {
                Console.WriteLine();
                Console.WriteLine("*********************************");
                PrintError(test);
            }
        }

        private void PrintError(TestCaseResult testCaseResult)
        {
            switch (testCaseResult.Error)
            {
                case FuspecParserException fusParsExc:
                    break;
                case FunParseException funParsException:
                    _consoleWriter.PrintFunParseException(funParsException, testCaseResult.FileName, 
                        testCaseResult.Fus.Script, testCaseResult.Fus.Name,testCaseResult.Fus.StartLine);
                    break;
                case FunRuntimeException funRuntimeException:
                    _consoleWriter.PrintFuspecRunTimeException(funRuntimeException, testCaseResult.FileName, testCaseResult.Fus.Name);
                    break;
                case OutputInputException outputInputException:
                    _consoleWriter.PrintOutpitInputException(testCaseResult.FileName,testCaseResult.Fus.Name, outputInputException.Messages);
                    break;
                default:
                    _consoleWriter.PrintUnknownException(testCaseResult.FileName,testCaseResult.Fus.Name,testCaseResult.Error);
                    break;
            }
        }
    }

}