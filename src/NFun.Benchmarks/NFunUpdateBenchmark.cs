using System.Linq;
using BenchmarkDotNet.Attributes;
using NFun;
using NFun.Runtime;
// ReSharper disable InconsistentNaming

namespace NFun.Benchmarks
{
    public class NFunUpdateBenchmark
    {
        private FunRuntime _varkxb_runtime;
        private FunRuntime _const_true_runtime;
        private FunRuntime _const_Kxb_runtime;

        [GlobalSetup]
        public void Setup()
        {
            var scripts = new Scripts();

            _const_true_runtime = Funny.Hardcore.Build(scripts.ConstTrue);
            _const_Kxb_runtime = Funny.Hardcore.Build(scripts.ConstKxb);
            _varkxb_runtime = Funny.Hardcore.Build(scripts.VarKxb);
            var x = _varkxb_runtime.GetAllVariableSources().First(v => !v.IsOutput);
            x.FunnyValue = 100.0;
        }
        [Benchmark(Description = "dotnet [1.1000].SUM()", Baseline = true)]
        public int BaselineDotnetTest1000() => Enumerable.Range(1, 1000).Sum();
        [Benchmark(Description = "true calc")] public void True() => _const_true_runtime.Update();
        [Benchmark(Description = "const kxb calc")] public void ConstKxb() => _const_Kxb_runtime.Update();
        [Benchmark(Description = "kxb with var calc")] public void VarKxb() => _varkxb_runtime.Update();
    }
}