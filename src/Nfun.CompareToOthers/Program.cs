using System;
using System.Collections.Generic;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using NCalc;
using NFun;
using NFun.Types;

namespace Nfun.CompareToOthers
{
    class Program
    {
        class ExpressionContext
        {
            public double x { get; set; }
        }
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
            int batchCount = 100;

            
            var engine = Python.CreateEngine();
            
            /*
            var ex1 = "(4 * 12 / 7) + ((9 * 2) / 8)";

            Action buildEva    = () => new Expression(expression: ex1);
            Action buildLambda = () => new Expression(expression: ex1).ToLambda<object>();
            Action buildFun    = () => FunBuilder.Build(text: ex1);
            Action buildNCalcPy     = () => engine.Execute(expression: ex1);

            
            var expression = new Expression(expression: ex1);
            var lambda = expression.ToLambda<object>();
            var funrt = FunBuilder.Build(text: ex1);
            Action calcEva = () => expression.Evaluate();
            Action calcLambda = () => lambda();
            Action calcFun   = () => funrt.Calculate();*/
            
            
            var ex1 = "10*x*x + 12*x + 1";
            var ncalcEx1 = "10*[x]*[x] + 12*[x] + 1";

            var pyscope =  engine.CreateScope(new Dictionary<string, object>() {{"x", 42}});
            Action buildEva    = () => new Expression(expression: ncalcEx1);
            Action buildLambda = () => new Expression(expression: ncalcEx1).ToLambda<ExpressionContext,Double>();
            Action buildFun    = () => FunBuilder.Build(text: ex1);
            Action buildNCalcPy = () =>
            {
                pyscope.SetVariable("x",12.0);
                engine.Execute(expression: ex1,pyscope);
            };

            
            var expression = new Expression(expression: ncalcEx1);
            var lambda = expression.ToLambda<ExpressionContext,Double>();
            var funrt = FunBuilder.Build(text: ex1);
            Action calcEva = () =>
            {
                expression.Parameters["x"] = 12.0;
                expression.Evaluate();
            };
            Action calcLambda = () => lambda(new ExpressionContext(){ x = 12});
            Action calcFun   = ()  => funrt.Calculate(VarVal.New("x",12.0));
            
            MeasureAll(
                buildEva: buildEva, 
                buildLambda: buildLambda, 
                buildFun: buildFun,
                buildNCalcPy: buildNCalcPy, 
                calcEva: calcEva, 
                calcLambda: calcLambda, 
                calcFun: calcFun, 
                batchIterations: batchIterations,
                batchCount: batchCount);
        }

        private static void MeasureAll(Action buildEva, int batchIterations, Action buildLambda, Action buildFun,
            Action calcEva, Action calcLambda, Action calcFun, Action buildNCalcPy, int batchCount)
        {
            Console.WriteLine("Heating");
            BenchHelper.Measure(buildEva, batchIterations, out _);
            BenchHelper.Measure(buildLambda, batchIterations, out _);
            BenchHelper.Measure(buildFun, batchIterations, out _);
            BenchHelper.Measure(calcEva, batchIterations, out _);
            BenchHelper.Measure(calcLambda, batchIterations, out _);
            BenchHelper.Measure(calcFun, batchIterations, out _);
            BenchHelper.Measure(buildNCalcPy, batchIterations, out _);


            var evaluateTs = TimeSpan.Zero;
            var lambdaTs = TimeSpan.Zero;
            var funTs = TimeSpan.Zero;

            var calcEvaluateTs = TimeSpan.Zero;
            var calcLambdaTs = TimeSpan.Zero;
            var calcFunTs = TimeSpan.Zero;
            var bcPyTs = TimeSpan.Zero;

            Console.WriteLine("Iterating");
            int i = 0;
            while (true)
            {
                i++;
                evaluateTs += BenchHelper.Measure(buildEva, batchIterations, out var evaluateAlloc);
                lambdaTs += BenchHelper.Measure(buildLambda, batchIterations, out var lambdaAlloc);
                funTs += BenchHelper.Measure(buildFun, batchIterations, out var funAlloc);
                calcEvaluateTs += BenchHelper.Measure(calcEva, batchIterations, out var calcEvaluateAlloc);
                calcLambdaTs += BenchHelper.Measure(calcLambda, batchIterations, out var calcLambdaAlloc);
                calcFunTs += BenchHelper.Measure(calcFun, batchIterations, out var calcFunAlloc);
                bcPyTs += BenchHelper.Measure(buildNCalcPy, batchIterations, out var bcPyAlloc);
                if (i > batchCount)
                {
                    i = 0;
                    Console.Clear();
                    Console.WriteLine($"{batchCount} iterations done");
                    Console.WriteLine($"build nfun:     {funTs.TotalMilliseconds}   {funAlloc}");
                    Console.WriteLine($"build evaluate: {evaluateTs.TotalMilliseconds}    {evaluateAlloc}");
                    Console.WriteLine($"build lambda:   {lambdaTs.TotalMilliseconds}   {lambdaAlloc}");

                    Console.WriteLine($"calc nfun:     {calcFunTs.TotalMilliseconds}   {calcFunAlloc}");
                    Console.WriteLine($"calc evaluate: {calcEvaluateTs.TotalMilliseconds}   {calcEvaluateAlloc}");
                    Console.WriteLine($"calc lambda:   {calcLambdaTs.TotalMilliseconds}   {calcLambdaAlloc}");

                    Console.WriteLine($"python     :   {bcPyTs.TotalMilliseconds}    {bcPyAlloc}");
                    funTs = evaluateTs = lambdaTs = calcFunTs = calcEvaluateTs = calcLambdaTs = bcPyTs = TimeSpan.Zero;
                }
            }
        }
    }
}