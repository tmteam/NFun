using System;
using System.Collections.Generic;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;
using NFun.Runtime;

namespace FuspecHandler
{
    public class statsCollector
    {
        private int _numberOfTests = 0;
        private readonly int _numberOfFiles;
        private readonly List<Exception> _errors = new List<Exception>();
        private readonly ConsoleWriter _consoleWriter = new ConsoleWriter();

        public statsCollector(int numberOfFiles) => _numberOfFiles = numberOfFiles;

        public void AddError(Exception e) => _errors.Add(e);

        public void AddSpecsCount(int specsCount) => _numberOfTests += specsCount;

        public void PrintStatistic()
        {
            var fuspecReaderErrors = _errors.OfType<FuspecParserException>().ToArray();
            var funRuntimeErrors = _errors.OfType<FunRuntimeException>().ToArray();
            var funParseErrors = _errors.OfType<FunParseException>().ToArray();

            Console.WriteLine();
            Console.WriteLine("##########################");
            Console.WriteLine("Statistic:");
            Console.WriteLine("##########################");

            Console.WriteLine("Number of files: {0}", _numberOfFiles);
            Console.WriteLine("Number of Successful parsed Files: {0}",
                _numberOfFiles - fuspecReaderErrors.Length);

            foreach (var error in fuspecReaderErrors)
            {
                var fuspecEx = error.Errors.FirstOrDefault();
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\tName of broken file: ");
                Console.ResetColor();
                Console.WriteLine("{0} LINE: {1}  MESSAGE: {2}", error.Data["File"], fuspecEx.LineNumber,
                    fuspecEx.ErrorType);
            }

            Console.WriteLine("Number of SuccessfulParsedTests: {0}", _numberOfTests);
            Console.WriteLine("Number of SuccessfulCompleteTests: {0}",
                _numberOfTests - funRuntimeErrors.Count() - funParseErrors.Length);
            Console.WriteLine("Number of FunRuntime Error: {0}", _errors.Count(e => e is FunRuntimeException));
            Console.WriteLine("Number of FunParse Error: {0}", _errors.Count(e => e is FunParseException));
        }

        public void PrintFullStatistic()
        {
            foreach (var error in _errors)
            {
                var file = error.Data["File"].ToString();
                var script = "";
                var test = "";

                if (error is FunParseException || error is FunRuntimeException)
                {
                    script = error.Data["Script"].ToString();
                    test = error.Data["Test"].ToString();
                }

                switch (error)
                {
                    case FuspecParserException fusParsExc:
                        break;
                    case FunParseException funParsException:
                        _consoleWriter.PrintFunParseException(funParsException, file, script, test);
                        break;
                    case FunRuntimeException funRuntimeException:
                        _consoleWriter.PrintFuspecRunTimeException(funRuntimeException, file, test);
                        break;
                    default:
                        _consoleWriter.PrintUnknownException(error);
                        break;
                }
            }
        }
    }
}