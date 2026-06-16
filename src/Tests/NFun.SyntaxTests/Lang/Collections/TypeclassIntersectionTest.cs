using NFun;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang.Collections;

/// <summary>
/// Exhaustive coverage of typeclass / constraint intersection on a single
/// generic parameter. NFun has two typeclasses:
/// <list type="bullet">
///   <item><b>Comparable</b> — element-level, primitive (Preferred = char).</item>
///   <item><b>Clearable</b> — container-level (Preferred = List). Encoded as
///     <c>StateCompositeConstraints.IsClearable</c> flag (orthogonal to the
///     <c>[Descendant..Ancestor]</c> interval).</item>
/// </list>
/// Plus container-level constraints (bound only, no Preferred):
/// <list type="bullet">
///   <item><c>[..Enumerable]</c> — <c>count</c>, <c>for x in xs:</c>, <c>contains</c>,
///     <c>sort</c>, <c>fold</c>, <c>map</c>, …</item>
///   <item><c>[..FixedArray]</c> — <c>xs[i]</c> read</item>
///   <item><c>[..Array]</c> — <c>xs[i] = v</c> write</item>
/// </list>
/// And List-specific (concrete pin): <c>add</c>, <c>removeAt</c>, …
///
/// <para>The intersection bug (#131) — typeclass + bound on the same generic
/// in order-dependent ways — was resolved by separating Clearable from the
/// lattice cap (<c>Ancestor</c>) into the orthogonal <c>IsClearable</c> flag
/// that Unify ORs and Concretest narrows to List preferred.</para>
/// </summary>
public class TypeclassIntersectionTest {

    // ────────────────────────────────────────────────────────────────
    // Singles — baseline, all must pass
    // ────────────────────────────────────────────────────────────────

    [Test] public void Single_ClearOnly() {
        var rt = Funny.Hardcore.BuildLang("fun foo(xs): xs.clear()\nout = foo(list(1,2,3))");
        rt.Run();
    }

    [Test] public void Single_CountOnly() {
        var rt = Funny.Hardcore.BuildLang("fun foo(xs): return xs.count()\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(3, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Single_AddOnly() {
        var rt = Funny.Hardcore.BuildLang("fun foo(xs): xs.add(99)\nout = foo(list(1,2,3))");
        rt.Run();
    }

    [Test] public void Single_IndexedRead() {
        var rt = Funny.Hardcore.BuildLang("fun foo(xs): return xs[0]\nout = foo(list(7,8,9))");
        rt.Run();
        Assert.AreEqual(7, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Single_ForLoop() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    s = 0\n    for x in xs: s = s + x\n    return s\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(6, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // Constraint + Constraint — no Clearable, both pure bounds
    // Intersection works via existing Gcd on the lattice.
    // ────────────────────────────────────────────────────────────────

    [Test] public void Pair_CountAndRead() {
        // Enumerable + FixedArray → FixedArray
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    c = xs.count()\n    a = xs[0]\n    return c + a\nout = foo(list(10,20,30))");
        rt.Run();
        Assert.AreEqual(13, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Pair_ReadAndWrite() {
        // FixedArray + Array → Array
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    a = xs[0]\n    xs[0] = 99\n    return a\nout = foo(list(7,8,9))");
        rt.Run();
        Assert.AreEqual(7, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Pair_CountAndWrite() {
        // Enumerable + Array → Array
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    c = xs.count()\n    xs[0] = 99\n    return c\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(3, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // Clearable + Enumerable constraint — order matters today (#131)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Pair_CountThenClear_OK() {
        // count first, clear second — works.
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    c = xs.count()\n    xs.clear()\n    return c\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(3, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Pair_ClearThenCount_FAILS() {
        // clear first, count second — expected to resolve to list<T>, currently FU710.
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    return xs.count()\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Pair_ForThenClear_OK() {
        // for-loop iter (Enumerable) first, clear second — works (same as count first).
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    s = 0\n    for x in xs: s = s + x\n    xs.clear()\n    return s\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(6, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Pair_ClearThenFor_FAILS() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    s = 0\n    for x in xs: s = s + x\n    return s\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Pair_ContainsThenClear_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    b = xs.contains(2)\n    xs.clear()\n    return b\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Pair_ClearThenContains_FAILS() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    return xs.contains(2)\nout = foo(list(1,2,3))");
        rt.Run();
    }

    // ────────────────────────────────────────────────────────────────
    // Clearable + FixedArray-constraint (indexed read) — fails BOTH orders
    // Expected: intersection {Array, FixedArray, List} ∩ {List, Set, Map} = {List}
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Pair_ReadThenClear_FAILS() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    a = xs[0]\n    xs.clear()\n    return a\nout = foo(list(7,8,9))");
        rt.Run();
        Assert.AreEqual(7, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Pair_ClearThenRead_FAILS() {
        // Compiles: xs intersects to List (Clearable + indexed-read).
        // Runtime correctly throws "argument out of range" because xs is
        // empty after clear() — we're not asserting that, just the build.
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    return xs[0]\nout = foo(list(7,8,9))");
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    // ────────────────────────────────────────────────────────────────
    // Clearable + Array-constraint (indexed write) — fails BOTH orders
    // Expected: intersection {Array, List} ∩ {List, Set, Map} = {List}
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Pair_WriteThenClear_FAILS() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs[0] = 99\n    xs.clear()\nout = foo(list(1,2,3))");
        rt.Run();
    }

    [Test]
    public void Pair_ClearThenWrite_FAILS() {
        // Compiles: xs intersects Clearable + Array → List. Runtime correctly
        // throws on xs[0]=v after clear() (index 0 on empty list).
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    xs[0] = 99\nout = foo(list(1,2,3))");
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    // ────────────────────────────────────────────────────────────────
    // Clearable + List-pinned operations (add / removeAt) — work both orders.
    // No bug: List-pinned operations narrow xs to List directly, no
    // intersection-via-typeclass needed.
    // ────────────────────────────────────────────────────────────────

    [Test] public void Pair_AddThenClear_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.add(99)\n    xs.clear()\n    return xs.count()\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Pair_ClearThenAdd_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    xs.add(99)\n    return xs.count()\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(1, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Pair_RemoveAtThenClear_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.removeAt(0)\n    xs.clear()\n    return xs.count()\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // Element-axis + container-axis: Comparable on T + Clearable on container.
    // Different TIC nodes — no intersection conflict.
    // ────────────────────────────────────────────────────────────────

    [Test] public void Pair_SortThenClear_OK() {
        // sort: Enumerable<T> + T Comparable. Then clear: Clearable<T>.
        // Enumerable first → resolves; clear after → OK pattern.
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.sort()\n    xs.clear()\nout = foo(list(3,1,2))");
        rt.Run();
    }

    [Test]
    public void Pair_ClearThenSort_FAILS() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    xs.sort()\nout = foo(list(3,1,2))");
        rt.Run();
    }

    // ────────────────────────────────────────────────────────────────
    // Triple intersections
    // ────────────────────────────────────────────────────────────────

    [Test] public void Triple_CountForClearLast_OK() {
        // Two Enumerable constraints + Clearable LAST: works in left-to-right rule.
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    c = xs.count()\n    s = 0\n    for x in xs: s = s + x\n    xs.clear()\n    return c + s\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(9, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Triple_CountClearWrite_FAILS() {
        // Compiles: xs intersects Enumerable + Clearable + Array → List.
        // Runtime correctly throws on xs[0]=v after clear() (empty list).
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    c = xs.count()\n    xs.clear()\n    xs[0] = 99\n    return c\nout = foo(list(1,2,3))");
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    [Test] public void Triple_ForClearAddLast_OK() {
        // Two Enumerable + Clearable + List-pinned. add is List-pinned so
        // the resolution narrows even without typeclass intersection.
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    s = 0\n    for x in xs: s = s + x\n    xs.clear()\n    xs.add(99)\n    return s + xs.count()\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(7, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // Idempotence — duplicate typeclass / constraint calls
    // ────────────────────────────────────────────────────────────────

    [Test] public void Idempotent_CountCount_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    a = xs.count()\n    b = xs.count()\n    return a + b\nout = foo(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(6, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Idempotent_ClearClear_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(xs):\n    xs.clear()\n    xs.clear()\nout = foo(list(1,2,3))");
        rt.Run();
    }

    // ────────────────────────────────────────────────────────────────
    // Negative cases — TIC should reject at build time
    // ────────────────────────────────────────────────────────────────

    [Test] public void Negative_ClearOnFixedArray_Rejected() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("fun foo(): a = fixedArray(1,2,3); a.clear()\nout = foo()"));
    }

    [Test] public void Negative_ClearOnLangIntArray_Rejected() {
        // Lang-mode `int[]` is mutable Array. Clear is for Clearable only (List/Set/Map).
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("fun foo(): a:int[] = [1,2,3]; a.clear(); return a\nout = foo()"));
    }

    [Test] public void Negative_ClearOnEeArray_Rejected() {
        // ee-mode int[] is StateArray (immutable covariant) — definitely not Clearable.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("a = [1,2,3]; out = a.clear()"));
    }

    // ────────────────────────────────────────────────────────────────
    // Direct (non-generic) calls — bypass intersection issue
    // ────────────────────────────────────────────────────────────────

    [Test] public void Direct_ListClearCount_OK() {
        var rt = Funny.Hardcore.BuildLang("a = list(1,2,3); a.clear(); out = a.count()");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Direct_SetClear_OK() {
        var rt = Funny.Hardcore.BuildLang("a = set(1,2,3); a.clear(); out = a.count()");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test] public void Direct_MapClear_OK() {
        var rt = Funny.Hardcore.BuildLang(
            "a = __mkMap({key=1,value=10},{key=2,value=20}); a.clear(); out = a.count()");
        rt.Run();
        Assert.AreEqual(0, System.Convert.ToInt32(rt["out"].Value));
    }
}
