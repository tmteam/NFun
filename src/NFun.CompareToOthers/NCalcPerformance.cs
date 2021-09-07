using System;
using NCalc;

namespace NFun.CompareToOthers {

public class NCalcPerformance {
    private const int Iterations = 100000;

    // [Theory]
    // [InlineData("(4 * 12 / 7) + ((9 * 2) % 8)")]
    // [InlineData("5 * 2 = 2 * 5 && (1 / 3.0) * 3 = 1")]
    public void Arithmetics(string formula) {
        var expression = new Expression(formula);
        var lambda = expression.ToLambda<object>();

        var m1 = Measure(() => expression.Evaluate());
        var m2 = Measure(() => lambda());

        PrintResult(formula, m1, m2);
    }

    // [Theory]
    // [InlineData("[Param1] * 7 + [Param2]")]
    public void ParameterAccess(string formula) {
        var expression = new Expression(formula);
        var lambda = expression.ToLambda<calculator, int>();

        var calculator = new calculator { Param1 = 4, Param2 = 9 };
        expression.Parameters["Param1"] = 4;
        expression.Parameters["Param2"] = 9;

        var m1 = Measure(() => expression.Evaluate());
        var m2 = Measure(() => lambda(calculator));

        PrintResult(formula, m1, m2);
    }

    // [Theory]
    // [InlineData("[Param1] * 7 + [Param2]")]
    public void DynamicParameterAccess(string formula) {
        var expression = new Expression(formula);
        var lambda = expression.ToLambda<calculator, int>();

        var calculator = new calculator { Param1 = 4, Param2 = 9 };
        expression.EvaluateParameter += (name, args) => {
            if (name == "Param1") args.Result = calculator.Param1;
            if (name == "Param2") args.Result = calculator.Param2;
        };

        var m1 = Measure(() => expression.Evaluate());
        var m2 = Measure(() => lambda(calculator));

        PrintResult(formula, m1, m2);
    }

    // [Theory]
    // [InlineData("Foo([Param1] * 7, [Param2])")]
    public void FunctionWithDynamicParameterAccess(string formula) {
        var expression = new Expression(formula);
        var lambda = expression.ToLambda<calculator, int>();

        var calculator = new calculator { Param1 = 4, Param2 = 9 };
        expression.EvaluateParameter += (name, args) => {
            if (name == "Param1") args.Result = calculator.Param1;
            if (name == "Param2") args.Result = calculator.Param2;
        };
        expression.EvaluateFunction += (name, args) => {
            if (name == "Foo")
            {
                var param = args.EvaluateParameters();
                args.Result = calculator.Foo((int)param[0], (int)param[1]);
            }
        };

        var m1 = Measure(() => expression.Evaluate());
        var m2 = Measure(() => lambda(calculator));

        PrintResult(formula, m1, m2);
    }

    private static TimeSpan Measure(Action a) { return BenchHelper.Measure(a, Iterations, out _); }

    private static void PrintResult(string formula, TimeSpan m1, TimeSpan m2) {
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Formula: {0}", formula);
        Console.WriteLine("Expression: {0:N} evaluations / sec", Iterations / m1.TotalSeconds);
        Console.WriteLine("Lambda: {0:N} evaluations / sec", Iterations / m2.TotalSeconds);
        Console.WriteLine(
            "Lambda Speedup: {0:P}%",
            Iterations / m2.TotalSeconds / (Iterations / m1.TotalSeconds) - 1);
        Console.WriteLine(new string('-', 60));
    }

    private class calculator {
        public int Param1 { get; set; }
        public int Param2 { get; set; }

        public int Foo(int a, int b) { return Math.Min(a, b); }
    }
}

}