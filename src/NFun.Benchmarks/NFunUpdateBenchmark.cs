using System.Linq;
using BenchmarkDotNet.Attributes;
using NFun.Runtime;

// ReSharper disable InconsistentNaming

namespace NFun.Benchmarks {

public class NFunUpdateBenchmark {
    private FunnyRuntime _const_Kxb_runtime;
    private FunnyRuntime _const_true_runtime;
    private FunnyRuntime _varkxb_runtime;

    [GlobalSetup]
    public void Setup() {
        var scripts = new Scripts();

        _const_true_runtime = Funny.Hardcore.Build(scripts.ConstTrue);
        _const_Kxb_runtime = Funny.Hardcore.Build(scripts.ConstKxb);
        _varkxb_runtime = Funny.Hardcore.Build(scripts.VarKxb);
        var x = _varkxb_runtime.Variables.First(v => !v.IsOutput);
        x.Value = 100.0;
    }

    [Benchmark(Description = "dotnet [1.1000].SUM()", Baseline = true)]
    public int BaselineDotnetTest1000() {
        return Enumerable.Range(1, 1000).Sum();
    }

    [Benchmark(Description = "true calc")]
    public void True() {
        _const_true_runtime.Run();
    }

    [Benchmark(Description = "const kxb calc")]
    public void ConstKxb() {
        _const_Kxb_runtime.Run();
    }

    [Benchmark(Description = "kxb with var calc")]
    public void VarKxb() {
        _varkxb_runtime.Run();
    }
}

}