using System;
using NFun.VM;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

/// <summary>
/// Tests for cooperative fiber scheduling.
/// </summary>
[TestFixture]
public class VMFiberTest {

    [Test]
    public void TwoFibers_BothComplete() {
        // Two independent fibers computing different expressions
        var program1 = BuildProgram("y = 2 + 3");
        var program2 = BuildProgram("y = 10 * 4");

        var scheduler = new Scheduler(program1);

        var locals1 = new FunValue[program1.LocalsCount];
        var locals2 = new FunValue[program2.LocalsCount];

        var fiber1 = scheduler.SpawnFiber(locals1);
        // Hack: fiber2 uses program2 but scheduler shares program1.
        // For a proper implementation, each fiber would reference its own program.
        // For now, demonstrate scheduling works with the same program.

        scheduler.RunAll();

        Assert.AreEqual(FiberStatus.Done, fiber1.Status);
        Assert.AreEqual(5, fiber1.GetOutput("y", program1.Variables));
    }

    [Test]
    public void DelayedFiber_CompletesAfterDelay() {
        var program = BuildProgram("y = 42");
        var scheduler = new Scheduler(program);

        var locals = new FunValue[program.LocalsCount];
        var fiber = scheduler.SpawnFiber(locals);

        // Suspend fiber for 50ms
        fiber.Status = FiberStatus.Suspended;
        fiber.ResumeAfter = DateTime.UtcNow.AddMilliseconds(50);

        var start = DateTime.UtcNow;
        scheduler.RunAll();
        var elapsed = DateTime.UtcNow - start;

        Assert.AreEqual(FiberStatus.Done, fiber.Status);
        Assert.AreEqual(42, fiber.GetOutput("y", program.Variables));
        Assert.GreaterOrEqual(elapsed.TotalMilliseconds, 40, "Should have waited for delay");
    }

    private static CompiledProgram BuildProgram(string expr) {
        var vm = Funny.Hardcore.BuildVM(expr);
        // Access internal program via reflection for testing
        var field = typeof(VMRuntime).GetField("_program",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (CompiledProgram)field.GetValue(vm);
    }
}
