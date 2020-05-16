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
using NFun.Fuspec.Parser.Interfaces;
using NFun.Runtime;

namespace FuspecHandler
{
    public class TestHandler
    {
        private readonly ConsoleWriter _consoleWriter = new ConsoleWriter();
        private TestCaseResult _testCaseResult;

        public Statistic RunTests(string directoryPath)
        {
       //     var allFoundFiles = Directory.GetFiles(directoryPath, "arithmetic.fuspec", SearchOption.AllDirectories);

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
                            }
                            else
                            {
                                try
                                {
                                    var runtime = FunBuilder.With(fus.Script).Build();
                                    CkeckTypesAndValues(runtime,fus);
                                    RunOneTest(runtime, fus);
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

        private void CkeckTypesAndValues(FunRuntime runtime, FuspecTestCase fus)
        {
            TypeAndValuesException outputInputException = new TypeAndValuesException();

            // Check OutputInputsErrors
            outputInputException.AddErrorMessage(CompareInOutTypeAndGetMessageErrorOrNull(fus.InputVarList, runtime.Inputs));
            outputInputException.AddErrorMessage(CompareInOutTypeAndGetMessageErrorOrNull(fus.OutputVarList, runtime.Outputs));

            //check SetCheckErrors
            var numberOfKit = 0;
            foreach (var checkOrSetKit in fus.SetChecks)
            {
                numberOfKit++;
                if (checkOrSetKit is SetData setKit)
                    outputInputException.AddErrorMessage(CompareSetCheckTypesAndGetMessageOrNull(numberOfKit, checkOrSetKit.ValuesKit, runtime.Inputs));
                else
                    outputInputException.AddErrorMessage(CompareSetCheckTypesAndGetMessageOrNull(numberOfKit, checkOrSetKit.ValuesKit, runtime.Outputs));
            }

            // if find some output/input errors
            if (outputInputException.Messages.Any())
                throw outputInputException;
        }

        private void RunOneTest(FunRuntime runtime, FuspecTestCase fus)
        {                       
            TypeAndValuesException outputInputException = new TypeAndValuesException();
            var numberOfKit = 0;
            var setKit = new SetData();

            if (!runtime.Inputs.Any())
                runtime.Calculate();

            foreach (var checkOrSetKit in fus.SetChecks)
            {
                numberOfKit++;
                if (checkOrSetKit is SetData)
                {
                    setKit = (SetData)checkOrSetKit;
                    runtime.Calculate(setKit.ValuesKit);
                }
          /*      if (checkOrSetKit is CheckData checkKit)
                {
                    CalculationResult results;
                    if (!runtime.Inputs.Any())
                        results = runtime.Calculate();
                    else
                        results = runtime.Calculate(setKit.ValuesKit);

                    foreach (var res in results.Results)
                    {
                        //cравниваем полученные значения и сheck
                    }
                }*/
            }

            if (outputInputException.Messages.Any())
                throw outputInputException;
        }       
        


        private string[] CompareSetCheckTypesAndGetMessageOrNull(int numberOfSetCheckKit, VarVal[] testSetCheckKit, VarInfo[] scriptTypes)
        {
            List<string> messages = new List<string>();

            foreach (var varVal in scriptTypes)
            {
                var setCheckType = testSetCheckKit.Where(s => s.Name == varVal.Name);
                if (setCheckType.Count() > 1)
                    messages.Add($"Error of SetCheckTypes in {numberOfSetCheckKit} SetCheckkit:\n\r\t\t\t" +
                        "There are two similar data");
                if (!varVal.IsOutput)
                    if (!setCheckType.Any())
                        messages.Add($"Error of SetCheckTypes in {numberOfSetCheckKit} SetCheckkit:\n\r\t\t\t" +
                            "Value( " + varVal.ToString() + " )" + " - not found this valueName in Kit! But it is in script!");
            }
            foreach (var varVal in testSetCheckKit)
            {
                IEnumerable<VarInfo> scriptType;
                    scriptType = scriptTypes.Where(s => s.Name == varVal.Name);

                if (!scriptType.Any())
                    messages.Add($"Error of SetCheckTypes in {numberOfSetCheckKit} SetCheckkit:\n\r\t\t\t" +
                        "Value( " + varVal.ToString() + " )" + " - not found this valueName in script!");
                else 
                // сравнение типов!

                {
             /*       string scriptForCheckType = ($"{varVal.Name} : {scriptType.Single().Type} = {varVal.Value}").ToLower(); 
                    try
                    {
                        var runtimeForCheckType = FunBuilder.With(scriptForCheckType).Build();
                    }
                    catch(Exception e)
                    {
                        messages.Add($"Error of SetCheckTypes in {numberOfSetCheckKit} SetCheckkit:\n\r\t\t\t" +
                                   "Error of Types. Your value: " + varVal.ToString() +
                                   ". But in script it has Type: " + scriptType.Single().ToString());
                    } */                
                }
            }
            return messages.ToArray();
        }
          
        private string[] CompareInOutTypeAndGetMessageErrorOrNull(VarInfo[] testTypes, VarInfo[] scriptTypes)
        {
            List<string> messages = new List<string>();

            // FIND OutputInputERROR: Script has some Inputs or Outputs, but they don't write in fuspek 
            //   foreach (var varInfo in scriptTypes)
            //    {
            //        var inputRes = testTypes.Where(s => s.Name == varInfo.Name);
            //        if (!inputRes.Any())
            //            messages.Add(varInfo.ToString() + " - missed in test!");
            //    }

            // FIND OutputInputERROR:  Fuspec has Inputs or Outputs, but Script doesn't   
            // FIND OutputInputERROR:  Inputs or Outpits of Fuspec and  Inputs or Outpits of Fuspec are differ  
            if (scriptTypes.Any())
            {
                foreach (var varInfo in testTypes)
                {
                    var inputRes = scriptTypes.Where(s => s.Name == varInfo.Name);
                    if (!inputRes.Any())
                        messages.Add(varInfo.ToString() + " - not found in script!");
                    else
                        if (inputRes.Single().Type != varInfo.Type)
                            messages.Add( "Error in Out/In types:\n\r\t\t\t"+
                                "Your value : " + varInfo.ToString() + ". But in script: " + inputRes.Single().ToString());
                }
            }
            return messages.ToArray();
        }
    }
}



