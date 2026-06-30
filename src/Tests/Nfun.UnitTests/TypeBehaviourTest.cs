using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests;

/// <summary>
/// Direct pins for TypeBehaviour polymorphic API surface — CLR/funny conversion,
/// real-literal materialization, dialect-specific defaults.
/// </summary>
[TestFixture]
public class TypeBehaviourTest {

    // ─── GetRealConstantValue(long) ────────────────────────────────

    [Test] public void RealIsDouble_GetRealConstantValue_Long_Zero() =>
        Assert.AreEqual(0.0, TypeBehaviour.RealIsDouble.GetRealConstantValue(0L));

    [Test] public void RealIsDouble_GetRealConstantValue_Long_Positive() =>
        Assert.AreEqual(42.0, TypeBehaviour.RealIsDouble.GetRealConstantValue(42L));

    [Test] public void RealIsDouble_GetRealConstantValue_Long_Negative() =>
        Assert.AreEqual(-42.0, TypeBehaviour.RealIsDouble.GetRealConstantValue(-42L));

    // long.MaxValue = 2^63 - 1. Cast to double loses precision (≥ 2^53).
    [Test] public void RealIsDouble_GetRealConstantValue_Long_MaxValue_LossyToDouble() =>
        Assert.AreEqual((double)long.MaxValue, TypeBehaviour.RealIsDouble.GetRealConstantValue(long.MaxValue));

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_Zero() =>
        Assert.AreEqual(0m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(0L));

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_Positive() =>
        Assert.AreEqual(42m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(42L));

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_Negative() =>
        Assert.AreEqual(-42m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(-42L));

    // long.MaxValue fits exactly in decimal — no precision loss.
    [Test] public void RealIsDecimal_GetRealConstantValue_Long_MaxValue_Exact() =>
        Assert.AreEqual(9223372036854775807m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(long.MaxValue));

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_MinValue_Exact() =>
        Assert.AreEqual(-9223372036854775808m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(long.MinValue));

    // ─── GetRealConstantValue(ulong) — reached for literals > long.MaxValue ──

    [Test] public void RealIsDouble_GetRealConstantValue_ULong_Zero() =>
        Assert.AreEqual(0.0, TypeBehaviour.RealIsDouble.GetRealConstantValue(0UL));

    [Test] public void RealIsDouble_GetRealConstantValue_ULong_AboveLongMax() =>
        Assert.AreEqual((double)(ulong)9223372036854775808UL,
            TypeBehaviour.RealIsDouble.GetRealConstantValue(9223372036854775808UL));

    // ulong.MaxValue = 2^64 - 1. Cast to double loses precision.
    [Test] public void RealIsDouble_GetRealConstantValue_ULong_MaxValue_LossyToDouble() =>
        Assert.AreEqual((double)ulong.MaxValue, TypeBehaviour.RealIsDouble.GetRealConstantValue(ulong.MaxValue));

    [Test] public void RealIsDecimal_GetRealConstantValue_ULong_Zero() =>
        Assert.AreEqual(0m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(0UL));

    [Test] public void RealIsDecimal_GetRealConstantValue_ULong_AboveLongMax_Exact() =>
        Assert.AreEqual(9223372036854775808m,
            TypeBehaviour.RealIsDecimal.GetRealConstantValue(9223372036854775808UL));

    // ulong.MaxValue fits exactly in decimal (max decimal ~7.9e28).
    [Test] public void RealIsDecimal_GetRealConstantValue_ULong_MaxValue_Exact() =>
        Assert.AreEqual(18446744073709551615m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(ulong.MaxValue));

    // ─── F32F64 inherits Double implementation ─────────────────────

    [Test] public void F32F64_GetRealConstantValue_Long_InheritsDoubleBehavior() =>
        Assert.AreEqual(42.0, TypeBehaviour.RealIsDoubleWithFloatFamily.GetRealConstantValue(42L));

    [Test] public void F32F64_GetRealConstantValue_ULong_InheritsDoubleBehavior() =>
        Assert.AreEqual((double)ulong.MaxValue,
            TypeBehaviour.RealIsDoubleWithFloatFamily.GetRealConstantValue(ulong.MaxValue));

    // ─── RealLiteralIsGeneric ──────────────────────────────────────

    [Test] public void RealIsDouble_RealLiteralIsGeneric_False() =>
        Assert.IsFalse(TypeBehaviour.RealIsDouble.RealLiteralIsGeneric);

    [Test] public void RealIsDecimal_RealLiteralIsGeneric_False() =>
        Assert.IsFalse(TypeBehaviour.RealIsDecimal.RealLiteralIsGeneric);

    [Test] public void F32F64_RealLiteralIsGeneric_True() =>
        Assert.IsTrue(TypeBehaviour.RealIsDoubleWithFloatFamily.RealLiteralIsGeneric);

    // ─── CoerceParsedRealLiteral ───────────────────────────────────

    [Test] public void RealIsDouble_CoerceParsedRealLiteral_Identity() =>
        Assert.AreEqual(3.14, TypeBehaviour.RealIsDouble.CoerceParsedRealLiteral(3.14, FunnyType.Real));

    [Test] public void RealIsDecimal_CoerceParsedRealLiteral_Identity() =>
        Assert.AreEqual(3.14m, TypeBehaviour.RealIsDecimal.CoerceParsedRealLiteral(3.14m, FunnyType.Real));

    [Test] public void F32F64_CoerceParsedRealLiteral_ToFloat32_CastsDouble() =>
        Assert.AreEqual(3.14f,
            TypeBehaviour.RealIsDoubleWithFloatFamily.CoerceParsedRealLiteral(3.14, FunnyType.Float32));

    [Test] public void F32F64_CoerceParsedRealLiteral_ToReal_LeavesDouble() =>
        Assert.AreEqual(3.14,
            TypeBehaviour.RealIsDoubleWithFloatFamily.CoerceParsedRealLiteral(3.14, FunnyType.Real));

    // ─── ConvertClrValueToReal ─────────────────────────────────────

    [Test] public void RealIsDouble_ConvertClrValueToReal_FromInt() =>
        Assert.AreEqual(42.0, TypeBehaviour.RealIsDouble.ConvertClrValueToReal(42));

    [Test] public void RealIsDouble_ConvertClrValueToReal_FromFloat() =>
        Assert.AreEqual(3.14, (double)TypeBehaviour.RealIsDouble.ConvertClrValueToReal(3.14f), 0.0001);

    [Test] public void RealIsDecimal_ConvertClrValueToReal_FromInt() =>
        Assert.AreEqual(42m, TypeBehaviour.RealIsDecimal.ConvertClrValueToReal(42));

    [Test] public void RealIsDecimal_ConvertClrValueToReal_FromDouble() =>
        Assert.AreEqual(3.14m, TypeBehaviour.RealIsDecimal.ConvertClrValueToReal(3.14));

    [Test] public void F32F64_ConvertClrValueToReal_InheritsDouble() =>
        Assert.AreEqual(42.0, TypeBehaviour.RealIsDoubleWithFloatFamily.ConvertClrValueToReal(42));

    // ─── RealType property ─────────────────────────────────────────

    [Test] public void RealIsDouble_RealType_IsDouble() =>
        Assert.AreEqual(typeof(double), TypeBehaviour.RealIsDouble.RealType);

    [Test] public void RealIsDecimal_RealType_IsDecimal() =>
        Assert.AreEqual(typeof(decimal), TypeBehaviour.RealIsDecimal.RealType);

    [Test] public void F32F64_RealType_IsDouble() =>
        Assert.AreEqual(typeof(double), TypeBehaviour.RealIsDoubleWithFloatFamily.RealType);

    // ─── Big-int → Real precision at IEEE 754 boundaries ───────────
    // double mantissa is 53 bits → integers exact up to 2^53 = 9007199254740992.
    // Above that, every-other integer becomes representable (round-to-even).
    // decimal has ~29 significant digits → any int64/uint64 fits exactly.

    private const long TwoTo53      = 9007199254740992L;  // last exact int in double
    private const long TwoTo53Plus1 = 9007199254740993L;  // rounds to 2^53 in double
    private const long TwoTo53Plus2 = 9007199254740994L;  // exact in double
    private const long TwoTo54      = 18014398509481984L; // exact in double
    private const long TwoTo54Plus1 = 18014398509481985L; // rounds down (to 2^54) in double

    [Test] public void RealIsDouble_GetRealConstantValue_Long_Below2Pow53_Exact() =>
        Assert.AreEqual(TwoTo53 - 1.0, TypeBehaviour.RealIsDouble.GetRealConstantValue(TwoTo53 - 1));

    [Test] public void RealIsDouble_GetRealConstantValue_Long_At2Pow53_Exact() =>
        Assert.AreEqual((double)TwoTo53, TypeBehaviour.RealIsDouble.GetRealConstantValue(TwoTo53));

    [Test] public void RealIsDouble_GetRealConstantValue_Long_2Pow53Plus1_RoundsDown() {
        var d = (double)TypeBehaviour.RealIsDouble.GetRealConstantValue(TwoTo53Plus1);
        Assert.AreEqual((double)TwoTo53, d,
            "2^53+1 has no exact double representation; nearest-even rounding lands on 2^53");
    }

    [Test] public void RealIsDouble_GetRealConstantValue_Long_2Pow53Plus2_ExactAgain() =>
        Assert.AreEqual((double)TwoTo53Plus2, TypeBehaviour.RealIsDouble.GetRealConstantValue(TwoTo53Plus2));

    [Test] public void RealIsDouble_GetRealConstantValue_Long_2Pow54Plus1_RoundsDown() {
        var d = (double)TypeBehaviour.RealIsDouble.GetRealConstantValue(TwoTo54Plus1);
        Assert.AreEqual((double)TwoTo54, d,
            "Past 2^54 every 2nd int rounds; 2^54+1 → 2^54");
    }

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_At2Pow53_Exact() =>
        Assert.AreEqual((decimal)TwoTo53, TypeBehaviour.RealIsDecimal.GetRealConstantValue(TwoTo53));

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_2Pow53Plus1_ExactUnlikeDouble() =>
        Assert.AreEqual(9007199254740993m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(TwoTo53Plus1));

    [Test] public void RealIsDecimal_GetRealConstantValue_Long_2Pow54Plus1_ExactUnlikeDouble() =>
        Assert.AreEqual(18014398509481985m, TypeBehaviour.RealIsDecimal.GetRealConstantValue(TwoTo54Plus1));

    // Direct A/B: same int literal materialises differently across Real-CLR-type dialects.
    [Test] public void DoubleVsDecimal_At2Pow53Plus1_DoubleLossyDecimalExact() {
        var asDouble  = (double) TypeBehaviour.RealIsDouble .GetRealConstantValue(TwoTo53Plus1);
        var asDecimal = (decimal)TypeBehaviour.RealIsDecimal.GetRealConstantValue(TwoTo53Plus1);
        Assert.AreEqual((double)TwoTo53,   asDouble,  "double rounds to 2^53");
        Assert.AreEqual(9007199254740993m, asDecimal, "decimal preserves full value");
        Assert.AreNotEqual((decimal)asDouble, asDecimal, "cross-dialect divergence must be visible");
    }
}
