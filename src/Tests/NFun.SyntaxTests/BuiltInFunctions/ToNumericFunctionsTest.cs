using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.BuiltInFunctions;

/// <summary>
/// toXxx / toXxxWrap / toXxxClamp — explicit numeric narrowing family (issue #135).
/// Semantics: Specs/Functions.md §Numeric narrowing family.
/// </summary>
[TestFixture]
public class ToNumericFunctionsTest {

    // ─────────────────────────────────────────────────────────────────
    // toXxx (checked) — happy path, exact boundaries
    // ─────────────────────────────────────────────────────────────────

    [TestCase("y = toByte(0)",                   (byte)0)]
    [TestCase("y = toByte(200)",                 (byte)200)]
    [TestCase("y = toByte(255)",                 (byte)255)]
    [TestCase("y = toUint16(0)",                 (ushort)0)]
    [TestCase("y = toUint16(65535)",             (ushort)65535)]
    [TestCase("y = toUint32(0)",                 (uint)0)]
    [TestCase("y = toUint32(4294967295)",        (uint)4294967295)]
    [TestCase("y = toUint64(0)",                 (ulong)0)]
    [TestCase("y = toUint64(42)",                (ulong)42)]
    [TestCase("y = toUint64(18446744073709551615)", ulong.MaxValue)]
    [TestCase("y = toInt8(127)",                 (sbyte)127)]
    [TestCase("y = toInt8(-128)",                (sbyte)(-128))]
    [TestCase("y = toInt16(32767)",              (short)32767)]
    [TestCase("y = toInt16(-32768)",             (short)(-32768))]
    [TestCase("y = toInt32(2147483647)",         2147483647)]
    [TestCase("y = toInt32(-2147483648)",        -2147483648)]
    [TestCase("y = toInt64(42)",                 (long)42)]
    [TestCase("y = toInt64(9223372036854775807)",  long.MaxValue)]
    [TestCase("y = toInt64(-9223372036854775808)", long.MinValue)]
    // identity conversions always succeed
    [TestCase("x:byte = 7\r y = x.toByte()",     (byte)7)]
    [TestCase("x:int64 = -7\r y = x.toInt64()",  (long)(-7))]
    public void Checked_ReturnsExpected(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // aliases: function names mirror type-keyword aliases
    [TestCase("y = toUint8(200)",  (byte)200)]
    [TestCase("y = toInt(42)",     42)]
    [TestCase("y = toUint(7)",     (uint)7)]
    [TestCase("y = toUint8Wrap(300)",  (byte)44)]
    [TestCase("y = toIntWrap(42)",     42)]
    [TestCase("y = toUintWrap(-1)",    (uint)4294967295)]
    [TestCase("y = toUint8Clamp(300)", (byte)255)]
    [TestCase("y = toIntClamp(42)",    42)]
    [TestCase("y = toUintClamp(-1)",   (uint)0)]
    public void Aliases_BehaveAsPrimaryNames(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // ─────────────────────────────────────────────────────────────────
    // toXxx (checked) — out of range → runtime error
    // ─────────────────────────────────────────────────────────────────

    [TestCase("y = toByte(256)")]
    [TestCase("y = toByte(-1)")]
    [TestCase("x:int = 300\r y = x.toByte()")]
    [TestCase("y = toUint16(65536)")]
    [TestCase("y = toUint16(-1)")]
    [TestCase("y = toUint32(-1)")]
    [TestCase("y = toInt8(128)")]
    [TestCase("y = toInt8(-129)")]
    [TestCase("y = toInt16(32768)")]
    [TestCase("y = toInt16(-32769)")]
    [TestCase("x:int64 = 2147483648\r y = x.toInt32()")]
    [TestCase("x:int64 = -2147483649\r y = x.toInt32()")]
    [TestCase("x:int64 = -1\r y = x.toUint64()")]
    [TestCase("x:uint64 = 18446744073709551615\r y = x.toInt64()")]
    [TestCase("x:uint64 = 9223372036854775808\r y = x.toInt64()")]
    [TestCase("x:int64 = 4294967296\r y = x.toUint32()")]
    [TestCase("x:int8 = -5\r y = x.toByte()")]
    [TestCase("a:byte = 200\r b:byte = 200\r y = (a + b).toByte()")]
    public void Checked_OutOfRange_Throws(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());

    // ─────────────────────────────────────────────────────────────────
    // toXxxWrap — two's-complement wrap
    // ─────────────────────────────────────────────────────────────────

    [TestCase("y = toByteWrap(42)",     (byte)42)]     // in range — identity
    [TestCase("y = toByteWrap(256)",    (byte)0)]
    [TestCase("y = toByteWrap(300)",    (byte)44)]
    [TestCase("y = toByteWrap(-1)",     (byte)255)]
    [TestCase("y = toInt8Wrap(200)",    (sbyte)(-56))]
    [TestCase("y = toInt8Wrap(-200)",   (sbyte)56)]
    [TestCase("y = toInt16Wrap(32768)", (short)(-32768))]
    [TestCase("y = toInt16Wrap(-32769)", (short)32767)]
    [TestCase("y = toUint16Wrap(65536)", (ushort)0)]
    [TestCase("y = toUint16Wrap(-1)",   (ushort)65535)]
    [TestCase("x:int64 = 2147483648\r y = x.toInt32Wrap()", -2147483648)]
    [TestCase("x:int64 = -2147483649\r y = x.toInt32Wrap()", 2147483647)]
    [TestCase("x:int64 = 4294967301\r y = x.toUint32Wrap()", (uint)5)]
    [TestCase("x:uint64 = 18446744073709551615\r y = x.toInt64Wrap()", (long)(-1))]
    [TestCase("x:int64 = -1\r y = x.toUint64Wrap()", ulong.MaxValue)]
    public void Wrap_ReturnsExpected(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // ─────────────────────────────────────────────────────────────────
    // toXxxClamp — saturate to [Min..Max]
    // ─────────────────────────────────────────────────────────────────

    [TestCase("y = toByteClamp(42)",    (byte)42)]     // in range — identity
    [TestCase("y = toByteClamp(300)",   (byte)255)]
    [TestCase("y = toByteClamp(-5)",    (byte)0)]
    [TestCase("y = toInt8Clamp(200)",   (sbyte)127)]
    [TestCase("y = toInt8Clamp(-200)",  (sbyte)(-128))]
    [TestCase("y = toInt16Clamp(32768)", (short)32767)]
    [TestCase("y = toInt16Clamp(-32769)", (short)(-32768))]
    [TestCase("y = toUint16Clamp(-1)",  (ushort)0)]
    [TestCase("y = toUint16Clamp(65536)", (ushort)65535)]
    [TestCase("y = toUint64Clamp(-1)",  (ulong)0)]
    [TestCase("x:int64 = 4294967296\r y = x.toUint32Clamp()", (uint)4294967295)]
    [TestCase("x:int64 = 9223372036854775807\r y = x.toInt32Clamp()", 2147483647)]
    [TestCase("x:int64 = -9223372036854775808\r y = x.toInt32Clamp()", -2147483648)]
    [TestCase("x:uint64 = 18446744073709551615\r y = x.toInt64Clamp()", long.MaxValue)]
    public void Clamp_ReturnsExpected(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // ─────────────────────────────────────────────────────────────────
    // real source (default dialect: double) — truncation toward zero
    // ─────────────────────────────────────────────────────────────────

    [TestCase("x:real = 1.9\r y = x.toByte()",       (byte)1)]
    [TestCase("x:real = 255.9\r y = x.toByte()",     (byte)255)]
    [TestCase("x:real = 1.5\r y = x.toInt()",        1)]
    [TestCase("x:real = -1.5\r y = x.toInt()",       -1)]      // toward zero, not banker's
    [TestCase("x:real = -1.9\r y = x.toInt8()",      (sbyte)(-1))]
    [TestCase("x:real = 42.0\r y = x.toByte()",      (byte)42)]
    // truncate-then-check order: trunc(-0.5) == 0 fits byte — must NOT throw
    [TestCase("x:real = -0.5\r y = x.toByte()",      (byte)0)]
    [TestCase("x:real = -0.0\r y = x.toByte()",      (byte)0)]
    [TestCase("x:real = -9223372036854775808.0\r y = x.toInt64()", long.MinValue)]
    [TestCase("x:real = 256.7\r y = x.toByteWrap()", (byte)0)]
    [TestCase("x:real = -1.5\r y = x.toByteWrap()",  (byte)255)]
    [TestCase("x:real = 9223372036854775807.0\r y = x.toInt64Wrap()", long.MinValue)]
    [TestCase("x:real = 256.7\r y = x.toByteClamp()",(byte)255)]
    [TestCase("x:real = -0.5\r y = x.toByteClamp()", (byte)0)]
    [TestCase("x:real = 1e30\r y = x.toByteClamp()", (byte)255)]
    [TestCase("x:real = 1e30\r y = x.toInt64Clamp()", long.MaxValue)]
    [TestCase("x:real = -1e30\r y = x.toInt64Clamp()", long.MinValue)]
    [TestCase("x:real = 1e30\r y = x.toUint64Clamp()", ulong.MaxValue)]
    public void RealSource_ReturnsExpected(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    [TestCase("x:real = 256.0\r y = x.toByte()")]
    [TestCase("x:real = -1.0\r y = x.toByte()")]
    [TestCase("x:real = 9223372036854775807.0\r y = x.toInt64()")]   // double image is 2^63 — out of int64 range
    [TestCase("x:real = 18446744073709551615.0\r y = x.toUint64()")] // double image is 2^64 — out of uint64 range
    public void RealSource_Checked_OutOfRange_Throws(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());

    // NaN / Infinity semantics
    [TestCase("z:real = 0.0\r x = z/z\r y = x.toByte()")]        // NaN → checked: error
    [TestCase("z:real = 0.0\r x = z/z\r y = x.toByteWrap()")]    // NaN → wrap: error
    [TestCase("z:real = 0.0\r x = z/z\r y = x.toByteClamp()")]   // NaN → clamp: error
    [TestCase("z:real = 1.0\r w:real = 0.0\r x = z/w\r y = x.toByte()")]      // +Inf → checked: error
    [TestCase("z:real = 1.0\r w:real = 0.0\r x = z/w\r y = x.toByteWrap()")]  // +Inf → wrap: error
    [TestCase("x:real = 1e30\r y = x.toByteWrap()")]             // beyond 64-bit domain → wrap: error
    [TestCase("x:real = -1e30\r y = x.toInt64Wrap()")]
    public void RealSource_NanInfUnrepresentable_Throws(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());

    [TestCase("z:real = 1.0\r w:real = 0.0\r x = z/w\r y = x.toByteClamp()", (byte)255)]      // +Inf → Max
    [TestCase("z:real = -1.0\r w:real = 0.0\r x = z/w\r y = x.toInt8Clamp()", (sbyte)(-128))] // -Inf → Min
    public void RealSource_Infinity_Clamps(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // ─────────────────────────────────────────────────────────────────
    // all integer sources sweep — every numeric source type accepted
    // ─────────────────────────────────────────────────────────────────

    [TestCase("x:byte = 200\r y = x.toInt64()",    (long)200)]
    [TestCase("x:uint16 = 200\r y = x.toByte()",   (byte)200)]
    [TestCase("x:uint32 = 42\r y = x.toInt16()",   (short)42)]
    [TestCase("x:uint64 = 42\r y = x.toInt8()",    (sbyte)42)]
    [TestCase("x:int8 = -5\r y = x.toInt64()",     (long)(-5))]
    [TestCase("x:int16 = -5\r y = x.toInt()",      -5)]
    [TestCase("x:int = -5\r y = x.toInt64()",      (long)(-5))]
    [TestCase("x:int64 = 200\r y = x.toByte()",    (byte)200)]
    public void AllIntSources_Accepted(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // ─────────────────────────────────────────────────────────────────
    // toReal — total for numeric sources; no Wrap/Clamp variants
    // ─────────────────────────────────────────────────────────────────

    [TestCase("y = toReal(42)",                     42.0)]
    [TestCase("x:int64 = 42\r y = x.toReal()",      42.0)]
    [TestCase("x:byte = 200\r y = x.toReal()",      200.0)]
    [TestCase("x:real = 1.5\r y = x.toReal()",      1.5)]      // identity
    [TestCase("x:uint64 = 18446744073709551615\r y = x.toReal()", 1.8446744073709552E+19)] // precision loss pinned
    public void ToReal_ReturnsExpected(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    [TestCase("y = toRealWrap(42)")]
    [TestCase("y = toRealClamp(42)")]
    public void ToRealWrapClamp_DoNotExist(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.Calc());

    // ─────────────────────────────────────────────────────────────────
    // chains — the motivating cases: narrow arithmetic via widen + explicit narrowing
    // ─────────────────────────────────────────────────────────────────

    [TestCase("a:byte = 41\r y = (a + 1).toByte()",              (byte)42)]
    [TestCase("a:byte = 200\r b:byte = 200\r y = (a + b).toByteClamp()", (byte)255)]
    [TestCase("a:byte = 200\r b:byte = 200\r y = (a + b).toByteWrap()",  (byte)144)]
    [TestCase("a:byte = 200\r b:byte = 100\r y = ((a + b) / 2).toByte()", (byte)150)]
    [TestCase("a:byte = 255\r y = (a + 1).toByteWrap()",          (byte)0)]
    public void Chained_AfterArithmetic(string expr, object expected) =>
        expr.Calc().AssertResultHas("y", expected);

    // ─────────────────────────────────────────────────────────────────
    // dialect independence: semantics live in the NAME, not the dialect flag
    // ─────────────────────────────────────────────────────────────────

    [Test]
    public void Checked_ThrowsEvenInUncheckedDialect() =>
        Assert.Throws<FunnyRuntimeException>(
            () => "y = toByte(300)".CalcWithDialect(integerOverflow: IntegerOverflow.Unchecked));

    [Test]
    public void Wrap_WrapsEvenInCheckedDialect() =>
        "y = toByteWrap(300)".CalcWithDialect(integerOverflow: IntegerOverflow.Checked)
            .AssertResultHas("y", (byte)44);

    // ─────────────────────────────────────────────────────────────────
    // decimal dialect (real = decimal): same truncation semantics, no NaN/Inf domain
    // ─────────────────────────────────────────────────────────────────

    [TestCase("x:real = 1.9\r y = x.toByte()",         (byte)1)]
    [TestCase("x:real = -1.9\r y = x.toInt()",         -1)]
    [TestCase("x:real = 300.0\r y = x.toByteWrap()",   (byte)44)]
    [TestCase("x:real = 300.5\r y = x.toByteClamp()",  (byte)255)]
    [TestCase("x:real = -1.5\r y = x.toByteClamp()",   (byte)0)]
    public void Decimal_ReturnsExpected(string expr, object expected) =>
        expr.CalcWithDialect(realClrType: RealClrType.IsDecimal).AssertResultHas("y", expected);

    [TestCase("x:real = 256.0\r y = x.toByte()")]
    // 1e20 > ulong.MaxValue (~1.8e19); representable in decimal
    [TestCase("x:real = 100000000000000000000.0\r y = x.toUint64()")]
    [TestCase("x:real = -100000000000000000000.0\r y = x.toInt64Wrap()")] // beyond 64-bit domain → wrap: error
    public void Decimal_OutOfRange_Throws(string expr) =>
        Assert.Throws<FunnyRuntimeException>(
            () => expr.CalcWithDialect(realClrType: RealClrType.IsDecimal));

    [Test]
    public void Decimal_ToReal_ReturnsDecimal() {
        var result = "y = toReal(42)".CalcWithDialect(realClrType: RealClrType.IsDecimal);
        result.AssertResultHas("y", (decimal)42);
    }

    // ─────────────────────────────────────────────────────────────────
    // float-family dialect: toFloat32 / toFloat64; float32 as a source
    // ─────────────────────────────────────────────────────────────────

    [TestCase("x:int = 42\r y = x.toFloat32()",       42f)]
    // 2^24+1 is not representable in float32 — rounds to 2^24
    [TestCase("x:int = 16777217\r y = x.toFloat32()", 16777216f)]
    [TestCase("x:float32 = 3.9\r y = x.toByte()",     (byte)3)]
    [TestCase("x:float32 = 300.0\r y = x.toByteWrap()",  (byte)44)]
    [TestCase("x:float32 = 300.0\r y = x.toByteClamp()", (byte)255)]
    [TestCase("x:int = 42\r y = x.toFloat64()",       42.0)]   // toFloat64 ≡ toReal
    [TestCase("x:float32 = 1.5\r y = x.toFloat32()",  1.5f)]   // identity
    public void FloatFamily_ReturnsExpected(string expr, object expected) =>
        expr.CalcWithFloats().AssertResultHas("y", expected);

    [TestCase("z:float32 = 0.0\r x = z/z\r y = x.toByte()")]   // NaN → checked: error
    [TestCase("x:float32 = 300.0\r y = x.toByte()")]           // out of range → checked: error
    public void FloatFamily_Throws(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.CalcWithFloats());

    [Test]
    public void FloatFamily_ToFloat32_RealOverflow_IsInfinity() {
        var result = "x:real = 1e40\r y = x.toFloat32()".CalcWithFloats();
        Assert.IsTrue(float.IsPositiveInfinity((float)result.Get("y")));
    }

    [TestCase("y = toFloat32Wrap(1)")]
    [TestCase("y = toFloat32Clamp(1)")]
    public void FloatFamily_ToFloat32WrapClamp_DoNotExist(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.CalcWithFloats());

    // toFloat32/toFloat64 are dialect-gated: absent in the default dialect
    [TestCase("y = toFloat32(1)")]
    [TestCase("y = toFloat64(1)")]
    public void DefaultDialect_FloatFamilyNames_DoNotExist(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.Calc());

    // ─────────────────────────────────────────────────────────────────
    // non-numeric sources are rejected at compile time
    // ─────────────────────────────────────────────────────────────────

    [TestCase("y = toByte('abc')")]
    [TestCase("y = toByte(true)")]
    [TestCase("y = toInt64([1,2,3])")]
    public void NonNumericSource_IsParseError(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.Calc());
}
