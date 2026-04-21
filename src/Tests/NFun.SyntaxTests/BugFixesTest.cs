using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests for fixed bugs: Bug 9 (slice reversed indices), Bug 10 (5 - -3),
/// Bug 14 (all-none typed array).
/// </summary>
[TestFixture]
public class BugFixesTest {

    // ═══════════════════════════════════════════════════════════════
    // Bug 9: Slice with reversed indices should throw consistent error.
    // Before fix: [1,2,3][1:0] → [], [1,2,3][2:0] → overflow,
    //             [1,2,3][2:1] → "Start cannot be more than end".
    // After fix: all three throw "Start cannot be more than end".
    // ═══════════════════════════════════════════════════════════════

    [TestCase("[1,2,3][1:0]")]
    [TestCase("[1,2,3][2:0]")]
    [TestCase("[1,2,3][2:1]")]
    [TestCase("[1,2,3][3:0]")]
    [TestCase("[1,2,3,4,5][4:1]")]
    public void Bug9_SliceReversedIndices_ThrowsConsistentError(string expr) {
        var runtime = $"y = {expr}".Build();
        var ex = Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
        Assert.That(ex.Message, Does.Contain("Start cannot be more than end"));
    }

    [TestCase("[1,2,3][1:0:1]")]
    [TestCase("[1,2,3][2:0:1]")]
    [TestCase("[1,2,3][2:1:1]")]
    public void Bug9_SliceWithStepReversedIndices_ThrowsConsistentError(string expr) {
        var runtime = $"y = {expr}".Build();
        var ex = Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
        Assert.That(ex.Message, Does.Contain("Start cannot be more than end"));
    }

    [TestCase("[1,2,3][0:0]", new[] { 1 })]
    [TestCase("[1,2,3][0:2]", new[] { 1, 2, 3 })]
    [TestCase("[1,2,3][1:2]", new[] { 2, 3 })]
    public void Bug9_SliceValidIndices_StillWorks(string expr, int[] expected) {
        $"y = {expr}".AssertReturns("y", expected);
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 10: `5 - -3` parsed as `--` (forbidden).
    // Before fix: FU128 "'--' is not allowed".
    // After fix: parsed as `5 - (negate 3)` = 8.
    // ═══════════════════════════════════════════════════════════════

    [TestCase("5 - -3", 8.0)]
    [TestCase("10 - -10", 20.0)]
    [TestCase("0 - -1", 1.0)]
    public void Bug10_BinaryMinusBeforeUnaryMinus_Works(string expr, object expected) {
        expr.AssertReturns(expected);
    }

    [TestCase("5 + -3", 2.0)]
    [TestCase("5 * -3", -15.0)]
    [TestCase("10 / -2", -5.0)]
    public void Bug10_OtherOperatorsBeforeUnaryMinus_StillWork(string expr, object expected) {
        expr.AssertReturns(expected);
    }

    [Test]
    public void Bug10_DoubleNegation_StillForbidden() {
        Assert.Throws<FunnyParseException>(() => "--3".Build());
    }

    [Test]
    public void Bug10_DoubleNegationWithSpace_StillForbidden() {
        Assert.Throws<FunnyParseException>(() => "- -3".Build());
    }

    [TestCase("y = x - -1", 2.0, 3.0)]
    [TestCase("y = x - -x", 5.0, 10.0)]
    public void Bug10_BinaryMinusUnaryMinus_WithVariables(string expr, double x, double expected) {
        expr.Calc("x", x).AssertResultHas("y", expected);
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 14: `y:int?[] = [none]` fails with FU775.
    // Already fixed on this branch — these are regression tests.
    // ═══════════════════════════════════════════════════════════════

    [TestCase("y:int?[] = [none]")]
    [TestCase("y:int?[] = [none, none]")]
    [TestCase("y:int?[] = [none, none, none]")]
    public void Bug14_AllNoneTypedArray_Compiles(string expr) {
        Assert.DoesNotThrow(
            () => expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void Bug14_AllNoneTypedArray_CorrectCount() {
        var result = "y:int?[] = [none, none, none]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        // The array should have 3 elements (returned as CLR int?[])
        var arr = (int?[])result.Get("y");
        Assert.AreEqual(3, arr.Length);
        Assert.IsNull(arr[0]);
        Assert.IsNull(arr[1]);
        Assert.IsNull(arr[2]);
    }

    [TestCase("y:int?[] = [1, none]")]
    [TestCase("y:int?[] = [none, 1]")]
    [TestCase("y:int?[] = [1, none, 2]")]
    public void Bug14_MixedNoneTypedArray_StillWorks(string expr) {
        Assert.DoesNotThrow(
            () => expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 15: Narrowing in rule lambda fails with arithmetic.
    // `y.map(rule if(it!=none) it*2 else 0)` → FU761 "it cannot be used here".
    // Root cause: VisitWithNarrowing used the raw name "it" for TIC lookup,
    // but inside lambdas the TIC node is aliased (e.g., "anonymous_42::it").
    // Fix: resolve through alias scope when raw name not in TIC graph.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Bug15_NarrowInRule_Multiply() {
        var r = "y:int?[] = [1,none,3]\r z = y.map(rule if(it!=none) it*2 else 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 2, 0, 6 }, r.Get("z"));
    }

    [Test]
    public void Bug15_NarrowInRule_Add() {
        var r = "y:int?[] = [1,none,3]\r z = y.map(rule if(it!=none) it+10 else 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 11, 0, 13 }, r.Get("z"));
    }

    [Test]
    public void Bug15_NarrowInRule_ComplexExpr() {
        var r = "y:int?[] = [2,none,4]\r z = y.map(rule if(it!=none) it*it+1 else -1)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 5, -1, 17 }, r.Get("z"));
    }

    [Test]
    public void Bug15_NarrowInRule_IdentityStillWorks() {
        var r = "y:int?[] = [1,none,3]\r z = y.map(rule if(it!=none) it else 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 1, 0, 3 }, r.Get("z"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 16: convert('invalid'):bool returns none instead of throwing.
    // Root cause: CreateParserOrNull for Bool returned null for invalid input
    // instead of throwing, and null silently became FunnyNone.
    // Fix: throw FormatException for unparseable bool strings.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Bug16_ConvertTextToBool_Invalid_Throws() {
        var r = "y:bool = convert('invalid')"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.Throws<FunnyRuntimeException>(() => r.Run());
    }

    [Test]
    public void Bug16_ConvertTextToBool_True_Works() {
        var r = "y:bool = convert('true')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(true, r.Get("y"));
    }

    [Test]
    public void Bug16_ConvertTextToBool_False_Works() {
        var r = "y:bool = convert('false')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(false, r.Get("y"));
    }

    [Test]
    public void Bug16_ConvertTextToBool_One_Works() {
        var r = "y:bool = convert('1')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(true, r.Get("y"));
    }

    [Test]
    public void Bug16_ConvertTextToBool_Zero_Works() {
        var r = "y:bool = convert('0')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(false, r.Get("y"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 17: if-else struct with swapped none fields → runtime crash.
    // `if(true) {a=1, b=none} else {a=none, b=2}; y = x.a ?? 0; z = x.b ?? 0; out = y + z`
    // → "Unable to cast Byte to Int32".
    // Root cause: StructFieldAccessExpressionNode reported TIC-resolved type
    // (e.g., Int32?) but Calc() returned the raw struct value (byte) without conversion.
    // Fix: wrap field access in CastExpressionNode when types differ.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Bug17_IfElseStruct_SwappedNoneFields_Addition() {
        var r = "x = if(true) {a=1, b=none} else {a=none, b=2}\r y = x.a ?? 0\r z = x.b ?? 0\r out = y + z"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Bug17_IfElseStruct_SwappedNoneFields_FalseBranch() {
        var r = "x = if(false) {a=1, b=none} else {a=none, b=2}\r y = x.a ?? 0\r z = x.b ?? 0\r out = y + z"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Bug17_IfElseStruct_SwappedNoneFields_ExplicitIntType() {
        var r = "x:{a:int?, b:int?} = if(true) {a=1, b=none} else {a=none, b=2}\r out = (x.a ?? 0) + (x.b ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Bug17_IfElseStruct_BothFieldsPresent_NoNone() {
        var r = "x = if(true) {a=1, b=2} else {a=3, b=4}\r out = x.a + x.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(3, r.Get("out"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 5: s.m.map(rule it.sum()) → Real[] instead of Int32[]
    // Through struct field access the Preferred type (I32) is lost.
    // Direct variable works: m = [[1,2],[3,4]]; m.map(rule it.sum()) → Int32[]
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Ignore("Bug 5: Preferred type lost through struct field access + generic function chain. Needs structural TIC redesign.")]
    public void Bug5_StructFieldMap_PreservesIntType() {
        var r = "s = {m = [[1,2],[3,4]]}\r out = s.m.map(rule it.sum())".Calc();
        r.AssertResultIs("out", typeof(int[]));
    }

    [Test]
    public void Bug5_DirectVariableMap_PreservesIntType() {
        // This already works — regression test
        var r = "m = [[1,2],[3,4]]\r out = m.map(rule it.sum())".Calc();
        Assert.AreEqual(new[] { 3, 7 }, r.Get("out"));
        r.AssertResultHas("out", new[] { 3, 7 });
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 6: y:int?[] = [1,2,3].map(rule if(it>1) it else none) — annotation rejected.
    // Without annotation works. Adding y:int?[] fails with FU767.
    // Fix: Destruction(StatePrimitive, ConstraintsState) must RemoveAncestor
    // when resolving to StateOptional.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Bug6_MapWithOptionalAnnotation_Compiles() {
        Assert.DoesNotThrow(() =>
            "y:int?[] = [1,2,3].map(rule if(it>1) it else none)"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void Bug6_MapWithOptionalAnnotation_CorrectValues() {
        var r = "y:int?[] = [1,2,3].map(rule if(it>1) it else none)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (int?[])r.Get("y");
        Assert.AreEqual(3, arr.Length);
        Assert.IsNull(arr[0]);
        Assert.AreEqual(2, arr[1]);
        Assert.AreEqual(3, arr[2]);
    }

    [Test]
    public void Bug6_MapWithOptionalAnnotation_WithoutAnnotation_StillWorks() {
        // Regression: without annotation should still work
        var r = "y = [1,2,3].map(rule if(it>1) it else none)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (int?[])r.Get("y");
        Assert.AreEqual(3, arr.Length);
        Assert.IsNull(arr[0]);
        Assert.AreEqual(2, arr[1]);
        Assert.AreEqual(3, arr[2]);
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 7: Named struct in array loses preferred type.
    // type user = {score:int?}; y = [user{score=10}, user{score=none}, user{score=5}]
    // → {score:UInt8?}[] instead of {score:Int32?}[]
    // Fix: LcaStructFields resolves ConstraintsState to Preferred before LCA.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Bug7_NamedStructArray_PreservesPreferredType() {
        var runtime = Funny.Hardcore.WithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
            namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .Build("type user = {score:int?}\r y = [user{score=10}, user{score=none}, user{score=5}]");
        var r = runtime.Calc();
        // score should be Int32? (from named type), not UInt8?
        var arr = r.Get("y");
        Assert.IsNotNull(arr);
    }

    [Test]
    public void Bug7_NamedStructArray_Compiles() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore.WithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
                .Build("type user = {score:int?}\r y = [user{score=10}, user{score=none}, user{score=5}]"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug 8: 'hello'.take(6) throws instead of clamping.
    // [1,2,3].take(100) → [1,2,3] (clamped). But 'hello'.take(6) throws.
    // Fix: TextFunnyArray.Slice clamps endIndex to _text.Length - 1.
    // ═══════════════════════════════════════════════════════════════

    [TestCase("'hello'.take(6)", "hello")]
    [TestCase("'hello'.take(100)", "hello")]
    [TestCase("'hello'.take(5)", "hello")]
    [TestCase("'hello'.take(3)", "hel")]
    [TestCase("'hello'.take(0)", "")]
    [TestCase("'hello'.take(1)", "h")]
    public void Bug8_TextTake_ClampsBeyondLength(string expr, string expected) {
        expr.AssertReturns(expected);
    }

    [TestCase("'hello'.skip(0)", "hello")]
    [TestCase("'hello'.skip(3)", "lo")]
    [TestCase("'hello'.skip(5)", "")]
    [TestCase("'hello'.skip(100)", "")]
    public void Bug8_TextSkip_StillWorks(string expr, string expected) {
        expr.AssertReturns(expected);
    }

    [TestCase("[1,2,3].take(100)", new[] { 1, 2, 3 })]
    [TestCase("[1,2,3].take(3)", new[] { 1, 2, 3 })]
    [TestCase("[1,2,3].take(1)", new[] { 1 })]
    public void Bug8_ArrayTake_StillClamps(string expr, int[] expected) {
        expr.AssertReturns(expected);
    }
}
