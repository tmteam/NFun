using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nfun.InfinityProfiling.Sets;

namespace Nfun.InfinityProfiling
{
    public static class ProfileModes
    {
        public static void RunCalc(bool runOnlySimpliest)
        {
            Action<IProfileSet> runner;
            int reportTime;
            if (runOnlySimpliest)
            {
                runner = ProfileTools.RunFastExamples;
                reportTime = 100_000;
            }
            else
            {
                runner = ProfileTools.RunAllExamples;
                reportTime = 5_000;
            }

            var calculateBench = new ProfileCalculateSet();

            for (int i = 0; i < 3; i++)
            {
                runner(calculateBench);
            }

            int measurementsCount = 0;
            int historyCount = 10;

            var calcStopWatch = new Stopwatch();
            var calcHistory = new LinkedList<double>();


            for (int iterations = 1; !Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape; iterations++)
            {

                calcStopWatch.Start();
                runner(calculateBench);
                calcStopWatch.Stop();

                if (iterations >= reportTime)
                {
                    measurementsCount++;

                    calcHistory.AddAndTruncate(calcStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    var total = calcStopWatch.Elapsed;
                    var buildAndRunTime = calcStopWatch.Elapsed;

                    calcStopWatch.Reset();

                    PrintHeader("CALC", runOnlySimpliest, measurementsCount, total);
                    PrintResults("calculate", buildAndRunTime, calcHistory, iterations);
                    PrintFooter();

                    iterations = 1;
                }
            }
        }


        public static void RunParse(bool runOnlySimpliest)
        {

            Action<IProfileSet> runner;
            int reportTime;
            if (runOnlySimpliest)
            {
                runner = ProfileTools.RunFastExamples;
                reportTime = 10000;
            }
            else
            {
                runner = ProfileTools.RunAllExamples;
                reportTime = 5000;
            }

            var parseBench = new ProfileParserSet();

            for (int i = 0; i < 3; i++)
            {
                runner(parseBench);
            }

            int measurementsCount = 0;
            int historyCount = 10;

            var parseStopWatch = new Stopwatch();
            var parseHistory = new LinkedList<double>();


            for (int iterations = 1; !Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape; iterations++)
            {
                parseStopWatch.Start();
                runner(parseBench);
                parseStopWatch.Stop();

                if (iterations >= reportTime)
                {
                    measurementsCount++;

                    parseHistory.AddAndTruncate(parseStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    PrintHeader("PARSE", runOnlySimpliest, measurementsCount, parseStopWatch.Elapsed);
                    PrintResults("parse    ", parseStopWatch.Elapsed, parseHistory, iterations);
                    PrintFooter();
                    parseStopWatch.Reset();

                    iterations = 1;
                }
            }
        }


        public static void RunAll(bool runOnlySimpliest)
        {
            Action<IProfileSet> runner;
            int reportTime;
            if (runOnlySimpliest)
            {
                runner = ProfileTools.RunFastExamples;
                reportTime = 1500;
            }
            else
            {
                runner = ProfileTools.RunAllExamples;
                reportTime = 500;
            }

            var buildBench = new ProfileBuildAllSet();
            var parseBench = new ProfileParserSet();
            var updateBench = new ProfileUpdateSet();
            var calculateBench = new ProfileCalculateSet();

            for (int i = 0; i < 3; i++)
            {
                runner(parseBench);
                runner(buildBench);
                runner(updateBench);
                runner(calculateBench);
            }

            int measurementsCount = 0;
            int historyCount = 10;

            var parseStopWatch = new Stopwatch();
            var parseHistory = new LinkedList<double>();

            var buildStopWatch = new Stopwatch();
            var buildHistory = new LinkedList<double>();

            var interpritateHistory = new LinkedList<double>();

            var updateStopWatch = new Stopwatch();
            var updateHistory = new LinkedList<double>();

            var calcStopWatch = new Stopwatch();
            var calcHistory = new LinkedList<double>();


            for (int iterations = 1; !Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape; iterations++)
            {
                parseStopWatch.Start();
                runner(parseBench);
                parseStopWatch.Stop();

                buildStopWatch.Start();
                runner(buildBench);
                buildStopWatch.Stop();

                updateStopWatch.Start();
                runner(updateBench);
                updateStopWatch.Stop();

                calcStopWatch.Start();
                runner(calculateBench);
                calcStopWatch.Stop();

                if (iterations >= reportTime)
                {
                    measurementsCount++;

                    parseHistory.AddAndTruncate(parseStopWatch.Elapsed.TotalMilliseconds, historyCount);
                    buildHistory.AddAndTruncate(buildStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    interpritateHistory.AddAndTruncate(buildStopWatch.Elapsed.TotalMilliseconds -
                                                       parseStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    updateHistory.AddAndTruncate(updateStopWatch.Elapsed.TotalMilliseconds, historyCount);
                    calcHistory.AddAndTruncate(calcStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    var total = parseStopWatch.Elapsed + buildStopWatch.Elapsed + updateStopWatch.Elapsed +
                                calcStopWatch.Elapsed;
                    var buildAndRunTime = buildStopWatch.Elapsed + calcStopWatch.Elapsed;

                    parseStopWatch.Reset();
                    buildStopWatch.Reset();
                    updateStopWatch.Reset();
                    calcStopWatch.Reset();

                    PrintHeader("everything", runOnlySimpliest, measurementsCount, total);
                    PrintResults("parse    ", buildAndRunTime, parseHistory, iterations);
                    PrintResults("interprt ", buildAndRunTime, interpritateHistory, iterations);
                    PrintResults("calculate", buildAndRunTime, calcHistory, iterations);
                    PrintResults("update   ", buildAndRunTime, updateHistory, iterations);
                    PrintFooter();

                    iterations = 1;
                }
            }
        }


        public static void RunBuild(bool runOnlySimpliest)
        {
            Action<IProfileSet> runner;
            int reportTime;
            if (runOnlySimpliest)
            {
                runner = ProfileTools.RunFastExamples;
                reportTime = 1500;
            }
            else
            {
                runner = ProfileTools.RunAllExamples;
                reportTime = 1000;
            }

            var buildBench = new ProfileBuildAllSet();
            var parseBench = new ProfileParserSet();

            for (int i = 0; i < 3; i++)
            {
                runner(parseBench);
                runner(buildBench);
            }

            int measurementsCount = 0;
            int historyCount = 10;

            var parseStopWatch = new Stopwatch();
            var parseHistory = new LinkedList<double>();

            var buildStopWatch = new Stopwatch();
            var buildHistory = new LinkedList<double>();
            var interpritateHistory = new LinkedList<double>();


            for (int iterations = 1; !Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape; iterations++)
            {
                parseStopWatch.Start();
                runner(parseBench);
                parseStopWatch.Stop();

                buildStopWatch.Start();
                runner(buildBench);
                buildStopWatch.Stop();
                if (iterations >= reportTime)
                {
                    measurementsCount++;

                    parseHistory.AddAndTruncate(parseStopWatch.Elapsed.TotalMilliseconds, historyCount);
                    buildHistory.AddAndTruncate(buildStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    interpritateHistory.AddAndTruncate(buildStopWatch.Elapsed.TotalMilliseconds -
                                                       parseStopWatch.Elapsed.TotalMilliseconds, historyCount);

                    var total = parseStopWatch.Elapsed + buildStopWatch.Elapsed;

                    parseStopWatch.Reset();
                    buildStopWatch.Reset();
                    PrintHeader("build", runOnlySimpliest, measurementsCount, total);
                    PrintResults("parse    ", total, parseHistory, iterations);
                    PrintResults("interprt ", total, interpritateHistory, iterations);
                    PrintFooter();

                    iterations = 1;
                }
            }
        }

        private static void PrintHeader(string name, bool runOnlySimpliest, int measurementsCount, TimeSpan total)
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine(
                $"------ {name} {(runOnlySimpliest ? "basics" : "all")} iteration #{measurementsCount} in {(int) total.TotalMilliseconds} ms ------");

            Console.WriteLine($"          |    %    |  VAL ips |  AVG ips  |  MIN ips  |  MAX ips  |   RMS  |");
        }

        private static void PrintFooter()
        {
            Console.WriteLine("\r\nPress [esc] to stop");
        }

        private static void PrintResults(string name, TimeSpan ratioTime, LinkedList<double> history,
            int iterations)
        {
            var max = history.Max();
            var min = history.Min();
            var avg = history.Average();
            var rms = Math.Sqrt(history.Select(h => Math.Pow(avg - h, 2)).Sum());
            var current = history.Last.Value;

            var percents = 100 * current / ratioTime.TotalMilliseconds;
            var formatedPrecents = percents >= 100 ? " ---  " : $"{percents:00.00}%";
            Console.WriteLine($"{name} |  {formatedPrecents} | " +
                              $"{1000 * iterations / current:000000.0} |  " +
                              $"{1000 * iterations / avg:000000.0} |  " +
                              $"{1000 * iterations / max:000000.0} |  " +
                              $"{1000 * iterations / min:000000.0} |  " +
                              $"{rms * 10000 / iterations:0000}  |  ");
        }
    }
}