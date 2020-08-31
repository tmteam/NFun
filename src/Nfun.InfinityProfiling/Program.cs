using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Nfun.Benchmarks;

namespace Nfun.InfinityProfiling
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Build in loop");
            Console.WriteLine("Press esc to exit");

            var buildBench = new ProfileBuildAllSet();
            var parseBench = new ProfileParserSet();
            var updateBench = new ProfileUpdateSet();
            var calculateBench = new ProfileCalculateSet();

            for (int i = 0; i < 3; i++)
            {
                ProfileTools.RunAll(parseBench);
                ProfileTools.RunAll(buildBench);
                ProfileTools.RunAll(updateBench);
                ProfileTools.RunAll(calculateBench);
            }
            
            int measurementsCount = 0;
            int historyCount = 10;
            int reportTime = 600;
            
            var parseStopWatch = new Stopwatch();
            var parseHistory = new LinkedList<double>();

            var buildStopWatch = new Stopwatch();
            var buildHistory = new LinkedList<double>();
            
            var interpritateHistory = new LinkedList<double>();

            var updateStopWatch = new Stopwatch();
            var updateHistory = new LinkedList<double>();

            var calcStopWatch = new Stopwatch();
            var calcHistory = new LinkedList<double>();


            for (int iterations = 1; !Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape ; iterations++)
            {
                parseStopWatch.Start();
                ProfileTools.RunAll(parseBench);
                parseStopWatch.Stop();
                
                buildStopWatch.Start();
                ProfileTools.RunAll(buildBench);
                buildStopWatch.Stop();
                
                updateStopWatch.Start();
                ProfileTools.RunAll(updateBench);
                updateStopWatch.Stop();

                calcStopWatch.Start();
                ProfileTools.RunAll(calculateBench);
                calcStopWatch.Stop();
                
                if (iterations >= reportTime)
                {
                    measurementsCount++;
                    
                    parseHistory.AddAndTruncate(parseStopWatch.Elapsed.TotalMilliseconds,historyCount);
                    buildHistory.AddAndTruncate(buildStopWatch.Elapsed.TotalMilliseconds,historyCount);
                    
                    interpritateHistory.AddAndTruncate(buildStopWatch.Elapsed.TotalMilliseconds -
                                                       parseStopWatch.Elapsed.TotalMilliseconds,historyCount);
                    
                    updateHistory.AddAndTruncate(updateStopWatch.Elapsed.TotalMilliseconds,historyCount);
                    calcHistory.AddAndTruncate(calcStopWatch.Elapsed.TotalMilliseconds,historyCount);

                    var total = parseStopWatch.Elapsed+ buildStopWatch.Elapsed + updateStopWatch.Elapsed + calcStopWatch.Elapsed;
                    var buildAndRunTime = buildStopWatch.Elapsed + calcStopWatch.Elapsed;
                    
                    parseStopWatch.Reset();
                    buildStopWatch.Reset();
                    updateStopWatch.Reset();
                    calcStopWatch.Reset();

                    
                    Console.Clear(); 
                    Console.WriteLine();
                    Console.WriteLine($"------ Iteration #{measurementsCount} in {(int)total.TotalMilliseconds} ms ------");

                    Console.WriteLine($"          |   %   | VAL ips | AVG ips | MIN ips | MAX ips |  RMS  |");
                    PrintResults("parse    ", buildAndRunTime, parseHistory,         iterations);
                    PrintResults("interprt ", buildAndRunTime, interpritateHistory,  iterations);
                    //PrintResults("build all", buildAndRunTime, buildHistory,         iterations);
                    //PrintResults("update   ", buildAndRunTime, updateHistory,        iterations);
                    PrintResults("calculate", buildAndRunTime, calcHistory,          iterations);

                    Console.WriteLine("\r\nPress [esc] to exit");


                    iterations = 1;
                }
            }
            Console.WriteLine("Build stopped. Bye bye");
        }

        private static void PrintResults(string name, TimeSpan ratioTime,  LinkedList<double> history,
            int iterations)
        {
            var max = history.Max();
            var min = history.Min();
            var avg = history.Average();
            var rms = Math.Sqrt(history.Select(h => Math.Pow(avg - h, 2)).Sum());
            var current = history.Last.Value;
            
            Console.WriteLine($"{name} |  " +
                              $"{100*current/ratioTime.TotalMilliseconds:00.00}% |"+
                              $"{1000 * iterations / current:0000.0} |  " +
                              $"{1000 * iterations / avg:0000.0} |  " +
                              $"{1000 * iterations / max:0000.0} |  " +
                              $"{1000 * iterations / min:0000.0} |  " +
                              $"{rms * 100 / iterations:000}  |  ");
        }
    }
    
}