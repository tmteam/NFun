using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace NFun.Benchmarks
{
    public class NFunInterpritationBenchmark
    {
        private Scripts _scripts;

        [GlobalSetup]
        public void Setup()
        {
            _scripts = new Scripts();
        }

        [Benchmark(Description = "dotnet [1.1000].SUM()", Baseline = true)]
        public int BaseLineDotnetTest1000()
        {
            return Enumerable.Range(1, 1000).Sum();
        }

        [Benchmark(Description = "true")]
        public void TrueCalc()
        {
            Funny.Hardcore.Build(_scripts.ConstTrue);
        }

        [Benchmark(Description = "1")]
        public void Const1()
        {
            Funny.Hardcore.Build(_scripts.Const1);
        }

        [Benchmark(Description = "text")]
        public void Text()
        {
            Funny.Hardcore.Build(_scripts.ConstText);
        }

        [Benchmark(Description = "bool[]")]
        public void BoolArr()
        {
            Funny.Hardcore.Build(_scripts.ConstBoolArray);
        }

        [Benchmark(Description = "real[]")]
        public void RealArr()
        {
            Funny.Hardcore.Build(_scripts.ConstRealArray);
        }

        [Benchmark(Description = "const kxb")]
        public void ConstKxb()
        {
            Funny.Hardcore.Build(_scripts.ConstKxb);
        }

        [Benchmark(Description = "array multiply")]
        public void ArrayMulti()
        {
            Funny.Hardcore.Build(_scripts.MultiplyArrayItems);
        }

        [Benchmark(Description = "dummy Bubble")]
        public void DummyBubble()
        {
            Funny.Hardcore.Build(_scripts.DummyBubbleSort);
        }

        [Benchmark(Description = "Everything")]
        public void Everything()
        {
            Funny.Hardcore.Build(_scripts.Everything);
        }

        [Benchmark(Description = "kxb with var")]
        public void KxbNoTypes()
        {
            Funny.Hardcore.Build(_scripts.VarKxb);
        }

        [Benchmark(Description = "Dotnet kxb build")]
        public Func<double, double> DotnetKxb()
        {
            Expression<Func<double, double>> ex = x => 10 * x + 1;
            return ex.Compile();
        }
    }
}