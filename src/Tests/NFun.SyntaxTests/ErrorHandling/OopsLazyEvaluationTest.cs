using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.ErrorHandling;

[TestFixture]
public class OopsLazyEvaluationTest {
    // ── oops is lazy (not evaluated if branch not taken) ─────────

    [Test]
    public void IfElse_OopsInFalseBranch_NotEvaluated() =>
        "y = if(true) 42 else oops('should not fire')".AssertReturns("y", 42);

    [Test]
    public void IfElse_OopsInTrueBranch_NotEvaluated() =>
        "y = if(false) oops('should not fire') else 42".AssertReturns("y", 42);

    [Test]
    public void Coalesce_OopsOnRight_NotEvaluatedWhenLeft() {
        var r = "x:int? = 42\r y = x ?? oops('should not fire')".BuildWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        r.Run();
        Assert.AreEqual(42, r["y"].Value);
    }

    [Test]
    public void And_OopsOnRight_ShortCircuited() =>
        "y = false and oops()".AssertReturns("y", false);

    [Test]
    public void Or_OopsOnRight_ShortCircuited() =>
        "y = true or oops()".AssertReturns("y", true);

    // ── try/catch — catch is lazy (not evaluated on success) ─────

    [Test]
    public void TryCatch_CatchNotEvaluated_OnSuccess() =>
        "y = try 42 catch oops('catch should not fire')".AssertReturns("y", 42);

    [Test]
    public void TryCatch_CatchIsLazy_OopsInCatch_OnlyIfNeeded() =>
        "y = try 42 catch oops('unreachable')".AssertReturns("y", 42);

    // ── oops message is lazy ─────────────────────────────────────

    [Test]
    public void Oops_MessageNotEvaluated_InDeadBranch() =>
        "y = if(true) 1 else oops('should not fire')".AssertReturns("y", 1);

    // ── chained lazy: try + coalesce + oops ──────────────────────

    [Test]
    public void TryCatch_Coalesce_Oops_Chain() {
        var r = "x:int? = none\r y = try (x ?? oops('unwrap')) catch 0".BuildWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        r.Run();
        Assert.AreEqual(0, r["y"].Value);
    }

    [Test]
    public void TryCatch_Coalesce_ValuePresent_NoOops() {
        var r = "x:int? = 42\r y = try (x ?? oops('unwrap')) catch 0".BuildWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        r.Run();
        Assert.AreEqual(42, r["y"].Value);
    }

    // ── oops in conditional array (not all elements evaluated) ───

    [Test]
    public void Array_OopsInUnevaluatedMapBranch_NoThrow() =>
        "y = [1,2,3].map(rule if(it > 10) oops() else it * 2)"
            .AssertReturns("y", new[] { 2, 4, 6 });

    [Test]
    public void Array_OopsInMapBranch_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = [1,2,3].map(rule if(it > 1) oops() else it)".Calc());
}
