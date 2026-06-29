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
}
