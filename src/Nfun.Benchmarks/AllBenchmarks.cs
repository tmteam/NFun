using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Nfun.Benchmarks
{
 
    [TestFixture]
    public class AllBenchmarks
    {
        [Test] public void NfunInterpritationBenchmark() => BenchmarkRunner.Run<NfunInterpritationBenchmark>();
        [Test] public void NfunCalculateBenchmark()     => BenchmarkRunner.Run<NfunCalculateBenchmark>();
    }
}