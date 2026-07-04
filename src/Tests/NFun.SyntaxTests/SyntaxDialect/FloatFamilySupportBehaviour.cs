using System;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect;

/// <summary>
/// FloatFamilySupport dialect setting controls availability of `float32` / `float64`
/// keywords. Default = <c>None</c> (backward compat — keywords didn't exist before).
/// When enabled (<c>FloatFamily</c>): both keywords work. `real` is always available.
/// Incompatible with <c>RealClrType.IsDecimal</c> — dialect builder throws.
/// </summary>
public class FloatFamilySupportBehaviour {

    // ─── Default mode (None) — float32/float64 rejected, real works ─────────────

    [TestCase("out:float32 = 3.14")]
    [TestCase("out:float64 = 3.14")]
    public void DefaultMode_FloatKeyword_ParseError(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.Build());

    [Test]
    public void DefaultMode_RealKeyword_Works() =>
        "out:real = 3.14".AssertReturns("out", 3.14);

    // ─── FloatFamily explicit opt-in ─────────────────────────────────────

    [Test]
    public void Enabled_Float32Keyword_BuildsFloat32Variable() {
        var rt = Funny.Hardcore.WithDialect(floatFamilySupport: FloatFamilySupport.Float32AndFloat64)
            .Build("out:float32 = 3.14");
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.14f, rt["out"].Value);
    }

    [Test]
    public void Enabled_Float64Keyword_BuildsRealVariable() =>
        "out:float64 = 3.14".AssertResultHasReal("out", 3.14);

    [Test]
    public void Enabled_RealKeyword_StillWorks() =>
        Funny.Hardcore.WithDialect(floatFamilySupport: FloatFamilySupport.Float32AndFloat64)
            .Build("out:real = 3.14")
            .Calc()
            .AssertReturns("out", 3.14);

    // ─── Incompatible combo: IsDecimal + FloatFamily ─────────────────────

    [Test]
    public void IncompatibleCombo_DecimalPlusFloatFamily_ThrowsAtBuildTime() =>
        Assert.Throws<ArgumentException>(() =>
            Funny.Hardcore.WithDialect(
                realClrType: RealClrType.IsDecimal,
                floatFamilySupport: FloatFamilySupport.Float32AndFloat64));

    [Test]
    public void CompatibleCombo_DecimalPlusNone_BuildsSuccessfully() {
        // Decimal Real with no IEEE float keywords — `real` works (as Decimal), float32/float64 rejected.
        var rt = Funny.Hardcore.WithDialect(
                realClrType: RealClrType.IsDecimal,
                floatFamilySupport: FloatFamilySupport.AccordingToRealBehaviour)
            .Build("out:real = 3.14");
        rt.Run();
        Assert.AreEqual(3.14m, rt["out"].Value);
    }

    // ─── Real literal narrowing rule (dialect-observable) ──────────────────────

    [Test]
    public void Enabled_RealLiteral_NarrowsToFloat32_AtTypedTarget() {
        // Under Float family: real literals form [F32..Real, Pref=Real].
        "out:float32 = 3.14".AssertResultHasFloat32("out", 3.14f);
    }

    [TestCase("out = 3.14")]       // unconstrained — Preferred=Real wins without target constraint
    [TestCase("out:real = 3.14")]  // assigned to real
    public void Enabled_RealLiteral_StaysReal(string expr) =>
        expr.AssertResultHasReal("out", 3.14);

    // ─── Widening rules ───────────────────────────────────────────────────────

    [Test]
    public void Enabled_IntToFloat32_ImplicitWidening() =>
        "x:int = 5\rout:float32 = x".AssertResultHasFloat32("out", 5.0f);

    [Test]
    public void Enabled_Float32ToReal_ImplicitWidening() =>
        "x:float32 = 1.5\rout:real = x".AssertResultHasReal("out", 1.5);

    [Test]
    public void Enabled_RealToFloat32_NarrowingIsParseError() =>
        Assert.Throws<FunnyParseException>(
            () => "x:real = 3.14\rout:float32 = x".BuildWithFloats());

    // ─── Surface smoke tests: one canonical test per feature area ─────────────
    // Full operator/math/comparison/aggregate sweeps live below; these cover
    // orthogonal surface areas (convert, user-fn, struct, toText).

    [TestCase("x:float32 = 1.5\ry:float32 = 2.5\rout = x + y", 4.0f)]
    [TestCase("x:int = 1\ry:float32 = 2.5\rout = x + y",       3.5f)]
    public void Enabled_Arithmetic_Add_StaysOrWidensToFloat32(string expr, float expected) =>
        expr.AssertResultHasFloat32("out", expected);

    [Test]
    public void Enabled_Convert_IntToFloat32() =>
        "x:int = 42\rout:float32 = convert(x)".AssertResultHasFloat32("out", 42.0f);

    [Test]
    public void Enabled_UserFunction_TypedFloat32Signature() =>
        "f(x:float32):float32 = x + 1.0\rout = f(2.5)".AssertResultHasFloat32("out", 3.5f);

    [Test]
    public void Enabled_UserFunction_GenericMonomorphisesToFloat32() =>
        "id(a) = a\rout:float32 = id(1.5)".AssertResultHasFloat32("out", 1.5f);

    [Test]
    public void Enabled_Struct_FieldTypeFloat32() =>
        "v:float32 = 1.5\rp = {x = v}\rout = p.x".AssertResultHasFloat32("out", 1.5f);

    [Test]
    public void Enabled_ToText_Float32() =>
        "x:float32 = 3.14\rout = x.toText()".BuildWithFloats().Calc().AssertResultHas("out", "3.14");

    // ─── Full arithmetic operator sweep on float32 ────────────────────────────
    // f32 op f32 → f32 (6 ops). `+` already covered above; `**` with non-negative-int
    // right forces T=T (not Real) — under F32F64 that T is f32.

    [TestCase("x:float32 = 5.5\ry:float32 = 2.0\rout = x - y",  3.5f)]
    [TestCase("x:float32 = 2.5\ry:float32 = 4.0\rout = x * y", 10.0f)]
    [TestCase("x:float32 = 6.0\ry:float32 = 4.0\rout = x / y",  1.5f)]
    [TestCase("x:float32 = 7.5\ry:float32 = 2.0\rout = x % y",  1.5f)]
    [TestCase("x:float32 = 2.0\rout = x ** 3",                  8.0f)]
    [TestCase("x:float32 = 1.5\rout = -x",                     -1.5f)]
    public void Enabled_Arithmetic_Float32_StaysFloat32(string expr, float expected) =>
        expr.AssertResultHasFloat32("out", expected);

    // ─── Mixed-type arithmetic ────────────────────────────────────────────────
    // int + f32 → f32. f32 + real → real (widens to the wider IEEE type).

    [TestCase("x:int = 10\ry:float32 = 3.5\rout = x - y", 6.5f)]
    [TestCase("x:int = 3\ry:float32 = 2.5\rout = x * y",  7.5f)]
    [TestCase("x:int = 9\ry:float32 = 2.0\rout = x / y",  4.5f)]
    public void Enabled_MixedArithmetic_IntWithFloat32_WidensToFloat32(string expr, float expected) =>
        expr.AssertResultHasFloat32("out", expected);

    [TestCase("x:float32 = 1.5\ry:real = 2.5\rout = x + y", 4.0)]
    [TestCase("x:float32 = 5.5\ry:real = 2.5\rout = x - y", 3.0)]
    [TestCase("x:float32 = 6.0\ry:real = 4.0\rout = x / y", 1.5)]
    public void Enabled_MixedArithmetic_Float32WithReal_WidensToReal(string expr, double expected) =>
        expr.AssertResultHasReal("out", expected);

    // ─── Comparison operator sweep ────────────────────────────────────────────

    [TestCase("x:float32 = 3.5\ry:float32 = 2.5\rout = x > y",  true)]
    [TestCase("x:float32 = 2.5\ry:float32 = 2.5\rout = x <= y", true)]
    [TestCase("x:float32 = 3.5\ry:float32 = 2.5\rout = x >= y", true)]
    [TestCase("x:float32 = 2.5\ry:float32 = 2.5\rout = x == y", true)]
    [TestCase("x:float32 = 2.5\ry:float32 = 3.5\rout = x != y", true)]
    [TestCase("x:float32 = 1.5\ry:float32 = 2.5\rout = x < y",  true)]
    public void Enabled_Comparison_Float32(string expr, bool expected) =>
        expr.BuildWithFloats().Calc().AssertResultHas("out", expected);

    // ─── Math function sweep — Floats-generic functions on float32 ────────────
    // All BaseFunctions.GenericFunctions with `Floats` constraint, plus `abs` (SignedNumber).

    [TestCase("x:float32 = 4.0\rout = sqrt(x)",           2.0f)]
    [TestCase("x:float32 = 0.0\rout = sin(x)",            0.0f)]
    [TestCase("x:float32 = 0.0\rout = cos(x)",            1.0f)]
    [TestCase("x:float32 = 0.0\rout = tan(x)",            0.0f)]
    [TestCase("x:float32 = 0.0\rout = asin(x)",           0.0f)]
    [TestCase("x:float32 = 1.0\rout = acos(x)",           0.0f)]
    [TestCase("x:float32 = 0.0\rout = atan(x)",           0.0f)]
    [TestCase("x:float32 = 0.0\ry:float32 = 1.0\rout = atan2(x, y)", 0.0f)]
    [TestCase("x:float32 = 0.0\rout = exp(x)",            1.0f)]
    [TestCase("x:float32 = 1.0\rout = log(x)",            0.0f)]
    [TestCase("x:float32 = 8.0\rb:float32 = 2.0\rout = log(x, b)", 3.0f)]
    [TestCase("x:float32 = 100.0\rout = log10(x)",        2.0f)]
    [TestCase("x:float32 = 1.2\rout = ceil(x)",           2.0f)]
    [TestCase("x:float32 = 1.8\rout = floor(x)",          1.0f)]
    [TestCase("x:float32 = 1.5\rout = round(x, 0)",       2.0f)]
    [TestCase("x:float32 = -1.5\rout = abs(x)",           1.5f)]
    public void Enabled_Math_Float32_StaysFloat32(string expr, float expected) =>
        expr.AssertResultHasFloat32("out", expected);

    // sign returns integer regardless of input type.
    [Test]
    public void Enabled_Math_SignFloat32_ReturnsInt() {
        var rt = "x:float32 = -1.5\rout = sign(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(-1, System.Convert.ToInt32(rt["out"].Value));
    }

    // ─── Aggregates on float32[] ──────────────────────────────────────────────

    [TestCase("arr:float32[] = [1.5, 2.5, 3.5]\rout = min(arr)",           1.5f)]
    [TestCase("arr:float32[] = [1.5, 2.5, 3.5]\rout = max(arr)",           3.5f)]
    [TestCase("arr:float32[] = [1.0, 2.0, 3.0]\rout = sum(arr)",           6.0f)]
    [TestCase("arr:float32[] = [1.0, 2.0, 3.0]\rout = avg(arr)",           2.0f)]
    [TestCase("arr:float32[] = [1.0, 5.0, 2.0, 4.0, 3.0]\rout = median(arr)", 3.0f)]
    // .sum(rule ...) dispatches via MultiMapSumFunction with Arithmetical constraint.
    [TestCase("arr:float32[] = [1.0, 2.0, 3.0]\rout = arr.sum(rule it * 2.0)", 12.0f)]
    public void Enabled_Aggregate_Float32Array_StaysFloat32(string expr, float expected) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(expected, rt["out"].Value);
    }

    [Test]
    public void Enabled_Aggregate_SortDescFloat32Array() {
        var rt = "arr:float32[] = [1.0, 3.0, 2.0]\rout = arr.sortDescending()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 3.0f, 2.0f, 1.0f }, (float[])rt["out"].Value);
    }

    // ─── Divergent-mode pin: `x = 3/2` under both dialects ────────────────────
    // Regression pin for the DivideFunction fix in TicSetupVisitor.FinishBinOp:
    //   None mode  → T pinned to [Real..Real] → concrete Real at TIC level.
    //   F32F64 mode → T stays [F32..Real] → TicTypesConverter picks Real (ancestor).
    // Runtime output identical, TIC internals differ by design.

    [Test]
    public void Divergent_IntDivInt_NoneMode_IsReal() =>
        "x = 3 / 2".AssertReturns("x", 1.5);

    [Test]
    public void Divergent_IntDivInt_F32F64Mode_IsReal() =>
        "x = 3 / 2".AssertResultHasReal("x", 1.5);

    [Test]
    public void Divergent_IntDivInt_TypedFloat32_ResolvesToFloat32() =>
        "x:float32 = 3 / 2".AssertResultHasFloat32("x", 1.5f);

    // ─── None-mode sanity: float-only math still produces Real ────────────────
    // After the FinishBinOp fix, PureGenericFunctionBase with Floats constrains
    // covers /, sqrt, sin, cos, ..., avg. Under None mode all pin to Real without
    // TIC-level residue. Pins prevent silent regression.

    [TestCase("x = sqrt(4.0)",         2.0)]
    [TestCase("x = sin(0.0)",          0.0)]
    [TestCase("x = cos(0.0)",          1.0)]
    [TestCase("x = log(1.0)",          0.0)]
    [TestCase("x = exp(0.0)",          1.0)]
    [TestCase("x = avg([1.0, 2.0, 3.0])", 2.0)]
    [TestCase("x = 3.0 / 2",           1.5)]
    public void NoneMode_FloatsMath_ConcreteReal(string expr, double expected) =>
        expr.AssertReturns("x", expected);

    // ─── IEEE 754 semantics (dialect-observable) ──────────────────────────────

    [Test]
    public void Enabled_Float32_NaNInequalityHolds() {
        // NaN != NaN by IEEE 754 rule.
        var rt = "a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan != nan".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Enabled_Float32_PositiveInfinityFromDivByZero() {
        var rt = "a:float32=1.0\rb:float32=0.0\rout = a / b".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    // ─── Integer-literal → floatXX precision boundaries ───────────────────────
    // float32 mantissa = 24 bits → exact integers up to 2^24 = 16,777,216.
    // Above 2^24, only every-other integer is exactly representable.
    // Above 2^25, only every 4th, etc.
    // float64 mantissa = 53 bits → exact integers up to 2^53.

    [TestCase("out:float32 = 16777216", 16777216.0f)]  // 2^24 — last exact
    [TestCase("out:float32 = 16777217", 16777216.0f)]  // 2^24+1 — rounds down
    [TestCase("out:float32 = 16777218", 16777218.0f)]  // 2^24+2 — exact again
    [TestCase("out:float32 = 33554433", 33554432.0f)]  // 2^25+1 — half precision zone
    public void Enabled_IntLiteral_Float32Precision(string expr, float expected) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        Assert.AreEqual(expected, rt["out"].Value);
    }

    // float64 preserves the same literals that float32 truncates.
    [TestCase("out:float64 = 16777217",       16777217.0)]
    [TestCase("out:float64 = 9007199254740992", 9007199254740992.0)]  // 2^53
    public void Enabled_IntLiteral_Float64Precision(string expr, double expected) =>
        expr.AssertResultHasReal("out", expected);

    // Same big-int assigned to float32 vs float64 diverges — pin the observed values.
    [Test]
    public void Enabled_Same2Pow24Plus1_Float32VsFloat64_DivergentValues() {
        var rt = ("f32:float32 = 16777217\r" +
                  "f64:float64 = 16777217").BuildWithFloats();
        rt.Run();
        Assert.AreEqual(16777216.0f, rt["f32"].Value, "float32 loses 1 ULP at 2^24+1");
        Assert.AreEqual(16777217.0,  rt["f64"].Value, "float64 preserves 2^24+1 exactly");
    }

    // Sort on float32[] with NaN — CLR total order places NaN first (not IEEE).
    [Test]
    public void Enabled_Float32Sort_WithNaN_UsesClrTotalOrder() {
        var rt = "z:float32=0.0\rn = z/z\rarr:float32[] = [3.0, n, 1.0, 2.0]\rout = arr.sort()".BuildWithFloats();
        rt.Run();
        var arr = (float[])rt["out"].Value;
        Assert.IsTrue(float.IsNaN(arr[0]));
        Assert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, new[] { arr[1], arr[2], arr[3] });
    }
}

internal static class FloatFamilyAssertExtensions {
    public static void AssertResultHasFloat32(this string expr, string name, float expected) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt[name].Type.ToString());
        Assert.AreEqual(expected, rt[name].Value);
    }

    public static void AssertResultHasReal(this string expr, string name, double expected) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt[name].Type.ToString());
        Assert.AreEqual(expected, rt[name].Value);
    }
}
