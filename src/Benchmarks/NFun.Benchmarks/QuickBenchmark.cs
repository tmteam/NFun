using System;
using System.IO;
using System.Linq;
using System.Text;
using NFun;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NUnit.Framework;
using QuickBench;

namespace Nfun.Benchmarks;

[TestFixture]
[Category("Benchmark")]
public class QuickBenchmarkTests
{
    [TestCase(10, TestName = "Quick 15s")]
    [TestCase(25, TestName = "Default 30s")]
    [TestCase(60, TestName = "Precise 70s")]
    [TestCase(120, TestName = "HighPrecision 130s")]
    public void RunBenchmark(int seconds) => RunNFunBench(BenchSetV1.V1(), seconds);

    [TestCase(10, TestName = "V2 Quick")]
    [TestCase(25, TestName = "V2 Default")]
    [TestCase(60, TestName = "V2 Precise")]
    [TestCase(120, TestName = "V2 HighPrecision")]
    public void RunV2Benchmark(int seconds) => RunNFunBench(BenchSetV2.V2(), seconds);

    [TestCase(7, TestName = "V2 7s")]
    [TestCase(15, TestName = "V2 15s")]
    [TestCase(30, TestName = "V2 30s")]
    [TestCase(60, TestName = "V2 60s")]
    [TestCase(120, TestName = "V2 120s")]
    [TestCase(180, TestName = "V2 180s")]
    [TestCase(240, TestName = "V2 240s")]
    [TestCase(360, TestName = "V2 360s")]
    public void RunV2Experiment(int seconds) => RunNFunBench(BenchSetV2.V2(), seconds);

    [Test]
    public void VerifyAllScripts()
    {
        var errors = new StringBuilder();
        Verify(BenchSetV1.V1(), errors);
        Verify(BenchSetV2.V2(), errors);
        if (errors.Length > 0)
            throw new Exception(errors.ToString());
    }

    private static void RunNFunBench(NfunBenchSet nfunBenchSet, int seconds)
    {
        var builder = QuickBenchBuilder.Create(nfunBenchSet.Name);
        foreach (var nfunSubset in nfunBenchSet.Subsets)
            RegisterNFunOps(builder, nfunBenchSet.OptionalTypes, nfunSubset);

        var report = builder.Run(seconds);

        TestContext.WriteLine(QuickBenchReportRenderer.RenderText(report));
        TestContext.WriteLine(RenderNFunHeadline(report, nfunBenchSet));
        TestContext.WriteLine(RenderNFunSummary(report, nfunBenchSet));

        var jsonPath = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            $"bench_{nfunBenchSet.Name}_{seconds}s_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        report.SaveJson(jsonPath);
        TestContext.WriteLine($"JSON saved: {jsonPath}");
    }

    private static string RenderNFunHeadline(BenchmarkReport report, NfunBenchSet benchSet)
    {
        double scale = 10000.0 / report.Baseline.MeanUs;
        double totalWeight = benchSet.Subsets.Sum(s => s.Importance);
        double wB = 0, wU = 0;
        foreach (var ns in benchSet.Subsets) {
            wB += ns.Importance * (FindSlot(report, ns.Name, "Build")?.MeanUs ?? 0);
            wU += ns.Importance * (FindSlot(report, ns.Name, "Update")?.MeanUs ?? 0);
        }
        wB /= totalWeight; wU /= totalWeight;
        double bld10u = wB + 10 * wU;

        var sb = new StringBuilder();
        sb.AppendLine("NFun Headline:");
        sb.AppendLine("                  |       μs |       🍌");
        sb.AppendLine("------------------+----------+----------");
        sb.AppendLine($"Weighted Build    | {Fmt(wB),8} | {FmtScore(wB * scale),8}");
        sb.AppendLine($"Weighted Bld+10U  | {Fmt(bld10u),8} | {FmtScore(bld10u * scale),8}");
        sb.AppendLine($"Baseline          | {Fmt(report.Baseline.MeanUs),8} |    10000");
        sb.AppendLine();
        return sb.ToString();
    }

    private static SlotResult FindSlot(BenchmarkReport report, string row, string col) =>
        report.Slots.FirstOrDefault(s => s.Tags.Length >= 2 && s.Tags[0] == row && s.Tags[1] == col);

    /// <summary>NFun-specific: weighted summary by subset importance + TIC+Asm + Build+10U.</summary>
    private static string RenderNFunSummary(BenchmarkReport report, NfunBenchSet benchSet)
    {
        var sb = new StringBuilder();
        double scale = 10000.0 / report.Baseline.MeanUs;
        double blRelErr = report.Baseline.MeanUs > 0 && report.Baseline.PracticalCIUs > 0
            ? report.Baseline.PracticalCIUs / report.Baseline.MeanUs : 0;
        double totalWeight = benchSet.Subsets.Sum(s => s.Importance);

        sb.AppendLine("NFun Summary (weighted by importance):");
        sb.AppendLine("           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U ");
        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");

        foreach (var ns in benchSet.Subsets)
        {
            double parse = FindSlot(report, ns.Name, "Parse")?.MeanUs ?? 0;
            double build = FindSlot(report, ns.Name, "Build")?.MeanUs ?? 0;
            double run = FindSlot(report, ns.Name, "Run")?.MeanUs ?? 0;
            double update = FindSlot(report, ns.Name, "Update")?.MeanUs ?? 0;
            sb.AppendLine($"{ns.Name,-10} | {Fmt(parse),5} μs | {Fmt(build),5} μs | {Fmt(build - parse),5} μs | {Fmt(run),5} μs | {Fmt(update),5} μs | {Fmt(build + 10 * update),5} μs");
        }

        double wP = 0, wB = 0, wR = 0, wU = 0;
        foreach (var ns in benchSet.Subsets) {
            double w = ns.Importance;
            wP += w * (FindSlot(report, ns.Name, "Parse")?.MeanUs ?? 0);
            wB += w * (FindSlot(report, ns.Name, "Build")?.MeanUs ?? 0);
            wR += w * (FindSlot(report, ns.Name, "Run")?.MeanUs ?? 0);
            wU += w * (FindSlot(report, ns.Name, "Update")?.MeanUs ?? 0);
        }
        wP /= totalWeight; wB /= totalWeight; wR /= totalWeight; wU /= totalWeight;

        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");
        sb.AppendLine($"{"Weighted",-10} | {Fmt(wP),5} μs | {Fmt(wB),5} μs | {Fmt(wB - wP),5} μs | {Fmt(wR),5} μs | {Fmt(wU),5} μs | {Fmt(wB + 10 * wU),5} μs");
        sb.AppendLine();

        sb.AppendLine("NFun 🍌 (weighted):");
        sb.AppendLine("           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U ");
        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");

        foreach (var ns in benchSet.Subsets) {
            double p = (FindSlot(report, ns.Name, "Parse")?.MeanUs ?? 0) * scale;
            double b = (FindSlot(report, ns.Name, "Build")?.MeanUs ?? 0) * scale;
            double r = (FindSlot(report, ns.Name, "Run")?.MeanUs ?? 0) * scale;
            double u = (FindSlot(report, ns.Name, "Update")?.MeanUs ?? 0) * scale;
            sb.AppendLine($"{ns.Name,-10} | {FmtScore(p),6} b | {FmtScore(b),6} b | {FmtScore(b - p),6} b | {FmtScore(r),6} b | {FmtScore(u),6} b | {FmtScore(b + 10 * u),6} b");
        }

        double wPb = wP * scale, wBb = wB * scale, wRb = wR * scale, wUb = wU * scale;
        double wBci = 0;
        foreach (var ns in benchSet.Subsets) {
            var bsl = FindSlot(report, ns.Name, "Build");
            if (bsl != null) wBci += ns.Importance * bsl.PracticalCIUs;
        }
        wBci /= totalWeight;
        double opRel = wB > 0 && wBci > 0 ? wBci / wB : 0;
        double bldCi = wBb * Math.Sqrt(opRel * opRel + blRelErr * blRelErr);

        sb.AppendLine("-----------+----------+----------+----------+----------+----------+----------");
        sb.AppendLine($"{"Weighted",-10} | {FmtScore(wPb),6} b | {FmtScore(wBb),6} b | {FmtScore(wBb - wPb),6} b | {FmtScore(wRb),6} b | {FmtScore(wUb),6} b | {FmtScore(wBb + 10 * wUb),6} b");
        sb.AppendLine($"  Build ±{FmtScore(bldCi)}🍌   Bld+10U = {FmtScore(wBb + 10 * wUb)}🍌");

        return sb.ToString();
    }

    private static string Fmt(double us) => us < 10 ? $"{us:F2}" : us < 100 ? $"{us:F1}" : $"{us:F0}";
    private static string FmtScore(double s) => s < 10 ? $"{s:F1}" : s < 100 ? $"{s:F0}" : $"{s:N0}";

    private static void RegisterNFunOps(QuickBenchBuilder builder, OptionalTypesSupport optionalTypes, BenchSubSet subset)
    {
        var name = subset.Name;
        var funBuilder = Funny.Hardcore.WithDialect(optionalTypesSupport: optionalTypes);

        foreach (var s in subset.Scripts) {
            var script = s.Script;
            builder.Add(name, "Parse", () => Parser.Parse(Tokenizer.ToFlow(script)));
            builder.Add(name, "Build", () => funBuilder.Build(script));
        }

        var runtimes = subset.Scripts.Select(s => funBuilder.Build(s.Script)).ToArray();
        foreach (var rt in runtimes)
            builder.Add(name, "Run", () => rt.Run());

        var updatePairs = subset.Scripts
            .Select((s, i) => (script: s, rt: runtimes[i]))
            .Where(x => x.script.Inputs.Count > 0)
            .ToArray();
        foreach (var (script, rt) in updatePairs)
            builder.Add(name, "Update", () => {
                foreach (var (varName, val) in script.Inputs) rt[varName].Value = val;
                rt.Run();
                foreach (var outName in script.Outputs) _ = rt[outName].Value;
            });
    }

    private static void Verify(NfunBenchSet set, StringBuilder errors)
    {
        foreach (var subset in set.Subsets)
            for (int i = 0; i < subset.Scripts.Length; i++)
            {
                var s = subset.Scripts[i];
                try
                {
                    var rt = Funny.Hardcore
                        .WithDialect(optionalTypesSupport: set.OptionalTypes)
                        .Build(s.Script);
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
}
