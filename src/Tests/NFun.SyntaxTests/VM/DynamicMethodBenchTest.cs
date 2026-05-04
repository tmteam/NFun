using System;
using System.Diagnostics;
using System.Reflection.Emit;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

[TestFixture]
public class DynamicMethodBenchTest {

    [Test]
    public void Bench_DynamicMethod_BuildAndRun() {
        // ── DynamicMethod Build cost ──
        var sw = Stopwatch.StartNew();
        const int BuildN = 5000;

        Func<long, long> lastFn = null;
        for (int iter = 0; iter < BuildN; iter++) {
            var dm = new DynamicMethod($"eval_{iter}", typeof(long), new[] { typeof(long) },
                typeof(DynamicMethodBenchTest).Module);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);   // x
            il.Emit(OpCodes.Ldc_I8, 2L);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Ldc_I8, 1L);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);
            lastFn = (Func<long, long>)dm.CreateDelegate(typeof(Func<long, long>));
        }
        var buildUs = sw.Elapsed.TotalMilliseconds / BuildN * 1000;

        // ── DynamicMethod Run cost ──
        // Warmup
        for (int i = 0; i < 10000; i++) lastFn((long)i);

        const int RunN = 2_000_000;
        sw.Restart();
        long sum = 0;
        for (int i = 0; i < RunN; i++) sum += lastFn((long)i);
        var runNs = sw.Elapsed.TotalMilliseconds * 1_000_000 / RunN;

        // ── NFun tree-walker for comparison ──
        var tw = Funny.Hardcore.WithApriori("x", FunnyType.Int32).Build("y = 2 * x + 1");
        for (int i = 0; i < 1000; i++) { tw["x"].Value = i; tw.Run(); }
        sw.Restart();
        for (int i = 0; i < RunN; i++) { tw["x"].Value = i; tw.Run(); }
        var twRunNs = sw.Elapsed.TotalMilliseconds * 1_000_000 / RunN;

        // ── NFun VM (register) for comparison ──
        var vm = Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM("y = 2 * x + 1");
        for (int i = 0; i < 1000; i++) { vm.SetInput("x", i); vm.Run(); }
        sw.Restart();
        for (int i = 0; i < RunN; i++) { vm.SetInput("x", i); vm.Run(); }
        var vmRunNs = sw.Elapsed.TotalMilliseconds * 1_000_000 / RunN;

        // ── NFun Build cost comparison ──
        sw.Restart();
        for (int i = 0; i < BuildN; i++)
            Funny.Hardcore.WithApriori("x", FunnyType.Int32).Build("y = 2 * x + 1");
        var twBuildUs = sw.Elapsed.TotalMilliseconds / BuildN * 1000;

        sw.Restart();
        for (int i = 0; i < BuildN; i++)
            Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM("y = 2 * x + 1");
        var vmBuildUs = sw.Elapsed.TotalMilliseconds / BuildN * 1000;

        TestContext.WriteLine($"y = 2*x+1 benchmark:");
        TestContext.WriteLine($"");
        TestContext.WriteLine($"  BUILD (μs per build):");
        TestContext.WriteLine($"    DynamicMethod:  {buildUs:F1} μs");
        TestContext.WriteLine($"    NFun Build():   {twBuildUs:F1} μs");
        TestContext.WriteLine($"    NFun BuildVM(): {vmBuildUs:F1} μs");
        TestContext.WriteLine($"");
        TestContext.WriteLine($"  RUN (ns per call, {RunN:N0} iterations):");
        TestContext.WriteLine($"    DynamicMethod:  {runNs:F1} ns");
        TestContext.WriteLine($"    NFun VM:        {vmRunNs:F1} ns");
        TestContext.WriteLine($"    NFun TreeWalker:{twRunNs:F1} ns");
        TestContext.WriteLine($"");
        TestContext.WriteLine($"  Verify: DynMethod(21)={lastFn(21)}, sum={sum}");

        Assert.AreEqual(43, lastFn(21));
    }
}
