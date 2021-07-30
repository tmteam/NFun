using System;
using System.Collections.Generic;
using IronPython.Hosting;
using NCalc;

namespace NFun.CompareToOthers
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            /*
             *(4 * 12 / 7) + ((9 * 2) % 8)	Simple Arithmetics	474,247.87	32,691,490.41	6,793.33%
        5 * 2 = 2 * 5 && (1 / 3.0) * 3 = 1	Simple Arithmetics	276,226.31	93,222,709.05	33,648.67%
                    [Param1] * 7 + [Param2]	Constant Values	707,493.27	21,766,101.47	2,976.51%
                    [Param1] * 7 + [Param2]	Dynamic Values	582,832.10	21,400,445.13	3,571.80%
                Foo([Param1] * 7, [Param2])	Dynamic Values and Function Call	594,259.69	17,209,334.34	2,795.93%
             * 
             */

            var batchIterations = 2_000;
            var batchCount = 100;


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


            var pyEx1 = "10*x + 1";
            var funEx1 = "x:real; out:real = 10*x+ 1";
            var ncalcEx1 = "10*[x] + 1";

            var pyscope = engine.CreateScope(new Dictionary<string, object> { { "x", 42 } });
            Action buildEva = () => new Expression(ncalcEx1);
            Action buildLambda = () => new Expression(ncalcEx1).ToLambda<Expressioncalculator, double>();
            Action buildFun = () => Funny.Hardcore.Build(funEx1);
            pyscope.SetVariable("x", 12.0);
            Action pythonBuildNRun = () => { engine.Execute(pyEx1, pyscope); };

            var expression = new Expression(ncalcEx1);
            var lambda = expression.ToLambda<Expressioncalculator, double>();
            var funrt = Funny.Hardcore.Build(funEx1);
            expression.Parameters["x"] = 12.0;
            Action calcEva = () => { expression.Evaluate(); };
            var calculator = new Expressioncalculator { x = 12 };
            Action calcLambda = () => lambda(calculator);

            funrt["x"].Value = 12.0;
            Action calcFun = () => funrt.Run();

            MeasureAll(
                buildEva,
                buildLambda: buildLambda,
                buildFun: buildFun,
                buildNCalcPy: pythonBuildNRun,
                calcEva: calcEva,
                calcLambda: calcLambda,
                calcFun: calcFun,
                batchIterations: batchIterations,
                batchCount: batchCount);
        }

        private static void MeasureAll(Action buildEva, int batchIterations, Action buildLambda, Action buildFun,
            Action calcEva, Action calcLambda, Action calcFun, Action buildNCalcPy, int batchCount)
        {
            Console.WriteLine("Start measuring");
            Console.WriteLine("Heating");
            BenchHelper.Measure(buildEva, batchIterations, out _);
            BenchHelper.Measure(buildLambda, batchIterations, out _);
            BenchHelper.Measure(buildFun, batchIterations, out _);
            BenchHelper.Measure(calcEva, batchIterations, out _);
            BenchHelper.Measure(calcLambda, batchIterations, out _);
            BenchHelper.Measure(calcFun, batchIterations, out _);
            BenchHelper.Measure(buildNCalcPy, batchIterations, out _);


            var buildEvaluateTs = TimeSpan.Zero;
            var buildlambdaTs = TimeSpan.Zero;
            var buildFunTs = TimeSpan.Zero;

            var calcEvaluateTs = TimeSpan.Zero;
            var calcLambdaTs = TimeSpan.Zero;
            var calcFunTs = TimeSpan.Zero;
            var bcPyTs = TimeSpan.Zero;

            Console.WriteLine("Iterating");
            var i = 0;
            while (true)
            {
                i++;
                buildEvaluateTs += BenchHelper.Measure(buildEva, batchIterations, out var evaluateAlloc);
                buildlambdaTs += BenchHelper.Measure(buildLambda, batchIterations, out var lambdaAlloc);
                buildFunTs += BenchHelper.Measure(buildFun, batchIterations, out var funAlloc);
                calcEvaluateTs += BenchHelper.Measure(calcEva, batchIterations, out var calcEvaluateAlloc);
                calcLambdaTs += BenchHelper.Measure(calcLambda, batchIterations, out var calcLambdaAlloc);
                calcFunTs += BenchHelper.Measure(calcFun, batchIterations, out var calcFunAlloc);
                bcPyTs += BenchHelper.Measure(buildNCalcPy, batchIterations, out var bcPyAlloc);
                if (i > batchCount)
                {
                    i = 0;
                    Console.Clear();

                    var iterations = batchCount * batchIterations;

                    Console.WriteLine($"{iterations} iterations done");
                    PrintResult("build nfun    ", buildFunTs, iterations, funAlloc);
                    PrintResult("build evaluate", buildEvaluateTs, iterations, evaluateAlloc);
                    PrintResult("build lambda  ", buildlambdaTs, iterations, lambdaAlloc);
                    PrintResult("calc nfun    ", calcFunTs, iterations, calcFunAlloc);
                    PrintResult("calc evaluate", calcEvaluateTs, iterations, calcEvaluateAlloc);
                    PrintResult("calc lambda  ", calcLambdaTs, iterations, calcLambdaAlloc);
                    PrintResult("python       ", bcPyTs, iterations, bcPyAlloc);

                    buildFunTs = buildEvaluateTs =
                        buildlambdaTs = calcFunTs = calcEvaluateTs = calcLambdaTs = bcPyTs = TimeSpan.Zero;
                }
            }

            static void PrintResult(string name, TimeSpan time, int iterations, long allocated)
            {
                Console.WriteLine(
                    $"{name}: {time.Multiply(1_000).TotalMilliseconds / iterations:F3} mcs, allocated: {(double)allocated / iterations:F0} bytes");
            }
        }

        private class Expressioncalculator
        {
            public double x { get; set; }
        }
    }
}