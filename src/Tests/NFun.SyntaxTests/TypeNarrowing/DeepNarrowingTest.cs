using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.TypeNarrowing;

/// <summary>
/// Deep narrowing chains: or-progressive, nested conditions,
/// multi-level struct paths, combination patterns.
/// </summary>
[TestFixture]
public class DeepNarrowingTest {
    private static void AssertNarrowed(string expr, string varName, object expected) {
        var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(expected, r[varName].Value);
    }

    // ── or-progressive: x == none or <expr using x> ─────────────

    [Test]
    public void OrProgressive_EqNoneOrGreater() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none or x > 100) 0 else x",
            "y", 42);

    [Test]
    public void OrProgressive_EqNoneOrEquals() =>
        AssertNarrowed(
            "x:int? = 5\r y = if(x == none or x == 0) -1 else x + 1",
            "y", 6);

    [Test]
    public void OrProgressive_EqNoneOrComplex() =>
        AssertNarrowed(
            "x:int? = 10\r y = if(x == none or x * 2 > 100) 0 else x",
            "y", 10);

    [Test]
    public void OrProgressive_TwoVars() =>
        AssertNarrowed(
            "a:int? = 3\r b:int? = 4\r y = if(a == none or b == none or a + b > 100) 0 else a + b",
            "y", 7);

    // ── or-progressive: first WhenFalse narrows for second ──────

    [Test]
    public void OrProgressive_NeqNoneOrValue() =>
        // x != none or (something) — when x != none is FALSE (x is none),
        // the right side should NOT use x. This is correct — no narrowing needed.
        // But: x == none or x > 0 — when x == none is FALSE, x is narrowed.
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none or x < 0) 0 else x + 1",
            "y", 43);

    // ── Deep nested if-else with narrowing ───────────────────────

    [Test]
    public void NestedIfElse_OuterNarrows_InnerUses() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x != none) (if(x > 10) x * 2 else x) else 0",
            "y", 84);

    [Test]
    public void NestedIfElse_ThreeLevels() =>
        AssertNarrowed(
            "x:int? = 5\r y = if(x != none) (if(x > 0) (if(x < 100) x else 100) else 0) else -1",
            "y", 5);

    [Test]
    public void NestedIfElse_TwoOptionals() =>
        AssertNarrowed(
            "a:int? = 10\r b:int? = 20\r y = if(a != none and b != none) (if(a > b) a else b) else 0",
            "y", 20);

    // ── or with struct field narrowing ───────────────────────────

    [Test]
    public void OrProgressive_StructField_EqNoneOrCompare() =>
        AssertNarrowed(
            "s = {v = if(true) 42 else none}\r y = if(s.v == none or s.v < 0) 0 else s.v",
            "y", 42);

    // ── Chained or with multiple none checks + value check ──────

    [Test]
    public void ChainedOr_NoneNoneValue() =>
        AssertNarrowed(
            "a:int? = 1\r b:int? = 2\r y = if(a == none or b == none or a + b == 0) -1 else a + b",
            "y", 3);

    // ── and + or combinations ────────────────────────────────────

    [Test]
    public void AndOr_NarrowInAnd_UseInOr() =>
        // (x != none and x > 0) or flag — x narrowed inside the and
        AssertNarrowed(
            "x:int? = 5\r flag = false\r y = if(x != none and x > 0) x else 0",
            "y", 5);

    [Test]
    public void OrThenAnd_ElseNarrows() =>
        // if(a == none or b == none) → else: both narrowed
        // then use in arithmetic
        AssertNarrowed(
            "a:int? = 3\r b:int? = 7\r y = if(a == none or b == none) 0 else (if(a > b) a - b else b - a)",
            "y", 4);

    // ── Progressive or in filter lambda ──────────────────────────

    [Test]
    public void Filter_OrProgressive_NoneOrNegative() =>
        AssertNarrowed(
            "items:int?[] = [1, none, -2, 3, none, -1]\r y = items.filter(rule not(it == none or it < 0))",
            "y", new[] { 1, 3 });

    [Test]
    public void Filter_OrProgressive_NoneOrZero() =>
        AssertNarrowed(
            "items:int?[] = [0, none, 1, 2, none, 0]\r y = items.filter(rule not(it == none or it == 0))",
            "y", new[] { 1, 2 });

    // ── Multi-elif style narrowing ───────────────────────────────

    [Test]
    [Ignore("TODO: multi-elif progressive narrowing — second if should see x narrowed from first")]
    public void MultiElif_EachBranchNarrows() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none) -1\r if(x < 0) 0\r if(x > 100) 100\r else x",
            "y", 42);

    [Test]
    [Ignore("TODO: multi-elif progressive narrowing")]
    public void MultiElif_NoneFirst_ThenRange() =>
        AssertNarrowed(
            "x:int? = 50\r y = if(x == none) 0\r if(x < 10) 10\r if(x > 90) 90\r else x",
            "y", 50);
}
