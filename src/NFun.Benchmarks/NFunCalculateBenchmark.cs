using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using NFun.Runtime;

// ReSharper disable InconsistentNaming

namespace NFun.Benchmarks
{
    public class NFunCalculateBenchmark
    {
        private FunnyRuntime _const_1_runtime;

        private FunnyRuntime _const_boolArray_runtime;

        // private FunRuntime _const_sum_1000_runtime;
        private FunnyRuntime _const_dummyBubbleSort_runtime;
        private FunnyRuntime _const_everything_runtime;
        private FunnyRuntime _const_Kxb_runtime;
        private FunnyRuntime _const_multiplyArrays_runtime;
        private FunnyRuntime _const_realArray_runtime;
        private FunnyRuntime _const_text_runtime;
        private FunnyRuntime _const_true_runtime;
        private FunnyRuntime _varkxb_runtime;
        private Func<double, double> dotnetEx;

        private (string, object) value;


        [GlobalSetup]
        public void Setup()
        {
            var scripts = new Scripts();

            _const_true_runtime = Funny.Hardcore.Build(scripts.ConstTrue);
            _const_1_runtime = Funny.Hardcore.Build(scripts.Const1);
            _const_text_runtime = Funny.Hardcore.Build(scripts.ConstText);
            _const_boolArray_runtime = Funny.Hardcore.Build(scripts.ConstBoolArray);
            _const_realArray_runtime = Funny.Hardcore.Build(scripts.ConstRealArray);
            _const_Kxb_runtime = Funny.Hardcore.Build(scripts.ConstKxb);
            _const_multiplyArrays_runtime = Funny.Hardcore.Build(scripts.MultiplyArrayItems);
            // _const_sum_1000_runtime  = FunBuilder.Build(scripts.ConstThousandSum);
            _const_dummyBubbleSort_runtime = Funny.Hardcore.Build(scripts.DummyBubbleSort);
            _const_everything_runtime = Funny.Hardcore.Build(scripts.Everything);

            _varkxb_runtime = Funny.Hardcore.Build(scripts.VarKxb);

            value = ("x", 100);
            Expression<Func<double, double>> ex = x => 10 * x + 1;
            dotnetEx = ex.Compile();
        }

        [Benchmark(Description = "dotnet [1.1000].SUM()", Baseline = true)]
        public int BaselineDotnetTest1000()
        {
            return Enumerable.Range(1, 3000).Sum();
        }

        [Benchmark(Description = "true calc")]
        public CalculationResult True()
        {
            return _const_true_runtime.Calc();
        }

        [Benchmark(Description = "1 calc")]
        public CalculationResult Const1()
        {
            return _const_1_runtime.Calc();
        }

        [Benchmark(Description = "text calc")]
        public CalculationResult Text()
        {
            return _const_text_runtime.Calc();
        }

        [Benchmark(Description = "bool[] calc")]
        public CalculationResult BoolArray()
        {
            return _const_boolArray_runtime.Calc();
        }

        [Benchmark(Description = "real[] calc")]
        public CalculationResult RealArray()
        {
            return _const_realArray_runtime.Calc();
        }

        [Benchmark(Description = "const kxb calc")]
        public CalculationResult ConstKxb()
        {
            return _const_Kxb_runtime.Calc();
        }

        [Benchmark(Description = "array multiply calc")]
        public CalculationResult MultiArrays()
        {
            return _const_multiplyArrays_runtime.Calc();
        }

        //[Benchmark(Description = "sum1000 calc")] public CalculationResult Sum1000() => _const_sum_1000_runtime.Calculate();
        [Benchmark(Description = "dummy bubble calc")]
        public CalculationResult DummyBubble()
        {
            return _const_dummyBubbleSort_runtime.Calc();
        }

        [Benchmark(Description = "everything calc")]
        public CalculationResult Everything()
        {
            return _const_everything_runtime.Calc();
        }

        [Benchmark(Description = "kxb with var calc")]
        public CalculationResult VarKxb()
        {
            return _varkxb_runtime.Calc(value);
        }

        [Benchmark(Description = "dotnet kxb calc")]
        public double DotnetKxb()
        {
            return dotnetEx(100);
        }
    }
}