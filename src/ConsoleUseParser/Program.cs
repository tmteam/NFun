using System;
using System.IO;
using System.Linq;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace ConsoleUseParser
{
    class Program
    {
        public static void Main()
        {
            using (var streamReader = new StreamReader(@"CompareIntReal.txt", System.Text.Encoding.Default))
            {
                try
                {
                    var specs = FuspecParser.Read(streamReader);
                    if (specs.Any())
                        foreach (var fus in specs)
                            fus.Print();
                    else Console.WriteLine("No tests!");
                }
                catch (FuspecParserException e)
                {
                    var error = e.Errors.FirstOrDefault();
                        Console.WriteLine("Error in line: {0}", error.LineNumber);
                        Console.WriteLine(error.ErrorType.ToString());
                   
                    throw;
                }
            }
        }
    }
}