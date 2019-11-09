using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NFun;
using NFun.Interpritation;
using NFun.Jet;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Types;

namespace Nfun.CustomBenchmark
{
    class Program
    {
        static TimeSpan Measure(Action action, int times)
        {
            Stopwatch buildSw = Stopwatch.StartNew();
            for (int i = 0; i < times; i++)
            {
                action();
            }
            buildSw.Stop();
            return buildSw.Elapsed;
        }

        static double MeasureWithHeat(string name, Action action)
        {
            Measure(action, 100);
            Thread.Sleep(5);
            Measure(action, 200);
            GC.Collect();
            Thread.Sleep(5);
            List<double> results = new List<double>(100);
            for (int i = 0; i < 100; i++)
            {
                results.Add(Measure(action, 1000).TotalMilliseconds);
                Thread.Sleep(10);
            }

            Console.WriteLine($"{name} : {Math.Round(results.Average())} mks");
            return results.Average();
        }

        static EquationMeasureResult MeasureEquation(string name, string expression)
        {
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine($"----------{name}------- \r\n" + expression + "\r\n");

            FunRuntime runtime = null;

            try
            {
                runtime = FunBuilder.With(expression).Build();
            }
            catch (FunParseException e)
            {
                Console.WriteLine("parse failed: " + e.Message);
                return new EquationMeasureResult(name,0,0,0);
            }
            var buildTime = MeasureWithHeat("build  ",() => FunBuilder.With(expression).Build());


            MeasureWithHeat("jetting",() =>
            {
                var mvisitor = new JetSerializerVisitor();
                runtime.ApplyEntry(mvisitor);
                var mjetText = mvisitor.GetResult().ToString();
            });

            var visitor = new JetSerializerVisitor();
            runtime.ApplyEntry(visitor);
            var jetText = visitor.GetResult().ToString();

            var jetBuildTime = MeasureWithHeat("jet build", () =>
            {
                var functionsDictionary = new FunctionsDictionary(BaseFunctions.Functions);
                JetDeserializer.Deserialize(jetText, functionsDictionary);
            });

            Console.WriteLine("Size: " + jetText.Split().Length);
            return new EquationMeasureResult(name, buildTime, jetBuildTime, jetText.Split().Length);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Measurements start");
            FunctionsDictionary functionsDictionary = null;
            MeasureWithHeat("Initialize fun dictionary", () =>
            {
                functionsDictionary = new FunctionsDictionary(BaseFunctions.Functions);
            });
            MeasureWithHeat("Search overload",() =>
            {
                var res = functionsDictionary.GetOrNullConcrete("*", VarType.Real, VarType.Real, VarType.Real);
            });

            var results = new List<EquationMeasureResult>
            {
                MeasureEquation("const", "y = 42"),
                MeasureEquation("io", "y = x"),
                MeasureEquation("kxb", "y = 10*x+1"),

                MeasureEquation("mlt_2", "y = 21*2"),
                MeasureEquation("mlt_4", "y = 21*2*21*2"), 
                MeasureEquation("mlt_8", "y = 21*2*21*2*21*2*21*2"),
                MeasureEquation("mlt_16", "y = 21*2*21*2*21*2*21*2*21*2*21*2*21*2*21*2"),

                MeasureEquation("mlt_x2", "y = x*x"),
                MeasureEquation("mlt_x4", "y = x*x*x*x"),
                MeasureEquation("mlt_x8", "y = x*x*x*x*x*x*x*x"),
                MeasureEquation("mlt_x16", "y = x*x*x*x*x*x*x*x*x*x*x*x*x*x*x*x"),

                MeasureEquation("ifarr", "y = if (true) [1, 2, 3] else [0]"),
                MeasureEquation("funfun", "y = sqrt(10 * x ** x) + 12.5 * x + 13"),
                MeasureEquation("ifarr2", "y = if(x>3) [1,2,x]; if(x<3) [3,2,1]; else [0]"),
                MeasureEquation("iffun", "y = if(x**2>14) x+x-sqrt(x)+ max(x,2);  else x"),
                MeasureEquation("iffun2","y = if(x**2>14) x+x-sqrt(x)+ max(x,2); if (x-x ==0) x*x*x*x; else x;z = y * y * y")
            };

            results.Sort((c1,c2)=> c1.Size.CompareTo(c2.Size));
            Console.WriteLine();
            Console.WriteLine($"name\t\tsize   build    jet");
            Console.WriteLine("__________________________________");
            foreach (var e in results)
            {
                Console.WriteLine($"{e.Name}\t\t {e.Size:000}  {e.Build:##.###}  {e.JetBuild:##.###}");
            }
            Console.WriteLine($"Average\t\t {results.Select(r=>r.Size).Average():000}  {results.Select(r => r.Build).Average():##.###}  {results.Select(r=>r.JetBuild).Average():##.###}");
        }
    }

    class EquationMeasureResult
    {
        public EquationMeasureResult(string name, double build, double jetBuild, int size)
        {
            Name = name;
            Build = build;
            JetBuild = jetBuild;
            Size = size;
        }

        public string Name { get; }
        public double Build { get; }
        public double JetBuild { get; }
        public int Size { get; }
    }
}
