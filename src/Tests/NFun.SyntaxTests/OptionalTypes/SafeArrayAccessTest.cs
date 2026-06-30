namespace NFun.SyntaxTests.OptionalTypes;

using Exceptions;
using TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for the ?[i] safe array access operator.
/// arr?[i] — safe indexing on an optional array.
/// If arr is none, returns none. If arr has a value, returns the element (as optional).
/// </summary>
[TestFixture]
public class SafeArrayAccessTest {

    [Test]
    public void IntArray_HasValue_FirstElement() =>
        "arr:int[]? = [10,20,30]\r y = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 10);

    [Test]
    public void IntArray_None_CoalescesToDefault() =>
        "arr:int[]? = none\r y = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);

    [Test]
    public void IntArray_HasValue_MiddleIndex() =>
        "arr:int[]? = [10,20,30]\r y = arr?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 20);

    [Test]
    public void IntArray_HasValue_LastIndex() =>
        "arr:int[]? = [10,20,30]\r y = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 30);

    [Test]
    public void RealArray_HasValue() =>
        "arr:real[]? = [1.5, 2.5]\r y = arr?[0] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 1.5);

    [Test]
    public void TextArray_HasValue() =>
        "arr:text[]? = ['hello', 'world']\r y = arr?[1] ?? ''"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "world");

    [Test]
    public void BoolArray_HasValue() =>
        "arr:bool[]? = [true, false]\r y = arr?[0] ?? false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", true);

    [Test]
    public void IntArray_HasValue_ForceUnwrap() =>
        "arr:int[]? = [42]\r y = arr?[0]!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);

    [Test]
    public void ChainedSafeFieldAccess_ThenSafeArrayAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r y = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 1);

    [Test]
    public void IntArray_HasValue_ResultIsOptional() {
        var result = "arr:int[]? = [10]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(10, result.Get("y"));
    }

    [Test]
    public void IntArray_None_ResultIsNone() {
        var result = "arr:int[]? = none\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void ArrayOfOptionals_NoneElement_Coalesces() =>
        "arr:int?[]? = [1, none, 3]\r y = arr?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);

    [Test]
    public void SafeAccess_InArithmeticWithCoalesce() =>
        "arr:int[]? = [10]\r y = (arr?[0] ?? 0) + 5"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 15);

    [Test]
    public void SafeAccess_InIfCondition_HasValue() =>
        "arr:int[]? = [42]\r y = if(arr?[0] != none) arr?[0]! else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);

    [Test]
    public void MultipleSafeAccesses_BothHaveValue() =>
        "a:int[]? = [1]\r b:int[]? = [2]\r y = (a?[0] ?? 0) + (b?[0] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 3);

    [Test]
    public void SafeAccess_VariableIndex() =>
        "arr:int[]? = [10,20,30]\r i:int = 1\r y = arr?[i] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 20);

    [Test]
    public void NonOptionalArray_SafeAccess_ReturnsOptional() {
        var result = "arr:int[] = [1,2,3]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1, result.Get("y"));
    }

    [Test]
    public void ChainedSafeFieldAccess_StructIsNone_CoalescesToDefault() =>
        "s = if(false) {items = [1,2,3]} else none\r y = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);

    [Test]
    public void SafeAccess_TextArray_CoalesceThenCount_HasValue() =>
        "arr:text[]? = ['hello']\r y = (arr?[0] ?? '').count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 5);

    [Test]
    public void NestedArray_SafeAccess_ReturnsOptionalInnerArray() {
        var result = "arr:int[][]? = [[1,2],[3,4]]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        var inner = (int[])result.Get("y");
        Assert.AreEqual(new[] { 1, 2 }, inner);
    }

    [Test]
    public void SafeArrayAccess_ValidIndex_ReturnsValue() =>
        "arr = [10,20,30]\r out:int = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 10);

    [Test]
    public void SafeArrayAccess_OutOfBounds_ReturnsNone_CoalescesToZero() =>
        "arr = [10,20,30]\r out:int = arr?[99] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 0);

    [Test]
    public void SafeArrayAccess_NegativeIndex_ReturnsNone() =>
        "arr = [10,20,30]\r out:int = arr?[-1] ?? -99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -99);

    [Test]
    public void SafeArrayAccess_NoneArray_ReturnsNone() {
        var result = "arr:int[]? = none\r out = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("out"));
    }

    [Test]
    public void SafeArrayAccess_LastElement_CoalescesToDefault() =>
        "arr = [10,20,30]\r out:int = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccess_RealArray_ValidIndex() =>
        "arr:real[]? = [1.5, 2.5, 3.5]\r out = arr?[1] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 2.5);

    [Test]
    public void SafeArrayAccess_TextArray_OutOfBounds() =>
        "arr:text[]? = ['hello', 'world']\r out = arr?[5] ?? 'empty'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", "empty");

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r out = s?.items?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 2);

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_NoneStruct() =>
        "s = if(false) {items = [1,2,3]} else none\r out = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -1);

    [Test]
    public void SafeArrayAccess_InArithmeticExpression() =>
        "arr = [10,20,30]\r out:int = (arr?[0] ?? 0) + (arr?[1] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccessOnNonOptVar_CoalesceTypeCorrect() {
        "a = [1,2,3]; z = a?[0] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 1);
    }

    [Test]
    public void ChainedSafeArrayAccess_NoError() {
        Assert.DoesNotThrow(() =>
            "a = [1]; b = [2]; y = a?[0] ?? b?[0] ?? 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_SafeArrayAccessLosesOptThroughComposite_Crash() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_PrimitiveElem_Works() {
        var rt = "arr:int[]? = none\rout = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void MR6Bug2_Boundary_PrimitiveElem_ArithmeticRejected_FU767() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[]? = none\rout = arr?[0] + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_ArrayElem_Sum_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].sum()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_ArrayElem_First_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].first()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_ArrayElem_ChainedIndex_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0][0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_ArrayElem_Slice_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0][:2]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_3DeepArray_BugCompounds_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][][]? = none\rout = arr?[0].first().count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_Workaround_SafeMethodChain_Works() {
        var rt = "arr:int[][]? = none\rout = arr?[0]?.count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void MR6Bug2_Boundary_Workaround_SafeMethodChainWithDefault_Works() {
        "arr:int[][]? = none\rout = arr?[0]?.count() ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -1);
    }

    [Test]
    public void MR6Bug2_Boundary_Workaround_ExplicitDefaultArray_Works() {
        "arr:int[][]? = none\rout = (arr?[0] ?? []).count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 0);
    }

    [Test]
    public void MR6Bug2_Boundary_StructElem_AlreadyRejected() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:{v:int}[]? = none\rout = arr?[0].v"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR6Bug2_Boundary_DirectOptArray_FU783_Control() {
        Assert.Throws<FunnyParseException>(() =>
            "b:int[]? = none\rout = b.count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }
}
