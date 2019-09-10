using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NFun;
using NFun.BuiltInFunctions;
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
                var expression = ReadMultiline();
                if (expression == "/exit")
                    return;
                try
                {
                    Stopwatch calcSw;
                    Stopwatch build = Stopwatch.StartNew();
                    var runtime = FunBuilder.With(expression).Build();
                    build.Stop();
                    Console.WriteLine($"Built in {build.Elapsed.TotalMilliseconds}");

                    if (runtime.Inputs.Any())
                    {
                        Console.WriteLine("Inputs: " + string.Join(", ", runtime.Inputs.Select(s => s.ToString())));
                        Console.WriteLine("Ouputs: " + string.Join(", ", runtime.Outputs.Select(s => s.ToString())));
                    }
                    else 
                    {
                        calcSw = Stopwatch.StartNew();
                        var res = runtime.Calculate();
                        calcSw.Stop();
                        Console.WriteLine($"Calc in {calcSw.Elapsed.TotalMilliseconds}");
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
                Console.WriteLine("--------------");
            }
        }

        private static string ReadMultiline()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                var expression = Console.ReadLine();
                if (expression == "")
                    return sb.ToString();
                else
                    sb.Append("\r\n" + expression);
            }
        }
    }


}