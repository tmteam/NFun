using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using NFun;
using NFun.Interpritation;

namespace Nfun.Benchmarks
{
    public class NfunInterpritationBenchmark
    {
        private FunctionDictionary _dictionary;
        private Scripts _scripts;
     
        [GlobalSetup]
        public void Setup()
        {
            _scripts = new Scripts();
            _dictionary = BaseFunctions.CreateDefaultDictionary();
        }
        [Benchmark(Description = "dotnet [1.1000].SUM()", Baseline = true)]
        public int BaseLineDotnetTest1000() => Enumerable.Range(1, 1000).Sum();

        [Benchmark(Description = "true")]
        public void TrueCalc()
        {
            FunBuilder
                .With(_scripts.ConstTrue)
                .With(_dictionary)
                .Build();
        }
        [Benchmark(Description = "1")]
        public void Const1()
        {
            FunBuilder
                .With(_scripts.Const1)
                .With(_dictionary)
                .Build();
        }
        [Benchmark(Description = "text")]
        public void Text()
        {
            FunBuilder
                .With(_scripts.ConstText)
                .With(_dictionary)
                .Build();
        }
        [Benchmark(Description = "bool[]")]
        public void BoolArr()
        {
            FunBuilder
                .With(_scripts.ConstBoolArray)
                .With(_dictionary)
                .Build();
        }
        [Benchmark(Description = "real[]")]
        public void RealArr()
        {
            FunBuilder
                .With(_scripts.ConstRealArray)
                .With(_dictionary)
                .Build();
        }
        [Benchmark(Description = "const kxb")]
        public void ConstKxb()
        {
            FunBuilder
                .With(_scripts.ConstKxb)
                .With(_dictionary)
                .Build();
        }
        [Benchmark(Description = "array multiply")]
        public void ArrayMulti()
        {
            FunBuilder
                .With(_scripts.MultiplyArrayItems)
                .With(_dictionary)
                .Build();
        }

        /*[Benchmark(Description = "sum1000")]
        public void Sum1000()
        {
            FunBuilder
                .With(_scripts.ConstThousandSum)
                .With(_dictionary)
                .Build();
        }*/

        [Benchmark(Description = "dummy Bubble")]
        public void DummyBubble()
        {
            FunBuilder
                .With(_scripts.DummyBubbleSort)
                .With(_dictionary)
                .Build();
        }

        [Benchmark(Description = "Everything")]
        public void Everything()
        {
            FunBuilder
                .With(_scripts.Everything)
                .With(_dictionary)
                .Build();
        }

        [Benchmark(Description = "kxb with var")]
        public void KxbNoTypes()
        {
            FunBuilder
                .With(_scripts.VarKxb)
                .With(_dictionary)
                .Build();
        }

        [Benchmark(Description = "Dotnet kxb build")]

        public Func<double, double> DotnetKxb()
        {
            Expression<Func<double, double>> ex = x => 10*x+1;
            return ex.Compile();
        }
        

        
    }
}