using System;
using System.Collections.Generic;
using IronPython.Hosting;
using NCalc;

namespace NFun.CompareToOthers;

internal class Program {
    private static void Main(string[] args) {
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

        var pyEx1 = "10*x + 1";
        var funEx1 = "x:real; out:real = 10x+ 1";
        var ncalcEx1 = "10*[x] + 1";

        var pyscope = engine.CreateScope(new Dictionary<string, object> { { "x", 42 } });
        Action buildNcalcEva = () => new Expression(ncalcEx1);
        Action buildNcalcLambda = () => new Expression(ncalcEx1).ToLambda<Expressioncalculator, double>();
        Action buildNFun = () => Funny.Hardcore.Build(funEx1);
        pyscope.SetVariable("x", 12.0);
        Action pythonBuildNRun = () => { engine.Execute(pyEx1, pyscope); };

        var expression = new Expression(ncalcEx1);
        var lambda = expression.ToLambda<Expressioncalculator, double>();
        var funrt = Funny.Hardcore.Build(funEx1);
        expression.Parameters["x"] = 12.0;
        Action calcNcalcEva = () => { expression.Evaluate(); };
        var calculator = new Expressioncalculator { x = 12 };
        Action calcNcalcLambda = () => lambda(calculator);

        funrt["x"].Value = 12.0;
        Action calcNFun = () => funrt.Run();

        MeasureAll(
            buildNcalcEva: buildNcalcEva,
            buildNcalcLambda: buildNcalcLambda,
            buildNFun: buildNFun,
            buildNCalcPy: pythonBuildNRun,
            calcNcalcEva: calcNcalcEva,
            calcNcalcLambda: calcNcalcLambda,
            calcNFun: calcNFun,
            batchIterations: batchIterations,
            batchCount: batchCount);
    }

    private static void MeasureAll(
        Action buildNcalcEva,Action buildNcalcLambda, Action buildNFun,
        Action calcNcalcEva, Action calcNcalcLambda, Action calcNFun, Action buildNCalcPy,int batchIterations, int batchCount) {
        Console.WriteLine("Start measuring");
        Console.WriteLine("Heating");
        BenchHelper.Measure(buildNcalcEva, batchIterations, out _);
        BenchHelper.Measure(buildNcalcLambda, batchIterations, out _);
        BenchHelper.Measure(buildNFun, batchIterations, out _);
        BenchHelper.Measure(calcNcalcEva, batchIterations, out _);
        BenchHelper.Measure(calcNcalcLambda, batchIterations, out _);
        BenchHelper.Measure(calcNFun, batchIterations, out _);
        BenchHelper.Measure(buildNCalcPy, batchIterations, out _);


        var buildNcalcEvaluateTs = TimeSpan.Zero;
        var buildNcalclambdaTs = TimeSpan.Zero;
        var buildNFunTs = TimeSpan.Zero;

        var calcNcalcEvaluateTs = TimeSpan.Zero;
        var calcNcalcLambdaTs = TimeSpan.Zero;
        var calcNFunTs = TimeSpan.Zero;
        var bcPyTs = TimeSpan.Zero;

        Console.WriteLine("Iterating");
        var i = 0;
        while (true)
        {
            i++;
            buildNcalcEvaluateTs += BenchHelper.Measure(buildNcalcEva, batchIterations, out var evaluateAlloc);
            buildNcalclambdaTs += BenchHelper.Measure(buildNcalcLambda, batchIterations, out var lambdaAlloc);
            buildNFunTs += BenchHelper.Measure(buildNFun, batchIterations, out var funAlloc);
            calcNcalcEvaluateTs += BenchHelper.Measure(calcNcalcEva, batchIterations, out var calcEvaluateAlloc);
            calcNcalcLambdaTs += BenchHelper.Measure(calcNcalcLambda, batchIterations, out var calcLambdaAlloc);
            calcNFunTs += BenchHelper.Measure(calcNFun, batchIterations, out var calcFunAlloc);
            bcPyTs += BenchHelper.Measure(buildNCalcPy, batchIterations, out var bcPyAlloc);
            if (i > batchCount)
            {
                i = 0;
                Console.Clear();

                var iterations = batchCount * batchIterations;

                Console.WriteLine($"{iterations} iterations done");
                PrintResult("NFun build          ", buildNFunTs, iterations, funAlloc);
                PrintResult("NCalc build evaluate", buildNcalcEvaluateTs, iterations, evaluateAlloc);
                PrintResult("NCalc build lambda  ", buildNcalclambdaTs, iterations, lambdaAlloc);
                PrintResult("NFun calc           ", calcNFunTs, iterations, calcFunAlloc);
                PrintResult("NCalc calc evaluate ", calcNcalcEvaluateTs, iterations, calcEvaluateAlloc);
                PrintResult("NCalc calc lambda   ", calcNcalcLambdaTs, iterations, calcLambdaAlloc);
                PrintResult("python              ", bcPyTs, iterations, bcPyAlloc);

                buildNFunTs = buildNcalcEvaluateTs =
                    buildNcalclambdaTs = calcNFunTs = calcNcalcEvaluateTs = calcNcalcLambdaTs = bcPyTs = TimeSpan.Zero;
            }
        }

        static void PrintResult(string name, TimeSpan time, int iterations, long allocated) {
            Console.WriteLine(
                $"{name}: {time.Multiply(1_000).TotalMilliseconds / iterations:F3} mcs, allocated: {(double)allocated / iterations:F0} bytes");
        }
    }

    private class Expressioncalculator {
        public double x { get; set; }
    }
}
