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
        private List<Exception> _errors = new List<Exception>();
        private Dictionary<string,int> _filesNamesCollection = new Dictionary<string, int>();
        private ConsoleWriter _consoleWriter = new ConsoleWriter();
        
        public Exception[] Errors => _errors.ToArray();


        public void AddFileToStatistic(string nameOfFile, int errorLine) => 
            _filesNamesCollection.Add(nameOfFile,errorLine);
        
        public void AddError(Exception e)
        {
            _errors.Add(e);
            if (e is FuspecParserException)
            {
                var fuspecEx = e as FuspecParserException;
                AddFileToStatistic(fuspecEx.Data["File"].ToString(),fuspecEx.Errors.FirstOrDefault().LineNumber);
            }
        }

        public void AddSpecsCount(int specsCount) =>
            _numberOfTests += specsCount;
        
        public void PrintStatistic()
        {
            Console.WriteLine();
            Console.WriteLine("##########################");
            Console.WriteLine("Statistic:");
            Console.WriteLine("##########################");
            
            Console.WriteLine("Number of files: {0}", _filesNamesCollection.Count);
            Console.WriteLine("Number of Successful parsed Files: {0}", _filesNamesCollection.Count(file => file.Value<0));

            var brokenFiles = _filesNamesCollection.Where(file => file.Value>=0);
            if (brokenFiles.Any())
            {
                Console.WriteLine("Name of broken files: ");
                foreach (var file in brokenFiles)
                    Console.WriteLine("\t\t\t{0} LINE: {1}", file.Key,file.Value);
            }

            Console.WriteLine("Number of SucsessfulParsedTests: {0}", _numberOfTests);
            Console.WriteLine("Number of SucsessfulCompleteTests: {0}", _numberOfTests - _errors.Count(e=>(e is FunRuntimeException || e is FunParseException)));
            Console.WriteLine("Number of FunRuntime Error: {0}", _errors.Count(e=>e is FunRuntimeException));
            Console.WriteLine("Number of FunParse Error: {0}", _errors.Count(e=>e is FunParseException));
        }

        public void PrintFullStatistic()
        {
            foreach (var error in _errors)
            {
                var file = error.Data["File"].ToString();
                var script="";
                var test="";
                
                if (error is FunParseException || error is FunRuntimeException)
                {
                    script = error.Data["Script"].ToString();
                    test = error.Data["Test"].ToString();
                }
                
                switch (error)
                {
                    case FuspecParserException fusParsExc:
                        _consoleWriter.PrintFuspecParserException(fusParsExc,file);
                        break;
                    case FunParseException funParsException:
                        _consoleWriter.PrintFunParseException(funParsException, file, script,test);
                        break;
                    case FunRuntimeException funRuntimeException:
                        _consoleWriter.PrintFuspecRunTimeException(funRuntimeException,file,test);
                        break;
                    default:
                        _consoleWriter.PrintUnknownException(error);
                        break;
                }
                
            }
        }
    }
}