using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;

namespace FuspecHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello! I'm a Fuspec Handler! ");
            Console.WriteLine();
            
            var testHandler = new TestHandler();
            var stats=testHandler.NonDetailedTest();
            
            Console.WriteLine();
            Console.WriteLine("##########################");
            Console.WriteLine("Statistic:");
            Console.WriteLine("##########################");

            stats.PtintStatistic();

            Console.WriteLine("Do you want to detail Errors?");
            var errors = stats.Errors;
            
            
        }

      
    }
}