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
            Console.WriteLine($"~byte(1)={~(byte)1}");
            Console.WriteLine($"~byte(5)={~(byte)5}");
            Console.WriteLine($"~byte(0)={~(byte)0}");
            Console.WriteLine($"~~byte(1)={~~(byte)1}");
            
            Console.WriteLine($"~int16(1)={~(short)1}");
            Console.WriteLine($"~int16(5)={~(short)5}");
            Console.WriteLine($"~int16(0)={~(short)0}");
            Console.WriteLine($"~int16(-1)={~(short)-1}");
            Console.WriteLine($"~int16(-5)={~(short)-5}");
            Console.WriteLine($"~~int16(1)={~~(short)1}");

            Console.WriteLine($"~uint16(1)={~(ushort)1}");
            Console.WriteLine($"~uint16(5)={~(ushort)5}");
            Console.WriteLine($"~uint16(0)={~(ushort)0}");
            Console.WriteLine($"~~uint16(1)={~~(ushort)1}");

            
            Console.WriteLine($"~int(1)={~(int)1}");
            Console.WriteLine($"~int(5)={~(int)5}");
            Console.WriteLine($"~int(0)={~(int)0}");
            Console.WriteLine($"~int(-1)={~(int)-1}");
            Console.WriteLine($"~int(-5)={~(int)-5}");
            Console.WriteLine($"~~int(1)={~~(int)1}");

            Console.WriteLine($"~uint(1)={~(uint)1}");
            Console.WriteLine($"~uint(5)={~(uint)5}");
            Console.WriteLine($"~uint(0)={~(uint)0}");
            Console.WriteLine($"~~uint(1)={~~(uint)1}");
            

            Console.WriteLine($"~int64(1)={~(long)1}");
            Console.WriteLine($"~int64(5)={~(long)5}");
            Console.WriteLine($"~int64(0)={~(long)0}");
            Console.WriteLine($"~int64(-1)={~(long)-1}");
            Console.WriteLine($"~int64(-5)={~(long)-5}");
            Console.WriteLine($"~~int64(1)={~~(long)1}");

            Console.WriteLine($"~uint64(1)={~(ulong)1}");
            Console.WriteLine($"~uint64(5)={~(ulong)5}");
            Console.WriteLine($"~uint64(0)={~(ulong)0}");
            Console.WriteLine($"~~uint64(1)={~~(ulong)1}");

            
            
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