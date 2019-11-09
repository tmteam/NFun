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
using NFun.Types;


namespace FuspecHandler
{
    public class TestHandler
    {
        private readonly ConsoleWriter _consoleWriter = new ConsoleWriter();
        private TestCaseResult _testCaseResult;
   //     private Statistic _statistic;


        public Statistic RunTests(string directoryPath)
        {
            var allFoundFiles =  Directory.GetFiles(directoryPath, "*.fuspec", SearchOption.AllDirectories);
            var statistic = new Statistic(allFoundFiles.Length);
            
            foreach (var file in allFoundFiles)
            {
                _consoleWriter.PrintTestingFileName(file);
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
                            if (!fus.IsTestExecuted)
                            {
                                _consoleWriter.PrintTODOTest();
                            //    statistic.AddTestToStatistic(_testCaseResult);
                                    // continue;
                            }
                            else
                            {
                                try
                                {
                                    _testCaseResult.SetInputs(RunOneTest(fus));
                                    _consoleWriter.PrintOkTest();

                                }
                                catch (Exception e)
                                {
                                    _testCaseResult.SetError(e);
                                    _consoleWriter.PrintError(e);
                                }
                            }
                            statistic.AddTestToStatistic(_testCaseResult);
                            
                        }
                        if (!specs.Any()) _consoleWriter.PrintNoTests();
                    }
                    catch (FuspecParserException e)
                    {
                        statistic.AddFileReadingError(file, e);
                        _consoleWriter.PrintError(e);
                    }
                }
            }
            return statistic;
        }

        private VarInfo[]  RunOneTest(FuspecTestCase fus)
        {
            var runtime = FunBuilder.With(fus.Script).Build();
            if(!runtime.Inputs.Any())
            {
                var res = runtime.Calculate();
            }
            
           OutputInputException outputInputException = new OutputInputException();

            // Printing process
       //     Console.WriteLine();

            if (runtime.Inputs.Any())
            {
                
              //  Console.WriteLine("Fun Inputs:  " + string.Join(", ", runtime.Inputs.Select(s => s.ToString())));
              //  Console.Write("Test Inputs: ");

                foreach (var varInfo in fus.InputVarList)
                {
             //       Console.Write("(in) {0}", varInfo.ToString());
                    var inputRes = runtime.Inputs.Where(s => s.Name == varInfo.Id);
                    if (inputRes.Any())
                        if (inputRes.Single().Type != varInfo.VarType)
                        {
                       //     Console.Write("(it should be {0})", inputRes.Single().ToString());
                         //    errors.Add(new OutputInputError("It should be "+inputRes.Single().ToString(),varInfo, inputRes.Single()));                                   
                                outputInputException.AddErrorMessage("Test :(in) "+ varInfo.ToString()+". In script: "+inputRes.Single().ToString());
                        }
                        else
                        {
                      //      Console.Write("(nonexistent in!)");
                            outputInputException.AddErrorMessage("(in) "+varInfo.ToString() + " - missed!");

                        }

               //     Console.Write(", ");

                }

                //   Console.WriteLine();
            }

            if (runtime.Outputs.Any())
            {
            //    Console.WriteLine("Fun Outputs:  " + string.Join(", ", runtime.Outputs.Select(s => s.ToString())));
            //    Console.Write("Test Outputs: ");

                foreach (var varInfo in fus.OutputVarList)
                {
             //       Console.Write("(out) {0}",varInfo.ToString());
                    
                    var outputRes = runtime.Outputs.Where(s => s.Name == varInfo.Id);
                    if (outputRes.Any())
                    {
                        if (outputRes.Single().Type != varInfo.VarType)
               //             Console.Write("(it should be {0})", outputRes.Single().ToString());
                            outputInputException.AddErrorMessage("Test: (out) "+ varInfo.ToString()+". In script: "+outputRes.Single().ToString());

                    }
                    else
                    {
                       // Console.Write("(nonexistent out!)");
                       outputInputException.AddErrorMessage("(out) "+varInfo.ToString() + " - missed!");

                    }
                //    Console.Write(", ");
                }     
             //   Console.WriteLine();
             if (outputInputException.Messages.Any())
                 throw outputInputException;

            }
            return runtime.Inputs;
        }

        private bool CompareInputs()
        {
            return true;
        }
    }
}



