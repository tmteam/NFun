using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests for bug hunt session 10 fixes:
/// - Bug 4: catch(e) variable leaks as unbound input
/// - Bug 6: `not 42` crashes with ArgumentOutOfRangeException
/// - Bug 9: `~true` crashes with NFunImpossibleException
/// - Bug 11: CLI displays none as "null" (no code test — manual verification)
/// </summary>
[TestFixture]
public class BugHunt10FixesTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ══════════════════════════════════════════════════════════════
    // Bug 4: catch(e) variable leaks as unbound input
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void Bug4_CatchVariable_DoesNotLeakAsInput() {
        var runtime = "y = try oops('hello') catch(e) e.message".Build();
        // The error variable 'e' should NOT appear as a script-level input
        foreach (var v in runtime.Variables)
            Assert.AreNotEqual("e", v.Name,
                "catch(e) variable 'e' should not leak as a script-level variable");
    }

    [Test]
    public void Bug4_CatchVariable_NoInputs() {
        var runtime = "y = try 42 catch(e) 0".Build();
        // Script has no inputs — only output 'y'
        runtime.AssertInputsCount(0, "catch(e) should not create input variables");
    }

    [Test]
    public void Bug4_CatchVariable_StillWorks() =>
        "y = try oops('hello') catch(e) e.message".AssertReturns("y", "hello");

    [Test]
    public void Bug4_CatchVariable_CustomName_DoesNotLeak() {
        var runtime = "y = try oops('test') catch(err) err.message".Build();
        foreach (var v in runtime.Variables)
            Assert.AreNotEqual("err", v.Name,
                "catch(err) variable 'err' should not leak as a script-level variable");
    }

    [Test]
    public void Bug4_CatchVariable_WithExternalInput() {
        // Ensure real inputs still work alongside catch variables
        var runtime = "y = try x + oops('fail') catch(e) x".Build();
        runtime["x"].Value = 42;
        runtime.Run();
        Assert.AreEqual(42, runtime["y"].Value);
        // Only 'x' and 'y' should be variables, not 'e'
        foreach (var v in runtime.Variables)
            Assert.AreNotEqual("e", v.Name,
                "catch(e) variable 'e' should not leak when real inputs exist");
    }

    // ══════════════════════════════════════════════════════════════
    // Bug 6: `not 42` crashes with ArgumentOutOfRangeException
    // ══════════════════════════════════════════════════════════════

    [TestCase("y = not 42")]
    [TestCase("y = not 3.14")]
    [TestCase("y = if(42) 1 else 2")]
    [TestCase("y = if(0) 'a' else 'b'")]
    public void Bug6_BoolOperatorOnNumeric_GivesTypeError(string expr) =>
        expr.AssertObviousFailsOnParse();

    // ══════════════════════════════════════════════════════════════
    // Bug 9: `~true` crashes with NFunImpossibleException
    // ══════════════════════════════════════════════════════════════

    [TestCase("y = ~true")]
    [TestCase("y = ~false")]
    [TestCase("y = true & 42")]
    [TestCase("y = false | 7")]
    public void Bug9_BitwiseOnBool_GivesTypeError(string expr) =>
        expr.AssertObviousFailsOnParse();
}
