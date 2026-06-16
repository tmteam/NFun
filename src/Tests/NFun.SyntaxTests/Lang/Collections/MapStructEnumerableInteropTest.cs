using NFun;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang.Collections;

/// <summary>
/// Probe tests at the intersection of Map / Struct / Enumerable to discriminate
/// between four representation options for Map in TIC:
///
/// <list type="bullet">
///   <item><b>Status quo</b> — `StateMap` is a separate 2-arg state class;
///     `CompCsApply.ForwardPullCompCsStateMap` synthesises a frozen pair-struct
///     on the typeclass-cross path. Inconsistent across direct vs. typeclass paths.</item>
///   <item><b>Option A</b> — delete `StateMap`; Map becomes
///     `StateCollection(Map, structNode{key, value})`. Single-element. Professor
///     flagged: covariant struct-LCA breaks Map invariance + no field-as-generic
///     primitive in TIC.</item>
///   <item><b>Option D</b> — keep `StateMap` 2-arg, but canonicalise (lazy
///     identity-stable) the pair-struct synthesis; reuse across all Map↔Enumerable
///     paths. Minimal change. Preserves invariance.</item>
///   <item><b>Option E</b> — introduce TIC-internal `KeyValuePair[of K, V]`
///     composite (its own 2-arg invariant state); structs `{key, value}`
///     implicitly convert; Map's element is `KeyValuePair`. Like A but the
///     element class enforces invariance.</item>
/// </list>
///
/// <para>Each test below targets ONE discriminating behaviour. The
/// <c>[Ignore("PROBE: …")]</c> tag tells you the expected behaviour and which
/// option(s) would resolve / break it. Run against the current codebase to
/// establish baseline; if all probes are already green/red as expected, the
/// table tells us which option to pick.</para>
/// </summary>
public class MapStructEnumerableInteropTest {

    // ────────────────────────────────────────────────────────────────
    // §1 — LCA on Map K/V — script-friendly widening
    // ────────────────────────────────────────────────────────────────
    //
    // When no annotation is present, integer literals carry Preferred=I32
    // (soft); LCA at if-else widens them to match the Real side, so BOTH
    // branches resolve to the wider type. Matches established behaviour for
    // `list` (`[1,2,3] + [1.0]` → list<Real>) and `struct` (`{x=1} + {x=1.0}`
    // → {x:Real}). Strict Stage-0-style invariance (collapse to Any) still
    // applies for explicit `list<int>` vs `list<real>` annotations — see
    // PullPushTest.IfElse_DifferentLists_CollapsesToAny.

    /// <summary>K-axis widening: `map<int,T>` + `map<real,T>` infer to
    /// `map<real,T>` on both sides via pair-struct field-wise LCA.</summary>
    [Test]
    public void LCA_MapIntInt_vs_MapRealInt_WidensKeyToReal() {
        var rt = Funny.Hardcore.BuildLang(
            "a = __mkMap({key=1,value=10})\nb = __mkMap({key=1.0,value=10})\nout = if(true) a else b");
        Assert.AreEqual("map<Real,Int32>", rt["a"].Type.ToString());
        Assert.AreEqual("map<Real,Int32>", rt["b"].Type.ToString());
        Assert.AreEqual("map<Real,Int32>", rt["out"].Type.ToString());
    }

    /// <summary>V-axis widening (symmetric).</summary>
    [Test]
    public void LCA_MapIntInt_vs_MapIntReal_WidensValueToReal() {
        var rt = Funny.Hardcore.BuildLang(
            "a = __mkMap({key=1,value=10})\nb = __mkMap({key=1,value=10.0})\nout = if(true) a else b");
        Assert.AreEqual("map<Int32,Real>", rt["a"].Type.ToString());
        Assert.AreEqual("map<Int32,Real>", rt["b"].Type.ToString());
        Assert.AreEqual("map<Int32,Real>", rt["out"].Type.ToString());
    }

    // ────────────────────────────────────────────────────────────────
    // §2 — Map element identity vs struct-literal identity
    // ────────────────────────────────────────────────────────────────

    /// <summary>Can a struct literal flow into a Map slot via direct construction?
    /// `__mkMap({key=1,value=10})` already does this — baseline.</summary>
    [Test]
    public void StructLiteralIntoMap_DirectBuild_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.count()");
        rt.Run();
        Assert.AreEqual(2, System.Convert.ToInt32(rt["out"].Value));
    }

    /// <summary>Pair-struct from Map iteration: is `i` typed as a TIC struct
    /// the same way a literal `{key=1,value=10}` is? Probe via cross-pollination.</summary>
    [Test]
    public void Probe_MapIter_LiteralStruct_Cross() {
        // If `for i in m` types `i` as `struct{key:int, value:int}`, then
        // returning i and consuming it as a literal struct should compose.
        var rt = Funny.Hardcore.BuildLang(@"
fun first(xs):
    for i in xs: return i
    return {key=0,value=0}

m = __mkMap({key=1,value=10})
p = first(m)
out = p.key + p.value");
        rt.Run();
        Assert.AreEqual(11, System.Convert.ToInt32(rt["out"].Value));
    }

    /// <summary>Reverse: list of pair structs typed as Map?
    /// Under Option A with `Map = SC(Map, struct)`, the element shape `struct{key,value}`
    /// could erroneously LCA a `list&lt;{key,value}&gt;` and `map&lt;K,V&gt;` to something
    /// non-Any. Probe.</summary>
    [Test]
    public void Probe_LCA_ListOfPairs_vs_Map_CollapsesToAny() {
        // LCA(list<pair>, map) must collapse to Any (different ConstructorKinds).
        // Then count() rejects because Any isn't Enumerable<T>.
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.BuildLang(@"
list1 = [{key=1,value=10},{key=2,value=20}]
map1 = __mkMap({key=1,value=10},{key=2,value=20})
mix = if(true) list1 else map1
out = mix.count()"));
    }

    // ────────────────────────────────────────────────────────────────
    // §3 — Set of structs vs Map (same element shape, different kind)
    // ────────────────────────────────────────────────────────────────

    /// <summary>Set's element shape can be `{key, value}` struct too. Probe whether
    /// the kind discriminator (Set vs Map) is enforced by TIC despite identical
    /// element-struct.</summary>
    [Test]
    public void Probe_LCA_SetOfPairs_vs_Map_CollapsesToAny() {
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.BuildLang(@"
s = set({key=1,value=10},{key=2,value=20})
m = __mkMap({key=1,value=10},{key=2,value=20})
out = if(true) s else m"));
    }

    // ────────────────────────────────────────────────────────────────
    // §4 — Generic function over Map: K/V binding correctness
    // ────────────────────────────────────────────────────────────────

    /// <summary>`m.get(k)` — does K bind correctly from the user-side argument
    /// regardless of representation?</summary>
    [Test]
    public void MapGet_KeyTypeBinding_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key='hello', value=42}, {key='world', value=99})\nout = m.get('hello') ?? -1");
        rt.Run();
        Assert.AreEqual(42, System.Convert.ToInt32(rt["out"].Value));
    }

    /// <summary>Generic user function over Map — receive Map, return its element type.
    /// This forces TIC to bind the K and V (or element struct) generic positions.</summary>
    [Test]
    public void Probe_GenericUserFn_OverMap_ReturnsValue() {
        var rt = Funny.Hardcore.BuildLang(@"
fun foo(m):
    return m.get(1) ?? 'oops'

m = __mkMap({key=1,value='hello'},{key=2,value='world'})
out = foo(m)");
        rt.Run();
        Assert.AreEqual("hello", rt["out"].Value);
    }

    /// <summary>Generic user function: receive Map, iterate, return sum of values.
    /// Probes the `for in` over a Map flowing through a generic parameter.</summary>
    [Test]
    public void Probe_GenericUserFn_OverMap_ForLoop_SumValues() {
        var rt = Funny.Hardcore.BuildLang(@"
fun foo(xs):
    s = 0
    for i in xs: s = s + i.value
    return s

m = __mkMap({key=1,value=10},{key=2,value=20})
out = foo(m)");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // §5 — Recursive Map types (cycle-guard interactions)
    // ────────────────────────────────────────────────────────────────

    /// <summary>Nested map — `map<int, map<text, int>>`. Stresses cycle guards
    /// during Lca/Unify and structural printing. Option A's per-call fresh
    /// struct nodes could fragment cycle detection.</summary>
    [Test]
    public void Probe_NestedMap_Iteration() {
        var rt = Funny.Hardcore.BuildLang(@"
inner1 = __mkMap({key='a',value=1})
inner2 = __mkMap({key='b',value=2})
outer = __mkMap({key=1,value=inner1},{key=2,value=inner2})
out = outer.count()");
        rt.Run();
        Assert.AreEqual(2, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // §6 — Named pair-struct vs synthesised pair-struct
    // ────────────────────────────────────────────────────────────────

    /// <summary>If user declares a NAMED struct `type entry = {key:int, value:int}`
    /// matching the synthesised pair-struct shape, does iteration assign `entry` to
    /// the iterator, or stay anonymous? Under Option A, anonymous synth-struct
    /// may collide with named ones via width-subtyping.</summary>
    [Test]
    public void Probe_NamedPairStruct_VsMapIteration() {
        var rt = Funny.Hardcore.BuildLang(@"
type entry = {key:int, value:int}
m = __mkMap({key=1,value=10},{key=2,value=20})

fun first(xs):
    for i in xs: return i
    return {key=0,value=0}

e = first(m)
out = e.key + e.value");
        rt.Run();
        Assert.AreEqual(11, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // §7 — Map keys/values projections
    // ────────────────────────────────────────────────────────────────

    /// <summary>Map keys via `.map(rule it.key).sum()` — Enumerable projection
    /// + struct field access. Under current code this works (typeclass-cross path
    /// synthesises pair-struct).</summary>
    [Test]
    public void Probe_MapKeysViaMapField_Sum() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.map(rule it.key).sum()");
        rt.Run();
        Assert.AreEqual(3, System.Convert.ToInt32(rt["out"].Value));
    }

    /// <summary>Map values via `.map(rule it.value).sum()` — works on current code,
    /// baseline confirmation.</summary>
    [Test]
    public void MapValuesViaMapField_Sum() {
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1,value=10},{key=2,value=20})\nout = m.map(rule it.value).sum()");
        rt.Run();
        Assert.AreEqual(30, System.Convert.ToInt32(rt["out"].Value));
    }

    // ────────────────────────────────────────────────────────────────
    // §8 — Map element used as Comparable (typeclass intersection)
    // ────────────────────────────────────────────────────────────────

    /// <summary>Can the pair-struct element flow into a Comparable typeclass
    /// slot? Pair-structs are NOT Comparable (struct is not Comparable per
    /// Stage 0). Reject expected at compile time — closed by the
    /// `SimplifyOrNull` gate added to `MergeOrNull` + `Push.Apply(CS,CS)` that
    /// validates trans-axis typeclass × Descendant consistency on every
    /// CS-update path (not just Pull's `ApplyAncestorConstrains`).</summary>
    [Test]
    public void Probe_MapValues_Sort_RequiresComparable() {
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.BuildLang(@"
m = __mkMap({key=1,value=10})
fun sorted(xs):
    return xs.sort()
out = sorted(m).count()"));
    }
}
