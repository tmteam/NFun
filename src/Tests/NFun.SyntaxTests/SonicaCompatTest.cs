using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class SonicaCompatTest {
    private static FunnyRuntime Build(string s, IntegerPreferredType pref) =>
        Funny.Hardcore
            .WithDialect(IfExpressionSetup.IfIfElse, pref, RealClrType.IsDouble)
            .Build(s);

    private static object Run(string s, IntegerPreferredType pref) {
        var rt = Build(s, pref);
        rt.Run();
        return rt["out"].Value;
    }

    private static BaseFunnyType TypeOf(string s, IntegerPreferredType pref) =>
        Build(s, pref)["out"].Type.BaseType;

    [TestCase(IntegerPreferredType.I32, BaseFunnyType.Int32)]
    [TestCase(IntegerPreferredType.I64, BaseFunnyType.Int64)]
    [TestCase(IntegerPreferredType.Real, BaseFunnyType.Real)]
    public void BareLiteral_FollowsDialect(IntegerPreferredType pref, BaseFunnyType expected)
        => Assert.AreEqual(expected, TypeOf("1", pref));

    [TestCase("1 + 1", IntegerPreferredType.I32, BaseFunnyType.Int32, 2)]
    [TestCase("1 - 1", IntegerPreferredType.I32, BaseFunnyType.Int32, 0)]
    [TestCase("2 * 3", IntegerPreferredType.I32, BaseFunnyType.Int32, 6)]
    [TestCase("1 + 1", IntegerPreferredType.I64, BaseFunnyType.Int64, 2L)]
    [TestCase("1 + 1", IntegerPreferredType.Real, BaseFunnyType.Real, 2.0)]
    public void LiteralArithmetic_FollowsDialect(string expr, IntegerPreferredType pref,
        BaseFunnyType type, object value) {
        Assert.AreEqual(type, TypeOf(expr, pref));
        Assert.AreEqual(value, Run(expr, pref));
    }

    [TestCase("1 & 1",   1)]
    [TestCase("15 & 1",  1)]
    [TestCase("1 | 1",   1)]
    [TestCase("15 | 1",  15)]
    [TestCase("15 ^ 0",  15)]
    [TestCase("15 ^ 15", 0)]
    public void LiteralBitwise_I32Dialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.I32));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.I32));
    }

    [TestCase("1 & 1",   1)]
    [TestCase("1 & 0",   0)]
    [TestCase("15 & 1",  1)]
    [TestCase("1 | 1",   1)]
    [TestCase("15 | 1",  15)]
    [TestCase("15 ^ 0",  15)]
    [TestCase("15 ^ 15", 0)]
    public void LiteralBitwise_RealDialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.Real));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.Real));
    }

    [TestCase("1<<0", 1)]
    [TestCase("1<<1", 2)]
    [TestCase("1<<2", 4)]
    [TestCase("1>>0", 1)]
    [TestCase("2>>1", 1)]
    [TestCase("4>>2", 1)]
    public void LiteralShift_RealDialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.Real));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.Real));
    }

    [TestCase(IntegerPreferredType.I32)]
    [TestCase(IntegerPreferredType.I64)]
    [TestCase(IntegerPreferredType.Real)]
    public void HugeLiteral_AlwaysInt64(IntegerPreferredType pref) {
        Assert.AreEqual(BaseFunnyType.Int64, TypeOf("5000000000 & 1", pref));
        Assert.AreEqual(0L, Run("5000000000 & 1", pref));
    }

    [TestCase("x:int;  q:int;  out = x & q", BaseFunnyType.Int32)]
    [TestCase("x:uint; q:uint; out = x & q", BaseFunnyType.UInt32)]
    [TestCase("x:byte; q:byte; out = x & q", BaseFunnyType.UInt8)]
    [TestCase("x:int8; q:int8; out = x & q", BaseFunnyType.Int8)]
    [TestCase("x:int16; q:int16; out = x & q", BaseFunnyType.Int16)]
    [TestCase("x:int64; q:int64; out = x & q", BaseFunnyType.Int64)]
    public void TypedInputs_Bitwise_ResultIsArgType(string expr, BaseFunnyType expected) {
        foreach (var pref in new[] { IntegerPreferredType.I32, IntegerPreferredType.I64, IntegerPreferredType.Real })
            Assert.AreEqual(expected, TypeOf(expr, pref), $"dialect={pref}");
    }

    [TestCase("x:int;  out = x << 1",  BaseFunnyType.Int32)]
    [TestCase("x:uint; out = x << 1",  BaseFunnyType.UInt32)]
    [TestCase("x:byte; out = x << 1",  BaseFunnyType.UInt8)]
    [TestCase("x:int8; out = x << 1",  BaseFunnyType.Int8)]
    [TestCase("x:int64; out = x << 1", BaseFunnyType.Int64)]
    public void TypedShift_ResultIsLhsType(string expr, BaseFunnyType expected)
        => Assert.AreEqual(expected, TypeOf(expr, IntegerPreferredType.Real));

    [TestCase("out:int16 = 1 & 1", BaseFunnyType.Int16)]
    [TestCase("out:int8 = 1 & 1",  BaseFunnyType.Int8)]
    [TestCase("out:byte = 1 & 1",  BaseFunnyType.UInt8)]
    [TestCase("out:uint = 1 & 1",  BaseFunnyType.UInt32)]
    [TestCase("out:int64 = 1 & 1", BaseFunnyType.Int64)]
    [TestCase("out:int = 1 & 1",   BaseFunnyType.Int32)]
    public void OutputAnnotation_OverridesInference(string expr, BaseFunnyType expected) {
        foreach (var pref in new[] { IntegerPreferredType.I32, IntegerPreferredType.I64, IntegerPreferredType.Real })
            Assert.AreEqual(expected, TypeOf(expr, pref), $"dialect={pref}");
    }

    [TestCase("10 // 3", 3)]
    public void NonBit_PureGenericIntegers_RealDialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.Real));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.Real));
    }

    [Test]
    public void Remainder_RealDialect_IsReal() {
        Assert.AreEqual(BaseFunnyType.Real, TypeOf("10 % 3", IntegerPreferredType.Real));
        Assert.AreEqual(1.0, Run("10 % 3", IntegerPreferredType.Real));
    }

    [Test]
    public void Remainder_I32Dialect_IsInt32() {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf("10 % 3", IntegerPreferredType.I32));
        Assert.AreEqual(1, Run("10 % 3", IntegerPreferredType.I32));
    }

    [TestCase("0xff & 0xf",  IntegerPreferredType.Real, 15)]
    [TestCase("0xff & 0xf",  IntegerPreferredType.I32,  15)]
    [TestCase("0xff & 0xf",  IntegerPreferredType.I64,  15)]
    public void HexLiteral_Bitwise_RecordedBehaviour(string expr, IntegerPreferredType pref, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, pref));
        Assert.AreEqual(expected, Run(expr, pref));
    }

    [TestCase("x:int;  out = x & 1", BaseFunnyType.Int32)]
    [TestCase("x:uint; out = x & 1", BaseFunnyType.UInt32)]
    [TestCase("x:byte; out = x & 1", BaseFunnyType.UInt8)]
    [TestCase("x:int8; out = x & 1", BaseFunnyType.Int8)]
    [TestCase("x:int16; out = x & 1", BaseFunnyType.Int16)]
    public void TypedTimesLiteral_Bitwise_AdaptsToTyped(string expr, BaseFunnyType expected)
        => Assert.AreEqual(expected, TypeOf(expr, IntegerPreferredType.Real));

    [Test]
    public void UintShiftByUint_RecordedBehaviour() {
        // Status: PARSE ERROR — `<<`'s RHS is fixed UInt8, can't accept UInt32.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => Build("x:uint; out = x << x", IntegerPreferredType.Real));
    }
}
