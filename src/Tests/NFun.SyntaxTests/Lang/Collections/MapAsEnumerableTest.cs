using NFun;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang.Collections;

/// <summary>
/// Map ↔ Enumerable&lt;{key, value}&gt; isomorphism — the design promise that
/// a <c>map&lt;K,V&gt;</c> behaves like an <c>Enumerable</c> of pair-structs.
///
/// <para>The current TIC has TWO disjoint state classes for collection-ish
/// shapes: <c>StateMap</c> (specialised, 2 element nodes) and
/// <c>StateCompositeConstraints</c> + <c>StateCollection</c> (1 element
/// node). The <c>CompCsApply.ForwardPullCompCsStateMap</c> bridges them
/// by SYNTHESISING a frozen struct &#123;key, value&#125; whenever a Map
/// flows into an <c>Enumerable&lt;T&gt;</c> typeclass slot. But this
/// synthesis happens ONLY in the typeclass-cross path — the direct
/// statement-level <c>for i in m:</c> still drives the runtime through
/// the legacy <c>StateMap</c> shape and the pair-struct never gets wired,
/// so <c>i.key</c> / <c>i.value</c> NREs at runtime.</para>
///
/// <para>The set of currently failing cases below pins the inconsistency.
/// Resolution: unify <c>StateMap</c> with <c>StateCompositeConstraints</c>
/// — represent map as a single-element composite whose element node holds
/// the frozen pair-struct. Eliminates the dual-representation bug class.</para>
/// </summary>
public class MapAsEnumerableTest {

    // ────────────────────────────────────────────────────────────────
    // Working baselines — control cases that DO succeed today.
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Map_Count_Works() {
        // count(xs: Enumerable<T>) bridges Map → CompCs{Anc=Enumerable},
        // ForwardPullCompCsStateMap synthesises pair-struct on the path.
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.count()");
        rt.Run();
        Assert.AreEqual(2, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_MapValueSum_Works() {
        // .map(rule it.value).sum() — same Enumerable bridge.
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.map(rule it.value).sum()");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_ForLoop_NoFieldAccess_Works() {
        // Iterating without touching i.key or i.value works — the runtime
        // just walks the map; no pair-struct synthesis needed.
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\ns = 0\nfor i in m: s = s + 1\nout = s");
        rt.Run();
        Assert.AreEqual(2, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void List_OfPairStructs_ForFieldAccess_Works() {
        // Control: when the SAME element shape arrives via list-of-structs
        // (no StateMap involved), field access works. Confirms the runtime
        // can do field access — the bug is in the StateMap path.
        var rt = Funny.Hardcore.BuildLang(
            "a = [{key=1,value=10},{key=2,value=20}]\ns = 0\nfor i in a: s = s + i.value\nout = s");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // Broken — Map's pair-struct iso violated when iteration meets
    // field access. The promised `Enumerable<{key, value}>` shape is
    // not honoured by the TIC + runtime when StateMap is the entry path.
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Map_ForLoop_IValue_NREs() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\ns = 0\nfor i in m: s = s + i.value\nout = s");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_ForLoop_IKey_NREs() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\ns = 0\nfor i in m: s = s + i.key\nout = s");
        rt.Run();
        Assert.AreEqual(3, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_ForLoop_IBothFields_NREs() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\ns = 0\nfor i in m: s = s + i.key + i.value\nout = s");
        rt.Run();
        Assert.AreEqual(33, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_ForLoop_ThenContainsKey() {
        // `containsKey(K)` is the map-specific key lookup. The Enumerable
        // `contains(T)` for Map expects a `{key, value}` pair, not a bare key.
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\ns = 0\nfor i in m: s = s + i.value\nk = m.containsKey(1)\nout = s");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_ContainsKey_ThenForLoop() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nk = m.containsKey(1)\ns = 0\nfor i in m: s = s + i.value\nout = s");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Map_EnumerableContains_PairStruct() {
        // Enumerable.contains(T) on a Map asks "is THIS pair-struct an entry?".
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.contains({key=1, value=10})");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Map_EnumerableContains_WrongPair_False() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.contains({key=1, value=99})");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Map_Contains_BareKey_Rejected() {
        // `m.contains(K)` is an error — the Enumerable contains takes a pair.
        // Use `m.containsKey(K)` for key lookup.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10})\nout = m.contains(1)"));
    }
}
