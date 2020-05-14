using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.Benchmarks
{
    public class NfunBenchmarkRun
    {
        private FunRuntime kxruntime;
        private FunRuntime true_const_runtime;
        private FunRuntime constKxb_runtime;

        private VarVal value;
        private Func<double, double> dotnetEx;
        private double k = 10;
        private double b = 1;

        [GlobalSetup]
        public void Setup()
        {
            true_const_runtime = FunBuilder.BuildDefault("y=true");
            constKxb_runtime   = FunBuilder.BuildDefault("y=10*100+1");
            kxruntime          = FunBuilder.BuildDefault("y=10*x+1");
            value = VarVal.New("x",100);
            Expression<Func<double, double>> ex = x => 10 * x + 1;
            dotnetEx = ex.Compile();
        }
        [Benchmark(Description = "kxb calculation")]
        public CalculationResult RunKxbNfun() => kxruntime.Calculate(value);

        [Benchmark(Description = "true calculation")]
        public CalculationResult RunTrueNfun() => true_const_runtime.Calculate();
        
        [Benchmark(Description = "const kxb calculation")]
        public CalculationResult RunConstKxbNfun() => constKxb_runtime.Calculate();
        
        [Benchmark(Description = "kxb fake")]
        public CalculationResult RunFakeNfun() => kxruntime.CalculateFake(value);

        [Benchmark(Description = "Dotnet kxb calculation")]

        public double DotnetKxb() => dotnetEx(100);

        [Benchmark(Description = "Dotnet simple kxb calculation")]

        public double DotnetSimpleKxb() => k*100+b;


        [Benchmark(Description = "Summ200")]
        public int Test200() => Enumerable.Range(1, 200).Sum();
    }
    public class NfunBenchmarkCompile
    {
        private FunctionDictionary dicitionary;
        private string hellExpr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi((acc,i)-> max(acc,t[i]))";
        [GlobalSetup]
        public void Setup()
        {
            dicitionary = BaseFunctions.GetDefaultDictionary();
        }
        [Benchmark(Description = "kxb no types")]
        public void KxbNoTypes()
        {
            FunBuilder
                .With("y=10*x+1")
                .With(dicitionary)
                .Build();
        }

        [Benchmark(Description = "Dotnet kxb build")]

        public Func<double, double> DotnetKxb()
        {
            Expression<Func<double, double>> ex = x => 10*x+1;
            return ex.Compile();
        }
        [Benchmark(Description = "kxb typed")]
        public void KxbWithTypes()
        {
            FunBuilder
                .With("x:real; y:real=10*x+1")
                .With(dicitionary)
                .Build();
        }
        [Benchmark(Description = "1")]
        public void GenericConstant()
        {
            FunBuilder
                .With("y = 1")
                .With(dicitionary)
                .Build();
        }
        [Benchmark(Description = "true")]
        public void BoolConstant()
        {
            FunBuilder
                .With("y = true")
                .With(dicitionary)
                .Build();
        }


        [Benchmark(Description = "y:bool = true")]
        public void ConcreteBoolConstant()
        {
            FunBuilder
                .With("y:bool = true")
                .With(dicitionary)
                .Build();
        }

        [Benchmark(Description = "Hell expr with custom func")]
        public void HellExpr()
        {
            FunBuilder
                .With(hellExpr)
                .With(dicitionary)
                .Build();
        }

        [Benchmark(Description = "Summ200")]
        public int Test200()
        {
            return Enumerable.Range(1, 200).Sum();
        }
    }
    public class TheEasiestBenchmark
    {
        [Benchmark(Description = "Summ100")]
        public int Test100()
        {
            return Enumerable.Range(1, 100).Sum();
        }

        [Benchmark(Description = "Summ200")]
        public int Test200()
        {
            return Enumerable.Range(1, 200).Sum();
        }
    }

    [TestFixture]
    public class UnitTest1
    {
        [Test] public void TestMethod1()     => BenchmarkRunner.Run<TheEasiestBenchmark>();
        [Test] public void NfunCompileTest() => BenchmarkRunner.Run<NfunBenchmarkCompile>();
        [Test] public void NfunRunTest()     => BenchmarkRunner.Run<NfunBenchmarkRun>();
    }
}