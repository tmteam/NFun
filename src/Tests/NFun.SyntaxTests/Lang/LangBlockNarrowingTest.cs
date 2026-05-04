using System;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>
/// Block-level type narrowing after early-exit guards (#120).
/// After `if x == none: return ...`, the variable is narrowed in subsequent
/// statements so direct access (no `?.` / `??`) is allowed.
/// </summary>
[TestFixture]
public class LangBlockNarrowingTest {

    // ─── Inverse guards: narrow inside then-branch ───

    [Test]
    public void InverseGuard_NotNone_NarrowedInThenBranch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: int?):\n" +
            "    if x != none:\n" +
            "        return x + 100\n" +
            "    return -1\n" +
            "y = f(42)");
        rt.Run();
        Assert.AreEqual(142, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void InverseGuard_NotNone_NoneFallsThrough() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: int?):\n" +
            "    if x != none:\n" +
            "        return x + 100\n" +
            "    return -1\n" +
            "y = f(none)");
        rt.Run();
        Assert.AreEqual(-1, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Else-branch narrowing ───

    [Test]
    public void ElseBranch_NarrowedAfterIsNone() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: int?):\n" +
            "    if x == none:\n" +
            "        return -1\n" +
            "    else:\n" +
            "        return x + 100\n" +
            "y = f(42)");
        rt.Run();
        Assert.AreEqual(142, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void BothBranchesReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: int?):\n" +
            "    if x == none:\n" +
            "        return -1\n" +
            "    else:\n" +
            "        return x * 2\n" +
            "y = f(21)");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Different optional payload types ───

    [Test]
    public void TextOptional_NarrowedReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: text?):\n" +
            "    if x == none: return 'fb'\n" +
            "    return x\n" +
            "y = f('hi')");
        rt.Run();
        Assert.AreEqual("hi", rt["y"].Value.ToString());
    }

    [Test]
    public void TextOptional_NarrowedCountCall() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: text?):\n" +
            "    if x == none: return -1\n" +
            "    return x.count()\n" +
            "y = f('hello')");
        rt.Run();
        Assert.AreEqual(5, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TextOptional_NarrowedConcatViaMethod() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: text?):\n" +
            "    if x == none: return 'fb'\n" +
            "    return x.concat('!')\n" +
            "y = f('hi')");
        rt.Run();
        Assert.AreEqual("hi!", rt["y"].Value.ToString());
    }

    [Test]
    public void BoolOptional_NarrowedBoolean() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: bool?):\n" +
            "    if x == none: return false\n" +
            "    return x and true\n" +
            "y = f(true)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void StructOptional_DirectFieldAfterGuard() {
        var rt = Funny.Hardcore.BuildLang(
            "type p = {x: int, y: int}\n" +
            "fun f(s: p?):\n" +
            "    if s == none: return -1\n" +
            "    return s.x + s.y\n" +
            "y = f({x=10, y=20})");
        rt.Run();
        Assert.AreEqual(30, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Multiple params, ordering ───

    [Test]
    public void TwoParams_OneNarrowed_OtherStillOptional() {
        // Guard on `a`. Inside body, `a` is narrowed but `b` is still optional —
        // accessed via `??`.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a: int?, b: int?):\n" +
            "    if a == none: return -1\n" +
            "    return a + (b ?? 0)\n" +
            "y = f(10, none)");
        rt.Run();
        Assert.AreEqual(10, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ChainedGuards_BothNarrowed() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a: int?, b: int?):\n" +
            "    if a == none: return -1\n" +
            "    if b == none: return -2\n" +
            "    return a * b\n" +
            "y = f(6, 7)");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Local variable narrowed after assignment from optional source ───

    [Test]
    public void NarrowedLocal_AfterGuardOnOptionalAssignment() {
        var rt = Funny.Hardcore.BuildLang(
            "type p = {x: int}\n" +
            "fun f(s: p?):\n" +
            "    v = s?.x\n" +
            "    if v == none: return -1\n" +
            "    return v + 100\n" +
            "y = f({x=42})");
        rt.Run();
        Assert.AreEqual(142, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Narrowing persists across multiple subsequent statements ───

    [Test]
    public void Narrowing_PersistsAcrossMultipleStatements() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: int?):\n" +
            "    if x == none: return -1\n" +
            "    a = x + 1\n" +
            "    b = x + 2\n" +
            "    return a + b\n" +
            "y = f(10)");
        rt.Run();
        Assert.AreEqual(23, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Idempotent guards ───

    [Test]
    public void DuplicateGuards_RedundantButLegal() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x: int?):\n" +
            "    if x == none: return -1\n" +
            "    if x == none: return -2\n" +
            "    return x\n" +
            "y = f(42)");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Narrowing across return inside for-loop body ───

    [Test]
    public void Narrowing_ReturnInsideForLoop() {
        // `return` inside the loop body narrows the iteration variable for the
        // rest of that iteration's statements.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(xs: int?[]):\n" +
            "    for x in xs:\n" +
            "        if x == none: return -1\n" +
            "        if x > 100: return x\n" +
            "    return 0\n" +
            "y = f([1, 2, 3])");
        rt.Run();
        Assert.AreEqual(0, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void Narrowing_EarlyExitViaCoalesceContinue() {
        // `x ?? continue` works as an alternative pattern to a guard.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(xs: int?[]):\n" +
            "    total = 0\n" +
            "    for x in xs:\n" +
            "        v = x ?? continue\n" +
            "        total += v\n" +
            "    return total\n" +
            "y = f([1, none, 2, none, 3])");
        rt.Run();
        Assert.AreEqual(6, Convert.ToInt32(rt["y"].Value));
    }

    // ─── Narrowing across continue / break ───

    [Test]
    public void Narrowing_AcrossContinueInForLoop() {
        // `if x == none: continue` narrows x for the rest of THAT loop iteration —
        // same algebra as `return`, just exits the iteration instead of the function.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(xs: int?[]):\n" +
            "    total = 0\n" +
            "    for x in xs:\n" +
            "        if x == none: continue\n" +
            "        total += x\n" +
            "    return total\n" +
            "y = f([1, none, 2, none, 3])");
        rt.Run();
        Assert.AreEqual(6, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void Narrowing_AcrossBreakInForLoop() {
        // `if x == none: break` narrows x for the rest of THAT iteration.
        // After break, the remaining loop body for this iteration is dead — but TIC
        // still needs the rest of the body to typecheck against the narrowed type.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(xs: int?[]):\n" +
            "    total = 0\n" +
            "    for x in xs:\n" +
            "        if x == none: break\n" +
            "        total += x\n" +
            "    return total\n" +
            "y = f([1, 2, none, 4])");
        rt.Run();
        Assert.AreEqual(3, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void Narrowing_AcrossContinue_StructDirectFieldAccess() {
        var rt = Funny.Hardcore.BuildLang(
            "type p = {v: int}\n" +
            "fun f(xs: p?[]):\n" +
            "    total = 0\n" +
            "    for s in xs:\n" +
            "        if s == none: continue\n" +
            "        total += s.v\n" +
            "    return total\n" +
            "y = f([{v=1}, none, {v=2}, none, {v=3}])");
        rt.Run();
        Assert.AreEqual(6, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void Narrowing_AcrossBreak_TextConcat() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(xs: text?[]):\n" +
            "    result = ''\n" +
            "    for s in xs:\n" +
            "        if s == none: break\n" +
            "        result = result.concat(s)\n" +
            "    return result\n" +
            "y = f(['a', 'b', none, 'c'])");
        rt.Run();
        Assert.AreEqual("ab", rt["y"].Value.ToString());
    }
}
