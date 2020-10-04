using BenchmarkDotNet.Running;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Nfun.Benchmarks
{
    [TestFixture]
    public class AllBenchmarks
    {
        [Test] public void SipliestArithmeticsBenchmark() => BenchmarkRunner.Run<SimplestArithmCalcBenchmark>();
        [Test] public void NfunParserBenchmark() => BenchmarkRunner.Run<NfunParserBenchmark>();
        [Test] public void NfunInterpritationBenchmark() => BenchmarkRunner.Run<NfunInterpritationBenchmark>();
        [Test] public void NfunCalculateBenchmark()     => BenchmarkRunner.Run<NfunCalculateBenchmark>();
        [Test] public void NfunUpdateBenchmark() => BenchmarkRunner.Run<NfunUpdateBenchmark>();

    }
}