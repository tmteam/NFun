namespace NFun.SyntaxTests;

using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

/// <summary>
/// Tests for bug hunt session 8 fixes:
/// - Bug 8#7: ?[ safe array access returns none on out-of-bounds (already fixed)
/// - Bug 8#9: none > none should be compile error, not runtime crash
/// - Bug 8#10: struct equality broken after type coercion
/// </summary>
[TestFixture]
public class BugHunt8FixesTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    #region Bug 8#7: ?[ safe array access bounds (already fixed)

    [Test]
    public void SafeArrayAccess_ValidIndex_ReturnsValue() =>
        "arr = [10,20,30]\r out:int = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 10);

    [Test]
    public void SafeArrayAccess_OutOfBounds_ReturnsNone_CoalescesToZero() =>
        "arr = [10,20,30]\r out:int = arr?[99] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 0);

    [Test]
    public void SafeArrayAccess_NegativeIndex_ReturnsNone() =>
        "arr = [10,20,30]\r out:int = arr?[-1] ?? -99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -99);

    [Test]
    public void SafeArrayAccess_NoneArray_ReturnsNone() {
        var result = "arr:int[]? = none\r out = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("out"));
    }

    [Test]
    public void SafeArrayAccess_LastElement_CoalescesToDefault() =>
        "arr = [10,20,30]\r out:int = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccess_RealArray_ValidIndex() =>
        "arr:real[]? = [1.5, 2.5, 3.5]\r out = arr?[1] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 2.5);

    [Test]
    public void SafeArrayAccess_TextArray_OutOfBounds() =>
        "arr:text[]? = ['hello', 'world']\r out = arr?[5] ?? 'empty'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", "empty");

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r out = s?.items?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 2);

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_NoneStruct() =>
        "s = if(false) {items = [1,2,3]} else none\r out = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void SafeArrayAccess_InArithmeticExpression() =>
        "arr = [10,20,30]\r out:int = (arr?[0] ?? 0) + (arr?[1] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 30);

    #endregion

    #region Bug 8#9: none comparison — compile error

    [Test]
    public void NoneGreaterThanNone_CompileError() {
        Assert.Throws<FunnyParseException>(
            () => "out = none > none"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void NoneLessThanNone_CompileError() {
        Assert.Throws<FunnyParseException>(
            () => "out = none < none"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void NoneGreaterOrEqualNone_CompileError() {
        Assert.Throws<FunnyParseException>(
            () => "out = none >= none"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void NoneEqualNone_ReturnsTrue() =>
        "out = none == none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", true);

    [Test]
    public void NoneNotEqualNone_ReturnsFalse() =>
        "out = none != none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", false);

    #endregion

    #region Bug 8#10: struct equality with structural subtyping

    [Test]
    public void StructEquality_SameFields_Equal() =>
        "{x=1} == {x=1}".AssertReturns(true);

    [Test]
    public void StructEquality_DifferentFieldCount_NotEqual() =>
        // Per spec: structs must have same field list for equality
        "{x=1, y=2} == {x=1}".AssertReturns(false);

    [Test]
    public void StructEquality_SharedFieldDiffers_NotEqual() =>
        "{x=1, y=2} == {x=1, y=3}".AssertReturns(false);

    [Test]
    public void StructNotEqual_DifferentFieldCount_True() =>
        "{x=1} != {x=1, y=2}".AssertReturns(true);

    [Test]
    public void StructInArray_DifferentFieldCount_NotFound() =>
        // {x=1} not in [{x=1,y=2}] — different field counts
        "out = {x=1} in [{x=1, y=2}]".AssertReturns("out", false);

    #endregion
}
