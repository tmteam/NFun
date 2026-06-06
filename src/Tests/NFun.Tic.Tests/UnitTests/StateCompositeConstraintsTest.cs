namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using SolvingStates;

/// <summary>
/// Unit tests for the C.1-deliverable StateCompositeConstraints state class:
/// factory, copy-on-write With* transformers, TryCollapseToPoint, SimplifyOrNull.
/// Algebra operators (LCA/GCD/Unify/Concretest/Abstractest) land in C.2 and have
/// their own test file.
/// </summary>
public class StateCompositeConstraintsTest {

    private static TicNode Elem() => TicNode.CreateInvisibleNode(ConstraintsState.Empty);

    #region Create factory

    [Test]
    public void Create_Empty_NoConstraints() {
        var s = StateCompositeConstraints.Create(Elem());
        Assert.IsNotNull(s);
        Assert.IsFalse(s.HasAncestor);
        Assert.IsFalse(s.HasDescendant);
        Assert.IsFalse(s.IsOptional);
        Assert.IsTrue(s.NoConstraints);
    }

    [Test]
    public void Create_WithAncestor_SetsBound() {
        var s = StateCompositeConstraints.Create(Elem(), ancestor: ConstructorKind.Enumerable);
        Assert.IsNotNull(s);
        Assert.IsTrue(s.HasAncestor);
        Assert.AreEqual(ConstructorKind.Enumerable, s.Ancestor);
        Assert.IsFalse(s.HasDescendant);
    }

    [Test]
    public void Create_WithDescendant_SetsFloor() {
        var s = StateCompositeConstraints.Create(Elem(), descendant: ConstructorKind.List);
        Assert.IsNotNull(s);
        Assert.IsTrue(s.HasDescendant);
        Assert.AreEqual(ConstructorKind.List, s.Descendant);
    }

    [Test]
    public void Create_WithBothBounds_BuildsInterval() {
        // [list .. enumerable] — valid (list <: enumerable in lattice)
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.Enumerable,
            descendant: ConstructorKind.List);
        Assert.IsNotNull(s);
        Assert.AreEqual(ConstructorKind.List, s.Descendant);
        Assert.AreEqual(ConstructorKind.Enumerable, s.Ancestor);
    }

    [Test]
    public void Create_EmptyInterval_ReturnsNull() {
        // [enumerable .. list] — Desc > Anc → empty interval → reject
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.List,
            descendant: ConstructorKind.Enumerable);
        Assert.IsNull(s);
    }

    [Test]
    public void Create_CrossBranchInterval_ReturnsNull() {
        // [set .. list] — different branches → no subtype relation → reject
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.List,
            descendant: ConstructorKind.Set);
        Assert.IsNull(s);
    }

    [Test]
    public void Create_SameKindBothBounds_AcceptsButCollapseIsCallerJob() {
        // [list..list] — valid interval; Create returns CompCS, collapse happens via TryCollapseToPoint.
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.List,
            descendant: ConstructorKind.List);
        Assert.IsNotNull(s);
        // Spec §3.6: Create doesn't collapse; that's TryCollapseToPoint's job.
        Assert.IsTrue(s.HasDescendant && s.HasAncestor);
    }

    [Test]
    public void Empty_ProducesUnconstrainedCompCS() {
        var s = StateCompositeConstraints.Empty(Elem());
        Assert.IsTrue(s.NoConstraints);
    }

    #endregion

    #region With* copy-on-write

    [Test]
    public void WithAncestor_NoChange_ReturnsSameInstance() {
        var s = StateCompositeConstraints.Create(Elem(), ancestor: ConstructorKind.Enumerable);
        var r = s.WithAncestor(ConstructorKind.Enumerable);
        Assert.AreSame(s, r);
    }

    [Test]
    public void WithAncestor_Change_ReturnsNewInstance() {
        var s = StateCompositeConstraints.Create(Elem());
        var r = s.WithAncestor(ConstructorKind.Array);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        Assert.AreNotSame(s, r);
        var cc = (StateCompositeConstraints)r;
        Assert.AreEqual(ConstructorKind.Array, cc.Ancestor);
        // Original unchanged (immutability)
        Assert.IsFalse(s.HasAncestor);
    }

    [Test]
    public void WithDescendant_Change_ReturnsNewInstance() {
        var s = StateCompositeConstraints.Create(Elem());
        var r = s.WithDescendant(ConstructorKind.List);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.AreEqual(ConstructorKind.List, cc.Descendant);
    }

    [Test]
    public void WithDescendant_CollapsesToPoint_ReturnsStateCollection() {
        // Anc=List, set Desc=List → interval [List..List] → collapse to StateCollection(List, e).
        var elem = Elem();
        var s = StateCompositeConstraints.Create(elem, ancestor: ConstructorKind.List);
        var r = s.WithDescendant(ConstructorKind.List);
        Assert.IsInstanceOf<StateCollection>(r);
        var sc = (StateCollection)r;
        Assert.AreEqual(ConstructorKind.List, sc.Constructor);
        Assert.AreSame(elem, sc.ElementNode);
    }

    [Test]
    public void WithAncestor_CollapsesAndWrapsOptional() {
        // CompCS{Desc=Array, IsOpt=true}, set Anc=Array → collapse to opt(SC(Array)).
        var elem = Elem();
        var s = StateCompositeConstraints.Create(
            elem,
            descendant: ConstructorKind.Array,
            isOptional: true);
        var r = s.WithAncestor(ConstructorKind.Array);
        Assert.IsInstanceOf<StateOptional>(r);
        var opt = (StateOptional)r;
        Assert.IsInstanceOf<StateCollection>(opt.Element);
        var sc = (StateCollection)opt.Element;
        Assert.AreEqual(ConstructorKind.Array, sc.Constructor);
    }

    [Test]
    public void WithAncestor_Contradicts_ReturnsNull() {
        // CompCS{Desc=Enumerable}, set Anc=List → empty interval → null.
        var s = StateCompositeConstraints.Create(Elem(), descendant: ConstructorKind.Enumerable);
        var r = s.WithAncestor(ConstructorKind.List);
        Assert.IsNull(r);
    }

    [Test]
    public void WithIsOptional_PropagatesFlag() {
        var s = StateCompositeConstraints.Create(Elem());
        var r = s.WithIsOptional(true);
        Assert.IsInstanceOf<StateCompositeConstraints>(r);
        var cc = (StateCompositeConstraints)r;
        Assert.IsTrue(cc.IsOptional);
        Assert.IsFalse(s.IsOptional);  // original immutable
    }

    [Test]
    public void With_Immutability_OriginalUnchangedAfterCascade() {
        var s = StateCompositeConstraints.Create(Elem());
        var step1 = (StateCompositeConstraints)s.WithAncestor(ConstructorKind.Enumerable);
        var step2 = (StateCompositeConstraints)step1.WithDescendant(ConstructorKind.List);
        // Each step a new instance; intermediate unchanged.
        Assert.AreNotSame(s, step1);
        Assert.AreNotSame(step1, step2);
        Assert.IsFalse(s.HasAncestor);
        Assert.IsFalse(step1.HasDescendant);
        Assert.AreEqual(ConstructorKind.Enumerable, step1.Ancestor);
        Assert.AreEqual(ConstructorKind.List, step2.Descendant);
        Assert.AreEqual(ConstructorKind.Enumerable, step2.Ancestor);
    }

    #endregion

    #region TryCollapseToPoint

    [Test]
    public void TryCollapseToPoint_NoBounds_ReturnsNull() {
        var s = StateCompositeConstraints.Create(Elem());
        Assert.IsNull(s.TryCollapseToPoint());
    }

    [Test]
    public void TryCollapseToPoint_OnlyOneBound_ReturnsNull() {
        var s = StateCompositeConstraints.Create(Elem(), ancestor: ConstructorKind.List);
        Assert.IsNull(s.TryCollapseToPoint());
        var t = StateCompositeConstraints.Create(Elem(), descendant: ConstructorKind.List);
        Assert.IsNull(t.TryCollapseToPoint());
    }

    [Test]
    public void TryCollapseToPoint_BothBoundsButDifferent_ReturnsNull() {
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.Enumerable,
            descendant: ConstructorKind.List);
        Assert.IsNull(s.TryCollapseToPoint());
    }

    [Test]
    public void TryCollapseToPoint_PointInterval_ReturnsStateCollection() {
        var elem = Elem();
        var s = StateCompositeConstraints.Create(
            elem,
            ancestor: ConstructorKind.Array,
            descendant: ConstructorKind.Array);
        var r = s.TryCollapseToPoint();
        Assert.IsInstanceOf<StateCollection>(r);
        var sc = (StateCollection)r;
        Assert.AreEqual(ConstructorKind.Array, sc.Constructor);
        Assert.AreSame(elem, sc.ElementNode);
    }

    [Test]
    public void TryCollapseToPoint_PointIntervalWithIsOptional_WrapsInOptional() {
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.List,
            descendant: ConstructorKind.List,
            isOptional: true);
        var r = s.TryCollapseToPoint();
        Assert.IsInstanceOf<StateOptional>(r);
        var opt = (StateOptional)r;
        Assert.IsInstanceOf<StateCollection>(opt.Element);
    }

    [Test]
    public void TryCollapseToPoint_PureFunction_DoesNotMutate() {
        var elem = Elem();
        var s = StateCompositeConstraints.Create(
            elem,
            ancestor: ConstructorKind.List,
            descendant: ConstructorKind.List);
        s.TryCollapseToPoint();
        // Original still a CompCS with the same fields.
        Assert.AreEqual(ConstructorKind.List, s.Ancestor);
        Assert.AreEqual(ConstructorKind.List, s.Descendant);
        Assert.AreSame(elem, s.ElementNode);
    }

    #endregion

    #region ITicNodeState contract

    [Test]
    public void IsMutable_True() {
        var s = StateCompositeConstraints.Create(Elem());
        Assert.IsTrue(s.IsMutable);
    }

    [Test]
    public void IsSolved_False() {
        var s = StateCompositeConstraints.Create(Elem());
        Assert.IsFalse(s.IsSolved);
    }

    [Test]
    public void CanBePessimisticConvertedTo_Any_True() {
        var s = StateCompositeConstraints.Create(Elem());
        Assert.IsTrue(s.CanBePessimisticConvertedTo(StatePrimitive.Any));
    }

    [Test]
    public void CanBePessimisticConvertedTo_NonAnyPrimitive_False() {
        var s = StateCompositeConstraints.Create(Elem());
        Assert.IsFalse(s.CanBePessimisticConvertedTo(StatePrimitive.I32));
        Assert.IsFalse(s.CanBePessimisticConvertedTo(StatePrimitive.Bool));
    }

    [Test]
    public void PrintState_RendersInterval() {
        var s = StateCompositeConstraints.Create(
            Elem(),
            ancestor: ConstructorKind.Enumerable,
            descendant: ConstructorKind.List);
        var printed = s.PrintState(0);
        StringAssert.Contains("list", printed);
        StringAssert.Contains("enumerable", printed);
    }

    [Test]
    public void PrintState_Optional_HasMark() {
        var s = StateCompositeConstraints.Create(Elem(), isOptional: true);
        StringAssert.Contains("?", s.PrintState(0));
    }

    #endregion

    #region Cycle-guard mark constants — collision check

    [Test]
    public void MarkConstants_NoCollisionWithStateCollection() {
        // StateCollection.CycleGuard = -57600 (private — checked via spec).
        // CompCS range is -59000..-59600.
        Assert.Less(StateCompositeConstraints.CompCsXArrayMark, -57600);
    }

    [Test]
    public void MarkConstants_AllDistinct() {
        var marks = new[] {
            StateCompositeConstraints.CompCsLcaMark,
            StateCompositeConstraints.CompCsGcdMark,
            StateCompositeConstraints.CompCsUnifyMark,
            StateCompositeConstraints.CompCsConcretestMark,
            StateCompositeConstraints.CompCsAbstractestMark,
            StateCompositeConstraints.CompCsXCollMark,
            StateCompositeConstraints.CompCsXArrayMark,
        };
        var set = new System.Collections.Generic.HashSet<int>(marks);
        Assert.AreEqual(marks.Length, set.Count, "Cycle-guard mark constants must be unique.");
    }

    [Test]
    public void MarkConstants_InReservedRange() {
        // Spec §3.8.1: -59000..-59600.
        var marks = new[] {
            StateCompositeConstraints.CompCsLcaMark,
            StateCompositeConstraints.CompCsGcdMark,
            StateCompositeConstraints.CompCsUnifyMark,
            StateCompositeConstraints.CompCsConcretestMark,
            StateCompositeConstraints.CompCsAbstractestMark,
            StateCompositeConstraints.CompCsXCollMark,
            StateCompositeConstraints.CompCsXArrayMark,
        };
        foreach (var m in marks) {
            Assert.LessOrEqual(m, -59000);
            Assert.GreaterOrEqual(m, -59600);
        }
    }

    #endregion
}
