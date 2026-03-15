using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NUnit.Framework;

namespace Nfun.Benchmarks;

/// <summary>
/// Quick A/B performance benchmark for comparing branches.
/// Round-robin execution with isolated baseline for stable ±3% measurements.
/// Run with: dotnet test --filter "FullyQualifiedName~QuickBenchmark" -v n
/// </summary>
[TestFixture]
[Category("Benchmark")]
public class QuickBenchmark
{
    #region Configuration

    const int WarmupIterations = 500;
    const int TargetBatchMs = 10;
    const int CalibrationRuns = 50;
    const int DefaultMeasurementSeconds = 25;
    const double TrimFraction = 0.15;
    const double DropFirstFraction = 0.10;
    const int GcEveryNRounds = 100;

    #endregion

    #region Baseline

    const int BaselineBatch = 200;
    const int BaselineRounds = 50;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BaselineOp()
    {
        long sum = 0;
        for (int i = 0; i < 80_000; i++)
        {
            sum += (long)i * i;
            sum ^= sum >> 7;
        }
        return sum;
    }

    #endregion

    [Test]
    public void VerifyAllScripts()
    {
        var errors = new System.Text.StringBuilder();
        VerifyBenchSet(BenchSets.V1(), errors);
        VerifyBenchSet(BenchSetsLcaOptional.LcaOptional(), errors);
        if (errors.Length > 0)
            throw new Exception(errors.ToString());
    }

    static void VerifyBenchSet(BenchSet set, System.Text.StringBuilder errors)
    {
        foreach (var subset in set.Subsets)
            for (int i = 0; i < subset.Scripts.Length; i++)
            {
                var s = subset.Scripts[i];
                try
                {
                    var rt = Funny.Hardcore.Build(s.Script);
                    foreach (var inp in s.Inputs.Keys)
                        if (rt[inp] == null)
                            errors.AppendLine($"{set.Name}/{subset.Name}[{i}]: input '{inp}' not found");
                    foreach (var outp in s.Outputs)
                        if (rt[outp] == null)
                            errors.AppendLine($"{set.Name}/{subset.Name}[{i}]: output '{outp}' not found");
                }
                catch (Exception ex) { errors.AppendLine($"{set.Name}/{subset.Name}[{i}]: {ex.Message.Split('\n')[0]}"); }
            }
    }

    [TestCase(10, TestName = "Quick 15s")]
    [TestCase(25, TestName = "Default 30s")]
    [TestCase(60, TestName = "Precise 70s")]
    [TestCase(120, TestName = "HighPrecision 130s")]
    public void RunBenchmark(int measurementSeconds) => RunBench(BenchSets.V1(), measurementSeconds);

    [TestCase(10, TestName = "LcaOpt Quick")]
    [TestCase(25, TestName = "LcaOpt Default")]
    [TestCase(60, TestName = "LcaOpt Precise")]
    [TestCase(120, TestName = "LcaOpt HighPrecision")]
    public void RunLcaOptionalBenchmark(int measurementSeconds) => RunBench(BenchSetsLcaOptional.LcaOptional(), measurementSeconds);

    void RunBench(BenchSet benchSet, int measurementSeconds)
    {
        var originalPriority = System.Threading.Thread.CurrentThread.Priority;
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        try { RunBenchCore(benchSet, measurementSeconds); }
        finally { System.Threading.Thread.CurrentThread.Priority = originalPriority; }
    }

    void RunBenchCore(BenchSet benchSet, int measurementSeconds)
    {
        double ticksToUs = 1_000_000.0 / Stopwatch.Frequency;
        int numSubsets = benchSet.Subsets.Length;

        // ================================================================
        // PRE-BUILD: runtimes and ops for each subset
        // ================================================================
        var subsetInfos = new SubsetInfo[numSubsets];
        var ops = new List<(string name, int scriptCount, Action action)>();

        for (int si = 0; si < numSubsets; si++)
        {
            var subset = benchSet.Subsets[si];
            var allScripts = subset.Scripts.Select(s => s.Script).ToArray();
            var allRuntimes = allScripts.Select(s => Funny.Hardcore.Build(s)).ToArray();

            var updatePairs = subset.Scripts
                .Select((s, i) => (script: s, rt: allRuntimes[i]))
                .Where(x => x.script.Inputs.Count > 0)
                .ToArray();

            int nAll = allScripts.Length;
            int nUpd = updatePairs.Length;

            int parseIdx = ops.Count;
            ops.Add(($"{subset.Name}.Parse", nAll, () => {
                foreach (var s in allScripts) Parser.Parse(Tokenizer.ToFlow(s));
            }));

            int buildIdx = ops.Count;
            ops.Add(($"{subset.Name}.Build", nAll, () => {
                foreach (var s in allScripts) Funny.Hardcore.Build(s);
            }));

            int runIdx = ops.Count;
            ops.Add(($"{subset.Name}.Run", nAll, () => {
                foreach (var rt in allRuntimes) rt.Run();
            }));

            int updateIdx = ops.Count;
            if (nUpd > 0)
            {
                ops.Add(($"{subset.Name}.Update", nUpd, () => {
                    foreach (var (script, rt) in updatePairs)
                    {
                        foreach (var (varName, val) in script.Inputs)
                            rt[varName].Value = val;
                        rt.Run();
                        foreach (var outName in script.Outputs)
                            _ = rt[outName].Value;
                    }
                }));
            }
            else
            {
                ops.Add(($"{subset.Name}.Update", 1, () => { })); // placeholder
            }

            subsetInfos[si] = new SubsetInfo(subset, parseIdx, buildIdx, runIdx, updateIdx, nAll, nUpd);
        }

        var opsArr = ops.ToArray();
        int numOps = opsArr.Length;

        // ================================================================
        // PHASE 1: WARMUP
        // ================================================================
        for (int w = 0; w < WarmupIterations; w++)
        {
            BaselineOp();
            foreach (var (_, _, action) in opsArr)
                action();
        }
        ForceGC();

        // ================================================================
        // PHASE 2: MEASURE BASELINE (isolated)
        // ================================================================
        for (int i = 0; i < 200; i++) BaselineOp();
        ForceGC();

        var baselineSamples = new List<double>(BaselineRounds);
        for (int i = 0; i < BaselineRounds; i++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int b = 0; b < BaselineBatch; b++)
                BaselineOp();
            long end = Stopwatch.GetTimestamp();
            baselineSamples.Add((end - start) * ticksToUs / BaselineBatch);
        }

        double baselineUs = TrimmedMean(baselineSamples);
        double baselineCv = CV(baselineSamples);
        ForceGC();

        // ================================================================
        // PHASE 3: CALIBRATE
        // ================================================================
        var calibrationTimes = new double[numOps];
        for (int i = 0; i < numOps; i++)
        {
            var action = opsArr[i].action;
            long total = 0;
            for (int r = 0; r < CalibrationRuns; r++)
            {
                long s = Stopwatch.GetTimestamp();
                action();
                total += Stopwatch.GetTimestamp() - s;
            }
            calibrationTimes[i] = (double)total / CalibrationRuns * ticksToUs;
        }

        int targetBatchUs = TargetBatchMs * 1000;
        var batchSizes = new int[numOps];
        for (int i = 0; i < numOps; i++)
            batchSizes[i] = Math.Max(1, (int)Math.Ceiling(targetBatchUs / Math.Max(calibrationTimes[i], 0.01)));

        ForceGC();

        // ================================================================
        // PHASE 4: ROUND-ROBIN MEASUREMENT
        // ================================================================
        double roundMs = numOps * TargetBatchMs;
        int maxRounds = (int)(measurementSeconds * 1000.0 / roundMs);

        var results = new List<double>[numOps];
        for (int i = 0; i < numOps; i++)
            results[i] = new List<double>(maxRounds);

        var totalSw = Stopwatch.StartNew();

        for (int round = 0; round < maxRounds; round++)
        {
            for (int i = 0; i < numOps; i++)
            {
                int batch = batchSizes[i];
                var action = opsArr[i].action;

                long start = Stopwatch.GetTimestamp();
                for (int b = 0; b < batch; b++)
                    action();
                long end = Stopwatch.GetTimestamp();

                results[i].Add((end - start) * ticksToUs / batch / opsArr[i].scriptCount);
            }

            if (round % GcEveryNRounds == GcEveryNRounds - 1)
                GC.Collect(0, GCCollectionMode.Forced);
        }

        totalSw.Stop();

        // ================================================================
        // PHASE 5: MEMORY MEASUREMENT (allocations are deterministic)
        // ================================================================
        const int MemoryRuns = 5;
        var allocKbPerScript = new double[numOps];
        for (int i = 0; i < numOps; i++)
        {
            var action = opsArr[i].action;
            var samples = new long[MemoryRuns];
            for (int r = 0; r < MemoryRuns; r++)
            {
                ForceGC();
                long before = GC.GetTotalAllocatedBytes(true);
                action();
                long after = GC.GetTotalAllocatedBytes(true);
                samples[r] = after - before;
            }
            Array.Sort(samples);
            allocKbPerScript[i] = samples[MemoryRuns / 2] / 1024.0 / opsArr[i].scriptCount;
        }

        // ================================================================
        // PHASE 6: STATISTICAL PROCESSING
        // ================================================================
        int dropCount = (int)(maxRounds * DropFirstFraction);
        for (int i = 0; i < numOps; i++)
            results[i] = results[i].Skip(dropCount).ToList();

        int usedRounds = maxRounds - dropCount;

        var means = new double[numOps];
        var cvs = new double[numOps];
        for (int i = 0; i < numOps; i++)
        {
            means[i] = TrimmedMean(results[i]);
            cvs[i] = CV(results[i]);
        }

        // ================================================================
        // PHASE 7: REPORT
        // ================================================================
        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"NFun Quick Benchmark — {benchSet.Name}");
        sb.AppendLine(new string('=', 90));

        // --- Environment info ---
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Machine: {Environment.MachineName}  OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        string buildConfig =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        sb.AppendLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  CPU: {Environment.ProcessorCount} cores  Config: {buildConfig}");
        sb.AppendLine($"Memory: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024)}MB available");
        sb.Append(GetGitInfo());
        sb.AppendLine(new string('-', 90));

        sb.AppendLine($"Baseline: ArithLoop 80K = {baselineUs:F1} μs  (CV {baselineCv:F1}%)");
        double turtleNsHeader = baselineUs / 10000 * 1000;
        sb.AppendLine($"1 turtle (t) = {turtleNsHeader:F1} ns = baseline / 10000. Relative unit; lower = faster.");
        foreach (var info in subsetInfos)
            sb.Append($"  {info.Subset.Name}={info.TotalScripts}({info.UpdateScripts}upd)");
        sb.AppendLine();
        string mode = measurementSeconds <= 15 ? "Quick" :
                      measurementSeconds <= 30 ? "Default" :
                      measurementSeconds <= 90 ? "Precise" : "HighPrecision";
        sb.AppendLine($"Mode: {mode} ({measurementSeconds}s measurement, {totalSw.Elapsed.TotalSeconds:F0}s total)");
        sb.AppendLine($"Rounds: {maxRounds} ({dropCount} dropped, {usedRounds} used)  Batch: {TargetBatchMs}ms");

        if (baselineCv > 5.0)
            sb.AppendLine("!! Baseline CV > 5% — environment unstable, results unreliable");
        sb.AppendLine();

        // --- Absolute table (10-char columns with μs suffix) ---
        sb.AppendLine("Absolute (μs per script):");
        sb.AppendLine("           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U  ");
        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");
        foreach (var info in subsetInfos)
        {
            double parse = means[info.ParseIdx], build = means[info.BuildIdx];
            double ticAsm = build - parse, run = means[info.RunIdx], update = means[info.UpdateIdx];
            double bld10u = build + 10 * update;
            sb.AppendLine($"{info.Subset.Name,-10} | {Fmt(parse),5} μs | {Fmt(build),5} μs | {Fmt(ticAsm),5} μs | {Fmt(run),5} μs | {Fmt(update),5} μs | {Fmt(bld10u),5} μs");
        }
        sb.AppendLine();

        // --- Turtles table (relative units, lower = faster) ---
        double turtleNs = baselineUs / 10000 * 1000;
        double scale = 10000.0 / baselineUs;
        sb.AppendLine($"Turtles (1t = {turtleNs:F1}ns = baseline/10000, lower is better):");
        sb.AppendLine("           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U  ");
        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");

        // Collect turtle values for weighted average
        var turtleRows = new double[numSubsets][];
        for (int si = 0; si < numSubsets; si++)
        {
            var info = subsetInfos[si];
            double parse = means[info.ParseIdx] * scale, build = means[info.BuildIdx] * scale;
            double ticAsm = (means[info.BuildIdx] - means[info.ParseIdx]) * scale;
            double run = means[info.RunIdx] * scale, update = means[info.UpdateIdx] * scale;
            double bld10u = build + 10 * update;
            turtleRows[si] = new[] { parse, build, ticAsm, run, update, bld10u };
            sb.AppendLine($"{info.Subset.Name,-10} | {FmtScore(parse),6} t | {FmtScore(build),6} t | {FmtScore(ticAsm),6} t | {FmtScore(run),6} t | {FmtScore(update),6} t | {FmtScore(bld10u),6} t");
        }

        // Weighted average: (importance[0]*row[0] + ...) / sum(importance)
        double totalWeight = subsetInfos.Sum(si => si.Subset.Importance);
        var weightedTurtles = new double[6];
        for (int col = 0; col < 6; col++)
        {
            double sum = 0;
            for (int si = 0; si < numSubsets; si++)
                sum += subsetInfos[si].Subset.Importance * turtleRows[si][col];
            weightedTurtles[col] = sum / totalWeight;
        }
        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");
        sb.Append("Weighted   ");
        for (int col = 0; col < 6; col++)
            sb.Append($"| {FmtScore(weightedTurtles[col]),6} t ");
        sb.AppendLine();
        sb.AppendLine();

        // --- Stability table ---
        sb.AppendLine("Stability (CV%):");
        sb.AppendLine("           | Parse  | Build  |  Run   | Update");
        sb.AppendLine("-----------+--------+--------+--------+-------");
        foreach (var info in subsetInfos)
            sb.AppendLine($"{info.Subset.Name,-10} | {cvs[info.ParseIdx],4:F1}%  | {cvs[info.BuildIdx],4:F1}%  | {cvs[info.RunIdx],4:F1}%  | {cvs[info.UpdateIdx],4:F1}%");
        sb.AppendLine();

        // --- Memory table (KB allocated per script) ---
        sb.AppendLine("Memory (KB allocated per script):");
        sb.AppendLine("           |  Parse   |  Build   |   Run    |  Update  ");
        sb.AppendLine("-----------+----------+----------+----------+----------");
        foreach (var info in subsetInfos)
        {
            double parse = allocKbPerScript[info.ParseIdx], build = allocKbPerScript[info.BuildIdx];
            double run = allocKbPerScript[info.RunIdx], update = allocKbPerScript[info.UpdateIdx];
            sb.AppendLine($"{info.Subset.Name,-10} | {FmtKb(parse),5} KB | {FmtKb(build),5} KB | {FmtKb(run),5} KB | {FmtKb(update),5} KB");
        }
        sb.AppendLine();

        // --- Compact ---
        sb.AppendLine("Compact (μs/script):  Parse / Build / Run / Update / Bld+10U");
        foreach (var info in subsetInfos)
        {
            double parse = means[info.ParseIdx], build = means[info.BuildIdx];
            double run = means[info.RunIdx], update = means[info.UpdateIdx];
            double bld10u = build + 10 * update;
            sb.AppendLine($"  {info.Subset.Name,-8}  {Fmt(parse),7}  {Fmt(build),7}  {Fmt(run),7}  {Fmt(update),7}  {Fmt(bld10u),7}");
        }
        sb.AppendLine();

        sb.AppendLine("Batch sizes: " + string.Join(", ", opsArr.Select((o, i) => $"{o.name}={batchSizes[i]}")));

        string report = sb.ToString();
        TestContext.WriteLine(report);
    }

    record SubsetInfo(BenchSubSet Subset, int ParseIdx, int BuildIdx, int RunIdx, int UpdateIdx, int TotalScripts, int UpdateScripts);

    #region Statistics

    static (int start, int end) TrimRange(int count)
    {
        int trim = (int)(count * TrimFraction);
        if (trim * 2 >= count - 1)
            return (count / 2, count / 2 + 1); // median only
        return (trim, count - trim);
    }

    static double TrimmedMean(List<double> samples)
    {
        var sorted = samples.OrderBy(x => x).ToArray();
        var (start, end) = TrimRange(sorted.Length);
        double sum = 0;
        int count = 0;
        for (int i = start; i < end; i++) { sum += sorted[i]; count++; }
        return sum / count;
    }

    static double CV(List<double> samples)
    {
        var sorted = samples.OrderBy(x => x).ToArray();
        var (start, end) = TrimRange(sorted.Length);
        double sum = 0, sumSq = 0;
        int count = 0;
        for (int i = start; i < end; i++) { sum += sorted[i]; sumSq += sorted[i] * sorted[i]; count++; }
        double mean = sum / count;
        if (mean < 0.001) return 0;
        double variance = sumSq / count - mean * mean;
        return Math.Sqrt(Math.Max(0, variance)) / mean * 100.0;
    }

    static string GetGitInfo()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            string branch = RunGit("rev-parse --abbrev-ref HEAD");
            string commit = RunGit("rev-parse --short HEAD");
            bool dirty = RunGit("status --porcelain").Length > 0;
            if (branch.Length > 0)
                sb.AppendLine($"Git: {branch} @ {commit}{(dirty ? " (dirty)" : "")}");
            return sb.ToString();
        }
        catch { return ""; }
    }

    static string RunGit(string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = System.Diagnostics.Process.Start(psi);
        string output = p!.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit(2000);
        return output;
    }

    static string Fmt(double us) => us < 10 ? $"{us:F2}" : us < 100 ? $"{us:F1}" : $"{us:F0}";
    static string FmtScore(double s) => s < 10 ? $"{s:F1}" : s < 1000 ? $"{s:F0}" : $"{s:N0}";
    static string FmtKb(double kb) => kb < 10 ? $"{kb:F2}" : kb < 100 ? $"{kb:F1}" : $"{kb:F0}";

    static void ForceGC()
    {
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
    }

    #endregion
}
