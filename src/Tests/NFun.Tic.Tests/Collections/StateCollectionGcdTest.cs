using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static NFun.Tic.SolvingStates.StatePrimitive;

namespace NFun.Tic.Tests.Collections;

/// <summary>
/// Gcd over unified single-arg invariant collections:
///   Gcd(SC(Ka, ea), SC(Kb, eb)) = SC(KindGcd(Ka, Kb), Unify(ea, eb))
/// Kind meets via the ConstructorLattice; elements are INVARIANT, so they unify
/// (same discipline as the Unify SC arm and GcdStructFields invariant fields).
/// Regression: this pair used to fall through to `throw NotSupportedException`
/// while Unify had its SC arm — the meet of two lang-collection upper bounds
/// (shared arg caps) crashed instead of computing.
/// </summary>
public class StateCollectionGcdTest {

    private static StateCollection Coll(ConstructorKind kind, ITicNodeState elem)
        => StateCollection.Of(kind, elem);

    [Test]
    public void SameKind_SameElement_ReturnsSame() {
        var r = Coll(ConstructorKind.List, I32).Gcd(Coll(ConstructorKind.List, I32));
        Assert.IsInstanceOf<StateCollection>(r, $"got {r}");
        var sc = (StateCollection)r;
        Assert.AreEqual(ConstructorKind.List, sc.Constructor);
        Assert.AreEqual(I32, sc.Element);
    }

    [Test]
    public void SameKind_IncompatibleElements_ReturnsNull() {
        var r = Coll(ConstructorKind.Set, I32).Gcd(Coll(ConstructorKind.Set, Bool));
        Assert.IsNull(r, $"meet of set<I32> and set<Bool> must not exist, got {r}");
    }

    [Test]
    public void CrossKind_ListVsArray_MeetsToList() {
        // list ⊑ array in the lattice ⇒ meet = list.
        var r = Coll(ConstructorKind.List, I32).Gcd(Coll(ConstructorKind.Array, I32));
        Assert.IsInstanceOf<StateCollection>(r, $"got {r}");
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)r).Constructor);
    }

    [Test]
    public void CrossKind_DisjointBranches_ReturnsNull() {
        // set and list live in disjoint lattice sub-trees.
        var r = Coll(ConstructorKind.Set, I32).Gcd(Coll(ConstructorKind.List, I32));
        Assert.IsNull(r, $"got {r}");
    }

    [Test]
    public void SameKind_UnresolvedElements_Unify() {
        // Invariant elements: CS[U8..] ⊓ CS[..Re] unify to one interval.
        var a = Coll(ConstructorKind.List, ConstraintsState.Of(U8));
        var b = Coll(ConstructorKind.List, ConstraintsState.Of(anc: Real));
        var r = a.Gcd(b);
        Assert.IsInstanceOf<StateCollection>(r, $"got {r}");
        var elem = ((StateCollection)r).Element;
        Assert.IsInstanceOf<ConstraintsState>(elem, $"got {elem}");
        var cs = (ConstraintsState)elem;
        Assert.AreEqual(U8, cs.Descendant);
        Assert.AreEqual(Real, cs.Ancestor);
    }
}
