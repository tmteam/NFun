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

        private VarInfo[] RunOneTest(FuspecTestCase fus)
        {
            var runtime = FunBuilder.With(fus.Script).Build();
            if (!runtime.Inputs.Any())
            {
                var res = runtime.Calculate();
            }

            OutputInputException outputInputException = new OutputInputException();

            var mes = CompareTypeAndGetMessageErrorOrNull(fus.InputVarList, runtime.Inputs);
            if (mes.Any())
                outputInputException.AddErrorMessage(mes);

            mes = CompareTypeAndGetMessageErrorOrNull(fus.OutputVarList, runtime.Outputs);
            if (mes != null)
                outputInputException.AddErrorMessage(mes);

            if (outputInputException.Messages.Any())
                throw outputInputException;

            return runtime.Inputs;
        }

        private string[] CompareTypeAndGetMessageErrorOrNull(VarInfo[] testTypes, VarInfo[] scriptTypes)
        {
            List<string> messages = new List<string>();

            foreach (var varInfo in scriptTypes)
            {
                var inputRes = testTypes.Where(s => s.Name == varInfo.Name);
                if (!inputRes.Any())
                    messages.Add(varInfo.ToString() + " - missed in test!");
            }
                
            
            if (scriptTypes.Any())
            {
                foreach (var varInfo in testTypes)
                {
                    var inputRes = scriptTypes.Where(s => s.Name == varInfo.Name);
                    if (!inputRes.Any())
                        messages.Add(varInfo.ToString() + " - not found in script!");
                    else
                        if (inputRes.Single().Type != varInfo.Type)
                            messages.Add( "Test : " + varInfo.ToString() + ". Script: " + inputRes.Single().ToString());
                   
                }
            }
            return messages.ToArray();
        }
    }
}



