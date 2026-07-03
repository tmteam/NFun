namespace NFun.Tic.Tests.UnitTests;

using NFun.Tic.Algebra;
using NUnit.Framework;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Gcd(CompCs, legacy StateArray) — the cross-family meet.
/// Regressions pinned:
///  - CompCs with Ancestor = Enumerable (the `Enumerable&lt;T&gt;` typeclass cap) used to
///    construct `new StateCollection(Enumerable, …)` whose ctor THROWS — the meet of a
///    typeclass-capped arg with an ee-mode array crashed instead of computing.
///  - `IsOptional` was dropped from the result (the SC sibling GcdCompCsXColl wraps).
/// </summary>
public class CompCsGcdArrayTest {

    private static StateCompositeConstraints CompCs(
        ConstructorKind? anc = null, ConstructorKind? desc = null,
        bool isOptional = false, bool isClearable = false)
        => StateCompositeConstraints.Create(
            TicNode.CreateTypeVariableNode("e", ConstraintsState.Of(I32)),
            anc, desc, isOptional, isClearable);

    [Test]
    public void EnumerableCap_MeetsArray_NoCrash_KeepsArray() {
        // Enumerable cap cannot materialize as StateCollection — the array side IS
        // the concrete shape of the meet.
        var r = CompCs(anc: ConstructorKind.Enumerable).Gcd(StateArray.Of(I32));
        Assert.NotNull(r, "meet must exist");
        Assert.IsInstanceOf<StateArray>(r, $"got {r}");
    }

    [Test]
    public void ConcreteCap_MeetsArray_CollapsesToKind() {
        var r = CompCs(anc: ConstructorKind.Array).Gcd(StateArray.Of(I32));
        Assert.IsInstanceOf<StateCollection>(r, $"got {r}");
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)r).Constructor);
    }

    [Test]
    public void OptionalCompCs_MeetsArray_KeepsOptional() {
        var r = CompCs(anc: ConstructorKind.Array, isOptional: true).Gcd(StateArray.Of(I32));
        Assert.IsInstanceOf<StateOptional>(r,
            $"IsOptional must survive the meet, got {r}");
    }

    [Test]
    public void OptionalCompCs_LcaWithArray_KeepsOptional() {
        // The JOIN must also carry the Optional axis (T ≤ opt(T)) — LcaCompCsXArray
        // used to drop IsOptional while its SC sibling preserved it.
        var r = CompCs(anc: ConstructorKind.Array, isOptional: true).Lca(StateArray.Of(I32));
        Assert.IsInstanceOf<StateOptional>(r, $"got {r}");
    }

    [Test]
    public void ClearableCap_MeetsArray_CollapsesToClearableKind() {
        // Clearable typeclass + no concrete cap: T[] itself is not clearable; the
        // narrowest array-branch clearable constructor is List.
        var r = CompCs(isClearable: true).Gcd(StateArray.Of(I32));
        Assert.IsInstanceOf<StateCollection>(r, $"got {r}");
        Assert.IsTrue(ConstructorLattice.IsClearable(((StateCollection)r).Constructor),
            $"result kind must satisfy the Clearable typeclass, got {r}");
    }
}
