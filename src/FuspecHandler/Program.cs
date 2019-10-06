using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace FuspecHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello! I'm a Fuspec Handler! ");
            Console.WriteLine();

            string[] allFoundFiles = Directory.GetFiles("fuspecs\\", "*.fuspec", SearchOption.AllDirectories);

            foreach (var file in allFoundFiles)
            {
                ConsoleWriter.PrintTestName(file);
                
                using (var streamReader = new StreamReader(file, System.Text.Encoding.Default))
                {
                    try
                    {
                        var specs = FuspecParser.Read(streamReader);
                        if (specs.Any())
                        {
                            foreach (var fus in specs)
                            {
                                ConsoleWriter.PrintFuspecName(fus.Name, file);
                                TestHandler.RunTest(fus);
                            }
                        }
                        else Console.WriteLine("No tests!");
                    }
                    catch (FuspecParserException e)
                    {
                        ConsoleWriter.PrintFuspecParserException(e, file);
                        //   throw;
                    }
                    catch (Exception e)
                    {
                       ConsoleWriter.PrintUnknownException(e,file);
                    }
                }
            }
        }

      
    }
}