using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Collections;

/// <summary>
/// Unit tests for the <see cref="ConstructorLattice"/> introduced in Stage 1
/// of the mutable collections feature.
///
/// Hierarchy under test:
/// <code>
///   Any
///    └── Enumerable
///         ├── FixedArray
///         │    └── Array
///         │         └── List
///         ├── Set
///         └── Map
/// </code>
/// </summary>
[TestFixture]
public class ConstructorLatticeTest {

    // ─── Lca ────────────────────────────────────────────────────────

    [TestCase(ConstructorKind.List,       ConstructorKind.List,       ConstructorKind.List)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.Array,      ConstructorKind.Array)]
    [TestCase(ConstructorKind.Set,        ConstructorKind.Set,        ConstructorKind.Set)]
    [TestCase(ConstructorKind.Map,        ConstructorKind.Map,        ConstructorKind.Map)]
    [TestCase(ConstructorKind.Enumerable, ConstructorKind.Enumerable, ConstructorKind.Enumerable)]
    public void Lca_SameConstructor_ReturnsItself(ConstructorKind a, ConstructorKind b, ConstructorKind expected)
        => Assert.AreEqual(expected, ConstructorLattice.Lca(a, b));

    [TestCase(ConstructorKind.List,       ConstructorKind.Array,      ConstructorKind.Array)]
    [TestCase(ConstructorKind.List,       ConstructorKind.FixedArray, ConstructorKind.FixedArray)]
    [TestCase(ConstructorKind.List,       ConstructorKind.Enumerable, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.FixedArray, ConstructorKind.FixedArray)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.Enumerable, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.FixedArray, ConstructorKind.Enumerable, ConstructorKind.Enumerable)]
    public void Lca_LinearChain_ReturnsCommonAncestor(ConstructorKind child, ConstructorKind ancestor, ConstructorKind expected)
        => Assert.AreEqual(expected, ConstructorLattice.Lca(child, ancestor));

    [TestCase(ConstructorKind.List, ConstructorKind.Set, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.List, ConstructorKind.Map, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.Set,  ConstructorKind.Map, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.Array, ConstructorKind.Set, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.Array, ConstructorKind.Map, ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.FixedArray, ConstructorKind.Set, ConstructorKind.Enumerable)]
    public void Lca_DivergentBranches_ClimbToEnumerable(ConstructorKind a, ConstructorKind b, ConstructorKind expected)
        => Assert.AreEqual(expected, ConstructorLattice.Lca(a, b));

    [TestCase(ConstructorKind.Any, ConstructorKind.List)]
    [TestCase(ConstructorKind.Any, ConstructorKind.Set)]
    [TestCase(ConstructorKind.Any, ConstructorKind.Enumerable)]
    public void Lca_AnyVsAnything_IsAny(ConstructorKind a, ConstructorKind b)
        => Assert.AreEqual(ConstructorKind.Any, ConstructorLattice.Lca(a, b));

    [Test]
    public void Lca_IsSymmetric() {
        // Sanity property: LCA must be commutative on this lattice.
        var kinds = new[] {
            ConstructorKind.Enumerable, ConstructorKind.FixedArray, ConstructorKind.Array,
            ConstructorKind.List, ConstructorKind.Set, ConstructorKind.Map, ConstructorKind.Any
        };
        foreach (var a in kinds)
            foreach (var b in kinds)
                Assert.AreEqual(ConstructorLattice.Lca(a, b), ConstructorLattice.Lca(b, a),
                    $"Lca({a}, {b}) must equal Lca({b}, {a})");
    }

    // ─── IsSubtypeOf ────────────────────────────────────────────────

    [TestCase(ConstructorKind.List,       ConstructorKind.Array,      true)]
    [TestCase(ConstructorKind.List,       ConstructorKind.FixedArray, true)]
    [TestCase(ConstructorKind.List,       ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.FixedArray, true)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.FixedArray, ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.Set,        ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.Map,        ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.List,       ConstructorKind.Any,        true)]
    [TestCase(ConstructorKind.List,       ConstructorKind.List,       true)]
    public void IsSubtypeOf_True(ConstructorKind child, ConstructorKind parent, bool expected)
        => Assert.AreEqual(expected, ConstructorLattice.IsSubtypeOf(child, parent));

    [TestCase(ConstructorKind.Set,        ConstructorKind.List)]
    [TestCase(ConstructorKind.Set,        ConstructorKind.Array)]
    [TestCase(ConstructorKind.List,       ConstructorKind.Set)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.List)]
    [TestCase(ConstructorKind.FixedArray, ConstructorKind.Array)]
    [TestCase(ConstructorKind.Enumerable, ConstructorKind.List)]
    [TestCase(ConstructorKind.Map,        ConstructorKind.Set)]
    public void IsSubtypeOf_False(ConstructorKind child, ConstructorKind parent)
        => Assert.IsFalse(ConstructorLattice.IsSubtypeOf(child, parent));

    // ─── Gcd ────────────────────────────────────────────────────────

    [TestCase(ConstructorKind.List,       ConstructorKind.Array,      ConstructorKind.List)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.FixedArray, ConstructorKind.Array)]
    [TestCase(ConstructorKind.FixedArray, ConstructorKind.Enumerable, ConstructorKind.FixedArray)]
    [TestCase(ConstructorKind.Enumerable, ConstructorKind.Set,        ConstructorKind.Set)]
    public void Gcd_NestedBranch_ReturnsLowerOfPair(ConstructorKind a, ConstructorKind b, ConstructorKind expected)
        => Assert.AreEqual(expected, ConstructorLattice.Gcd(a, b));

    [TestCase(ConstructorKind.List, ConstructorKind.Set)]
    [TestCase(ConstructorKind.List, ConstructorKind.Map)]
    [TestCase(ConstructorKind.Set,  ConstructorKind.Map)]
    [TestCase(ConstructorKind.Array, ConstructorKind.Set)]
    public void Gcd_DivergentBranches_IsNull(ConstructorKind a, ConstructorKind b)
        => Assert.IsNull(ConstructorLattice.Gcd(a, b));

    // ─── Concretest ─────────────────────────────────────────────────

    // Stage C — Concretest(FixedArray) = FixedArray (was Array). Under the unified
    // ee↔lang model, `:fixedArray<T>` annotation resolves to itself; LINQ functions
    // declared with `FunnyType.FixedArrayOf` resolve their arg slot to fixedArray.
    [TestCase(ConstructorKind.Enumerable, ConstructorKind.List)]
    [TestCase(ConstructorKind.FixedArray, ConstructorKind.FixedArray)]
    [TestCase(ConstructorKind.List,       ConstructorKind.List)]
    [TestCase(ConstructorKind.Set,        ConstructorKind.Set)]
    [TestCase(ConstructorKind.Map,        ConstructorKind.Map)]
    [TestCase(ConstructorKind.Array,      ConstructorKind.Array)]
    public void Concretest_AbstractDescendsToConcrete_ConcreteIsItself(ConstructorKind input, ConstructorKind expected)
        => Assert.AreEqual(expected, ConstructorLattice.Concretest(input));

    [Test]
    public void Concretest_NoDialectDependency_AlwaysList() {
        // Spec §TIC implementation sketch: Concretest is dialect-free at TIC layer.
        // The parser chooses ee→Array vs lang→List; the algebra always returns List
        // for the abstract Enumerable.
        Assert.AreEqual(ConstructorKind.List, ConstructorLattice.Concretest(ConstructorKind.Enumerable));
    }

    // ─── IsConstraintOnly / RequiresConcretestDescent ──────────────

    [TestCase(ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.FixedArray, false)]   // instantiable via fixedArray(...)
    [TestCase(ConstructorKind.Array,      false)]
    [TestCase(ConstructorKind.List,       false)]
    [TestCase(ConstructorKind.Set,        false)]
    [TestCase(ConstructorKind.Map,        false)]
    [TestCase(ConstructorKind.Any,        false)]
    public void IsConstraintOnly_OnlyEnumerable_IsConstraint(ConstructorKind kind, bool expected)
        => Assert.AreEqual(expected, ConstructorLattice.IsConstraintOnly(kind));

    [TestCase(ConstructorKind.Enumerable, true)]
    [TestCase(ConstructorKind.FixedArray, true)]    // has concrete descendant Array
    [TestCase(ConstructorKind.Array,      false)]
    [TestCase(ConstructorKind.List,       false)]
    [TestCase(ConstructorKind.Set,        false)]
    [TestCase(ConstructorKind.Map,        false)]
    [TestCase(ConstructorKind.Any,        false)]
    public void RequiresConcretestDescent_AbstractKindsOnly(ConstructorKind kind, bool expected)
        => Assert.AreEqual(expected, ConstructorLattice.RequiresConcretestDescent(kind));

    // ─── ElementVariance ────────────────────────────────────────────

    [TestCase(ConstructorKind.Enumerable)]
    [TestCase(ConstructorKind.FixedArray)]
    [TestCase(ConstructorKind.Array)]
    [TestCase(ConstructorKind.List)]
    [TestCase(ConstructorKind.Set)]
    [TestCase(ConstructorKind.Map)]
    public void ElementVariance_AllNewConstructors_AreInvariant(ConstructorKind kind) {
        // Stage 0 uniform-invariance rule. No new collection is covariant.
        Assert.AreEqual(Variance.Invariant, ConstructorLattice.ElementVariance(kind));
    }
}
