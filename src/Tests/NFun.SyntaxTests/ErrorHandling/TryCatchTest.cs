using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.ErrorHandling;

[TestFixture]
public class TryCatchTest {
    // ── try expr catch expr — basic ──────────────────────────────

    [Test]
    public void TryCatch_NoError_ReturnsValue() =>
        "y = try 42 catch 0".AssertReturns("y", 42);

    [Test]
    public void TryCatch_OopsThrown_ReturnsFallback() =>
        "y = try oops() catch 0".AssertReturns("y", 0);

    [Test]
    public void TryCatch_OopsWithMessage_ReturnsFallback() =>
        "y = try oops('fail') catch -1".AssertReturns("y", -1);

    // ── type inference ───────────────────────────────────────────

    [Test]
    public void TryCatch_TypeInferred_Int() =>
        "y = try oops() catch 42".AssertReturns("y", 42);

    [Test]
    public void TryCatch_TypeInferred_Text() =>
        "y = try oops() catch 'fallback'".AssertReturns("y", "fallback");

    [Test]
    public void TryCatch_TypeInferred_Bool() =>
        "y = try oops() catch true".AssertReturns("y", true);

    [Test]
    public void TryCatch_TypeInferred_Array() =>
        "y = try oops() catch [1,2,3]".AssertReturns("y", new[] { 1, 2, 3 });

    // ── try with complex expressions ─────────────────────────────

    [Test]
    public void TryCatch_ComplexTryExpr_NoError() =>
        "x = 10\r y = try x * 2 + 1 catch 0".Calc().AssertResultHas("y", 21);

    [Test]
    public void TryCatch_OopsInExpression_CatchesFallback() =>
        "y = try (1 + oops()) catch 99".AssertReturns("y", 99);

    [Test]
    public void TryCatch_NestedTryCatch() =>
        "y = try (try oops() catch oops('inner')) catch 42".AssertReturns("y", 42);

    // ── try catch with if-else ───────────────────────────────────

    [Test]
    public void TryCatch_InIfElse_TrueBranch() =>
        "y = if(true) (try oops() catch 1) else 2".AssertReturns("y", 1);

    [Test]
    public void TryCatch_InIfElse_FalseBranch() =>
        "y = if(false) 1 else (try oops() catch 2)".AssertReturns("y", 2);

    // ── try catch with optional ──────────────────────────────────

    [Test]
    public void TryCatch_WithCoalesce_OopsCaught() {
        var r = "x:int? = none\r y = try (x ?? oops()) catch 0".BuildWithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(0, r["y"].Value);
    }

    // ── try without catch — syntax error ─────────────────────────

    [Test]
    public void Try_WithoutCatch_ParseError() =>
        Assert.Throws<FunnyParseException>(() => "y = try 42".Calc());

    // ── catch without try — syntax error ─────────────────────────

    [Test]
    public void Catch_WithoutTry_ParseError() =>
        Assert.Throws<FunnyParseException>(() => "y = 42 catch 0".Calc());

    // ── uncaught oops — propagates ───────────────────────────────

    [Test]
    public void Oops_WithoutTryCatch_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = 1 + oops()".Calc());
}
