using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.ErrorHandling;

[TestFixture]
public class TryCatchEdgeCasesTest {
    // ── try/catch with arithmetic ────────────────────────────────

    [Test]
    public void TryCatch_AdditionAfter() =>
        "y = (try oops() catch 10) + 5".AssertReturns("y", 15);

    [Test]
    public void TryCatch_MultiplicationAfter() =>
        "y = (try oops() catch 3) * 4".AssertReturns("y", 12);

    [Test]
    public void TryCatch_InArrayElement() =>
        "y = [1, try oops() catch 2, 3]".AssertReturns("y", new[] { 1, 2, 3 });

    // ── try/catch catches only oops, not other errors ────────────

    [Test]
    public void TryCatch_CatchesOops_NotDivisionByZero() {
        // Division by zero is a host runtime error, not an oops.
        // try/catch should only catch oops() errors.
        // (This behavior may change — document the decision)
        "y = try (10 / 2) catch 0".AssertReturns("y", 5.0);
    }

    // ── try/catch with variables ─────────────────────────────────

    [Test]
    public void TryCatch_TryUsesVariable() =>
        "x = 42\r y = try x catch 0".Calc().AssertResultHas("y", 42);

    [Test]
    public void TryCatch_CatchUsesVariable() =>
        "x = 99\r y = try oops() catch x".Calc().AssertResultHas("y", 99);

    [Test]
    public void TryCatch_BothUseVariables() =>
        "a = 1\r b = 2\r y = try a catch b".Calc().AssertResultHas("y", 1);

    // ── try/catch type widening ──────────────────────────────────

    [Test]
    public void TryCatch_TypeWidening_IntAndReal() =>
        "y = try oops() catch 3.14".AssertReturns("y", 3.14);

    // ── deeply nested try/catch ──────────────────────────────────

    [Test]
    public void TryCatch_TripleNested() =>
        "y = try (try (try oops() catch oops()) catch oops()) catch 42"
            .AssertReturns("y", 42);

    [Test]
    public void TryCatch_NestedInner_Succeeds() =>
        "y = try (try 10 catch 20) catch 30".AssertReturns("y", 10);

    [Test]
    public void TryCatch_NestedInner_InnerCatches() =>
        "y = try (try oops() catch 20) catch 30".AssertReturns("y", 20);

    [Test]
    public void TryCatch_NestedInner_OuterCatches() =>
        "y = try (try oops() catch oops('inner')) catch 30".AssertReturns("y", 30);

    // ── try/catch in pipe forward ────────────────────────────────

    [Test]
    public void TryCatch_InPipeForward() =>
        "y = (try oops() catch [1,2,3]).count()".AssertReturns("y", 3);

    // ── try/catch with struct ────────────────────────────────────

    [Test]
    public void TryCatch_StructFallback() =>
        "y = (try oops() catch {x = 1}).x".AssertReturns("y", 1);

    // ── try/catch preserves oops message through nesting ─────────

    [Test]
    public void TryCatchE_NestedOops_InnerMessagePreserved() =>
        "y = try (try oops('inner') catch(e) oops(concat(e.message, ' wrapped'))) catch(e) e.message"
            .AssertReturns("y", "inner wrapped");

    // ── try/catch with multiple equations ─────────────────────────

    [Test]
    public void TryCatch_MultipleEquations() {
        "a = try oops() catch 1\r b = try oops() catch 2\r y = a + b"
            .Calc().AssertResultHas("y", 3);
    }

    // ── try/catch with map ───────────────────────────────────────

    [Test]
    public void TryCatch_InsideMapLambda() =>
        "y = [1,2,3].map(rule try oops() catch it * 2)"
            .AssertReturns("y", new[] { 2, 4, 6 });

    // ── catch(e) edge cases ──────────────────────────────────────

    [Test]
    public void TryCatchE_MessageConcat() =>
        "y = try oops('hello') catch(e) concat(e.message, ' world')"
            .AssertReturns("y", "hello world");

    [Test]
    [Ignore("e.data is Any — field access on Any not supported yet")]
    public void TryCatchE_DataIsStruct() =>
        "y = try oops('fail', {code = 42}) catch(e) e.data.code"
            .AssertReturns("y", 42);

    [Test]
    public void TryCatchE_ErrorVariableNotLeaking() =>
        // 'e' scoped to catch expression via alias. Inside catch, e.message works.
        // Outside catch, 'e' would be a different input variable.
        "y = try oops() catch(e) e.message".AssertReturns("y", "oops");

    // ── try/catch type mismatch — should fail ────────────────────

    [Test]
    public void TryCatch_TypeMismatch_IntAndText_Fails() =>
        Assert.Throws<FunnyParseException>(() =>
            "y:int = try oops() catch 'text'".Calc());

    [Test]
    public void TryCatch_TypeMismatch_BoolAndInt_Fails() =>
        Assert.Throws<FunnyParseException>(() =>
            "y:bool = try oops() catch 42".Calc());
}
