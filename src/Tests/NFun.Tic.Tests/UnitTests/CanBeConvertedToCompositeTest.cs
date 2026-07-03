namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// CS.CanBeConvertedTo(composite target): the target is acceptable iff the interval's
/// lower bound converts into it — Descendant.FitsInto(target).
/// Regression: the composite arm used to compare C#-classes only
/// (`Descendant.GetType() != type.GetType() → false; else true`), so
/// CS{Desc=list&lt;I32&gt;} reported convertible to set&lt;text&gt; (both StateCollection)
/// and NOT convertible across the array-branch SC ≤ StateArray bridge that
/// Pull/Push/Fit all accept. This predicate gates TryBecomeConcrete /
/// SolveCovariant / FitsInto — the heart of resolution.
/// </summary>
public class CanBeConvertedToCompositeTest {

    private static ConstraintsState CsWithDesc(ITypeState desc) => ConstraintsState.Of(desc);

    [Test]
    public void ListDesc_ToSameList_True() {
        var cs = CsWithDesc(StateCollection.Of(ConstructorKind.List, I32));
        Assert.IsTrue(cs.CanBeConvertedTo(StateCollection.Of(ConstructorKind.List, I32)));
    }

    [Test]
    public void ListDesc_ToSetOfText_False() {
        // Same C# class, disjoint kind AND element — must be rejected.
        var cs = CsWithDesc(StateCollection.Of(ConstructorKind.List, I32));
        Assert.IsFalse(cs.CanBeConvertedTo(
            StateCollection.Of(ConstructorKind.Set, StateArray.Of(Char))));
    }

    [Test]
    public void ListDesc_ToLegacyArray_True() {
        // Array-branch bridge: list ≤ T[] (legacy StateArray) — accepted by
        // Pull/Push/Fit; the predicate must agree.
        var cs = CsWithDesc(StateCollection.Of(ConstructorKind.List, I32));
        Assert.IsTrue(cs.CanBeConvertedTo(StateArray.Of(I32)));
    }

    [Test]
    public void ArrayDesc_WideningElement_True() {
        // arr(U8) fits into arr(I32) — covariant legacy array element.
        var cs = CsWithDesc(StateArray.Of(U8));
        Assert.IsTrue(cs.CanBeConvertedTo(StateArray.Of(I32)));
    }

    [Test]
    public void ArrayDesc_NarrowingElement_False() {
        // arr(I32) does NOT fit into arr(U8).
        var cs = CsWithDesc(StateArray.Of(I32));
        Assert.IsFalse(cs.CanBeConvertedTo(StateArray.Of(U8)));
    }

    [Test]
    public void NoDescendant_Composite_True() {
        // Unconstrained interval accepts any composite (unchanged behavior).
        var cs = ConstraintsState.Of();
        Assert.IsTrue(cs.CanBeConvertedTo(StateCollection.Of(ConstructorKind.Set, I32)));
    }
}
