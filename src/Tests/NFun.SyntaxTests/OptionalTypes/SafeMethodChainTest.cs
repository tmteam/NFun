namespace NFun.SyntaxTests.OptionalTypes;

using Exceptions;
using TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for TypeScript-style safe method chain propagation.
/// Once ?. is used, none propagates through the entire chain of .method() calls.
/// </summary>
[TestFixture]
public class SafeMethodChainTest {

    private static CalculationResult Calc(string expr) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);

    [Test]
    public void SingleMethod_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.count() ?? 0").AssertResultHas("out", 3);

    [Test]
    public void SingleMethod_None() =>
        Calc("arr:int[]? = none; out = arr?.count() ?? 0").AssertResultHas("out", 0);

    [Test]
    public void SingleMethod_Sort_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort() ?? []").AssertResultHas("out", new[] { 1, 2, 3 });

    [Test]
    public void SingleMethod_Sort_None() =>
        Calc("arr:int[]? = none; out = arr?.sort() ?? []").AssertResultHas("out", new int[0]);

    [Test]
    public void Chain_SortReverse_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort().reverse() ?? []")
            .AssertResultHas("out", new[] { 3, 2, 1 });

    [Test]
    public void Chain_SortReverse_None() =>
        Calc("arr:int[]? = none; out = arr?.sort().reverse() ?? []")
            .AssertResultHas("out", new int[0]);

    [Test]
    public void Chain_SortReverseCount_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort().reverse().count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void Chain_SortReverseCount_None() =>
        Calc("arr:int[]? = none; out = arr?.sort().reverse().count() ?? 0")
            .AssertResultHas("out", 0);

    [Test]
    public void TextChain_HasValue() =>
        Calc("s:text? = 'hello'; out = s?.reverse().count() ?? 0")
            .AssertResultHas("out", 5);

    [Test]
    public void TextChain_None() =>
        Calc("s:text? = none; out = s?.reverse().count() ?? 0")
            .AssertResultHas("out", 0);

    [Test]
    public void StructField_SafeMethodChain() =>
        Calc("y = {items = if(true) [3,1,2] else none}; out = y.items?.sort().count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void StructField_SafeMethodChain_None() =>
        Calc("y = {items = if(false) [3,1,2] else none}; out = y.items?.sort().count() ?? 0")
            .AssertResultHas("out", 0);

    [Test]
    public void FieldThenMethod_HasValue() =>
        Calc("x = if(true) {items = [1,2,3]} else none; out = x?.items.count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void FieldThenMethod_None() =>
        Calc("x = if(false) {items = [1,2,3]} else none; out = x?.items.count() ?? 0")
            .AssertResultHas("out", 0);

    [Test]
    public void MR5Bug7_SafeMethodCallOnOptNamed_PreciseReturnType() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={f:rule(int)->int}\ra:p? = {f = rule it*2}\ry = a?.f(7)");
        var yVar = rt["y"];
        StringAssert.Contains("Int32", yVar.Type.ToString());
    }

    [Test]
    public void MR5Bug7b_CascadeSafeMethodCalls_BothPreservePreciseType() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={f:rule(int)->int, g:rule(int)->int}\ra:p? = {f=rule it*2, g=rule it*3}\ry = a?.f(7)\rz = a?.g(8)");
        var yType = rt["y"].Type.ToString();
        var zType = rt["z"].Type.ToString();
        StringAssert.Contains("Int32", yType);
        StringAssert.Contains("Int32", zType);
    }

    [Test]
    public void MR5Bug7b_CoalescePinsType_Workaround() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry = a?.f(7) ?? 99"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 14);
    }

    [Test]
    public void MR5Bug7b_MultiArgSafeMethodCall_PreservesPreciseReturnType() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={f:rule(int,int)->int}\ra:p? = {f=rule it1+it2}\ry = a?.f(3, 5)");
        StringAssert.Contains("Int32", rt["y"].Type.ToString());
    }

    [Test]
    public void MR5Bug7b_ExplicitAnnotationPinsType_Workaround() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry:int? = a?.f(7)"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", (int?)14);
    }

    [Test]
    public void MR5Bug7b_ArithmeticContextPinsType_Workaround() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry = (a?.f(7) ?? 0) + 1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 15);
    }

    [Test]
    public void MR5Bug7b_AnonStructReceiver_PreservesPreciseReturnType() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("a:{f:rule(int)->int}? = {f=rule it*2}\ry = a?.f(7)");
        StringAssert.Contains("Int32", rt["y"].Type.ToString());
    }

    [Test]
    public void MR5Bug7b_ValueIsCorrect_OnlyTypeIsWrong() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry = a?.f(7) ?? 0"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 14);
    }

    [Test]
    public void MR7Bug3_SafeMethodChainThenIndex_NoneCrashes() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[]? = none\ry = arr?.sort()[0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR7Bug3_SafeMethodChainReverse_NoneCrashes() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[]? = none\ry = arr?.reverse()[0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR7Bug3_Control_DirectOptArrayIndexRejected() {
        Assert.Throws<FunnyParseException>(() =>
            "z:int[]? = none\ry = z[0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR7Bug3_Workaround_UseSafeIndex() {
        var rt = "arr:int[]? = none\ry = arr?.sort()?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }
}
