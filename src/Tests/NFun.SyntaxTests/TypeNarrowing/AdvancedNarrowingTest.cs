using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.TypeNarrowing;

/// <summary>
/// Advanced type narrowing: or, not, De Morgan, struct fields, safe access, bool?.
/// Rule: if a condition chain PROVES a variable/field is not none → narrow it.
/// </summary>
[TestFixture]
public class AdvancedNarrowingTest {
    private static void AssertNarrowed(string expr, string varName, object expected) {
        var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(expected, r[varName].Value);
    }

    // ── == none narrowing in else branch ─────────────────────────

    [Test]
    public void EqNone_NarrowsInElse() =>
        AssertNarrowed("x:int? = 42\r y = if(x == none) 0 else x + 1", "y", 43);

    [Test]
    public void EqNone_NarrowsInElse_Text() =>
        AssertNarrowed("x:text? = 'hi'\r y = if(x == none) 'none' else x", "y", "hi");

    // ── or narrowing: if(a == none or b == none) → else: both non-none ──

    [Test]
    public void Or_BothNone_ElseNarrowsBoth() =>
        AssertNarrowed(
            "x:int? = 10\r z:int? = 20\r y = if(x == none or z == none) 0 else x + z",
            "y", 30);

    [Test]
    public void Or_OneNone_ElseNarrowsBoth() =>
        AssertNarrowed(
            "a:int? = 5\r b:int? = 3\r y = if(a == none or b == none) -1 else a * b",
            "y", 15);

    [Test]
    public void Or_ThreeVars_ElseNarrowsAll() =>
        AssertNarrowed(
            "a:int? = 1\r b:int? = 2\r c:int? = 3\r y = if(a == none or b == none or c == none) 0 else a + b + c",
            "y", 6);

    [Test]
    public void Or_NoneOrNegative_ElseNarrowsAndConstrains() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none or x < 0) 0 else x + 1",
            "y", 43);

    // ── not narrowing ────────────────────────────────────────────

    [Test]
    public void Not_EqNone_NarrowsInTrue() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(not(x == none)) x + 1 else 0",
            "y", 43);

    [Test]
    public void Not_NeqNone_NarrowsInElse() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(not(x != none)) 0 else x + 1",
            "y", 43);

    [Test]
    public void DoubleNot_NeqNone_NarrowsInTrue() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(not(not(x != none))) x + 1 else 0",
            "y", 43);

    // ── De Morgan ────────────────────────────────────────────────

    [Test]
    public void DeMorgan_NotOrNone_NarrowsBoth() =>
        AssertNarrowed(
            "x:int? = 10\r z:int? = 20\r y = if(not(x == none or z == none)) x + z else 0",
            "y", 30);

    [Test]
    public void DeMorgan_NotAndNone_ElseNarrowsBoth() =>
        AssertNarrowed(
            "x:int? = 10\r z:int? = 20\r y = if(not(x != none and z != none)) 0 else x + z",
            "y", 30);

    // ── Struct field narrowing ───────────────────────────────────

    [Test]
    public void StructField_EqNone_NarrowsInElse() =>
        AssertNarrowed(
            "s = {age = if(true) 42 else none}\r y = if(s.age == none) 0 else s.age + 1",
            "y", 43);

    [Test]
    public void StructFields_Or_ElseNarrowsBoth() =>
        AssertNarrowed(
            "s = {a = if(true) 1 else none, b = if(true) 2 else none}\r y = if(s.a == none or s.b == none) 0 else s.a + s.b",
            "y", 3);

    // ── Safe access implies non-none ─────────────────────────────

    [Test]
    public void SafeAccess_EqTrue_NarrowsStruct() =>
        AssertNarrowed(
            "s = if(true) {flag = true} else none\r y = if(s?.flag == true) 1 else 0",
            "y", 1);

    [Test]
    [Ignore("TODO: safe access comparison (s?.age > 18) should imply s != none")]
    public void SafeAccess_Comparison_NarrowsStruct() =>
        AssertNarrowed(
            "s = if(true) {age = 25} else none\r y = if(s?.age > 18) 1 else 0",
            "y", 1);

    // ── Bool? narrowing ──────────────────────────────────────────

    [Test]
    public void OptionalBool_EqTrue_Narrowed() =>
        AssertNarrowed(
            "flag:bool? = true\r y = if(flag == true) 1 else 0",
            "y", 1);

    [Test]
    public void OptionalBool_EqFalse_Narrowed() =>
        AssertNarrowed(
            "flag:bool? = false\r y = if(flag == false) 1 else 0",
            "y", 1);

    // ── Complex chains ───────────────────────────────────────────

    [Test]
    public void MultiVarOrChain_ElseNarrowsAll() =>
        AssertNarrowed(
            "a:int? = 1\r b:int? = 2\r c:int? = 3\r d:int? = 4\r y = if(a == none or b == none or c == none or d == none) 0 else a + b + c + d",
            "y", 10);

    [Test]
    public void MixedAndOr_NoneCheck() =>
        AssertNarrowed(
            "z:int? = 42\r y = if(z != none and z > 0) z * 2 else 0",
            "y", 84);

    // ── Collection narrowing ─────────────────────────────────────

    [Test]
    public void Filter_NotEqNone_Narrows() =>
        AssertNarrowed(
            "items:int?[] = [1, none, 3]\r y = items.filter(rule not(it == none))",
            "y", new[] { 1, 3 });

    [Test]
    public void Filter_OrCondition_NoNarrow() =>
        AssertNarrowed(
            "items:int?[] = [1, none, 3]\r y = items.filter(rule it != none or true).count()",
            "y", 3);
}
