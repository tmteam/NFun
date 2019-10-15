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
        private int _numberOfFiles=0;
        private int _numberOfTests = 0;
        private int _funErrors = 0;
        private List<Exception> _errors = new List<Exception>();
        private Dictionary<string,bool> _filesNamesCollection = new Dictionary<string, bool>();
        
        public Exception[] Errors => _errors.ToArray() ;
        
        public statsCollector(int numberOfFiles) => _numberOfFiles = numberOfFiles;
        
        public void AddFileToStatistic(string nameOfFile, bool isParsingSuccessful) => 
            _filesNamesCollection.Add(nameOfFile,isParsingSuccessful);
        
        public void AddError(Exception e)
        {
            switch (e)
            {
                case FuspecParserException fusParsExc:
                    _errors.Add(fusParsExc);
                     AddFileToStatistic(e.Source, false);
                    break;
                case FunParseException funParsException:
                    _funErrors++;
                    _errors.Add(funParsException);
                    break;
                case FunRuntimeException funRuntimeException:
                    _funErrors++;
                    _errors.Add(funRuntimeException);
                    break;
                default:
                    _errors.Add(e);
                    break;
            }
        }

        public void AddSpecsCount(int specsCount)
        {
            _numberOfTests += specsCount;
        }

        public void PtintStatistic()
        {
            
            Console.WriteLine("Number of files: {0}", _numberOfFiles);
            Console.WriteLine("number of Successful parsed Files: {0}", _filesNamesCollection.Count(file => file.Value));

            var brokenFiles = _filesNamesCollection.Where(file => !file.Value);
            if (_numberOfFiles != brokenFiles.Count())
            {
                Console.WriteLine("Name of broken files: ");
                foreach (var file in brokenFiles)
                    Console.WriteLine("\t{0}", file.Key);
            }

            Console.WriteLine("Number of SucsessfulParsedTests: {0}", _numberOfTests);
            Console.WriteLine("Number of SucsessfulCompleteTests: {0}", _numberOfTests - _funErrors);
            
        }
    }
}