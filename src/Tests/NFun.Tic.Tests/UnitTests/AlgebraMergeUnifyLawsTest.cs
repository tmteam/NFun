namespace NFun.Tic.Tests.UnitTests;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Law tests for the algebra fixes of debt items:
///  #13 — single ⊓: Unify(CS,CS) ≝ Merge (Algebra_Merge.md M1 / Algebra_Unify.md U1),
///  #14 — commutative Preferred transport (Algebra_Merge.md M2),
/// plus axis-survival through unification (the old dropping of opt/Preferred in
/// UnifyOrNull(CS,CS) was the bug) and the associativity law of ⊓.
/// Style follows AlgebraGcdFitLawsTest.
/// </summary>
public class AlgebraMergeUnifyLawsTest {

    private static ConstraintsState Cs(
        ITicNodeState desc = null, StatePrimitive anc = null,
        bool cmp = false, bool opt = false, StatePrimitive preferred = null) {
        var cs = ConstraintsState.Of(desc, anc, cmp, opt);
        cs.Preferred = preferred;
        return cs;
    }

    // Canonical CS shapes — the C fragment sample. Degenerate points [T..T] are omitted:
    // live states are canonicalized eagerly by Pull/Push (SimplifyOrNull collapses points),
    // so the laws are stated over canonical representatives.
    private static IEnumerable<ConstraintsState> CsShapes() {
        yield return Cs();                                        // [∅..∅] — neutral
        yield return Cs(desc: U8);                                // [U8..∅]
        yield return Cs(desc: Char);                              // [Char..∅]
        yield return Cs(anc: Real);                               // [∅..Real]
        yield return Cs(anc: I64);                                // [∅..I64]
        yield return Cs(anc: Bool);                               // [∅..Bool]
        yield return Cs(desc: U8, anc: Real);                     // [U8..Real]
        yield return Cs(desc: I16, anc: I64);                     // [I16..I64]
        yield return Cs(cmp: true);                               // comparable
        yield return Cs(desc: U8, anc: Real, cmp: true);          // bounded + cmp
        yield return Cs(opt: true);                               // optional flag form
        yield return Cs(desc: U8, opt: true);                     // [U8..∅]?
        yield return Cs(desc: I16, preferred: I32);               // hint, one-sided fits
        yield return Cs(desc: U8, anc: Real, preferred: I32);     // bounded + hint I32
        yield return Cs(desc: U8, anc: Real, preferred: I64);     // bounded + hint I64
        yield return Cs(desc: U8, anc: I64, preferred: Real);     // hint does not fit anc
        yield return Cs(desc: U8, opt: true, preferred: I32);     // opt + hint
        yield return Cs(desc: StateArray.Of(I32));                // solved composite desc
        yield return Cs(desc: StateStruct.Of("a", I32));          // solved struct desc
        yield return Cs(desc: StateOptional.Of(I32));             // solved optional desc
    }

    private static ITicNodeState UnifyFold(ITicNodeState x, ITicNodeState y) =>
        x == null || y == null ? null : x.UnifyOrNull(y);

    private static bool StatesEqual(ITicNodeState a, ITicNodeState b) =>
        a == null ? b == null : b != null && a.Equals(b);

    private static string Print(ITicNodeState s) => s == null ? "null" : s.ToString();

    // ================================================================
    // #14 — Merge commutativity INCLUDING the Preferred axis
    // ================================================================

    [Test]
    public void Merge_Commutative_IncludingPreferredAxis() {
        // C₁ ⊓ C₂ = C₂ ⊓ C₁ — full state equality, hint included
        // (ConstraintsState.Equals compares Preferred).
        var shapes = CsShapes().ToArray();
        var failures = new List<string>();
        foreach (var a in shapes)
        foreach (var b in shapes)
        {
            var ab = a.MergeOrNull(b);
            var ba = b.MergeOrNull(a);
            if (!StatesEqual(ab, ba))
                failures.Add($"a={a}, b={b}: a⊓b={Print(ab)} but b⊓a={Print(ba)}");
        }
        Assert.IsEmpty(failures, string.Join("\n", failures));
    }

    [Test]
    public void Merge_PreferredHints_Equal_Kept_BothOrders() {
        var a = Cs(desc: U8, anc: Real, preferred: I32);
        var b = Cs(desc: I16, anc: I64, preferred: I32);
        Assert.AreEqual(I32, (a.MergeOrNull(b) as ConstraintsState)?.Preferred);
        Assert.AreEqual(I32, (b.MergeOrNull(a) as ConstraintsState)?.Preferred);
    }

    [Test]
    public void Merge_PreferredHints_OneSided_Kept_BothOrders() {
        var a = Cs(desc: U8, anc: Real, preferred: I32);
        var b = Cs(desc: I16, anc: I64);
        Assert.AreEqual(I32, (a.MergeOrNull(b) as ConstraintsState)?.Preferred);
        Assert.AreEqual(I32, (b.MergeOrNull(a) as ConstraintsState)?.Preferred);
    }

    [Test]
    public void Merge_PreferredHints_DifferFitting_LcaKept_BothOrders() {
        // P₁ ≠ P₂, both set → P = LCA(P₁,P₂) = I64, which fits [U8..Real] → kept.
        // Before #14 the two implementations disagreed: IntersectIntervals kept the
        // RECEIVER's hint, MergeOrNull kept the ARGUMENT's — order-dependent results.
        var a = Cs(desc: U8, anc: Real, preferred: I32);
        var b = Cs(desc: U8, anc: Real, preferred: I64);
        Assert.AreEqual(I64, (a.MergeOrNull(b) as ConstraintsState)?.Preferred);
        Assert.AreEqual(I64, (b.MergeOrNull(a) as ConstraintsState)?.Preferred);
    }

    [Test]
    public void Merge_PreferredHints_DifferUnfitting_Dropped_BothOrders() {
        // LCA(I32, Real) = Real does not fit the merged interval [U8..I64] → hint dropped.
        var a = Cs(desc: U8, anc: I64, preferred: I32);
        var b = Cs(desc: U8, anc: I64, preferred: Real);
        var ab = a.MergeOrNull(b) as ConstraintsState;
        var ba = b.MergeOrNull(a) as ConstraintsState;
        Assert.IsNotNull(ab);
        Assert.IsNotNull(ba);
        Assert.IsNull(ab.Preferred);
        Assert.IsNull(ba.Preferred);
    }

    [Test]
    public void Merge_PreferredHints_UnrelatedFamilies_Dropped_BothOrders() {
        // LCA(Char, I32) = Any — the hints share no information → dropped.
        var a = Cs(preferred: Char);
        var b = Cs(preferred: I32);
        var ab = a.MergeOrNull(b) as ConstraintsState;
        var ba = b.MergeOrNull(a) as ConstraintsState;
        Assert.IsNotNull(ab);
        Assert.IsNotNull(ba);
        Assert.IsNull(ab.Preferred);
        Assert.IsNull(ba.Preferred);
    }

    // ================================================================
    // #13 — Unify(CS,CS) ≝ Merge
    // ================================================================

    [Test]
    public void Unify_OnConstraints_EqualsMerge_Matrix() {
        // For every CS pair, UnifyOrNull and MergeOrNull are the SAME operator.
        // Equal pairs are skipped: the Unify dispatcher's a.Equals(b) fast-path returns
        // the receiver uncanonicalized by design (idempotency law, AlgebraInvariantsTest).
        var shapes = CsShapes().ToArray();
        var failures = new List<string>();
        foreach (var a in shapes)
        foreach (var b in shapes)
        {
            if (a.Equals(b)) continue;
            var unified = ((ITicNodeState)a).UnifyOrNull(b);
            var merged = a.MergeOrNull(b);
            if (!StatesEqual(unified, merged))
                failures.Add($"a={a}, b={b}: Unify={Print(unified)} but Merge={Print(merged)}");
        }
        Assert.IsEmpty(failures, string.Join("\n", failures));
    }

    // ================================================================
    // #13 — axis survival through unification
    // (the old UnifyOrNull(CS,CS) dropped opt + Preferred — that was the bug)
    // ================================================================

    [Test]
    public void Unify_OptionalAxis_SurvivesUnification() {
        var r = ((ITicNodeState)Cs(desc: U8, opt: true)).UnifyOrNull(Cs(desc: I16)) as ConstraintsState;
        Assert.IsNotNull(r);
        Assert.IsTrue(r.IsOptional, $"opt axis must survive ⊓, got {r}");
        Assert.AreEqual(I16, r.Descendant);
    }

    [Test]
    public void Unify_Preferred_SurvivesUnification() {
        var r = ((ITicNodeState)Cs(desc: U8, anc: Real, preferred: I32))
            .UnifyOrNull(Cs(desc: I16, anc: I64)) as ConstraintsState;
        Assert.IsNotNull(r);
        Assert.AreEqual(I32, r.Preferred, $"Preferred must survive ⊓, got {r}");
    }

    [Test]
    public void Unify_OptionalAndPreferred_SurviveTogether() {
        var r = ((ITicNodeState)Cs(desc: U8, opt: true, preferred: I32))
            .UnifyOrNull(Cs(desc: I16)) as ConstraintsState;
        Assert.IsNotNull(r);
        Assert.IsTrue(r.IsOptional, $"opt axis must survive ⊓, got {r}");
        Assert.AreEqual(I32, r.Preferred, $"Preferred must survive ⊓, got {r}");
    }

    // ================================================================
    // Associativity: (C₁ ⊓ C₂) ⊓ C₃ = C₁ ⊓ (C₂ ⊓ C₃)
    // ================================================================

    [Test]
    [Ignore("REFUTED (2026-07-09, debt #22; re-analyzed at #16 closure). Root 2 of the original " +
            "analysis (T ⊓ CS cell rejecting the Optional lift — opt(I32) vs [U8..]) is FIXED by " +
            "the unified satisfaction predicate: the #16 change removed 64 counterexamples and " +
            "introduced none (sweep diff vs c6cd0edc). Two INHERENT roots remain (321 triples): " +
            "(1) Preferred drop-order — the hint rule (LCA then drop-if-unfit) is commutative " +
            "but not associative: a=[∅..∅], b=[U8..I64]Re!, c=[U8..Re]I32! gives " +
            "(a⊓b)⊓c=[U8..I64]I32! (Re dropped early, I32 survives) but " +
            "a⊓(b⊓c)=[U8..I64] (hint-LCA(Re,I32)=Re dropped, I32 info destroyed). " +
            "Metadata-only: the interval cores agree; the law holds only modulo hints. " +
            "(2) Sat-changing solved-composite collapse [D..∅] → D (D solved Struct/Optional): " +
            "the mid-fold exit from CS-land narrows Sat from {T ≥ D} to {D}, so a later " +
            "⊓ [P..] gives null on one side but LCA-widens to [Any..] on the other " +
            "(a=[∅..∅], b=[{a:I32}..], c=[U8..]). Resolution admixture in ⊓ — debt #19 " +
            "spirit; not fixable by the predicate. See Algebra_Merge.md §Законы.")]
    public void Merge_Associative_OverCsTriples() {
        // Fold via the full ⊓ (UnifyOrNull): a mid-fold collapse may leave CS-land,
        // so the second step goes through the T ⊓ CS cell.
        var shapes = CsShapes().ToArray();
        var failures = new List<string>();
        foreach (var a in shapes)
        foreach (var b in shapes)
        foreach (var c in shapes)
        {
            var left = UnifyFold(UnifyFold(a, b), c);
            var right = UnifyFold(a, UnifyFold(b, c));
            if (!StatesEqual(left, right))
                failures.Add($"a={a}, b={b}, c={c}: (a⊓b)⊓c={Print(left)} but a⊓(b⊓c)={Print(right)}");
        }
        Assert.IsEmpty(failures,
            string.Join("\n", failures.Take(20)) + $"\n... {failures.Count} failures total");
    }

    [Test]
    public void Merge_Associative_HintFree_NonCollapsing_OverCsTriples() {
        // The fragment WITHOUT the two inherent refutation sources IS associative:
        // no Preferred hints (root 1) and no collapsing descendants — solved Struct/Optional
        // descendants trigger the Sat-changing [D..∅] → D collapse (root 2 of the pin above).
        // Widened at #16 closure from the old primitive-interval fragment: solved ARRAY
        // descendants and all cmp/opt flag combinations are now covered (arrays do not
        // collapse, and the unified predicate carries the Optional lift in the T ⊓ CS cell).
        var shapes = CsShapes()
            .Where(s => s.Preferred == null && s.Descendant is not (StateStruct or StateOptional))
            .ToArray();
        var failures = new List<string>();
        foreach (var a in shapes)
        foreach (var b in shapes)
        foreach (var c in shapes)
        {
            var left = UnifyFold(UnifyFold(a, b), c);
            var right = UnifyFold(a, UnifyFold(b, c));
            if (!StatesEqual(left, right))
                failures.Add($"a={a}, b={b}, c={c}: (a⊓b)⊓c={Print(left)} but a⊓(b⊓c)={Print(right)}");
        }
        Assert.IsEmpty(failures, string.Join("\n", failures));
    }

    // ================================================================
    // #16 — the T ⊓ CS cell uses the authoritative satisfaction predicate
    // ================================================================

    [Test]
    public void Merge_TCsCell_OptionalLift_MidFoldCollapse_Associates() {
        // The exact root-2 counterexample from the associativity pin: the mid-fold collapse
        // [opt(I32)..∅] → opt(I32) routes the second step through the T ⊓ CS cell, which
        // must accept opt(I32) against [U8..∅] via the implicit lift U8 ≤ opt(U8) ≤ opt(I32).
        var a = Cs();
        var b = Cs(desc: StateOptional.Of(I32));
        var c = Cs(desc: U8);
        var left = UnifyFold(UnifyFold(a, b), c);
        var right = UnifyFold(a, UnifyFold(b, c));
        Assert.AreEqual(StateOptional.Of(I32), left, $"(a⊓b)⊓c = {Print(left)}");
        Assert.AreEqual(StateOptional.Of(I32), right, $"a⊓(b⊓c) = {Print(right)}");
    }
}
