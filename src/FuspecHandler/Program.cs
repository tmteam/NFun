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
            
            var testHandler = new TestHandler();
            var stats = testHandler.RunTests();

            stats.PrintStatistic();

            Console.WriteLine();
            Console.WriteLine("######################################");
            Console.Write("Do you want to detail Errors?  Y/N  ");
            Console.WriteLine();
            var answer = Console.ReadLine();
            if (answer=="y") 
                stats.PrintFullStatistic();
            
            
        }

      
    }
}