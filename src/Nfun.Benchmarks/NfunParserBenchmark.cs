using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using NFun;
using NFun.Interpritation;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace Nfun.Benchmarks
{
    public class SimplestArithmCalcBenchmark
    {
        private string _expression;
        private FunRuntime _runtime;
        private FunBuilderWithDictionary _builder;

        [GlobalSetup]
        public void Setup()
        {
            _expression = "(4 * 12 / 7) + ((9 * 2) / 8)";
            _runtime = FunBuilder.Build(_expression);
            _builder = FunBuilder
                .With(_expression)
                .With(BaseFunctions.CreateDefaultDictionary());
            
        }
        [Benchmark()]
        public TokFlow Lexing() => Tokenizer.ToFlow(_expression);

        [Benchmark]
        public SyntaxTree Parsing()
        {
            var flow = Tokenizer.ToFlow(_expression);
            return NFun.SyntaxParsing.Parser.Parse(flow);
        }

        [Benchmark()]
        public void Building() => _builder.Build();

        /*[Benchmark()]
        public void Calc10000()
        {
            for (int i = 0; i < 10000; i++)
            {
                _runtime.Calculate();
            }
        }*/

        [Benchmark()]
        public void Updating() => _runtime.Update();
        [Benchmark()]
        public CalculationResult Calculate() => _runtime.Calculate();
    }
    public class NfunParserBenchmark
    {
        private FunctionDictionary _dictionary;
        private Scripts _scripts;
     
        [GlobalSetup]
        public void Setup()
        {
            _scripts = new Scripts();
            _dictionary = BaseFunctions.CreateDefaultDictionary();
        }

        private SyntaxTree Parse(string expr)
        {
            var flow = Tokenizer.ToFlow(expr);
            return Parser.Parse(flow);

        }
        [Benchmark(Description = "dotnet [1.1000].SUM()", Baseline = true)]
        public int BaseLineDotnetTest1000() => Enumerable.Range(1, 1000).Sum();

        [Benchmark(Description = "true")]
        public void TrueCalc() => Parse(_scripts.ConstTrue);

        [Benchmark(Description = "1")]
        public void Const1() => Parse(_scripts.Const1);
        [Benchmark(Description = "text")]
        public void Text() => Parse(_scripts.ConstText);
        [Benchmark(Description = "bool[]")]
        public void BoolArr() => Parse(_scripts.ConstBoolArray);
        [Benchmark(Description = "real[]")]
        public void RealArr() => Parse(_scripts.ConstRealArray);
        [Benchmark(Description = "const kxb")]
        public void ConstKxb() => Parse(_scripts.ConstKxb);
        [Benchmark(Description = "array multiply")]
        public void ArrayMulti() => Parse(_scripts.MultiplyArrayItems);

        //[Benchmark(Description = "sum1000")]
        //public void Sum1000() => Parse(_scripts.ConstThousandSum);

        [Benchmark(Description = "dummy Bubble")]
        public void DummyBubble() => Parse(_scripts.DummyBubbleSort);

        [Benchmark(Description = "Everything")]
        public void Everything() => Parse(_scripts.Everything);

        [Benchmark(Description = "kxb with var")]
        public void KxbNoTypes() => Parse(_scripts.VarKxb);

        [Benchmark(Description = "Dotnet kxb build")]

        public Func<double, double> DotnetKxb()
        {
            Expression<Func<double, double>> ex = x => 10*x+1;
            return ex.Compile();
        }
        

        
    }
}