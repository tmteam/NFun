using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting (300 iterations, 2026-05-05).
/// Each test is a confirmed bug — expected behavior per specification
/// does not match actual behavior.
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    // Bug #1: convert(bool)->int crashes with unhandled InvalidOperationException
    // instead of a graceful compile-time error. Spec does not list bool->int conversion.
    [Test][Ignore("Bug hunt #1: convert(true) to int crashes with InvalidOperationException")]
    public void Bug1_ConvertBoolToIntCrash() {
        Assert.DoesNotThrow(() => "y:int = convert(true)".Calc());
    }

    // Bug #2: 3-level nested inline optional struct expressions fail TIC.
    // 2 levels work; 3 levels produce "Unable to cast from none to {z:Int32}".
    [Test][Ignore("Bug hunt #2: 3-level nested optional struct inline fails TIC")]
    public void Bug2_ThreeLevelNestedOptionalStructFails() {
        "s = {x = if(true) {y = if(true) {z = 42} else none} else none}\r out = s.x?.y?.z ?? 0"
            .AssertReturns("out", 42);
    }

    // Bug #3: compact() not implemented. Spec (Optionals.md line 489) says
    // compact() is a synonym for filterNotNull(), but it's not in the codebase.
    [Test][Ignore("Bug hunt #3: compact() synonym for filterNotNull() not implemented")]
    public void Bug3_CompactNotImplemented() {
        "y = [1, none, 3].compact()".AssertReturns("y", new[] { 1, 3 });
    }
}
