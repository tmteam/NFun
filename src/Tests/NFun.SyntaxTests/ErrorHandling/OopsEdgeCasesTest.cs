using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.ErrorHandling;

[TestFixture]
public class OopsEdgeCasesTest {
    // ── oops in different expression positions ───────────────────

    [Test]
    public void Oops_AsReturnValue_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = oops()".Calc());

    [Test]
    public void Oops_InArithmetic_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = 1 + oops()".Calc());

    [Test]
    public void Oops_InComparison_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = oops() > 0".Calc());

    [Test]
    public void Oops_InBooleanAnd_ShortCircuit_NotThrown() =>
        "y = false and oops()".AssertReturns("y", false);

    [Test]
    public void Oops_InBooleanAnd_Evaluated_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = true and oops()".Calc());

    [Test]
    public void Oops_InBooleanOr_ShortCircuit_NotThrown() =>
        "y = true or oops()".AssertReturns("y", true);

    [Test]
    public void Oops_InBooleanOr_Evaluated_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = false or oops()".Calc());

    // ── oops in collection contexts ──────────────────────────────

    [Test]
    public void Oops_InArrayFirst_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = [oops(), 2, 3]".Calc());

    [Test]
    public void Oops_InArrayMiddle_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = [1, oops(), 3]".Calc());

    [Test]
    public void Oops_InArrayLast_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = [1, 2, oops()]".Calc());

    [Test]
    public void Oops_InStructField_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "y = {x = oops()}".Calc());

    // ── oops with map/filter ─────────────────────────────────────

    [Test]
    public void Oops_InMapLambda_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = [1,2,3].map(rule oops())".Calc());

    [Test]
    public void Oops_InFilterLambda_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = [1,2,3].filter(rule oops())".Calc());

    // ── oops type compatibility ──────────────────────────────────

    [TestCase("y:int = if(false) oops() else 42", 42)]
    [TestCase("y:real = if(false) oops() else 3.14", 3.14)]
    [TestCase("y:bool = if(false) oops() else true", true)]
    public void Oops_TypeCompatible_WithAnnotation(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [Test]
    public void Oops_TypeCompatible_WithText() =>
        "y:text = if(false) oops() else 'hello'".AssertReturns("y", "hello");

    // ── oops message variants ────────────────────────────────────

    [Test]
    public void Oops_EmptyMessage() {
        var ex = Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('')".Calc());
        Assert.That(ex.Message, Does.Contain(""));
    }

    [Test]
    public void Oops_LongMessage() {
        var ex = Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('this is a very long error message that describes the problem in detail')".Calc());
        Assert.That(ex.Message, Does.Contain("very long error message"));
    }

    [Test]
    public void Oops_MessageFromVariable() {
        var ex = Assert.Throws<FunnyRuntimeException>(() =>
            "msg = 'dynamic error'\r y = oops(msg)".Calc());
        Assert.That(ex.Message, Does.Contain("dynamic error"));
    }

    // ── oops with data ───────────────────────────────────────────

    [Test]
    public void Oops_WithIntData_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('fail', 42)".Calc());

    [Test]
    public void Oops_WithArrayData_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('fail', [1,2,3])".Calc());

    [Test]
    public void Oops_WithStructData_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('fail', {code = 404})".Calc());

    [Test]
    public void Oops_WithBoolData_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('fail', true)".Calc());

    // ── oops without parentheses — treated as variable name ──

    [Test]
    public void Oops_WithoutParentheses_IsVariableName() =>
        // "oops" without () is treated as an input variable, not a function call
        "y = oops".Calc();

    // ── multiple oops in expression ──────────────────────────────

    [Test]
    public void MultipleOops_FirstOneThrows() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = oops('first') + oops('second')".Calc());

    [Test]
    public void Oops_InTernaryBothBranches_TrueThrows() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = if(true) oops('a') else oops('b')".Calc());

    [Test]
    public void Oops_InTernaryBothBranches_FalseThrows() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "y = if(false) oops('a') else oops('b')".Calc());
}
