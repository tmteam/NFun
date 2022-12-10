using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NFun.Exceptions;

namespace NFun.ConsoleApp;

class Program {
    static void Main(string[] args) {
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
                var runtime = Funny.Hardcore.Build(expression);
                build.Stop();
                Console.WriteLine($"Built in {build.Elapsed.TotalMilliseconds}");

                if (runtime.Variables.Any(v => !v.IsOutput))
                {
                    Console.WriteLine(
                        "Inputs: " +
                        string.Join(
                            ", ",
                            runtime.Variables.Where(v => !v.IsOutput).Select(s => s.ToString())));
                    Console.WriteLine(
                        "Ouputs: " +
                        string.Join(
                            ", ",
                            runtime.Variables.Where(v => v.IsOutput).Select(s => s.ToString())));
                }
                else
                {
                    calcSw = Stopwatch.StartNew();
                    runtime.Run();

                    calcSw.Stop();
                    Console.WriteLine($"Calc in {calcSw.Elapsed.TotalMilliseconds}");
                    Console.WriteLine("Results:");
                    foreach (var result in runtime.Variables.Where(v => v.IsOutput))
                        Console.WriteLine(
                            result.Name +
                            ": " +
                            result.Type +
                            " (" +
                            result.Value.GetType().Name +
                            ")");
                }
            }
            catch (FunnyRuntimeException e)
            {
                Console.WriteLine("Expression cannot be calculated: " + e.Message);
            }
            catch (FunnyParseException e)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" ERROR [FU" + e.ErrorCode + "] ");
                Console.Write($" {e.Message} ");

                Console.ResetColor();

                if (e.End != -1)
                {
                    if (e.Start > 0)
                        Console.Write(expression[..e.Start]);

                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.White;

                    Console.Write(e.Interval.SubString(expression));
                    Console.ResetColor();
                    if (expression.Length >= e.End)
                        Console.Write(expression[e.End..]);
                    Console.WriteLine();
                }
            }

            Console.WriteLine("--------------");
        }
    }

    private static string ReadMultiline() {
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
