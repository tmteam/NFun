using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests for ?. (safe field access) operator.
/// Tests for ?[ (safe array indexing) are in SafeArrayAccessCandidateTest.cs.
/// Tests that require {name:type} struct type annotation syntax remain in
/// OptionalChainingTest.cs with class-level [Ignore].
/// </summary>
[TestFixture]
public class OptionalChainingWorkingTest {

    // ═══════════════════════════════════════════════════════════════
    // ?. safe field access (using if-else for optional struct inference)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeFieldAccess_Text_HasValue() =>
        "x = if(true) {name = 'Alice'} else none\r y = x?.name"
            .AssertResultHas("y", "Alice");


    [Test]
    public void SafeFieldAccess_Text_None() {
        var result = "x = if(false) {name = 'Alice'} else none\r y = x?.name".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void SafeFieldAccess_TextWithCoalesce_HasValue() =>
        "x = if(true) {name = 'Alice'} else none\r y = x?.name ?? 'default'"
            .AssertResultHas("y", "Alice");


    [Test]
    public void SafeFieldAccess_TextWithCoalesce_None() =>
        "x = if(false) {name = 'Alice'} else none\r y = x?.name ?? 'default'"
            .AssertResultHas("y", "default");


    [Test]
    public void SafeFieldAccess_IntWithCoalesce_HasValue() =>
        "x = if(true) {age = 25} else none\r y:int = x?.age ?? 0"
            .AssertResultHas("y", 25);


    [Test]
    public void SafeFieldAccess_IntWithCoalesce_None() =>
        "x = if(false) {age = 25} else none\r y:int = x?.age ?? 0"
            .AssertResultHas("y", 0);


    [Test]
    public void SafeFieldAccess_BoolWithCoalesce_HasValue() =>
        "x = if(true) {flag = true} else none\r y = x?.flag ?? false"
            .AssertResultHas("y", true);


    [Test]
    public void SafeFieldAccess_BoolWithCoalesce_None() =>
        "x = if(false) {flag = true} else none\r y = x?.flag ?? false"
            .AssertResultHas("y", false);


    // ═══════════════════════════════════════════════════════════════
    // Negative tests: ?. on wrong types
    // ═══════════════════════════════════════════════════════════════

    [TestCase("x:int\r y = x?.name")]
    [TestCase("x:text\r y = x?.count")]
    public void SafeFieldAccess_OnNonStruct_Fails(string expr) =>
        expr.AssertObviousFailsOnParse();


    [Test]
    public void SafeFieldAccess_OnNonOptionalStruct_Fails() =>
        "x = {name = 'hi'}\r y = x?.name".AssertObviousFailsOnParse();


    // ═══════════════════════════════════════════════════════════════
    // ?. on fields that are already optional (flatten opt(opt(T)))
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeFieldAccess_OptionalIntField_HasValue() {
        var result = "x:int? = 5\r s = if(true) {v = x} else none\r y = s?.v".Calc();
        Assert.AreEqual(5, result.Get("y"));
    }


    [Test]
    public void SafeFieldAccess_OptionalIntField_None() {
        var result = "x:int? = 5\r s = if(false) {v = x} else none\r y = s?.v".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void SafeFieldAccess_OptionalArrayField_HasValue() {
        var result = "x:int[]? = [1,2]\r s = if(true) {arr = x} else none\r y = s?.arr".Calc();
        Assert.IsNotNull(result.Get("y"));
    }


    [Test]
    public void SafeFieldAccess_OptionalArrayField_None() {
        var result = "x:int[]? = [1,2]\r s = if(false) {arr = x} else none\r y = s?.arr".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- ?. chained: x?.a?.b ---

    [Test]
    public void SafeFieldAccess_ChainedTwoLevels_HasValue() =>
        "x = if(true) {a = {b = 42}} else none\r y = x?.a?.b"
            .AssertResultHas("y", 42);


    [Test]
    public void SafeFieldAccess_ChainedTwoLevels_None() {
        var result = "x = if(false) {a = {b = 42}} else none\r y = x?.a?.b".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- Chained ?? with ?. (right-associative ??) ---

    [Test]
    public void ChainedCoalesce_WithSafeFieldAccess() =>
        "x = if(true) {a=1, b=2} else none\r y:int = x?.a ?? x?.b ?? 0"
            .AssertResultHas("y", 1);


    [Test]
    public void ChainedCoalesce_WithSafeFieldAccess_None() =>
        "x = if(false) {a=1, b=2} else none\r y:int = x?.a ?? x?.b ?? 0"
            .AssertResultHas("y", 0);


}
