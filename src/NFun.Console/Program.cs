using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NFun;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace Funny
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Let's make some fun.");
            Console.WriteLine("Type an expression or '/exit' to return");

            while (true)
            {
                var expression = Console.ReadLine();
                if (expression == "/exit")
                    return;
                try
                {
                    var runtime = FunBuilder.With(expression).Build();
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
                    Console.WriteLine("Expression cannot be calculated: " + e.Message);
                }
                catch (FunParseException e)
                {
                    Console.WriteLine($"Error: {e.Message} from {e.Start} to {e.End}");
                }
            }
        }

    }


}