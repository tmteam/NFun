using NFun;
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

    // Bug #1: convert(bool)→numeric is now defined (true→1, false→0) instead
    // of throwing an unhandled InvalidOperationException.
    [Test]
    public void Bug1_ConvertBoolToInt_True() {
        "y:int = convert(true)".AssertReturns("y", 1);
    }

    [Test]
    public void Bug1_ConvertBoolToInt_False() {
        "y:int = convert(false)".AssertReturns("y", 0);
    }

    [Test]
    public void Bug1_ConvertBoolToReal() {
        "y:real = convert(true)".AssertReturns("y", 1.0);
    }

    // Bug #2: 3-level nested inline optional struct — fixed by master's
    // recursive-types / cycle-rescue work. Kept as a regression sentinel.
    [Test]
    public void Bug2_ThreeLevelNestedOptionalStruct() {
        "s = {x = if(true) {y = if(true) {z = 42} else none} else none}\r out = s.x?.y?.z ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 42);
    }

    // (Bug #3 was `compact()` — withdrawn per user decision: filterNotNull()
    // is the canonical name and we don't want an alias.)
}
