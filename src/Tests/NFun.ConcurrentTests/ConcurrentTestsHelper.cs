using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFun.Runtime;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ConcurrentTests;

public static class ConcurrentTestsHelper {
    private const int TestsConcurrentDegree = 20; // minimum 10, maximum 300
    //10 -> 3sec
    //100 = 1min
    //200 = 5min
    //300 = 15min

    public static void AssertConcurrentHardcore(this string originExpr, Action<FunnyRuntime> action)
        => originExpr.Build().AssertConcurrentHardcore(action);

    public static void AssertConcurrentHardcore(this string origin) => origin.AssertRuntimes();

    public static void AssertConcurrentHardcore(this FunnyRuntime origin, Action<FunnyRuntime> action) {
        var runtime = origin;
        var clones = new ConcurrentQueue<FunnyRuntime>();
        clones.Enqueue(origin);

        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree / 5), _ => {
            var clone = runtime.Clone();
            var clone2 = clone.Clone();
            var clone3 = clone2.Clone();
            clones.Enqueue(clone);
            clones.Enqueue(clone2);
            clones.Enqueue(clone3);
        });
        Parallel.ForEach(clones,
            new ParallelOptions { MaxDegreeOfParallelism = 100 },
            rt => {
                for (int i = 0; i < TestsConcurrentDegree * 10; i++)
                {
                    action(rt);
                }
            });
    }

    public static void AssertStringTemplate(this StringTemplateCalculator origin,
        Action<StringTemplateCalculator> action) {
        var clones = new ConcurrentQueue<StringTemplateCalculator>();
        clones.Enqueue(origin);

        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree / 5), _ => {
            var clone = origin.Clone();
            var clone2 = clone.Clone();
            var clone3 = clone.Clone();
            clones.Enqueue(clone);
            clones.Enqueue(clone2);
            clones.Enqueue(clone3);
        });
        Parallel.ForEach(clones,
            new ParallelOptions { MaxDegreeOfParallelism = 100 },
            rt => {
                for (int i = 0; i < TestsConcurrentDegree * 10; i++)
                {
                    action(rt);
                }
            });
    }

    public static void CalcContextInDifferentWays<TContext>(this string expr, TContext origin, TContext expected)
        where TContext : ICloneable {
        var results = new ConcurrentQueue<object>();
        var calculator = Funny.BuildForCalcContext<TContext>();

        var action1 = calculator.ToLambda(expr);
        var action2 = calculator.ToLambda(expr);

        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree), _ => {
            for (int i = 0; i < TestsConcurrentDegree; i++)
            {
                var c1 = (TContext)origin.Clone();
                results.Enqueue(c1);
                calculator.Calc(expr, c1);

                var c2 = (TContext)origin.Clone();
                results.Enqueue(c2);
                calculator.Calc(expr, c2);

                var c3 = (TContext)origin.Clone();
                results.Enqueue(c3);
                action1(c3);

                var c4 = (TContext)origin.Clone();
                results.Enqueue(c4);
                action1(c4);

                var c5 = (TContext)origin.Clone();
                results.Enqueue(c5);
                action2(c5);

                var action3 = calculator.ToLambda(expr);
                var c6 = (TContext)origin.Clone();
                results.Enqueue(c6);
                action3(c6);

                var calculator2 = Funny.BuildForCalcContext<TContext>();

                var c7 = (TContext)origin.Clone();
                results.Enqueue(c7);
                calculator2.Calc(expr, c7);

                var c8 = (TContext)origin.Clone();
                results.Enqueue(c8);
                calculator2.ToLambda(expr)(c8);
            }
        });
        AssertAll(expected, results);
    }

    public static void CalcSingleUntypedInDifferentWays<TInput>(this string expr, object expected, TInput input) {
        var calculator = Funny.BuildForCalc<TInput>();
        var lambda1 = calculator.ToLambda(expr);
        var lambda2 = calculator.ToLambda(expr);

        var results = new ConcurrentQueue<object>();

        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree * 10), _ => {
            var result0 = calculator.Calc(expr, input);
            var result1 = calculator.Calc(expr, input);
            var result2 = lambda1(input);
            var result3 = lambda1(input);

            var result4 = lambda2(input);
            var result5 = lambda2(input);
            var result6 = Funny.Calc(expr, input);

            results.Enqueue(result0);
            results.Enqueue(result1);
            results.Enqueue(result2);
            results.Enqueue(result3);
            results.Enqueue(result4);
            results.Enqueue(result5);
            results.Enqueue(result6);
        });
        AssertAll(expected, results);
    }

    private static void AssertAll<TResult>(TResult expected, ConcurrentQueue<TResult> results) {
        var failedAnswers = new List<TResult>();
        foreach (var result in results)
        {
            if (!TestHelper.AreSame(expected, result))
            {
                failedAnswers.Add(result);
            }
        }

        Assert.IsTrue(failedAnswers.Count == 0, $"Failed results ({failedAnswers.Count} of {results.Count})." +
                                                $"\r\nExpected:\r\n {expected.ToStringSmart()}\r\n" +
                                                $"\r\nFailed items:\r\n" +
                                                string.Join(",\r\n", failedAnswers.Select(r => r?.ToStringSmart())));
    }

    public static void
        CalcSingleTypedInDifferentWays<TInput, TOutput>(this string expr, TInput input, TOutput expected) {
        var calculator = Funny.BuildForCalc<TInput, TOutput>();
        var lambda1 = calculator.ToLambda(expr);
        var lambda2 = calculator.ToLambda(expr);

        var results = new ConcurrentQueue<TOutput>();
        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree * 10), _ => {
            var result1 = Funny.Calc<TInput, TOutput>(expr, input);
            var result2 = calculator.Calc(expr, input);
            var result3 = calculator.Calc(expr, input);
            var result4 = lambda1(input);
            var result5 = lambda1(input);
            var result6 = lambda2(input);
            var result7 = lambda2(input);
            var result8 = Funny
                .WithConstant("SomeNotUsedConstant", 42)
                .BuildForCalc<TInput, TOutput>()
                .Calc(expr, input);

            results.Enqueue(result1);
            results.Enqueue(result2);
            results.Enqueue(result3);
            results.Enqueue(result4);
            results.Enqueue(result5);
            results.Enqueue(result6);
            results.Enqueue(result7);
            results.Enqueue(result8);
        });
        AssertAll(expected, results);
    }

    public static void
        CalcDynamicTypedInDifferentWays<TOutput>(this string expr, object input, TOutput expected) {
        var calculator = Funny.BuildForCalcDynamicInput<TOutput>(input.GetType());
        var lambda1 = calculator.ToLambda(expr);
        var lambda2 = calculator.ToLambda(expr);

        var results = new ConcurrentQueue<TOutput>();
        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree * 10), _ => {
            var result1 = Funny.CalcDynamicInput<TOutput>(expr, input);
            var result2 = calculator.Calc(expr, input);
            var result3 = calculator.Calc(expr, input);
            var result4 = lambda1(input);
            var result5 = lambda1(input);
            var result6 = lambda2(input);
            var result7 = lambda2(input);
            var result8 = Funny
                .WithConstant("SomeNotUsedConstant", 42)
                .BuildForCalcDynamicInput<TOutput>(input.GetType())
                .Calc(expr, input);

            results.Enqueue(result1);
            results.Enqueue(result2);
            results.Enqueue(result3);
            results.Enqueue(result4);
            results.Enqueue(result5);
            results.Enqueue(result6);
            results.Enqueue(result7);
            results.Enqueue(result8);
        });
        AssertAll(expected, results);
    }

    public static void
        CalcDynamicInDifferentWays(this string expr, object input, object expected) {
        var calculator = Funny.BuildForCalcDynamicInput(input.GetType());
        var lambda1 = calculator.ToLambda(expr);
        var lambda2 = calculator.ToLambda(expr);

        var results = new ConcurrentQueue<object>();
        Parallel.ForEach(Enumerable.Range(0, TestsConcurrentDegree * 10), _ => {
            var result1 = Funny.CalcDynamicInput(expr, input);
            var result2 = calculator.Calc(expr, input);
            var result3 = calculator.Calc(expr, input);
            var result4 = lambda1(input);
            var result5 = lambda1(input);
            var result6 = lambda2(input);
            var result7 = lambda2(input);
            var result8 = Funny
                .WithConstant("SomeNotUsedConstant", 42)
                .BuildForCalcDynamicInput(input.GetType())
                .Calc(expr, input);

            results.Enqueue(result1);
            results.Enqueue(result2);
            results.Enqueue(result3);
            results.Enqueue(result4);
            results.Enqueue(result5);
            results.Enqueue(result6);
            results.Enqueue(result7);
            results.Enqueue(result8);
        });
        AssertAll(expected, results);
    }
}
