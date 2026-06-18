using NFun.Tic;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

/// <summary>
/// Algebra-level unit tests pinning the Stage 1 uniform-invariance pin
/// in <see cref="StateCollection.LcaOrShareIdentity"/>.
///
/// These probe the pure-algebra surface (no GraphBuilder, no Solve). The
/// goal is to make the invariance-pin behavior EXPLICIT at the lowest
/// level so a future refactor of <c>LcaOrShareIdentity</c> sees these
/// pins fail (rather than only catching the regression at the Syntax
/// level much later).
///
/// Surface tests for the same family live in
/// <c>NFun.SyntaxTests/Stage1InvariancePinTests.cs</c>.
///
/// ## What's pinned
///
/// - <b>1D primitive elements</b> — same-kind merges, cross-kind narrows
///   via <c>LcaOrShareIdentity</c>'s non-composite branch
///   (<c>StateCollection.cs:223-234</c>). Works today; pin guards the
///   working baseline.
///
/// - <b>2D nested composite elements</b> (cross-kind outer) — returns
///   null per the Stage 1 guard at <c>StateCollection.cs:227-228</c>
///   (<c>Element is not ICompositeState</c>). The caller's downstream
///   Lca then widens to <c>Any</c>. Pinned as [Ignore]'d — when the
///   guard is properly lifted (recursive LCA primitive), these tests
///   un-ignore and assert the algebraic answer.
/// </summary>
public class Stage1InvariancePinAlgebraTests {

    // ────────────────────────────────────────────────────────────────
    // Baselines — what works today
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Lca_SameKindList_PrimitiveElement_Works() {
        var listA = StateCollection.OfList(I32);
        var listB = StateCollection.OfList(I32);
        var lca = listA.GetLastCommonAncestorOrNull(listB);
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)lca).Constructor);
    }

    [Test]
    public void Lca_CrossKind_ListVsMutArr_PrimitiveElement_Works() {
        // list ≤ mutArr per ConstructorLattice — widens to mutArr.
        var list = StateCollection.OfList(I32);
        var mutArr = StateCollection.OfMutableArray(I32);
        var lca = list.Lca(mutArr);
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)lca).Constructor);
    }

    [Test]
    public void Lca_SameKindList_SameNestedCollection_Works() {
        // list<list<I32>> vs list<list<I32>>: same-kind both levels.
        // Resolved-element LCA via GetLastCommonAncestorOrNull recursion.
        var innerA = StateCollection.OfList(I32);
        var innerB = StateCollection.OfList(I32);
        var outerA = StateCollection.OfList(innerA);
        var outerB = StateCollection.OfList(innerB);
        var lca = outerA.GetLastCommonAncestorOrNull(outerB);
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)lca).Constructor);
    }

    // ────────────────────────────────────────────────────────────────
    // [Ignore]'d — Stage 1 invariance pin surfaces at the algebra
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// LCA(list&lt;list&lt;I32&gt;&gt;, mutArr&lt;mutArr&lt;I32&gt;&gt;): cross-kind outer +
    /// nested composite element. Algebraic answer is
    /// <c>mutArr&lt;mutArr&lt;I32&gt;&gt;</c> by recursive cross-kind lattice-upward.
    /// Today: returns null (Stage 1 guard at line 227-228 blocks
    /// composite elements), and the caller widens to <c>Any</c>.
    /// </summary>
    [Test, Ignore("Debt #10 (worklist Pull): pure-algebra LCA returns null when both sides have non-CS element nodes; the closed surface goes through TIC where the literal carries a CS element. Re-firing post-CS-resolution closes the residual.")]
    public void Lca_CrossKind_NestedComposite_BothResolved_ShouldWiden() {
        // Both elements are concrete StateCollection — same-shape composite.
        var listOfList = StateCollection.OfList(StateCollection.OfList(I32));
        var arrOfArr = StateCollection.OfMutableArray(StateCollection.OfMutableArray(I32));
        var lca = listOfList.Lca(arrOfArr);
        Assert.IsNotNull(lca, "Expected mutArr<mutArr<I32>> by recursive cross-kind widening");
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)lca).Constructor);
    }

    /// <summary>
    /// Same shape at 3 levels deep. Bounded depth — path (b) rejects deeper
    /// nesting to avoid downstream FU758. Residual of debt #17.
    /// </summary>
    [Test, Ignore("Debt #10 (worklist Pull): 3D cross-kind LCA bounded — path (b) rejects deeper nesting; re-firing closes after deep CS resolution")]
    public void Lca_CrossKind_NestedComposite_3D_ShouldWiden() {
        var ll3 = StateCollection.OfList(StateCollection.OfList(StateCollection.OfList(I32)));
        var aa3 = StateCollection.OfMutableArray(StateCollection.OfMutableArray(StateCollection.OfMutableArray(I32)));
        var lca = ll3.Lca(aa3);
        Assert.IsNotNull(lca);
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)lca).Constructor);
    }

    /// <summary>
    /// Asymmetric: outer same-kind, inner cross-kind. Today the inner
    /// recursion fires <c>GetLastCommonAncestorOrNull</c>'s composite
    /// branch at <c>StateCollection.cs:184-188</c> which handles the
    /// resolved-element case correctly — so this MAY already pass.
    /// Pin to discover.
    /// </summary>
    [Test]
    public void Lca_SameKindOuter_CrossKindInner_AlreadyWorks() {
        // list<list<I32>> vs list<mutArr<I32>>: outer same (list), inner cross.
        var listOfList = StateCollection.OfList(StateCollection.OfList(I32));
        var listOfArr = StateCollection.OfList(StateCollection.OfMutableArray(I32));
        var lca = listOfList.GetLastCommonAncestorOrNull(listOfArr);
        // GetLastCommonAncestorOrNull recurses into the composite-element branch
        // and returns list<mutArr<I32>> by widening inner kind. Pin the working
        // path so a future LcaOrShareIdentity refactor doesn't accidentally
        // break the recursion that already works.
        Assert.IsNotNull(lca);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)lca).Constructor);
    }
}
