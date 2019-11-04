using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        private TestCaseResult _testCaseResult;
        private Statistic _statistic;


        public Statistic RunTests(string directoryPath)
        {
            string[] allFoundFiles =  Directory.GetFiles(directoryPath, "*.fuspec", SearchOption.AllDirectories);
            _statistic = new Statistic(allFoundFiles.Length);
            
            foreach (var file in allFoundFiles)
            {
                _consoleWriter.PrintTestName(file);
                using (var streamReader = new StreamReader(file, System.Text.Encoding.Default))
                {
                    IEnumerable<FuspecTestCase> specs= new FuspecTestCase[0];
                    try
                    {
                        specs = FuspecParser.Read(streamReader);
                        foreach (var fus in specs)
                        {
                            _testCaseResult = new TestCaseResult(file, fus);
                            _consoleWriter.PrintFuspecName(fus.Name);
                            if (fus.IsTestExecuted)
                                try
                                {
                                    RunOneTest(fus);
                                }
                                catch (Exception e)
                                {
                                    _testCaseResult.SetError(e);
                                    _consoleWriter.PrintError(e);
                                }
                            else
                                _consoleWriter.PrintTODOTest();
                            _statistic.AddTestToStatistic(_testCaseResult);
                        }
                        if (!specs.Any()) _consoleWriter.PrintNoTests();
                    }
                    catch (FuspecParserException e)
                    {
                        _statistic.AddFileReadingError(file, e);
                        _consoleWriter.PrintBrokenFile();
                    }

                }
            }
            return _statistic;
        }

        private void RunOneTest(FuspecTestCase fus)
        {
            var runtime = FunBuilder.With(fus.Script).Build();
            //    if (runtime.Inputs.Any())
            //   {
            /* CompareInputs();
             Console.WriteLine("Inputs: " + string.Join(", ", runtime.Inputs.Select(s => s.ToString())));
             Console.WriteLine("Ouputs: " + string.Join(", ", runtime.Outputs.Select(s => s.ToString())));
         */
            // }
            // else
            if(!runtime.Inputs.Any())
            {
                var res = runtime.Calculate();
            }

            // Printing process
            _consoleWriter.PrintOkTest();
        }

        private bool CompareInputs()
        {
            return true;
        }
    }
}