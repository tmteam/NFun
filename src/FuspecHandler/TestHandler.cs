using System;
using System.Collections.Generic;
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
        private readonly ConsoleWriter _consoleWriter = new ConsoleWriter();


        public statsCollector RunTests()
        {
            string[] allFoundFiles = Directory.GetFiles("fuspecs\\", "*.fuspec", SearchOption.AllDirectories);
            statsCollector _stats = new statsCollector(allFoundFiles.Length);

            foreach (var file in allFoundFiles)
            {
                // Printing process
                //     _consoleWriter.PrintTestName(file);
                using (var streamReader = new StreamReader(file, System.Text.Encoding.Default))
                {
                    try
                    {
                        var specs = FuspecParser.Read(streamReader);
                        _stats.AddSpecsCount(specs.Length);

                        if (specs.Any())
                            foreach (var fus in specs)
                                RunOneTest(fus);
                            
                        else Console.WriteLine("No tests!");
                    }
                    catch (Exception e)
                    {
                        e.Data.Add("File",file);
                        _stats.AddError(e);
                        // Printing process
                        //         _consoleWriter.PrintError(e);
                    }
                }
            }
            return _stats;
        }

        private void RunOneTest(FuspecTestCase fuspec)
        {
            // Printing process
            //     _consoleWriter.PrintFuspecName(fuspec.Name);

            var script = fuspec.Script;
            try
            {
                var runtime = FunBuilder.With(script).Build();

                runtime.Inputs.Select((s => new IdType( s.Name, s.Type)));

                if (runtime.Inputs.Any())
                {
                   /* CompareInputs();
                    Console.WriteLine("Inputs: " + string.Join(", ", runtime.Inputs.Select(s => s.ToString())));
                    Console.WriteLine("Ouputs: " + string.Join(", ", runtime.Outputs.Select(s => s.ToString())));
                */
                }
                else
                {
                    var res = runtime.Calculate();
                }
                // Printing process
                //     _consoleWriter.PrintOkTest();
            }
            catch (Exception e)
            {
                e.Data.Add("Script",script);
                e.Data.Add("Test",fuspec.Name);
                throw;
            }
        }

        private bool CompareInputs()
        {
            return true;
        }
    }
}