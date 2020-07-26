using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Nfun.Benchmarks;

namespace Nfun.InfinityProfiling
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Build in loop");
            Console.WriteLine("Press esc to exit");
            var bench = new NfunInterpritationBenchmark();
            bench.Setup();
            Stopwatch sw = Stopwatch.StartNew();

            BuildAll(bench);
            BuildAll(bench);
            BuildAll(bench);
            
            int reportTime = 1000;
            for (int i = 1; !Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape ; i++)
            {
                sw.Start();
                
                BuildAll(bench);

                sw.Stop();
                if (i % reportTime == 0)
                {
                    Console.WriteLine("avg time: "+ (sw.ElapsedMilliseconds*reportTime)/i +" ms");
                }
            }
            Console.WriteLine("Build stopped. Bye bye");
        }

        private static void BuildAll(NfunInterpritationBenchmark bench)
        {
            bench.TrueCalc();
            bench.Const1();
            bench.Text();
            bench.BoolArr();
            bench.RealArr();
            bench.ConstKxb();
            bench.KxbNoTypes();
            bench.ArrayMulti();
            //bench.Sum1000();
            bench.DummyBubble();
            bench.Everything();
        }
    }
}