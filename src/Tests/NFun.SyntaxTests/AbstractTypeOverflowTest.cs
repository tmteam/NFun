using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests that abstract TIC types (U12, U24, U48, I48, I96) are mapped
/// to concrete .NET types large enough to hold all possible values.
/// Bug: U12→UInt8, U24→UInt16, U48→UInt32 caused overflow at runtime
/// when values exceeded the narrow type's range.
/// </summary>
public class AbstractTypeOverflowTest {

    #region U12 (0..4095) → UInt16

    [Test]
    public void U12_SmallAndLarge_NoOverflow() =>
        "out = if(false) [0] else [300]".Calc();

    [Test]
    public void U12_BoundaryValue_255_And_300() =>
        "out = if(false) [255] else [300]".Calc();

    [Test]
    public void U12_MaxU12Value_4095() =>
        "out = if(false) [0] else [4095]".Calc();

    [Test]
    public void U12_BothSmall_StillWorks() {
        var result = "out = if(false) [0] else [100]".Calc();
        // Should work regardless of branch order
    }

    [Test]
    public void U12_ReversedBranches_StillWorks() {
        var result = "out = if(false) [300] else [0]".Calc();
        // Was already working before fix
    }

    #endregion

    #region U24 (0..16M) → UInt32

    [Test]
    public void U24_ValueExceedsUInt16() =>
        "out = if(false) [0] else [70000]".Calc();

    [Test]
    public void U24_LargeValue() =>
        "out = if(false) [255] else [100000]".Calc();

    #endregion

    #region U48 (0..2^48) → UInt64

    [Test]
    public void U48_ValueExceedsUInt32() =>
        "out = if(false) [0] else [5000000000]".Calc();

    #endregion

    #region I48 → Int64

    [Test]
    public void I48_LargeUint32_NoOverflow() {
        // max(uint32, int) resolves through abstract I48 type
        // I48 must map to Int64 (not Int32) to hold all values
        var result = "x:uint32 = 3000000000; y:int = 1; out = max(x, y)".Calc();
        Assert.IsNotNull(result.Get("out"));
    }

    #endregion

    #region Sanity: normal cases still work

    [Test]
    public void NormalArray_Int32() =>
        "out = [1, 2, 300]".AssertReturns("out", new[] { 1, 2, 300 });

    [Test]
    public void NormalIfElse_SameType() =>
        "out = if(true) [1,2,3] else [4,5,6]".AssertReturns("out", new[] { 1, 2, 3 });

    #endregion
}
