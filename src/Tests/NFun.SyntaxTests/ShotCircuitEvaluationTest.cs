using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Short-circuit / lazy evaluation tests: `and`, `or`, `??` skip the second
/// operand when the first decides the result. `xor` does not short-circuit.
/// Uses `oops()` (Specs/Basics.md §6.4) as a sentinel — if the second operand
/// is evaluated, the script throws; otherwise the first-operand result wins.
/// </summary>
[TestFixture]
public class ShotCircuitEvaluationTest {

    // ── oops() sanity — throws unconditionally ─────────────────────

    [TestCase("y:int = oops()")]
    [TestCase("y = 1 + oops()")]
    public void OopsAlwaysThrows(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());

    // ── Short-circuit: `and` ───────────────────────────────────────

    [TestCase("y = false and oops()",                 false)]
    [TestCase("y = true and false",                   false)]
    [TestCase("y = false and (false and oops())",     false)] // nested
    [TestCase("y = true or (false and oops())",       true)]  // mixed and/or
    [TestCase("x = false\r y = x and oops()",         false)] // via variable
    public void And_ShortCircuit(string expr, bool expected) =>
        expr.AssertResultHas("y", expected);

    [TestCase("y = true and oops()")]
    public void AndOnTrue_EvaluatesSecond_AndThrows(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());

    // ── Short-circuit: `or` ────────────────────────────────────────

    [TestCase("y = true or oops()",                  true)]
    [TestCase("y = false or true",                   true)]
    [TestCase("y = true or (true or oops())",        true)] // nested
    [TestCase("x = true\r y = x or oops()",          true)] // via variable
    public void Or_ShortCircuit(string expr, bool expected) =>
        expr.AssertResultHas("y", expected);

    [TestCase("y = false or oops()")]
    public void OrOnFalse_EvaluatesSecond_AndThrows(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());

    // ── Short-circuit: `??` (null coalesce) ────────────────────────

    [TestCase("y = 42 ?? oops()",            42)]
    [TestCase("y = none ?? 42",              42)]
    [TestCase("y = 1 ?? 2 ?? oops()",        1)]
    [TestCase("y = (none ?? 5) + 10",        15)]
    public void Coalesce_ShortCircuit(string expr, object expected) {
        var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        r.Run();
        Assert.AreEqual(expected, r["y"].Value);
    }

    [TestCase("y = none ?? oops()")]
    public void CoalesceOnNone_EvaluatesFallback_AndThrows(string expr) {
        Assert.Throws<FunnyRuntimeException>(() => {
            var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
            r.Run();
        });
    }

    // ── `xor` is NOT lazy: both args always evaluated ──────────────

    [TestCase("y = false xor oops()")]
    public void Xor_NotLazy_AlwaysThrows(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());
}
