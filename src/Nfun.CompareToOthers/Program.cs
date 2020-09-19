using System;
using System.Diagnostics;
using IronPython.Hosting;
using NCalc;
using NFun;

namespace Nfun.CompareToOthers
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             *(4 * 12 / 7) + ((9 * 2) % 8)	Simple Arithmetics	474,247.87	32,691,490.41	6,793.33%
        5 * 2 = 2 * 5 && (1 / 3.0) * 3 = 1	Simple Arithmetics	276,226.31	93,222,709.05	33,648.67%
                    [Param1] * 7 + [Param2]	Constant Values	707,493.27	21,766,101.47	2,976.51%
                    [Param1] * 7 + [Param2]	Dynamic Values	582,832.10	21,400,445.13	3,571.80%
                Foo([Param1] * 7, [Param2])	Dynamic Values and Function Call	594,259.69	17,209,334.34	2,795.93%
             * 
             */

            int batchIterations = 10_00;
            var ex1 = "(4 * 12 / 7) + ((9 * 2) / 8)";
            int batchCount = 100;

            var engine = Python.CreateEngine();
            
            Action buildEva    = () => new Expression(ex1);
            Action buildLambda = () => new Expression(ex1).ToLambda<object>();
            Action buildFun    = () => FunBuilder.Build(ex1);
            Action buildNCalcPy     = () => engine.Execute(ex1);

            
            var expression = new Expression(ex1);
            var lambda = expression.ToLambda<object>();
            var funrt = FunBuilder.Build(ex1);
            Action calcEva = () => expression.Evaluate();
            Action calcLambda = () => lambda();
            Action calcFun   = () => funrt.Calculate();
            
            BenchHelper.Measure(buildEva,batchIterations);
            BenchHelper.Measure(buildLambda,batchIterations);
            BenchHelper.Measure(buildFun,  batchIterations);
            BenchHelper.Measure(calcEva,batchIterations);
            BenchHelper.Measure(calcLambda,batchIterations);
            BenchHelper.Measure(calcFun,  batchIterations);
            BenchHelper.Measure(buildNCalcPy,  batchIterations);

            
            var evaluateTs = TimeSpan.Zero;
            var lambdaTs   = TimeSpan.Zero;
            var funTs      = TimeSpan.Zero;

            var calcEvaluateTs = TimeSpan.Zero;
            var calcLambdaTs   = TimeSpan.Zero;
            var calcFunTs      = TimeSpan.Zero;
            var bcPy           = TimeSpan.Zero;
            
            for (int i = 0; i < batchCount; i++)
            {
                GC.Collect(0);
                evaluateTs    += BenchHelper.Measure(buildEva,batchIterations);
                GC.Collect(0);
                lambdaTs      += BenchHelper.Measure(buildLambda,batchIterations);
                GC.Collect(0);
                funTs         += BenchHelper.Measure(buildFun,  batchIterations);
                GC.Collect(0);
                calcEvaluateTs+= BenchHelper.Measure(calcEva,batchIterations);
                GC.Collect(0);
                calcLambdaTs  += BenchHelper.Measure(calcLambda,batchIterations);
                GC.Collect(0);
                calcFunTs     += BenchHelper.Measure(calcFun,  batchIterations);
                GC.Collect(0);
                bcPy          += BenchHelper.Measure(buildNCalcPy, batchIterations);
            }

            Console.WriteLine($"build nfun:     {funTs.TotalMilliseconds}");
            Console.WriteLine($"build evaluate: {evaluateTs.TotalMilliseconds}");
            Console.WriteLine($"build lambda:   {lambdaTs.TotalMilliseconds}");

            Console.WriteLine($"calc nfun:     {calcFunTs.TotalMilliseconds}");
            Console.WriteLine($"calc evaluate: {calcEvaluateTs.TotalMilliseconds}");
            Console.WriteLine($"calc lambda:   {calcLambdaTs.TotalMilliseconds}");
            
            Console.WriteLine($"python     :   {bcPy.TotalMilliseconds}");

        }
    }
    
    
       public class Performance
    {
        private const int Iterations = 100000;

        private class Context
        {
            public int Param1 { get; set; }
            public int Param2 { get; set; }

            public int Foo(int a, int b) => Math.Min(a, b);
        }

       // [Theory]
       // [InlineData("(4 * 12 / 7) + ((9 * 2) % 8)")]
       // [InlineData("5 * 2 = 2 * 5 && (1 / 3.0) * 3 = 1")]
        public void Arithmetics(string formula)
        {
            var expression = new Expression(formula);
            var lambda = expression.ToLambda<object>();

            var m1 = Measure(() => expression.Evaluate());
            var m2 = Measure(() => lambda());

            PrintResult(formula, m1, m2);
        }

       // [Theory]
       // [InlineData("[Param1] * 7 + [Param2]")]
        public void ParameterAccess(string formula)
        {
            var expression = new Expression(formula);
            var lambda = expression.ToLambda<Context, int>();

            var context = new Context {Param1 = 4, Param2 = 9};
            expression.Parameters["Param1"] = 4;
            expression.Parameters["Param2"] = 9;

            var m1 = Measure(() => expression.Evaluate());
            var m2 = Measure(() => lambda(context));

            PrintResult(formula, m1, m2);
        }

       // [Theory]
       // [InlineData("[Param1] * 7 + [Param2]")]
        public void DynamicParameterAccess(string formula)
        {
            var expression = new Expression(formula);
            var lambda = expression.ToLambda<Context, int>();

            var context = new Context { Param1 = 4, Param2 = 9 };
            expression.EvaluateParameter += (name, args) =>
            {
                if (name == "Param1") args.Result = context.Param1;
                if (name == "Param2") args.Result = context.Param2;
            };

            var m1 = Measure(() => expression.Evaluate());
            var m2 = Measure(() => lambda(context));

            PrintResult(formula, m1, m2);
        }

       // [Theory]
       // [InlineData("Foo([Param1] * 7, [Param2])")]
        public void FunctionWithDynamicParameterAccess(string formula)
        {
            var expression = new Expression(formula);
            var lambda = expression.ToLambda<Context, int>();

            var context = new Context { Param1 = 4, Param2 = 9 };
            expression.EvaluateParameter += (name, args) =>
            {
                if (name == "Param1") args.Result = context.Param1;
                if (name == "Param2") args.Result = context.Param2;
            };
            expression.EvaluateFunction += (name, args) =>
            {
                if (name == "Foo")
                {
                    var param = args.EvaluateParameters();
                    args.Result = context.Foo((int) param[0], (int) param[1]);
                }
            };

            var m1 = Measure(() => expression.Evaluate());
            var m2 = Measure(() => lambda(context));

            PrintResult(formula, m1, m2);
        }

        private static TimeSpan Measure(Action a) => BenchHelper.Measure(a, Iterations);

        private static void PrintResult(string formula, TimeSpan m1, TimeSpan m2)
        {
            Console.WriteLine(new string('-', 60));
            Console.WriteLine("Formula: {0}", formula);
            Console.WriteLine("Expression: {0:N} evaluations / sec", Iterations / m1.TotalSeconds);
            Console.WriteLine("Lambda: {0:N} evaluations / sec", Iterations / m2.TotalSeconds);
            Console.WriteLine("Lambda Speedup: {0:P}%", (Iterations / m2.TotalSeconds) / (Iterations / m1.TotalSeconds) - 1);
            Console.WriteLine(new string('-', 60));
        }
    }


       public static class BenchHelper
       {
           public static TimeSpan Measure(Action action, int iterations)
           {
               var sw = new Stopwatch();
               sw.Start();
               for (int i = 0; i < iterations; i++)
                   action();
               sw.Stop();
               return sw.Elapsed;
           }
       }
}