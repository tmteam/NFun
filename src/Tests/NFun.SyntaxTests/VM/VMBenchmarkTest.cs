using System;
using System.Diagnostics;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

/// <summary>
/// Micro-benchmarks comparing VM vs tree-walker execution.
/// Not scientifically rigorous — just directional indicators.
/// </summary>
[TestFixture]
public class VMBenchmarkTest {

    [Test]
    public void SimpleArithmetic_VMvsTreeWalker() {
        const string expr = "y = 2 * x + 1";
        const int iterations = 100_000;

        // Tree-walker
        var tw = Funny.Hardcore.WithApriori("x", FunnyType.Int32).Build(expr);
        tw["x"].Value = 10;
        tw.Run(); // warmup

        var swTW = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) {
            tw["x"].Value = i;
            tw.Run();
        }
        swTW.Stop();

        // VM
        var vm = Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM(expr);
        vm.SetInput("x", 10);
        vm.Run(); // warmup

        var swVM = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) {
            vm.SetInput("x", i);
            vm.Run();
        }
        swVM.Stop();

        var ratio = (double)swTW.ElapsedTicks / swVM.ElapsedTicks;

        TestContext.WriteLine($"Expression: {expr}");
        TestContext.WriteLine($"Iterations: {iterations:N0}");
        TestContext.WriteLine($"Tree-walker: {swTW.ElapsedMilliseconds}ms ({swTW.ElapsedTicks / iterations} ticks/iter)");
        TestContext.WriteLine($"VM:          {swVM.ElapsedMilliseconds}ms ({swVM.ElapsedTicks / iterations} ticks/iter)");
        TestContext.WriteLine($"VM speedup:  {ratio:F2}x");

        // VM should not be catastrophically slower
        Assert.Greater(ratio, 0.1, "VM should not be more than 10x slower than tree-walker");
    }

    [Test]
    public void ComplexExpression_VMvsTreeWalker() {
        const string expr = "a = 10\r b = a * 2 + 3\r c = if(b > 20) b else -b\r y = c + 1";
        const int iterations = 50_000;

        var tw = Funny.Hardcore.Build(expr);
        tw.Run();

        var swTW = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) tw.Run();
        swTW.Stop();

        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();

        var swVM = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) vm.Run();
        swVM.Stop();

        var ratio = (double)swTW.ElapsedTicks / swVM.ElapsedTicks;

        TestContext.WriteLine($"Expression: {expr}");
        TestContext.WriteLine($"Iterations: {iterations:N0}");
        TestContext.WriteLine($"Tree-walker: {swTW.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"VM:          {swVM.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"VM speedup:  {ratio:F2}x");

        Assert.Greater(ratio, 0.1);
    }

    [Test]
    public void BuildTime_VMvsTreeWalker() {
        const string expr = "a = 10\r b = a * 2 + 3\r c = if(b > 20) b else -b\r y = c + 1";
        const int iterations = 1000;

        // Tree-walker build
        var swTW = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            Funny.Hardcore.Build(expr);
        swTW.Stop();

        // VM build
        var swVM = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            Funny.Hardcore.BuildVM(expr);
        swVM.Stop();

        var ratio = (double)swTW.ElapsedTicks / swVM.ElapsedTicks;

        TestContext.WriteLine($"Build iterations: {iterations:N0}");
        TestContext.WriteLine($"Tree-walker build: {swTW.ElapsedMilliseconds}ms ({swTW.ElapsedTicks / iterations} ticks/build)");
        TestContext.WriteLine($"VM build:          {swVM.ElapsedMilliseconds}ms ({swVM.ElapsedTicks / iterations} ticks/build)");
        TestContext.WriteLine($"VM build ratio:    {ratio:F2}x (>1 = VM faster build)");
    }
}
