using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NUnit.Framework;

namespace Nfun.Benchmarks;

/// <summary>
/// Quick A/B performance benchmark for comparing branches.
/// Uses auto-balancing + shuffled slots for stable measurements on MacBook.
///
/// Three modes:
///   Quick1 — single run (~13s), fast sanity check
///   Quick3 — median of 3 runs (~40s), normal A/B comparison
///   Quick5 — median of 5 runs (~65s), precise measurement for reports
///
/// Run with: dotnet test --filter "FullyQualifiedName~QuickBenchmark.Quick1" -v n
/// </summary>
[TestFixture]
public class QuickBenchmark {

    // Scripts copied from Benchmarks/Scripts.cs — present in both master and branch
    const string SimpleScript = "y = 10 * x + 1";

    // From Benchmarks/Scripts.cs — generic function + if-else + array ops
    const string MediumScript = @"multi(a,b) =
                              if(a.count()!=b.count()) []
                              else
                                  [0..a.count()-1].map(rule a[it]*b[it])

                          a =  [1,2,3]
                          b =  [4,5,6]
                          expected = [4,10,18]

                          passed = a.multi(b)==expected";

    // From GenericUserFunctionsTest.GenericBubbleSort (exact copy, known to pass)
    const string ComplexScript = @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j)
                            = arr.twiceSet(i,j,arr[j], arr[i])

                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)

                          onelineSort(input) =
  	                        [0..input.count()-2].fold(input, swapIfNotSorted)

                          bubbleSort(input)= [0..input.count()-1].fold(input, rule onelineSort(it1))

                          i:int[]  = [1,4,3,2,5].bubbleSort()
                          r:real[] = [1,4,3,2,5].bubbleSort()";

    const int WarmupIterations = 200;
    const int CalibrationRuns = 30;
    const int TargetBatchUs = 500; // each slot targets ~500μs
    const int MaxRounds = 3000;
    const int RandomSeed = 42;

    const int BaselineN = 1000;
    static readonly int[] BaselineSource = GenerateShuffled(BaselineN, seed: 123);

    static int[] GenerateShuffled(int n, int seed) {
        var arr = new int[n];
        for (int i = 0; i < n; i++) arr[i] = i + 1;
        var rng = new Random(seed);
        for (int i = n - 1; i > 0; i--) { int j = rng.Next(i + 1); (arr[i], arr[j]) = (arr[j], arr[i]); }
        return arr;
    }

    /// <summary>
    /// Baseline: copy + sort int[1000] + linear scan + binary search.
    /// ~8-10μs on M-series MacBook — good "parrot" scale for NFun measurements.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int BaselineOp() {
        var arr = new int[BaselineN];
        Array.Copy(BaselineSource, arr, BaselineN);
        Array.Sort(arr);
        int sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i] * (i & 1);
        sum += Array.BinarySearch(arr, 42);
        sum += Array.BinarySearch(arr, 250);
        sum += Array.BinarySearch(arr, 499);
        return sum;
    }

    [Test] public void Quick1() => RunMultiple(1);
    [Test] public void Quick3() => RunMultiple(3);
    [Test] public void Quick5() => RunMultiple(5);

    void RunMultiple(int totalRuns) {
        // Collect medians from each run: [run][opIdx]
        var allMedians = new double[totalRuns][];
        var allBatchSizes = new int[0];
        var allRounds = 0;
        string[] opNames = null;

        var totalSw = Stopwatch.StartNew();

        for (int run = 0; run < totalRuns; run++) {
            var (medians, batchSizes, rounds, names) = RunSingle();
            allMedians[run] = medians;
            allBatchSizes = batchSizes;
            allRounds = rounds;
            opNames = names;
        }

        totalSw.Stop();

        // Compute per-operation median across runs
        int numOps = allMedians[0].Length;
        var finalMedians = new double[numOps];
        for (int op = 0; op < numOps; op++) {
            var values = new double[totalRuns];
            for (int run = 0; run < totalRuns; run++)
                values[run] = allMedians[run][op];
            Array.Sort(values);
            finalMedians[op] = values[totalRuns / 2];
        }

        PrintReport(finalMedians, allBatchSizes, allRounds, opNames, totalRuns, totalSw.Elapsed);
    }

    (double[] medians, int[] batchSizes, int rounds, string[] names) RunSingle() {
        // Pre-build runtimes for Run/Update measurements
        var simpleRuntime = Funny.Hardcore.Build(SimpleScript);
        var mediumRuntime = Funny.Hardcore.Build(MediumScript);
        var complexRuntime = Funny.Hardcore.Build(ComplexScript);

        // Update values array (cycle through to prevent constant-folding)
        var updateValues = new double[] { 1.0, 42.0, 100.0, -3.14, 0.0, 999.9, 7.7, 13.0 };
        int updateIdx = 0;

        // Define all operations
        var ops = new (string name, Action action)[] {
            ("Baseline",       () => { BaselineOp(); }),
            ("Simple.Parse",   () => { Parser.Parse(Tokenizer.ToFlow(SimpleScript)); }),
            ("Simple.Build",   () => { Funny.Hardcore.Build(SimpleScript); }),
            ("Simple.Run",     () => { simpleRuntime.Run(); }),
            ("Simple.Update",  () => {
                simpleRuntime["x"].Value = updateValues[updateIdx++ & 7];
                simpleRuntime.Run();
            }),
            ("Medium.Parse",   () => { Parser.Parse(Tokenizer.ToFlow(MediumScript)); }),
            ("Medium.Build",   () => { Funny.Hardcore.Build(MediumScript); }),
            ("Medium.Run",     () => { mediumRuntime.Run(); }),
            ("Complex.Parse",  () => { Parser.Parse(Tokenizer.ToFlow(ComplexScript)); }),
            ("Complex.Build",  () => { Funny.Hardcore.Build(ComplexScript); }),
            ("Complex.Run",    () => { complexRuntime.Run(); }),
        };

        int numOps = ops.Length;

        // === PHASE 1: WARMUP ===
        for (int w = 0; w < WarmupIterations; w++)
            foreach (var (_, action) in ops)
                action();

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        // === PHASE 2: CALIBRATE ===
        var calibrationTimes = new double[numOps][];
        for (int i = 0; i < numOps; i++)
            calibrationTimes[i] = new double[CalibrationRuns];

        double ticksToUs = 1_000_000.0 / Stopwatch.Frequency;

        for (int r = 0; r < CalibrationRuns; r++) {
            for (int i = 0; i < numOps; i++) {
                long start = Stopwatch.GetTimestamp();
                ops[i].action();
                long end = Stopwatch.GetTimestamp();
                calibrationTimes[i][r] = (end - start) * ticksToUs;
            }
        }

        var batchSizes = new int[numOps];
        for (int i = 0; i < numOps; i++) {
            Array.Sort(calibrationTimes[i]);
            double median = calibrationTimes[i][CalibrationRuns / 2];
            batchSizes[i] = Math.Max(1, (int)Math.Ceiling(TargetBatchUs / Math.Max(median, 0.01)));
        }

        // === PHASE 3: CREATE & SHUFFLE SLOTS ===
        int rounds = Math.Min(MaxRounds, (int)(15_000_000.0 / (numOps * TargetBatchUs)));
        var slots = new List<int>(numOps * rounds);
        for (int r = 0; r < rounds; r++)
            for (int i = 0; i < numOps; i++)
                slots.Add(i);

        // Fisher-Yates shuffle with fixed seed
        var rng = new Random(RandomSeed);
        for (int i = slots.Count - 1; i > 0; i--) {
            int j = rng.Next(i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        // Prepare result storage
        var results = new List<double>[numOps];
        for (int i = 0; i < numOps; i++)
            results[i] = new List<double>(rounds);

        // === PHASE 4: EXECUTE ===
        foreach (int opIdx in slots) {
            int batch = batchSizes[opIdx];
            var action = ops[opIdx].action;

            long start = Stopwatch.GetTimestamp();
            for (int b = 0; b < batch; b++)
                action();
            long end = Stopwatch.GetTimestamp();

            double batchUs = (end - start) * ticksToUs;
            results[opIdx].Add(batchUs / batch);
        }

        // === PHASE 5: COMPUTE MEDIANS ===
        var medians = new double[numOps];
        for (int i = 0; i < numOps; i++) {
            var sorted = results[i].OrderBy(x => x).ToArray();
            medians[i] = sorted[sorted.Length / 2];
        }

        return (medians, batchSizes, rounds, ops.Select(o => o.name).ToArray());
    }

    void PrintReport(double[] medians, int[] batchSizes, int rounds,
                     string[] opNames, int totalRuns, TimeSpan elapsed) {
        double baselineUs = medians[0];

        var table = new (string label, int parseIdx, int buildIdx, int runIdx, int updateIdx)[] {
            ("Simple",  1, 2, 3, 4),
            ("Medium",  5, 6, 7, -1),
            ("Complex", 8, 9, 10, -1),
        };

        var report = new System.Text.StringBuilder();
        report.AppendLine();
        report.AppendLine($"NFun Quick Benchmark (Quick{totalRuns})");
        report.AppendLine(new string('=', 72));
        report.AppendLine($"Baseline: Sort+Scan int[{BaselineN}] = {baselineUs:F2} μs");
        report.AppendLine($"Runs: {totalRuns} | Rounds/run: {rounds} | Total: {elapsed.TotalSeconds:F1}s");
        report.AppendLine();
        report.AppendLine("           |  Parse  |  Build  | TIC+Asm |   Run   | Update");
        report.AppendLine("-----------+---------+---------+---------+---------+--------");

        string Fmt(double us) => us < 1 ? $"{us,5:F2}" : $"{us,5:F1}";

        foreach (var (label, pi, bi, ri, ui) in table) {
            double parse = medians[pi];
            double build = medians[bi];
            double ticAsm = build - parse;
            double run = medians[ri];
            string updateStr = ui >= 0 ? $"{Fmt(medians[ui])} μs" : "   —   ";

            report.AppendLine(
                $"{label,-10} | {Fmt(parse)} μs | {Fmt(build)} μs | {Fmt(ticAsm)} μs | {Fmt(run)} μs | {updateStr}");

            string updateParrots = ui >= 0 ? $"{medians[ui] / baselineUs,6:F1}x " : "   —   ";
            report.AppendLine(
                $"           | {parse / baselineUs,5:F1}x  | {build / baselineUs,5:F1}x  | {ticAsm / baselineUs,5:F1}x  | {run / baselineUs,5:F1}x  | {updateParrots}");
        }

        report.AppendLine();
        report.AppendLine("Batch sizes: " + string.Join(", ",
            opNames.Select((n, i) => $"{n}={batchSizes[i]}")));

        string reportStr = report.ToString();
        TestContext.WriteLine(reportStr);
        Console.WriteLine(reportStr);
    }
}
