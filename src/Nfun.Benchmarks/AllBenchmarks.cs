using BenchmarkDotNet.Running;
using NFun.Tic;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Nfun.Benchmarks
{
    [TestFixture]
    public class SDicTests
    {
        [Test] public void DicBench() => BenchmarkRunner.Run<FastDicBench>();

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(100)]
        public void AddGet(int count)
        {
            var sds = new SmallStringDictionary<object>();

            for (int i = 0; i < count; i++)
            {
                sds.Add(i.ToString(), i);
                Assert.AreEqual(i+1, sds.Count);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(i,sds[i.ToString()]);
                Assert.IsTrue(sds.TryGetValue(i.ToString(), out var res));
                Assert.AreEqual(res, i);
            }
        }
    }
    [TestFixture]
    public class AllBenchmarks
    {
        [Test] public void DicBench() => BenchmarkRunner.Run<FastDicBench>();
        [Test] public void SipliestArithmeticsBenchmark() => BenchmarkRunner.Run<SimplestArithmCalcBenchmark>();
        [Test] public void NfunParserBenchmark() => BenchmarkRunner.Run<NfunParserBenchmark>();
        [Test] public void NfunInterpritationBenchmark() => BenchmarkRunner.Run<NfunInterpritationBenchmark>();
        [Test] public void NfunCalculateBenchmark()     => BenchmarkRunner.Run<NfunCalculateBenchmark>();
        [Test] public void NfunUpdateBenchmark() => BenchmarkRunner.Run<NfunUpdateBenchmark>();

    }
}