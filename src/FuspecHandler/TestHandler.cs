using System;
using System.Diagnostics;
using System.Linq;
using Nfun.Fuspec.Parser.Model;
using NFun;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;

namespace FuspecHandler
{
    public static class TestHandler
    {
        public static void RunTest(FuspecTestCase fuspec)
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

                ConsoleWriter.PrintOkTest();
            }

            catch (FunRuntimeException e)
            {
                Console.WriteLine("Expression cannot be calculated: " + e.Message);
            }
            catch (FunParseException e)
            {
                ConsoleWriter.PrintFunParseException(e, script);
            }
        }
    }
}

