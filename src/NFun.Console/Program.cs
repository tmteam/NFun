using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NFun;
using NFun.ParseErrors;
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
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" ERROR [FU"+ e.Code +"] ");
                    Console.Write($" {e.Message} ");

                    Console.ResetColor();

                    if (e.End != -1)
                    {
                        if (e.Start > 0)
                            Console.Write(expression.Substring(0, e.Start));

                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.Write(e.Interval.SubString(expression));
                        Console.ResetColor();
                        if(expression.Length>=e.End)
                            Console.Write(expression.Substring(e.End));
                        Console.WriteLine();
                    }
                }
            }
        }

    }


}