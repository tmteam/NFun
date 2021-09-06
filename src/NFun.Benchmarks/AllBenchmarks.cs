using BenchmarkDotNet.Running;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace NFun.Benchmarks {

[TestFixture]
public class AllBenchmarks {
    [Test]
    public void SipliestArithmeticsBenchmark() {
        BenchmarkRunner.Run<SimplestArithmCalcBenchmark>();
    }

    [Test]
    public void NFunParserBenchmark() {
        BenchmarkRunner.Run<NfunParserBenchmark>();
    }

    [Test]
    public void NFunInterpritationBenchmark() {
        BenchmarkRunner.Run<NFunInterpritationBenchmark>();
    }

    [Test]
    public void NFunCalculateBenchmark() {
        BenchmarkRunner.Run<NFunCalculateBenchmark>();
    }

    [Test]
    public void NFunUpdateBenchmark() {
        BenchmarkRunner.Run<NFunUpdateBenchmark>();
    }
}

}