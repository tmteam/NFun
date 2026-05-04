using System;
using System.Diagnostics;
using NFun.VM;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

[TestFixture]
public class RegisterVMTest {

    // y = 2 * x + 1, where x=21 → y=43
    // locals: r0=y(out), r1=x(in), r2=temp
    // constants: c0=2, c1=1
    [Test]
    public void PureArith_2xPlus1() {
        var code = new byte[] {
            (byte)RegisterOp.MulRI_I, 2, 1, 0,  // r2 = r1 * c0  (x * 2)
            (byte)RegisterOp.AddRI_I, 2, 2, 1,  // r2 = r2 + c1  (+ 1)
            (byte)RegisterOp.Mov,     0, 2, 0,  // r0 = r2       (y = result)
            (byte)RegisterOp.Halt,    0, 0, 0,
        };
        var locals = new FunValue[3];
        locals[1].I64 = 21; // x = 21
        var constants = new FunValue[] { FunValue.FromI64(2), FunValue.FromI64(1) };

        RegisterVM.Execute(code, locals, constants);
        Assert.AreEqual(43, locals[0].I64);
    }

    // y = if(x > 0) x else -x, where x=21 → y=21; x=-5 → y=5
    [Test]
    public void IfElse_Abs() {
        var code = new byte[] {
            // 0: GT r2, r1, c0   (r2 = x > 0)
            (byte)RegisterOp.GtRI_I,   2, 1, 0,
            // 4: JmpIfNot r2, → 16
            (byte)RegisterOp.JmpIfNot, 2, 0, 16,
            // 8: Mov r0, r1 (y = x)
            (byte)RegisterOp.Mov,      0, 1, 0,
            // 12: Halt
            (byte)RegisterOp.Halt,     0, 0, 0,
            // 16: Neg r0, r1 (y = -x)
            (byte)RegisterOp.NegR_I,   0, 1, 0,
            // 20: Halt
            (byte)RegisterOp.Halt,     0, 0, 0,
        };
        var constants = new FunValue[] { FunValue.FromI64(0) };

        // x = 21
        var locals = new FunValue[3];
        locals[1].I64 = 21;
        RegisterVM.Execute(code, locals, constants);
        Assert.AreEqual(21, locals[0].I64);

        // x = -5
        locals[1].I64 = -5;
        RegisterVM.Execute(code, locals, constants);
        Assert.AreEqual(5, locals[0].I64);
    }

    // Benchmark: y = 2*x+1 — register VM vs stack VM
    [Test]
    public void Bench_RegisterVsStack() {
        // ── Register VM ──
        var regCode = new byte[] {
            (byte)RegisterOp.MulRI_I, 2, 1, 0,
            (byte)RegisterOp.AddRI_I, 2, 2, 1,
            (byte)RegisterOp.Mov,     0, 2, 0,
            (byte)RegisterOp.Halt,    0, 0, 0,
        };
        var regLocals = new FunValue[3];
        var regConsts = new FunValue[] { FunValue.FromI64(2), FunValue.FromI64(1) };

        // ── Stack VM ──
        var stackVm = Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM("y = 2 * x + 1");

        // ── Tree-walker ──
        var tw = Funny.Hardcore.WithApriori("x", FunnyType.Int32).Build("y = 2 * x + 1");

        // Warmup
        for (int i = 0; i < 1000; i++) {
            regLocals[1].I64 = i;
            RegisterVM.Execute(regCode, regLocals, regConsts);
            stackVm.SetInput("x", i);
            stackVm.Run();
            tw["x"].Value = i;
            tw.Run();
        }

        const int N = 500_000;
        var sw = Stopwatch.StartNew();

        // Register VM
        sw.Restart();
        for (int i = 0; i < N; i++) {
            regLocals[1].I64 = i;
            RegisterVM.Execute(regCode, regLocals, regConsts);
        }
        var regTime = sw.Elapsed;

        // Stack VM
        sw.Restart();
        for (int i = 0; i < N; i++) {
            stackVm.SetInput("x", i);
            stackVm.Run();
        }
        var stackTime = sw.Elapsed;

        // Tree-walker
        sw.Restart();
        for (int i = 0; i < N; i++) {
            tw["x"].Value = i;
            tw.Run();
        }
        var twTime = sw.Elapsed;

        var regNs = regTime.TotalMilliseconds * 1_000_000 / N;
        var stackNs = stackTime.TotalMilliseconds * 1_000_000 / N;
        var twNs = twTime.TotalMilliseconds * 1_000_000 / N;

        TestContext.WriteLine($"y = 2*x+1 ({N:N0} iterations):");
        TestContext.WriteLine($"  Register VM: {regNs:F1} ns/op");
        TestContext.WriteLine($"  Stack VM:    {stackNs:F1} ns/op  ({stackNs/regNs:F2}x vs register)");
        TestContext.WriteLine($"  Tree-walker: {twNs:F1} ns/op  ({twNs/regNs:F2}x vs register)");

        // Verify correctness
        regLocals[1].I64 = 21;
        RegisterVM.Execute(regCode, regLocals, regConsts);
        Assert.AreEqual(43, regLocals[0].I64);
    }
}
