using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators;

public class IntegerBitOperatorsTest {
    [TestCase("y:int64 = 1 & 1", (Int64)1)]
    [TestCase("y:int64 = 1 & 2", (Int64)0)]
    [TestCase("y:int64 = 2 & 2", (Int64)2)]
    [TestCase("y:int64 = 1 & 3", (Int64)1)]
    [TestCase("y:int64 = 1 & 3", (Int64)1)]
    [TestCase("y:int32 = 1 & 1", (Int32)1)]
    [TestCase("y:int32 = 1 & 2", (Int32)0)]
    [TestCase("y:int32 = 2 & 2", (Int32)2)]
    [TestCase("y:int32 = 1 & 3", (Int32)1)]
    [TestCase("y:int16 = 1 & 1", (Int16)1)]
    [TestCase("y:int16 = 1 & 2", (Int16)0)]
    [TestCase("y:int16 = 2 & 2", (Int16)2)]
    [TestCase("y:int16 = 1 & 3", (Int16)1)]
    [TestCase("y:uint64 = 1 & 1", (UInt64)1)]
    [TestCase("y:uint64 = 1 & 2", (UInt64)0)]
    [TestCase("y:uint64 = 2 & 2", (UInt64)2)]
    [TestCase("y:uint64 = 1 & 3", (UInt64)1)]
    [TestCase("y:uint32 = 1 & 1", (UInt32)1)]
    [TestCase("y:uint32 = 1 & 2", (UInt32)0)]
    [TestCase("y:uint32 = 2 & 2", (UInt32)2)]
    [TestCase("y:uint32 = 1 & 3", (UInt32)1)]
    [TestCase("y:uint16 = 1 & 1", (UInt16)1)]
    [TestCase("y:uint16 = 1 & 2", (UInt16)0)]
    [TestCase("y:uint16 = 2 & 2", (UInt16)2)]
    [TestCase("y:uint16 = 1 & 3", (UInt16)1)]
    [TestCase("y:uint8 = 1 & 1", (byte)1)]
    [TestCase("y:uint8 = 1 & 2", (byte)0)]
    [TestCase("y:uint8 = 2 & 2", (byte)2)]
    [TestCase("y:uint8 = 1 & 3", (byte)1)]
    [TestCase("y:int8 = 1 & 1", (sbyte)1)]
    [TestCase("y:int8 = 1 & 2", (sbyte)0)]
    [TestCase("y:int8 = 2 & 2", (sbyte)2)]
    [TestCase("y:int8 = 1 & 3", (sbyte)1)]
    [TestCase("y = 0xFFFFFFFF & 0x0", (long)0)]
    [TestCase("y = 0xFFFFFFFF & 0xFFFFFFFF", (long)0xFFFFFFFF)]
    [TestCase("y:uint64 = 0xFFFFFFFF_FFFFFFFF & 0xFFFFFFFF_FFFFFFFF", (UInt64)0xFFFFFFFF_FFFFFFFF)]
    [TestCase("y:uint64 = 0xFFFFFFFF_FFFFFFFF & 0", (UInt64)0)]
    [TestCase("y:uint64 = 0 & 0xFFFFFFFF_FFFFFFFF", (UInt64)0)]
    [TestCase("y:uint32 = 0xFFFFFFFF & 0xFFFFFFFF", (UInt32)0xFFFFFFFF)]
    [TestCase("y:uint32 = 0xFFFFFFFF & 0", (UInt32)0)]
    [TestCase("y:uint32 = 0 & 0xFFFFFFFF", (UInt32)0)]
    [TestCase("y:uint16 = 0xFFFF & 0xFFFF", (UInt16)0xFFFF)]
    [TestCase("y:uint16 = 0xFFFF & 0", (UInt16)0)]
    [TestCase("y:uint16 = 0 & 0xFFFF", (UInt16)0)]
    [TestCase("y:uint8 = 0xFF & 0xFF", (byte)0xFF)]
    [TestCase("y:uint8 = 0xFF & 0", (byte)0)]
    [TestCase("y:uint8 = 0 & 0xFF", (byte)0)]
    [TestCase("y:int8 = 0x7F & 0x7F", (sbyte)0x7F)]
    [TestCase("y:int8 = 0x7F & 0",    (sbyte)0)]
    [TestCase("y:int8 = 0 & 0x7F",    (sbyte)0)]
    public void ConstantBitAnd(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = 0 | 2", 2)]
    [TestCase("y = 1 | 2", 3)]
    [TestCase("y = 1 | 4", 5)]
    [TestCase("y:int64 = 1 | 1", (Int64)1)]
    [TestCase("y:int64 = 1 | 2", (Int64)3)]
    [TestCase("y:int64 = 2 | 2", (Int64)2)]
    [TestCase("y:int64 = 1 | 3", (Int64)3)]
    [TestCase("y:int64 = 1 | 3", (Int64)3)]
    [TestCase("y:int32 = 1 | 1", (Int32)1)]
    [TestCase("y:int32 = 1 | 2", (Int32)3)]
    [TestCase("y:int32 = 2 | 2", (Int32)2)]
    [TestCase("y:int32 = 1 | 3", (Int32)3)]
    [TestCase("y:int16 = 1 | 1", (Int16)1)]
    [TestCase("y:int16 = 1 | 2", (Int16)3)]
    [TestCase("y:int16 = 2 | 2", (Int16)2)]
    [TestCase("y:int16 = 1 | 3", (Int16)3)]
    [TestCase("y:uint64 = 1 | 1", (UInt64)1)]
    [TestCase("y:uint64 = 1 | 2", (UInt64)3)]
    [TestCase("y:uint64 = 2 | 2", (UInt64)2)]
    [TestCase("y:uint64 = 1 | 3", (UInt64)3)]
    [TestCase("y:uint32 = 1 | 1", (UInt32)1)]
    [TestCase("y:uint32 = 1 | 2", (UInt32)3)]
    [TestCase("y:uint32 = 2 | 2", (UInt32)2)]
    [TestCase("y:uint32 = 1 | 3", (UInt32)3)]
    [TestCase("y:uint16 = 1 | 1", (UInt16)1)]
    [TestCase("y:uint16 = 1 | 2", (UInt16)3)]
    [TestCase("y:uint16 = 2 | 2", (UInt16)2)]
    [TestCase("y:uint16 = 1 | 3", (UInt16)3)]
    [TestCase("y:uint8 = 1 | 1", (byte)1)]
    [TestCase("y:uint8 = 1 | 2", (byte)3)]
    [TestCase("y:uint8 = 2 | 2", (byte)2)]
    [TestCase("y:uint8 = 1 | 3", (byte)3)]
    [TestCase("y:int8 = 1 | 1", (sbyte)1)]
    [TestCase("y:int8 = 1 | 2", (sbyte)3)]
    [TestCase("y:int8 = 2 | 2", (sbyte)2)]
    [TestCase("y:int8 = 1 | 3", (sbyte)3)]
    [TestCase("y = 0xFFFFFFFF | 0x0", (long)0xFFFFFFFF)]
    [TestCase("y = 0xFFFFFFFF | 0xFFFFFFFF", (long)0xFFFFFFFF)]
    [TestCase("y:uint64 = 0xFFFFFFFF_FFFFFFFF | 0xFFFFFFFF_FFFFFFFF", (UInt64)0xFFFFFFFF_FFFFFFFF)]
    [TestCase("y:uint64 = 0xFFFFFFFF_FFFFFFFF | 0", (UInt64)0xFFFFFFFF_FFFFFFFF)]
    [TestCase("y:uint64 = 0 | 0xFFFFFFFF_FFFFFFFF", (UInt64)0xFFFFFFFF_FFFFFFFF)]
    [TestCase("y:uint32 = 0xFFFFFFFF | 0xFFFFFFFF", (UInt32)0xFFFFFFFF)]
    [TestCase("y:uint32 = 0xFFFFFFFF | 0", (UInt32)0xFFFFFFFF)]
    [TestCase("y:uint32 = 0 | 0xFFFFFFFF", (UInt32)0xFFFFFFFF)]
    [TestCase("y:uint16 = 0xFFFF | 0xFFFF", (UInt16)0xFFFF)]
    [TestCase("y:uint16 = 0xFFFF | 0", (UInt16)0xFFFF)]
    [TestCase("y:uint16 = 0 | 0xFFFF", (UInt16)0xFFFF)]
    [TestCase("y:uint8 = 0xFF | 0xFF", (byte)0xFF)]
    [TestCase("y:uint8 = 0xFF | 0", (byte)0xFF)]
    [TestCase("y:uint8 = 0 | 0xFF", (byte)0xFF)]
    [TestCase("y:int8 = 0x7F | 0x7F", (sbyte)0x7F)]
    [TestCase("y:int8 = 0x7F | 0",    (sbyte)0x7F)]
    [TestCase("y:int8 = 0 | 0x7F",    (sbyte)0x7F)]
    public void ConstantBitOr(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y:int64 = 0 ^ 1", (Int64)1)]
    [TestCase("y:int64 = 1 ^ 0", (Int64)1)]
    [TestCase("y:int64 = 1 ^ 1", (Int64)0)]
    [TestCase("y:int64 = 0 ^ 0", (Int64)0)]
    [TestCase("y:int32 = 0 ^ 1", (Int32)1)]
    [TestCase("y:int32 = 1 ^ 0", (Int32)1)]
    [TestCase("y:int32 = 1 ^ 1", (Int32)0)]
    [TestCase("y:int32 = 0 ^ 0", (Int32)0)]
    [TestCase("y:int16 = 0 ^ 1", (Int16)1)]
    [TestCase("y:int16 = 1 ^ 0", (Int16)1)]
    [TestCase("y:int16 = 1 ^ 1", (Int16)0)]
    [TestCase("y:int16 = 0 ^ 0", (Int16)0)]
    [TestCase("y:uint64 = 0 ^ 1", (UInt64)1)]
    [TestCase("y:uint64 = 1 ^ 0", (UInt64)1)]
    [TestCase("y:uint64 = 1 ^ 1", (UInt64)0)]
    [TestCase("y:uint64 = 0 ^ 0", (UInt64)0)]
    [TestCase("y:uint32 = 0 ^ 1", (UInt32)1)]
    [TestCase("y:uint32 = 1 ^ 0", (UInt32)1)]
    [TestCase("y:uint32 = 1 ^ 1", (UInt32)0)]
    [TestCase("y:uint32 = 0 ^ 0", (UInt32)0)]
    [TestCase("y:uint16 = 0 ^ 1", (UInt16)1)]
    [TestCase("y:uint16 = 1 ^ 0", (UInt16)1)]
    [TestCase("y:uint16 = 1 ^ 1", (UInt16)0)]
    [TestCase("y:uint16 = 0 ^ 0", (UInt16)0)]
    [TestCase("y:uint8 = 0 ^ 1", (byte)1)]
    [TestCase("y:uint8 = 1 ^ 0", (byte)1)]
    [TestCase("y:uint8 = 1 ^ 1", (byte)0)]
    [TestCase("y:uint8 = 0 ^ 0", (byte)0)]
    [TestCase("y:int8 = 0 ^ 1", (sbyte)1)]
    [TestCase("y:int8 = 1 ^ 0", (sbyte)1)]
    [TestCase("y:int8 = 1 ^ 1", (sbyte)0)]
    [TestCase("y:int8 = 0 ^ 0", (sbyte)0)]
    [TestCase("y = 0xFFFFFFFF ^ 0x0", (long)0xFFFFFFFF)]
    [TestCase("y = 0xFFFFFFFF ^ 0xFFFFFFFF", (long)0)]
    [TestCase("y:uint64 = 0xFFFFFFFF_FFFFFFFF ^ 0xFFFFFFFF_FFFFFFFF", (UInt64)0)]
    [TestCase("y:uint64 = 0xFFFFFFFF_FFFFFFFF ^ 0", (UInt64)0xFFFFFFFF_FFFFFFFF)]
    [TestCase("y:uint64 = 0 ^ 0xFFFFFFFF_FFFFFFFF", (UInt64)0xFFFFFFFF_FFFFFFFF)]
    [TestCase("y:uint32 = 0xFFFFFFFF ^ 0xFFFFFFFF", (UInt32)0)]
    [TestCase("y:uint32 = 0xFFFFFFFF ^ 0", (UInt32)0xFFFFFFFF)]
    [TestCase("y:uint32 = 0 ^ 0xFFFFFFFF", (UInt32)0xFFFFFFFF)]
    [TestCase("y:uint16 = 0xFFFF ^ 0xFFFF", (UInt16)0)]
    [TestCase("y:uint16 = 0xFFFF ^ 0", (UInt16)0xFFFF)]
    [TestCase("y:uint16 = 0 ^ 0xFFFF", (UInt16)0xFFFF)]
    [TestCase("y:uint8 = 0xFF ^ 0xFF", (byte)0)]
    [TestCase("y:uint8 = 0xFF ^ 0", (byte)0xFF)]
    [TestCase("y:uint8 = 0 ^ 0xFF", (byte)0xFF)]
    [TestCase("y:int8 = 0x7F ^ 0x7F", (sbyte)0)]
    [TestCase("y:int8 = 0x7F ^ 0",    (sbyte)0x7F)]
    [TestCase("y:int8 = 0 ^ 0x7F",    (sbyte)0x7F)]
    public void ConstantBitXor(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = 1 << 3", 8)]
    [TestCase("y = 8 >> 3", 1)]
    [TestCase("y:int64 = 1 << 3", (Int64)8)]
    [TestCase("y:int64 = 8 >> 3", (Int64)1)]
    [TestCase("y:int32 = 1 << 3", (Int32)8)]
    [TestCase("y:int32 = 8 >> 3", (Int32)1)]
    [TestCase("y:int32 = 12345 >> 3", (Int32)0b0110_0000_0111)]
    [TestCase("y:uint64 = 1 << 3", (UInt64)8)]
    [TestCase("y:uint64 = 8 >> 3", (UInt64)1)]
    [TestCase("y:uint64 = 12345 >> 3", (UInt64)0b0110_0000_0111)]
    [TestCase("y:uint32 = 1 << 3", (UInt32)8)]
    [TestCase("y:uint32 = 8 >> 3", (UInt32)1)]
    [TestCase("y:uint32 = 12345 >> 3", (UInt32)0b0110_0000_0111)]
    [TestCase("y:int16 = 1 << 3", (Int16)8)]
    [TestCase("y:int16 = 8 >> 3", (Int16)1)]
    [TestCase("y:int16 = 12345 >> 3", (Int16)0b0110_0000_0111)]
    [TestCase("y:uint16 = 1 << 3", (UInt16)8)]
    [TestCase("y:uint16 = 8 >> 3", (UInt16)1)]
    [TestCase("y:uint16 = 255 >> 3", (UInt16)0b0001_1111)]
    [TestCase("y:uint16 = 12345 >> 3", (UInt16)0b0110_0000_0111)]
    [TestCase("y:byte = 1 << 3", (byte)0b0000_1000)]
    [TestCase("y:byte = 8 >> 3", (byte)0b0000_0001)]
    [TestCase("y:byte = 255 >> 3", (byte)0b0001_1111)]
    [TestCase("y:byte  = 60 << 3", (byte)0b1110_0000)]
    [TestCase("y:byte  = 60 << 1", (byte)0b0111_1000)]
    public void ConstantBitShift(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y:uint16 = ~1", (UInt16)0xFFFE)]
    [TestCase("y:uint16 = ~0xF0F0", (UInt16)0x0F0F)]
    [TestCase("y = ~1", (int)-2)]
    [TestCase("y = ~-1", (int)0)]
    [TestCase("y:int64 = ~1", (long)-2)]
    [TestCase("y:int64 = ~-1", (long)0)]
    [TestCase("y:int64 = ~0xF0F0F0F0", (long)-4042322161)]
    [TestCase("y:int = ~1", (int)-2)]
    [TestCase("y:int = ~-1", (int)0)]
    [TestCase("y:int = ~0x00F0F0F0", (int)-15790321)]
    [TestCase("y:int = ~60", (int)-61)]
    [TestCase("y:int16 = ~1", (Int16)(-2))]
    [TestCase("y:int16 = ~-1", (Int16)0)]
    [TestCase("y:int16 = ~0xF0F", (Int16)(-3856))]
    [TestCase("y:uint64 = ~1", (ulong)0xFFFF_FFFF_FFFF_FFFE)]
    [TestCase("y:uint64 = ~0xF0F0F0F0", (ulong)0xFFFF_FFFF_0F0F_0F0F)]
    [TestCase("y:uint32 = ~1", (uint)0xFFFFFFFE)]
    [TestCase("y:uint = ~60", (uint)4294967235)]
    [TestCase("y:uint32 = ~0xF0F0F0F0", (uint)0xF0F0F0F)]
    [TestCase("y:uint16 = ~1", (ushort)0xFFFE)]
    [TestCase("y:uint16 = ~0xF0F0", (ushort)0xF0F)]
    [TestCase("y:uint8 = ~1", (byte)0xFE)]
    [TestCase("y:uint8 = ~0xF0", (byte)0xF)]
    [TestCase("y:int8 = ~1",  (sbyte)(-2))]
    [TestCase("y:int8 = ~-1", (sbyte)0)]
    [TestCase("y:int8 = ~0x0F", (sbyte)(-16))]
    [TestCase("y = 1 == ~~1", true)]
    [TestCase("y = 0 == ~~0", true)]
    [TestCase("y = -1 == ~~-1", true)]
    [TestCase("y = 0xA == ~~0xA", true)]
    [TestCase("y = -0xA == ~~-0xA", true)]
    [TestCase("y = 0xABCD_EF01 == ~~0xABCD_EF01", true)]
    [TestCase("y = -0xABCD_EF01 == ~~-0xABCD_EF01", true)]
    public void ConstantBitInvert(string expression, object expected)
        => expression.AssertReturns("y", expected);


    // Bits shift OUT of the operand's range when the count is below `bits`:
    // genuine value-overflow → 0. Counts ≥ bits are covered by the wrapping
    // test below (mask to operand bit width per Specs/Operators.md L185).
    [TestCase("y:uint64 = 0xF000_0000_0000_0000<<8", (ulong)0)]
    [TestCase("y:uint64 = 0xFEC0_0000_0000_0000<<16", (ulong)0)]
    [TestCase("y:uint32 = 0xF000_0000<<8", (uint)0)]
    [TestCase("y:uint32 = 0xFEC0_0000<<16", (uint)0)]
    [TestCase("y:uint16 = 0xF000<<8", (UInt16)0)]
    public void BitShiftOverflow(string expression, object expected)
        => expression.AssertReturns("y", expected);


    // Bitshift wrapping: standard behavior (matches C#/Java/Rust).
    // Shift count is masked to bit width: x << n = x << (n % bits).
    [TestCase("y:uint32 = 0xFFFF_ffff<<1", (uint)0xFFFFFFFE)]
    [TestCase("y:uint32 = 0x1<<33", (uint)2)]                  // 33 % 32 = 1
    [TestCase("y:int32 = 0x1<<33", 2)]                          // 33 % 32 = 1
    [TestCase("y:uint16 = 0xFEC0<<16", (UInt16)0xFEC0)]         // 16 % 16 = 0
    [TestCase("y:uint16 = 0x1<<17", (UInt16)2)]                 // 17 % 16 = 1
    [TestCase("y:int16  = 0x1<<16", (Int16)1)]                  // 16 % 16 = 0
    [TestCase("y:uint8  = 0xF0<<8",  (byte)0xF0)]               // 8 % 8 = 0
    [TestCase("y:uint8  = 0xFE<<16", (byte)0xFE)]               // 16 % 8 = 0
    [TestCase("y:uint8  = 0x1<<9",   (byte)2)]                  // 9 % 8 = 1
    [TestCase("y:int8   = 0x1<<8",   (sbyte)1)]                 // 8 % 8 = 0
    [TestCase("y:int8   = 0x1<<9",   (sbyte)2)]                 // 9 % 8 = 1
    public void BitshiftWraps_AllWidths(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y:uint64 = 0xFFFF_ffff_FFFF_ffff<<1", (ulong)0xFFFFFFFFFFFFFFFE)]
    [TestCase("y:uint64 = 0x1<<65", (ulong)2)]        // 65 % 64 = 1
    [TestCase("y:int64 = 0x1<<65", (long)2)]           // 65 % 64 = 1
    public void BitshiftWraps_64bit(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y = ^2")]
    [TestCase("y = 2^^")]
    [TestCase("y = ^^2")]
    [TestCase("y = -")]
    [TestCase("y = ~")]
    [TestCase("~y=3")]
    [TestCase("y = ~")]
    [TestCase("y = ~-")]
    [TestCase("y = ~1.5")]
    public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();

    // ═══ Incompatible types: uint64 & int64 → abstract I96 → error ═══

    [Test]
    public void BitAnd_UInt64_Int64_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "x:uint64 = 5; y:int64 = 3; z = x & y".Build());

    [Test]
    public void BitOr_UInt64_Int64_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "x:uint64 = 5; y:int64 = 3; z = x | y".Build());

    [Test]
    public void BitXor_UInt64_Int64_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "x:uint64 = 5; y:int64 = 3; z = x ^ y".Build());

    // ═══ Same-type bitwise — works ═══

    [Test]
    public void BitAnd_Int32_Int32() =>
        Assert.AreEqual(1, "x:int = 5; y:int = 3; z = x & y".Calc().Get("z"));

    [Test]
    public void BitOr_UInt64_UInt64() =>
        Assert.AreEqual((ulong)7, "x:uint64 = 5; y:uint64 = 3; z = x | y".Calc().Get("z"));

    [Test]
    public void BitXor_Int64_Int64() =>
        Assert.AreEqual((long)6, "x:int64 = 5; y:int64 = 3; z = x ^ y".Calc().Get("z"));

    [Test]
    public void BitAnd_UInt32_UInt32() =>
        Assert.AreEqual((uint)1, "x:uint32 = 5; y:uint32 = 3; z = x & y".Calc().Get("z"));

    // ═══ Arithmetic uint64 + int64 — works (→ Real) ═══

    [Test]
    public void Add_UInt64_Int64_Works() =>
        Assert.AreEqual(8.0, "x:uint64 = 5; y:int64 = 3; z = x + y".Calc().Get("z"));

    [Test]
    public void Max_UInt64_Int64_Works() =>
        Assert.AreEqual(5.0, "x:uint64 = 5; y:int64 = 3; z = max(x,y)".Calc().Get("z"));

    // ═══ Bitwise on bool — type error ═══

    [TestCase("y = ~true")]
    [TestCase("y = ~false")]
    [TestCase("y = true & 42")]
    [TestCase("y = false | 7")]
    public void BitwiseOnBool_GivesTypeError(string expr) =>
        expr.AssertObviousFailsOnParse();

    // `~` preserves operand bit-width even when a surrounding expression widens
    // the result (e.g. `~a + 0` with a:byte). Without this, TIC unifies operand
    // and result via one generic and the operand gets widened BEFORE complement,
    // turning `~byte 5` into int32 -6 instead of byte 250 widened to 250.
    // Spec L176 + L193 — `~A = 0b1100_0011` for byte A is defined at byte width.
    [TestCase("a:byte=1\r y = ~a + 0", 254)]
    [TestCase("a:byte=5\r y = ~a + 0", 250)]
    [TestCase("a:byte=255\r y = ~a + 0", 0)]
    [TestCase("a:uint16=60\r y = ~a + 0", 65475)]
    [TestCase("a:uint16=0\r y = ~a + 0", 65535)]
    [TestCase("a:byte=5\r y:int32 = ~a", 250)]
    public void BitwiseNot_PreservesUnsignedOperandWidth(string expr, int expected) =>
        expr.AssertResultHas("y", expected);

    // Mixed signed/unsigned: TIC widens to the smallest int type containing both intervals.
    // U16+I16 → I32, U32+I32 → I64. U64+I64 has no common int type → parse error.
    [TestCase("a:uint32 = 5\r b:int32 = 3\r c = a & b", 1L)]
    [TestCase("a:uint16 = 5\r b:int16 = 3\r c = a & b", 1)]
    public void MixedSignedUnsigned_Bitwise_WidensToLargerSigned(string expr, object expected) =>
        expr.AssertResultHas("c", expected);

    [Test]
    public void MixedSignedUnsigned_Bitwise_U64_I64_HasNoCommonType_Rejected() =>
        Assert.Throws<Exceptions.FunnyParseException>(() =>
            "a:uint64 = 5\r b:int64 = 3\r c = a & b".Build());
}
