using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class LazyEvaluationTest {

    private static void AssertOptionalReturns(string expr, string varName, object expected) {
        var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(expected, r[varName].Value);
    }

    private static void AssertOptionalThrows(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => {
            var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
            r.Run();
        });

    // ── ___throwError sanity ────────────────────────────────────────

    [Test]
    public void ThrowError_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y:int = ___throwError()".Calc());

    [Test]
    public void ThrowError_InExpression_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = 1 + ___throwError()".Calc());

    // ── and short-circuit ───────────────────────────────────────────

    [Test]
    public void And_FalseShortCircuit() =>
        "y = false and ___throwError()".AssertReturns("y", false);

    [Test]
    public void And_TrueEvaluatesSecond() =>
        "y = true and false".AssertReturns("y", false);

    [Test]
    public void And_TrueAndThrow_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = true and ___throwError()".Calc());

    // ── or short-circuit ────────────────────────────────────────────

    [Test]
    public void Or_TrueShortCircuit() =>
        "y = true or ___throwError()".AssertReturns("y", true);

    [Test]
    public void Or_FalseEvaluatesSecond() =>
        "y = false or true".AssertReturns("y", true);

    [Test]
    public void Or_FalseAndThrow_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = false or ___throwError()".Calc());

    // ── ?? (null coalesce) short-circuit ────────────────────────────

    [Test]
    public void Coalesce_NonNoneShortCircuit() =>
        AssertOptionalReturns("y = 42 ?? ___throwError()", "y", 42);

    [Test]
    public void Coalesce_NoneEvaluatesFallback() =>
        AssertOptionalReturns("y = none ?? 42", "y", 42);

    [Test]
    public void Coalesce_NoneAndThrow_Throws() =>
        AssertOptionalThrows("y = none ?? ___throwError()");

    // ── Nested short-circuit ────────────────────────────────────────

    [Test]
    public void NestedAnd_ShortCircuit() =>
        "y = false and (false and ___throwError())".AssertReturns("y", false);

    [Test]
    public void NestedOr_ShortCircuit() =>
        "y = true or (true or ___throwError())".AssertReturns("y", true);

    [Test]
    public void ChainedCoalesce_ShortCircuit() =>
        AssertOptionalReturns("y = 1 ?? 2 ?? ___throwError()", "y", 1);

    // ── Mixed operators ─────────────────────────────────────────────

    [Test]
    public void AndOr_Mixed() =>
        "y = true or (false and ___throwError())".AssertReturns("y", true);

    [Test]
    public void CoalesceInExpression() =>
        AssertOptionalReturns("y = (none ?? 5) + 10", "y", 15);

    // ── xor is NOT lazy (both args always needed) ───────────────────

    [Test]
    public void Xor_AlwaysEvaluatesBoth() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = false xor ___throwError()".Calc());

    // ── Variables still work with short-circuit ─────────────────────

    [Test]
    public void And_VariableShortCircuit() =>
        "x = false\r y = x and ___throwError()".Calc().AssertResultHas("y", false);

    [Test]
    public void Or_VariableShortCircuit() =>
        "x = true\r y = x or ___throwError()".Calc().AssertResultHas("y", true);
}
