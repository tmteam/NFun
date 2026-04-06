using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bitwise operators require a concrete integer type. When operands are
/// uint64 and int64, the common type I96 is abstract — no concrete .NET
/// integer type covers the full range. This must be a compile-time error.
/// Arithmetic (+, -, *, max, min) is fine — they widen to Real.
/// </summary>
public class BitwiseIncompatibleTypesTest {

    #region Bitwise uint64 & int64 — must error

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

    #endregion

    #region Arithmetic uint64 + int64 — should work (→ Real)

    [Test]
    public void Add_UInt64_Int64_Works() {
        var r = "x:uint64 = 5; y:int64 = 3; z = x + y".Calc();
        Assert.AreEqual(8.0, r.Get("z"));
    }

    [Test]
    public void Max_UInt64_Int64_Works() {
        var r = "x:uint64 = 5; y:int64 = 3; z = max(x,y)".Calc();
        Assert.AreEqual(5.0, r.Get("z"));
    }

    #endregion

    #region Same-type bitwise — still works

    [Test]
    public void BitAnd_Int32_Int32() {
        var r = "x:int = 5; y:int = 3; z = x & y".Calc();
        Assert.AreEqual(1, r.Get("z"));
    }

    [Test]
    public void BitOr_UInt64_UInt64() {
        var r = "x:uint64 = 5; y:uint64 = 3; z = x | y".Calc();
        Assert.AreEqual((ulong)7, r.Get("z"));
    }

    [Test]
    public void BitXor_Int64_Int64() {
        var r = "x:int64 = 5; y:int64 = 3; z = x ^ y".Calc();
        Assert.AreEqual((long)6, r.Get("z"));
    }

    [Test]
    public void BitAnd_UInt32_UInt32() {
        var r = "x:uint32 = 5; y:uint32 = 3; z = x & y".Calc();
        Assert.AreEqual((uint)1, r.Get("z"));
    }

    #endregion
}
