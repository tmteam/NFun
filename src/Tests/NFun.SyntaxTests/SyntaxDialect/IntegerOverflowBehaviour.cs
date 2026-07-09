using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect;

public class IntegerOverflowBehaviour {
    [TestCase("y:uint32 = 0xFFFF_FFFF + 1", (uint)0)]
    [TestCase("y:int32 = 2_147_483_647 + 1", -2_147_483_648)]
    [TestCase("y:uint64 = 0xFFFF_FFFF_FFFF_FFFF + 1", (ulong)0)]
    [TestCase("y:int64 = 9223372036854775807 + 1", long.MinValue)]
    [TestCase("y:uint32 = 0 - 1", uint.MaxValue)]
    [TestCase("y:int32 = -2_147_483_648 - 1", int.MaxValue)]
    [TestCase("y:uint64 = 0 - 1", ulong.MaxValue)]
    [TestCase("y:int64 = -9223372036854775808 - 1", long.MaxValue)]
    [TestCase("y:uint32 = [0xFFFF_FFFF,1].sum()", (uint)0)]
    [TestCase("y:int32 = [2_147_483_647,1].sum()", -2_147_483_648)]
    [TestCase("y:uint64 = [0xFFFF_FFFF_FFFF_FFFF, 1].sum()", (ulong)0)]
    [TestCase("y:int64 = [9223372036854775807, 1].sum()", long.MinValue)]
    [TestCase("y:int32 = [-2_147_483_648, - 1].sum()", int.MaxValue)]
    [TestCase("y:int64 = [-9223372036854775808, - 1].sum()", long.MaxValue)]
    [TestCase("y:uint32 = 0xFFFF_FFFF * 2", uint.MaxValue - 1)]
    [TestCase("y:int32 = 2_147_483_647 * 2", -2)]
    [TestCase("y:uint64 = 0xFFFF_FFFF_FFFF_FFFF * 2", ulong.MaxValue - 1)]
    [TestCase("y:int64 = 9223372036854775807 * 2", (long)-2)]
    [TestCase("y:int32 = 2_147_483_647 * -2", 2)]
    [TestCase("y:int64 = 9223372036854775807 * -2", (long)2)]
    public void OperationsWithOverflow_returnsOverflowValue(string expr, object expected) {
        var runtime = Funny.Hardcore.WithDialect(integerOverflow: IntegerOverflow.Unchecked).Build(expr);
        runtime.Calc().AssertReturns(expected);
    }

    [TestCase("y:uint32 = 0xFFFF_FFFF + 1")]
    [TestCase("y:int32 = 2_147_483_647 + 1")]
    [TestCase("y:uint64 = 0xFFFF_FFFF_FFFF_FFFF + 1")]
    [TestCase("y:int64 = 9223372036854775807 + 1")]
    [TestCase("y:uint32 = 0 - 1")]
    [TestCase("y:int32 = -2_147_483_648 - 1")]
    [TestCase("y:uint64 = 0 - 1")]
    [TestCase("y:int64 = -9223372036854775808 - 1")]
    [TestCase("y:uint32 = [0xFFFF_FFFF,1].sum()")]
    [TestCase("y:int32 = [2_147_483_647,1].sum()")]
    [TestCase("y:int32 = [-2_147_483_648, - 1].sum()")]
    [TestCase("y:uint64 = [0xFFFF_FFFF_FFFF_FFFF, 1].sum()")]
    [TestCase("y:int64 = [9223372036854775807, 1].sum()")]
    [TestCase("y:int64 = [-9223372036854775808, - 1].sum()")]
    [TestCase("y:uint32 = 0xFFFF_FFFF * 2")]
    [TestCase("y:int32 = 2_147_483_647 * 2")]
    [TestCase("y:uint64 = 0xFFFF_FFFF_FFFF_FFFF * 2")]
    [TestCase("y:int64 = 9223372036854775807 * 2")]
    [TestCase("y:int32 = 2_147_483_647 * -2")]
    [TestCase("y:int64 = 9223372036854775807 * -2")]
    public void OperationsWithOverflow_Failes(string expr) {
        var runtime = Funny.Hardcore.WithDialect(integerOverflow: IntegerOverflow.Checked).Build(expr);

        try
        {
            var result = runtime.Calc();
            Assert.Fail($"No exception thrown: {result}");
        }
        catch (FunnyRuntimeException e) when (e.InnerException is OverflowException)
        {
            Assert.Pass();
        }
    }

    // Negate(MinValue) edge — symmetric across signed widths under both modes.
    // Int8 added alongside Int16: SignedNumber constraint now admits Int8 as
    // concrete descendant, so the checked/unchecked dispatch is reachable.
    [TestCase("x:int8=-128\r y=-x")]
    [TestCase("x:int16=-32768\r y=-x")]
    public void Negate_MinValue_Checked_Throws(string expr) {
        var runtime = Funny.Hardcore.WithDialect(integerOverflow: IntegerOverflow.Checked).Build(expr);
        try {
            var result = runtime.Calc();
            Assert.Fail($"No exception thrown: {result}");
        } catch (FunnyRuntimeException e) when (e.InnerException is OverflowException) {
            Assert.Pass();
        }
    }

    [TestCase("x:int8=-128\r y=-x",   (sbyte)-128)]
    [TestCase("x:int16=-32768\r y=-x", (short)-32768)]
    public void Negate_MinValue_Unchecked_Wraps(string expr, object expected) {
        var rt = Funny.Hardcore.WithDialect(integerOverflow: IntegerOverflow.Unchecked).Build(expr);
        Assert.AreEqual(expected, rt.Calc().Get("y"));
    }

    // MIN // -1 is the single overflowing case of signed integer division: the true
    // quotient |MIN| is not representable, so Checked throws and Unchecked wraps to MIN
    // (Specs/Operators.md §Integer overflow) — same rule as negate(MIN).
    [TestCase("x:int8=-128\r neg:int8=-1\r y=x//neg")]
    [TestCase("x:int16=-32768\r neg:int16=-1\r y=x//neg")]
    [TestCase("x:int32=-2147483648\r neg:int32=-1\r y=x//neg")]
    [TestCase("x:int64=-9223372036854775808\r neg:int64=-1\r y=x//neg")]
    public void DivideInt_MinByMinusOne_Checked_Throws(string expr) {
        var runtime = Funny.Hardcore.WithDialect(integerOverflow: IntegerOverflow.Checked).Build(expr);
        try {
            var result = runtime.Calc();
            Assert.Fail($"No exception thrown: {result}");
        } catch (FunnyRuntimeException e) when (e.InnerException is OverflowException) {
            Assert.Pass();
        }
    }

    // Checked is the DEFAULT dialect — the matrix above must throw with no dialect setup at all.
    [TestCase("x:int8=-128\r neg:int8=-1\r y=x//neg")]
    [TestCase("x:int16=-32768\r neg:int16=-1\r y=x//neg")]
    [TestCase("x:int32=-2147483648\r neg:int32=-1\r y=x//neg")]
    [TestCase("x:int64=-9223372036854775808\r neg:int64=-1\r y=x//neg")]
    public void DivideInt_MinByMinusOne_DefaultDialect_Throws(string expr) {
        var runtime = Funny.Hardcore.Build(expr);
        try {
            var result = runtime.Calc();
            Assert.Fail($"No exception thrown: {result}");
        } catch (FunnyRuntimeException e) when (e.InnerException is OverflowException) {
            Assert.Pass();
        }
    }

    [TestCase("x:int8=-128\r neg:int8=-1\r y=x//neg", (sbyte)-128)]
    [TestCase("x:int16=-32768\r neg:int16=-1\r y=x//neg", (short)-32768)]
    [TestCase("x:int32=-2147483648\r neg:int32=-1\r y=x//neg", int.MinValue)]
    [TestCase("x:int64=-9223372036854775808\r neg:int64=-1\r y=x//neg", long.MinValue)]
    public void DivideInt_MinByMinusOne_Unchecked_WrapsToMin(string expr, object expected) {
        var rt = Funny.Hardcore.WithDialect(integerOverflow: IntegerOverflow.Unchecked).Build(expr);
        Assert.AreEqual(expected, rt.Calc().Get("y"));
    }

    // In-range division is dialect-independent: truncation toward zero preserved,
    // unsigned division can never overflow at all.
    [TestCase("x:int8=7\r d:int8=2\r y=x//d", (sbyte)3)]
    [TestCase("x:int8=-7\r d:int8=2\r y=x//d", (sbyte)-3)]
    [TestCase("x:int16=7\r d:int16=2\r y=x//d", (short)3)]
    [TestCase("x:int16=-7\r d:int16=2\r y=x//d", (short)-3)]
    [TestCase("x:int32=7\r d:int32=2\r y=x//d", 3)]
    [TestCase("x:int32=-7\r d:int32=2\r y=x//d", -3)]
    [TestCase("x:int64=7\r d:int64=2\r y=x//d", (long)3)]
    [TestCase("x:int64=-7\r d:int64=2\r y=x//d", (long)-3)]
    [TestCase("x:uint8=255\r d:uint8=3\r y=x//d", (byte)85)]
    [TestCase("x:uint16=65535\r d:uint16=3\r y=x//d", (ushort)21845)]
    [TestCase("x:uint32=0xFFFF_FFFF\r d:uint32=3\r y=x//d", (uint)1431655765)]
    [TestCase("x:uint64=0xFFFF_FFFF_FFFF_FFFF\r d:uint64=3\r y=x//d", (ulong)6148914691236517205)]
    public void DivideInt_InRange_SameUnderBothDialects(string expr, object expected) {
        foreach (var mode in new[] { IntegerOverflow.Checked, IntegerOverflow.Unchecked }) {
            var rt = Funny.Hardcore.WithDialect(integerOverflow: mode).Build(expr);
            Assert.AreEqual(expected, rt.Calc().Get("y"), $"mode: {mode}");
        }
    }

    // % never overflows: MIN % -1 = 0 is representable at every width, so the result
    // is 0 under BOTH dialects. (The CLR idiv trap on 32/64-bit MIN%-1 is an
    // implementation detail of x86/ECMA rem, not algebra.)
    [TestCase("x:int8=-128\r neg:int8=-1\r y=x%neg", (sbyte)0)]
    [TestCase("x:int16=-32768\r neg:int16=-1\r y=x%neg", (short)0)]
    [TestCase("x:int32=-2147483648\r neg:int32=-1\r y=x%neg", 0)]
    [TestCase("x:int64=-9223372036854775808\r neg:int64=-1\r y=x%neg", (long)0)]
    public void Remainder_MinByMinusOne_IsZero_UnderBothDialects(string expr, object expected) {
        foreach (var mode in new[] { IntegerOverflow.Checked, IntegerOverflow.Unchecked }) {
            var rt = Funny.Hardcore.WithDialect(integerOverflow: mode).Build(expr);
            Assert.AreEqual(expected, rt.Calc().Get("y"), $"mode: {mode}");
        }
    }

    // Sign-of-dividend semantics of % preserved, dialect-independent.
    [TestCase("x:int32=7\r d:int32=2\r y=x%d", 1)]
    [TestCase("x:int32=-7\r d:int32=2\r y=x%d", -1)]
    [TestCase("x:int64=7\r d:int64=2\r y=x%d", (long)1)]
    [TestCase("x:int64=-7\r d:int64=2\r y=x%d", (long)-1)]
    public void Remainder_InRange_SameUnderBothDialects(string expr, object expected) {
        foreach (var mode in new[] { IntegerOverflow.Checked, IntegerOverflow.Unchecked }) {
            var rt = Funny.Hardcore.WithDialect(integerOverflow: mode).Build(expr);
            Assert.AreEqual(expected, rt.Calc().Get("y"), $"mode: {mode}");
        }
    }
}
