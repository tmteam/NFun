using NFun;
using NFun.Exceptions;
using NFun.Functions;
using NFun.Interpretation.Functions;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests;

/// <summary>
/// Unit-level pins for the toXxx narrowing algebra (no parser): the critical
/// double→64-bit boundary — (double)long.MaxValue and (double)ulong.MaxValue
/// round UP to 2^63 / 2^64, so those images are already out of range — plus
/// long.MinValue canonicalization and the decimal path.
/// Invoked through the public API: ToNumericFunction.CreateConcrete + Calc.
/// </summary>
[TestFixture]
public class ToNumericNarrowingUnitTest {
    // Exact double images of the 64-bit domain bounds (rounded UP from Max):
    private const double TwoPow63 = 9.2233720368547758E18;   // (double)long.MaxValue
    private const double TwoPow64 = 1.8446744073709552E19;   // (double)ulong.MaxValue
    // Largest doubles strictly below the bounds:
    private const double BelowTwoPow63 = 9223372036854774784.0;
    private const double BelowTwoPow64 = 18446744073709549568.0;

    // ── double source, checked ──

    [TestCase(BelowTwoPow63, BaseFunnyType.Int64, 9223372036854774784L)]
    [TestCase(-TwoPow63, BaseFunnyType.Int64, long.MinValue)]  // exact -2^63: canonical (bits, isNegative) roundtrip
    [TestCase(-0.0, BaseFunnyType.UInt8, (byte)0)]
    [TestCase(BelowTwoPow64, BaseFunnyType.UInt64, 18446744073709549568UL)]
    public void DoubleSource_Checked_ReturnsExpected(double value, BaseFunnyType target, object expected) =>
        AssertNarrow(expected, value, BaseFunnyType.Real, target, NumericNarrowMode.Checked);

    [TestCase(TwoPow63, BaseFunnyType.Int64)]    // 2^63 image of long.MaxValue — out of range
    [TestCase(TwoPow64, BaseFunnyType.UInt64)]   // 2^64 image of ulong.MaxValue — out of range
    [TestCase(-1.0, BaseFunnyType.UInt64)]
    public void DoubleSource_Checked_Throws(double value, BaseFunnyType target) =>
        Assert.Throws<FunnyRuntimeException>(
            () => Narrow(value, BaseFunnyType.Real, target, NumericNarrowMode.Checked));

    // ── double source, wrap ──

    [TestCase(TwoPow63, BaseFunnyType.Int64, long.MinValue)]  // bits of 2^63 reinterpreted
    [TestCase(-1.0, BaseFunnyType.UInt64, ulong.MaxValue)]
    public void DoubleSource_Wrap_ReturnsExpected(double value, BaseFunnyType target, object expected) =>
        AssertNarrow(expected, value, BaseFunnyType.Real, target, NumericNarrowMode.Wrap);

    [TestCase(TwoPow64, BaseFunnyType.UInt64)]                 // 2^64 — outside [-2^63..2^64) even for wrap
    [TestCase(-9.223372036854778E18, BaseFunnyType.Int64)]     // first double below -2^63
    public void DoubleSource_Wrap_Throws(double value, BaseFunnyType target) =>
        Assert.Throws<FunnyRuntimeException>(
            () => Narrow(value, BaseFunnyType.Real, target, NumericNarrowMode.Wrap));

    // ── double source, clamp ──

    [TestCase(TwoPow63, BaseFunnyType.Int64, long.MaxValue)]
    [TestCase(TwoPow64, BaseFunnyType.UInt64, ulong.MaxValue)]
    [TestCase(double.PositiveInfinity, BaseFunnyType.Int64, long.MaxValue)]
    [TestCase(double.NegativeInfinity, BaseFunnyType.Int64, long.MinValue)]
    public void DoubleSource_Clamp_ReturnsExpected(double value, BaseFunnyType target, object expected) =>
        AssertNarrow(expected, value, BaseFunnyType.Real, target, NumericNarrowMode.Clamp);

    // ── integer sources: 64-bit canonical form (bits, isNegative) ──

    [TestCase(ulong.MaxValue, BaseFunnyType.UInt64, BaseFunnyType.Int64, NumericNarrowMode.Wrap, -1L)]
    [TestCase(long.MinValue, BaseFunnyType.Int64, BaseFunnyType.UInt64, NumericNarrowMode.Wrap, 9223372036854775808UL)]
    [TestCase(long.MinValue, BaseFunnyType.Int64, BaseFunnyType.Int64, NumericNarrowMode.Checked, long.MinValue)]
    public void IntegerSource_ReturnsExpected(
        object value, BaseFunnyType source, BaseFunnyType target, NumericNarrowMode mode, object expected) =>
        AssertNarrow(expected, value, source, target, mode);

    [Test]
    public void IntegerSource_LongMin_ToUint64_Checked_Throws() =>
        Assert.Throws<FunnyRuntimeException>(
            () => Narrow(long.MinValue, BaseFunnyType.Int64, BaseFunnyType.UInt64, NumericNarrowMode.Checked));

    // ── float32 source reuses the double path via an exact float→double reader ──

    [Test]
    public void Float32Source_Wrap_300ToByte_Is44() =>
        AssertNarrow((byte)44, 300f, BaseFunnyType.Float32, BaseFunnyType.UInt8, NumericNarrowMode.Wrap);

    // ── decimal dialect path (decimal is not an attribute constant — plain [Test]s) ──

    [Test]
    public void DecimalSource_Wrap_300ToByte_Is44() =>
        AssertNarrow((byte)44, 300m, BaseFunnyType.Real, BaseFunnyType.UInt8, NumericNarrowMode.Wrap,
            FunnyConverter.RealIsDecimal);

    [Test] // ulong.MaxValue is exact in decimal — checked passes ('>' bound, unlike the double '>=' bound)
    public void DecimalSource_UlongMax_Checked_Passes() =>
        AssertNarrow(ulong.MaxValue, (decimal)ulong.MaxValue, BaseFunnyType.Real, BaseFunnyType.UInt64,
            NumericNarrowMode.Checked, FunnyConverter.RealIsDecimal);

    [Test]
    public void DecimalSource_AboveUlongMax_Checked_Throws() =>
        Assert.Throws<FunnyRuntimeException>(
            () => Narrow(18446744073709551616m, BaseFunnyType.Real, BaseFunnyType.UInt64,
                NumericNarrowMode.Checked, FunnyConverter.RealIsDecimal));

    // ── plumbing: public API only — ToNumericFunction.CreateConcrete + Calc ──

    private static void AssertNarrow(
        object expected, object value, BaseFunnyType source, BaseFunnyType target,
        NumericNarrowMode mode, FunnyConverter converter = null) {
        var actual = Narrow(value, source, target, mode, converter);
        Assert.AreEqual(expected, actual);
        Assert.AreEqual(expected.GetType(), actual.GetType(), "result CLR type must match the target exactly");
    }

    private static object Narrow(
        object value, BaseFunnyType source, BaseFunnyType target,
        NumericNarrowMode mode, FunnyConverter converter = null) {
        var fun = new ToNumericFunction("toTest", FunnyType.PrimitiveOf(target), mode);
        var concrete = (FunctionWithSingleArg)fun.CreateConcrete(
            new[] { FunnyType.PrimitiveOf(source) },
            new TestSelectorContext(converter ?? FunnyConverter.RealIsDouble));
        return concrete.Calc(value);
    }

    private sealed class TestSelectorContext : IFunctionSelectorContext {
        public TestSelectorContext(FunnyConverter converter) => Converter = converter;
        public FunnyConverter Converter { get; }
        public bool AllowIntegerOverflow => false;
    }
}
