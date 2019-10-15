using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Nfun.Fuspec.Parser;
using Nfun.Fuspec.Parser.Model;
using NFun;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;
using Nfun.Fuspec.Parser.FuspecParserErrors;


namespace FuspecHandler
{
    public class TestHandler
    {
        private statsCollector stats;
        private ConsoleWriter consoleWriter=new ConsoleWriter();


        public statsCollector NonDetailedTest()
        {

            string[] allFoundFiles = Directory.GetFiles("fuspecs\\", "*.fuspec", SearchOption.AllDirectories);
            stats = new statsCollector(allFoundFiles.Length);

            foreach (var file in allFoundFiles)
            {
                consoleWriter.PrintTestName(file);
                using (var streamReader = new StreamReader(file, System.Text.Encoding.Default))
                {
                    try
                    {
                        var specs = FuspecParser.Read(streamReader);
                        stats.AddFileToStatistic(file, true);

                        if (specs.Any())
                        {
                            stats.AddSpecsCount(specs.Length);
                            foreach (var fus in specs)
                            {
                                consoleWriter.PrintFuspecName(fus.Name);
                                RunTest(fus);
                                consoleWriter.PrintOkTest();
                            }
                        }
                        else Console.WriteLine("No tests!");
                    }
                    catch (Exception e)
                    {
                        e.Data.Add("file",file);
                        stats.AddError(e);
                        consoleWriter.PrintError(e);
                        if (e is FunParseException)
                        {
                            var funParseEx = e as FunParseException;
                            consoleWriter.PrintFunParseException(funParseEx, funParseEx.Data["script"].ToString());
                        }
                    }
                }
            }
            return stats;
        }

        private void RunTest(FuspecTestCase fuspec)
        {
            var script = fuspec.Script;
            try
            {
                var runtime = FunBuilder.With(script).Build();

                if (!runtime.Inputs.Any())
                {
                    var res = runtime.Calculate();
                }
            }
            catch (Exception e)
            {
                e.Data.Add("script",script);
                throw;
            }
        }

        private void RunDetailedTest(FuspecTestCase fuspec)
        {
            var script = fuspec.Script;
            try
            {
                var runtime = FunBuilder.With(script).Build();

                if (runtime.Inputs.Any())
                {
                    Console.WriteLine("Inputs: " + string.Join(", ", runtime.Inputs.Select(s => s.ToString())));
                    Console.WriteLine("Ouputs: " + string.Join(", ", runtime.Outputs.Select(s => s.ToString())));
                }
                else
                {
                    var res = runtime.Calculate();
                    Console.WriteLine("Results:");
                    foreach (var result in res.Results)
                        Console.WriteLine(result.Name + ": " + result.Value + " (" + result.Type + ")");
                }
            }
            catch (FunRuntimeException e)
            {
                int i;
                Console.WriteLine("Expression cannot be calculated: " + e.Message);
                throw;
            }
            catch (FunParseException e)
            {
                var consoleWriter = new ConsoleWriter();
                consoleWriter.PrintFunParseException(e, script);
                throw;
            }
        }
    }
}