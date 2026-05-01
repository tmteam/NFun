using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.ErrorHandling;

[TestFixture]
public class OopsTest {
    // ── oops() — bare, throws runtime error ───────────────────────

    [Test]
    public void Oops_Bare_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = oops()".Calc());

    [Test]
    public void Oops_InIfElse_TrueBranch_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = if(true) oops() else 42".Calc());

    [Test]
    public void Oops_InIfElse_FalseBranch_NotThrown() =>
        "y = if(true) 42 else oops()".AssertReturns("y", 42);

    [Test]
    public void Oops_InIfElse_TypeInferred_AsInt() =>
        "y = if(false) oops() else 42".AssertReturns("y", 42);

    // ── oops("message") — with text message ──────────────────────

    [Test]
    public void Oops_WithMessage_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('something went wrong')".Calc());

    [Test]
    public void Oops_WithMessage_MessagePreserved() {
        var ex = Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('custom error')".Calc());
        Assert.That(ex.Message, Does.Contain("custom error"));
    }

    // ── oops with ?? (coalesce) ──────────────────────────────────

    [Test]
    public void Oops_AsCoalesceFallback_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "x:int? = none\r y = x ?? oops('missing')".BuildWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled).Run());

    [Test]
    public void Oops_AsCoalesceFallback_NotThrownWhenHasValue() {
        var r = "x:int? = 42\r y = x ?? oops('missing')".BuildWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        r.Run();
        Assert.AreEqual(42, r["y"].Value);
    }

    // ── oops in array context ────────────────────────────────────

    [Test]
    public void Oops_InArray_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = [1, oops(), 3]".Calc());

    // ── oops type inference ──────────────────────────────────────

    [Test]
    public void Oops_TypeInferredFromContext_Int() =>
        "y:int = if(false) oops() else 42".AssertReturns("y", 42);

    [Test]
    public void Oops_TypeInferredFromContext_Text() =>
        "y:text = if(false) oops() else 'hello'".AssertReturns("y", "hello");

    [Test]
    public void Oops_TypeInferredFromContext_Array() =>
        "y = if(false) oops() else [1,2,3]".AssertReturns("y", new[] { 1, 2, 3 });

    // ── oops("msg", data) — with message and data ────────────────

    [Test]
    public void Oops_WithMessageAndData_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('failed', 42)".Calc());
}
