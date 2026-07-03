namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Unit tests for Stage C.2 Layer-0 algebra on StateCompositeConstraints:
/// LCA / GCD / Unify / Concretest / Abstractest cross-rules per
/// Specs/Tic/Algebra_CompositeConstraints.md v6.1 §3.
/// </summary>
public class StateCompositeConstraintsAlgebraTest {

    private static TicNode Elem() => TicNode.CreateInvisibleNode(ConstraintsState.Empty);
    private static TicNode ElemOf(StatePrimitive prim) => TicNode.CreateInvisibleNode(prim);

    private static StateCompositeConstraints Cc(
        TicNode elem,
        ConstructorKind? anc = null,
        ConstructorKind? desc = null,
        bool isOptional = false) =>
        StateCompositeConstraints.Create(elem, ancestor: anc, descendant: desc, isOptional: isOptional);

    #region LCA — same-class

    [Test]
    public void Lca_SameClass_BothEmpty_ReturnsEmptyCompCS() {
        var a = Cc(Elem());
        var b = Cc(Elem());
        var r = a.Lca(b);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.IsFalse(cc.HasAncestor);
        Assert.IsFalse(cc.HasDescendant);
    }

    [Test]
    public void Lca_SameClass_NullDesc_IsIdentity() {
        // CompCS{Anc=enumerable} ∨ CompCS{Desc=list} = CompCS{Anc=enumerable, Desc=list}
        // (null on each side is identity → other side survives)
        var a = Cc(Elem(), anc: ConstructorKind.Enumerable);
        var b = Cc(Elem(), desc: ConstructorKind.List);
        var r = a.Lca(b);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.AreEqual(ConstructorKind.Enumerable, cc.Ancestor);
        Assert.AreEqual(ConstructorKind.List, cc.Descendant);
    }

    [Test]
    public void Lca_SameClass_BothDesc_LcaOfFloors() {
        // CompCS{Desc=list} ∨ CompCS{Desc=set} → Desc=enumerable (join in lattice)
        var a = Cc(Elem(), desc: ConstructorKind.List);
        var b = Cc(Elem(), desc: ConstructorKind.Set);
        var r = a.Lca(b);
        var cc = (StateCompositeConstraints)r;
        Assert.AreEqual(ConstructorKind.Enumerable, cc.Descendant);
    }

    [Test]
    public void Lca_SameClass_IsOptionalOr() {
        var a = Cc(Elem(), isOptional: true);
        var b = Cc(Elem(), isOptional: false);
        var r = a.Lca(b);
        var cc = (StateCompositeConstraints)r;
        Assert.IsTrue(cc.IsOptional);
    }

    [Test]
    public void Lca_SameClass_Symmetric() {
        var a = Cc(Elem(), desc: ConstructorKind.List, anc: ConstructorKind.Enumerable);
        var b = Cc(Elem(), desc: ConstructorKind.Set);
        var r1 = a.Lca(b);
        var r2 = b.Lca(a);
        Assert.AreEqual(r1.ToString(), r2.ToString());
    }

    #endregion

    #region LCA — cross-class

    [Test]
    public void Lca_CrossSC_WidensToContainPoint() {
        // CompCS{Anc=enumerable} ∨ StateCollection(List) → CompCS{Desc=list, Anc=enumerable}
        // (Point K becomes floor via identity; cap stays widened to contain K)
        var elem = Elem();
        var a = Cc(elem, anc: ConstructorKind.Enumerable);
        var sc = new StateCollection(ConstructorKind.List, elem);
        var r = a.Lca(sc);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.AreEqual(ConstructorKind.List, cc.Descendant);
        Assert.AreEqual(ConstructorKind.Enumerable, cc.Ancestor);
    }

    [Test]
    public void Lca_CrossPrim_Any_AlsoAny() {
        var a = Cc(Elem());
        var r = a.Lca(Any);
        Assert.AreEqual(Any, r);
    }

    [Test]
    public void Lca_CrossPrim_NonAny_IsAny() {
        var a = Cc(Elem(), desc: ConstructorKind.List);
        var r = a.Lca(I32);
        Assert.AreEqual(Any, r);
    }

    [Test]
    public void Lca_CompCS_CompCSSymmetricViaMainEntrypoint() {
        // Verifies the main Lca entrypoint routes both directions through CompCS.
        var a = Cc(Elem(), anc: ConstructorKind.Enumerable);
        var b = new StateCollection(ConstructorKind.Array, Elem());
        ITicNodeState aa = a;
        ITicNodeState bb = b;
        var r1 = aa.Lca(bb);
        var r2 = bb.Lca(aa);
        Assert.AreEqual(r1.ToString(), r2.ToString());
    }

    #endregion

    #region GCD — cross-class with collapse

    [Test]
    public void Gcd_CrossSC_PointInInterval_CollapsesToSC() {
        // CompCS{Desc=list, Anc=enumerable} ∧ StateCollection(Array) → SC(Array)
        // (Array fits in [list..enumerable] since list ⊆ array ⊆ enumerable)
        var elem = Elem();
        var a = Cc(elem, desc: ConstructorKind.List, anc: ConstructorKind.Enumerable);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var r = a.Gcd(sc);
        Assert.IsInstanceOf<StateCollection>(r);
        var resultSc = (StateCollection)r;
        Assert.AreEqual(ConstructorKind.Array, resultSc.Constructor);
    }

    [Test]
    public void Gcd_CrossSC_PointAboveCap_ReturnsNull() {
        // CompCS{Anc=list} ∧ StateCollection(Array) → null (Array > list)
        var a = Cc(Elem(), anc: ConstructorKind.List);
        var sc = new StateCollection(ConstructorKind.Array, Elem());
        var r = a.Gcd(sc);
        Assert.IsNull(r);
    }

    [Test]
    public void Gcd_CrossSC_PointBelowFloor_ReturnsNull() {
        // CompCS{Desc=array} ∧ StateCollection(Enumerable) → impossible (enumerable not subtype of array)
        // Actually Enumerable can't be SC because it's constraint-only. Use list vs set instead.
        // CompCS{Desc=set} ∧ StateCollection(List) → null (different branches)
        var a = Cc(Elem(), desc: ConstructorKind.Set);
        var sc = new StateCollection(ConstructorKind.List, Elem());
        var r = a.Gcd(sc);
        Assert.IsNull(r);
    }

    [Test]
    public void Gcd_CrossPrim_Any_ReturnsA() {
        var a = Cc(Elem(), anc: ConstructorKind.Enumerable);
        var r = a.Gcd(Any);
        Assert.AreSame(a, r);
    }

    [Test]
    public void Gcd_CrossPrim_NonAny_ReturnsNull() {
        var a = Cc(Elem());
        var r = a.Gcd(I32);
        Assert.IsNull(r);
    }

    [Test]
    public void Gcd_CrossStateArray_NarrowerWinsRule() {
        // CompCS{Anc=list} ∧ StateArray(elem) → StateCollection(List, elem)
        // (Narrower-wins: list ⊂ array, so list survives — fix for v6 R1 regression)
        var elem = ElemOf(I32);
        var a = Cc(elem, anc: ConstructorKind.List);
        var sa = StateArray.Of(I32);
        var r = a.Gcd(sa);
        Assert.IsInstanceOf<StateCollection>(r);
        var sc = (StateCollection)r;
        Assert.AreEqual(ConstructorKind.List, sc.Constructor);
    }

    [Test]
    public void Gcd_CrossStateArray_NoCap_FallsBackToStateArray() {
        // Unconstrained CompCS ∧ StateArray → StateArray (legacy default)
        var a = Cc(Elem());
        var sa = StateArray.Of(I32);
        var r = a.Gcd(sa);
        Assert.IsInstanceOf<StateArray>(r);
    }

    #endregion

    #region Unify — same-class

    [Test]
    public void Unify_SameClass_IdentityFormula() {
        // CompCS{Desc=list, Anc=enumerable} ⊓ CompCS{Desc=set} →
        //   Desc = LCA(list, set) = enumerable
        //   Anc = GCD(enumerable, null) = enumerable
        //   Result: CompCS{Desc=enumerable, Anc=enumerable} → collapse to SC(Enumerable)?
        // Wait — Enumerable is constraint-only (cannot be SC).
        // Actually TryCollapseToPoint creates SC(Enumerable, e) which is forbidden by SC ctor!
        // This is a known case: collapse to a constraint-only kind would throw.
        //
        // Let's pick examples that don't hit constraint-only kinds.
        // CompCS{Desc=list} ⊓ CompCS{Desc=array, Anc=fixedArray} →
        //   Desc = LCA(list, array) = array
        //   Anc = GCD(null, fixedArray) = fixedArray
        //   Result: CompCS{Desc=array, Anc=fixedArray}
        var a = Cc(Elem(), desc: ConstructorKind.List);
        var b = Cc(Elem(), desc: ConstructorKind.Array, anc: ConstructorKind.FixedArray);
        var r = a.UnifyOrNull(b);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.AreEqual(ConstructorKind.Array, cc.Descendant);
        Assert.AreEqual(ConstructorKind.FixedArray, cc.Ancestor);
    }

    [Test]
    public void Unify_SameClass_DisjointBranches_ReturnsNull() {
        // CompCS{Anc=list} ⊓ CompCS{Anc=set} → GCD(list, set) = null → reject
        var a = Cc(Elem(), anc: ConstructorKind.List);
        var b = Cc(Elem(), anc: ConstructorKind.Set);
        var r = a.UnifyOrNull(b);
        Assert.IsNull(r);
    }

    [Test]
    public void Unify_SameClass_CollapsesToPoint() {
        // CompCS{Desc=list, Anc=array} ⊓ CompCS{Anc=list} →
        //   Desc = list, Anc = GCD(array, list) = list
        //   Point: collapse to SC(List)
        var elem = ElemOf(I32);
        var a = Cc(elem, desc: ConstructorKind.List, anc: ConstructorKind.Array);
        var b = Cc(elem, anc: ConstructorKind.List);
        var r = a.UnifyOrNull(b);
        Assert.IsInstanceOf<StateCollection>(r);
        var sc = (StateCollection)r;
        Assert.AreEqual(ConstructorKind.List, sc.Constructor);
    }

    [Test]
    public void Unify_SameClass_EmptyInterval_ReturnsNull() {
        // CompCS{Desc=array} ⊓ CompCS{Anc=list} →
        //   Desc=array, Anc=list. IsSubtypeOf(array, list)? array is parent of list → false → reject
        var a = Cc(Elem(), desc: ConstructorKind.Array);
        var b = Cc(Elem(), anc: ConstructorKind.List);
        var r = a.UnifyOrNull(b);
        Assert.IsNull(r);
    }

    #endregion

    #region Concretest

    [Test]
    public void Concretest_WithDescendant_PicksFloor() {
        var elem = ElemOf(I32);
        var a = Cc(elem, desc: ConstructorKind.List, anc: ConstructorKind.Enumerable);
        var r = a.Concretest();
        Assert.IsInstanceOf<StateCollection>(r);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)r).Constructor);
    }

    [Test]
    public void Concretest_OnlyAncestor_DescendsThroughLattice() {
        // CompCS{Anc=enumerable} → Concretest(enumerable) = list (per ConstructorLattice.cs:127)
        var elem = ElemOf(I32);
        var a = Cc(elem, anc: ConstructorKind.Enumerable);
        var r = a.Concretest();
        Assert.IsInstanceOf<StateCollection>(r);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)r).Constructor);
    }

    [Test]
    public void Concretest_Unresolvable_ReturnsSelf() {
        // CompCS{} — no Desc, no Anc → residual (dialect resolves)
        var a = Cc(Elem());
        var r = a.Concretest();
        Assert.AreSame(a, r);
    }

    [Test]
    public void Concretest_OptionalFlag_WrapsResult() {
        var elem = ElemOf(I32);
        var a = Cc(elem, desc: ConstructorKind.Array, isOptional: true);
        var r = a.Concretest();
        Assert.IsInstanceOf<StateOptional>(r);
        var opt = (StateOptional)r;
        Assert.IsInstanceOf<StateCollection>(opt.Element);
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)opt.Element).Constructor);
    }

    [Test]
    public void Concretest_AnyAncestor_StaysResidual() {
        // CompCS{Anc=Any} - Concretest doesn't descend through Any (per spec §3.4).
        var a = Cc(Elem(), anc: ConstructorKind.Any);
        var r = a.Concretest();
        Assert.AreSame(a, r);
    }

    #endregion

    #region Abstractest

    [Test]
    public void Abstractest_DropsFloor_KeepsCap() {
        var elem = ElemOf(I32);
        var a = Cc(elem, desc: ConstructorKind.List, anc: ConstructorKind.Enumerable);
        var r = a.Abstractest();
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.IsFalse(cc.HasDescendant);
        Assert.AreEqual(ConstructorKind.Enumerable, cc.Ancestor);
    }

    [Test]
    public void Abstractest_PreservesIsOptional() {
        var a = Cc(Elem(), desc: ConstructorKind.List, isOptional: true);
        var r = a.Abstractest();
        var cc = (StateCompositeConstraints)r;
        Assert.IsTrue(cc.IsOptional);
        Assert.IsFalse(cc.HasDescendant);
    }

    [Test]
    public void Abstractest_NeverRejects() {
        // Widening cannot create contradiction.
        var a = Cc(Elem());
        var r = a.Abstractest();
        Assert.IsNotNull(r);
    }

    #endregion

    #region Cycle guards — basic smoke

    [Test]
    public void Lca_SameInstance_DoesNotInfinite() {
        var elem = Elem();
        var a = Cc(elem, anc: ConstructorKind.Enumerable);
        // Same-instance LCA should not infinitely recurse.
        var r = a.Lca(a);
        Assert.IsNotNull(r);
    }

    [Test]
    public void Gcd_SameInstance_DoesNotInfinite() {
        var elem = Elem();
        var a = Cc(elem, anc: ConstructorKind.Enumerable, desc: ConstructorKind.List);
        var r = a.Gcd(a);
        Assert.IsNotNull(r);
    }

    #endregion
}
