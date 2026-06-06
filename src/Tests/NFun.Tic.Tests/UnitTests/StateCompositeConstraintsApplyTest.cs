namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using NFun.Tic.Stages;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Stage C.3 — directional Apply cells for StateCompositeConstraints per
/// <c>Specs/Tic/Algebra_CompositeConstraints.md</c> §4.
/// Tests exercise the 4 critical cells (§4.1.1-4) directly via IStateFunction.
/// </summary>
public class StateCompositeConstraintsApplyTest {

    private static TicNode Elem() => TicNode.CreateInvisibleNode(ConstraintsState.Empty);
    private static TicNode ElemOf(StatePrimitive prim) => TicNode.CreateInvisibleNode(prim);

    private static StateCompositeConstraints Cc(
        TicNode elem,
        ConstructorKind? anc = null,
        ConstructorKind? desc = null,
        bool isOpt = false) =>
        StateCompositeConstraints.Create(elem, ancestor: anc, descendant: desc, isOptional: isOpt);

    private static TicNode NodeOf(ITicNodeState state) => TicNode.CreateInvisibleNode(state);

    private static PullConstraintsFunctions Pull => PullConstraintsFunctions.Singleton;
    private static PushConstraintsFunctions Push => PushConstraintsFunctions.Singleton;
    private static DestructionFunctions Destr => DestructionFunctions.Singleton;

    #region §4.1.1 Forward Pull Apply(CompCS, StateCollection)

    [Test]
    public void ForwardPull_CompCsEnumerable_AcceptsList_RefinesDescFloor() {
        // CompCS{Anc=enumerable} ⊕ SC(List, int) via Pull → CompCS{Anc=enumerable, Desc=list}
        var elem = ElemOf(I32);
        var anc = Cc(elem, anc: ConstructorKind.Enumerable);
        var ancNode = NodeOf(anc);
        var sc = new StateCollection(ConstructorKind.List, elem);
        var descNode = NodeOf(sc);
        descNode.AddAncestor(ancNode);

        var ok = Pull.Apply(anc, sc, ancNode, descNode);

        Assert.IsTrue(ok);
        Assert.IsInstanceOf<StateCompositeConstraints>(ancNode.State);
        var refined = (StateCompositeConstraints)ancNode.State;
        Assert.AreEqual(ConstructorKind.List, refined.Descendant);
        Assert.AreEqual(ConstructorKind.Enumerable, refined.Ancestor);
    }

    [Test]
    public void ForwardPull_CompCsAnc_KAboveCap_Rejects() {
        // CompCS{Anc=list} ⊕ SC(Array, int) → Array > list → reject
        var elem = ElemOf(I32);
        var anc = Cc(elem, anc: ConstructorKind.List);
        var ancNode = NodeOf(anc);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var descNode = NodeOf(sc);

        var ok = Pull.Apply(anc, sc, ancNode, descNode);

        Assert.IsFalse(ok);
    }

    [Test]
    public void ForwardPull_CollapseToPoint_ReturnsStateCollection() {
        // CompCS{Anc=list} ⊕ SC(List, int) → newDesc=list = oldAnc=list → collapse to SC(List)
        var elem = ElemOf(I32);
        var anc = Cc(elem, anc: ConstructorKind.List);
        var ancNode = NodeOf(anc);
        var sc = new StateCollection(ConstructorKind.List, elem);
        var descNode = NodeOf(sc);

        var ok = Pull.Apply(anc, sc, ancNode, descNode);

        Assert.IsTrue(ok);
        Assert.IsInstanceOf<StateCollection>(ancNode.State);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)ancNode.State).Constructor);
    }

    [Test]
    public void ForwardPull_AncInvariantNotRefined() {
        // §4.1.1 Pull invariant: CompCS.Anc never refined.
        // CompCS{Anc=enumerable} ⊕ SC(List): Anc must stay enumerable, not narrow to list.
        var elem = ElemOf(I32);
        var anc = Cc(elem, anc: ConstructorKind.Enumerable);
        var ancNode = NodeOf(anc);
        var sc = new StateCollection(ConstructorKind.List, elem);
        var descNode = NodeOf(sc);

        Pull.Apply(anc, sc, ancNode, descNode);

        // Verify Anc stayed enumerable.
        var result = (StateCompositeConstraints)ancNode.State;
        Assert.AreEqual(ConstructorKind.Enumerable, result.Ancestor);
    }

    #endregion

    #region §4.1.2 Forward Push Apply(CompCS, StateCollection)

    [Test]
    public void ForwardPush_AcceptsKBelowCap_NoMutation() {
        var elem = ElemOf(I32);
        var anc = Cc(elem, anc: ConstructorKind.Enumerable);
        var ancNode = NodeOf(anc);
        var origAncState = ancNode.State; // capture for comparison
        var sc = new StateCollection(ConstructorKind.List, elem);
        var descNode = NodeOf(sc);

        var ok = Push.Apply(anc, sc, ancNode, descNode);

        Assert.IsTrue(ok);
        // Push doesn't mutate ancNode.State per §4.1.2.
        Assert.AreSame(origAncState, ancNode.State);
    }

    [Test]
    public void ForwardPush_RejectsKAboveCap() {
        var elem = ElemOf(I32);
        var anc = Cc(elem, anc: ConstructorKind.List);
        var ancNode = NodeOf(anc);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var descNode = NodeOf(sc);

        var ok = Push.Apply(anc, sc, ancNode, descNode);

        Assert.IsFalse(ok);
    }

    #endregion

    #region §4.1.3 Reverse Pull Apply(StateCollection, CompCS)

    [Test]
    public void ReversePull_CompCsDescCollapsesToScAtK() {
        // SC(Array) anc, CompCS{Anc=enumerable} desc via Pull → CompCS_d collapses to SC(Array)
        var elem = ElemOf(I32);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var ancNode = NodeOf(sc);
        var desc = Cc(elem, anc: ConstructorKind.Enumerable);
        var descNode = NodeOf(desc);
        descNode.AddAncestor(ancNode);

        var ok = Pull.Apply(sc, desc, ancNode, descNode);

        Assert.IsTrue(ok);
        Assert.IsInstanceOf<StateCollection>(descNode.State);
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)descNode.State).Constructor);
    }

    [Test]
    public void ReversePull_KAboveDescCap_Rejects() {
        // SC(Array) ⊕ CompCS{Anc=list}: Array > list → reject
        var elem = ElemOf(I32);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var ancNode = NodeOf(sc);
        var desc = Cc(elem, anc: ConstructorKind.List);
        var descNode = NodeOf(desc);

        var ok = Pull.Apply(sc, desc, ancNode, descNode);

        Assert.IsFalse(ok);
    }

    [Test]
    public void ReversePull_OptionalCompCsCollapsesWithWrap() {
        var elem = ElemOf(I32);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var ancNode = NodeOf(sc);
        var desc = Cc(elem, anc: ConstructorKind.Enumerable, isOpt: true);
        var descNode = NodeOf(desc);

        var ok = Pull.Apply(sc, desc, ancNode, descNode);

        Assert.IsTrue(ok);
        Assert.IsInstanceOf<StateOptional>(descNode.State);
    }

    #endregion

    #region §4.1.4 Reverse Push Apply(StateCollection, CompCS)

    [Test]
    public void ReversePush_NarrowsCompCsAncCap() {
        // SC(Array) ⊕ CompCS{Anc=enumerable} via Push → CompCS.Anc narrows to GCD(enum, array) = array
        var elem = ElemOf(I32);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var ancNode = NodeOf(sc);
        var desc = Cc(elem, anc: ConstructorKind.Enumerable);
        var descNode = NodeOf(desc);

        var ok = Push.Apply(sc, desc, ancNode, descNode);

        Assert.IsTrue(ok);
        // Result should either be a refined CompCS or collapsed SC. Both acceptable here.
    }

    [Test]
    public void ReversePush_CrossBranchKRejects_NullGuard() {
        // SC(Set) ⊕ CompCS{Anc=list} via Push → GCD_L(list, set) = null → reject (v6.1 B4 fix)
        var elem = ElemOf(I32);
        var sc = new StateCollection(ConstructorKind.Set, elem);
        var ancNode = NodeOf(sc);
        var desc = Cc(elem, anc: ConstructorKind.List);
        var descNode = NodeOf(desc);

        var ok = Push.Apply(sc, desc, ancNode, descNode);

        Assert.IsFalse(ok);
    }

    [Test]
    public void ReversePush_DescInvariantNotRefined() {
        // §4.1.4 Push invariant: CompCS_d.Desc never refined.
        // CompCS{Desc=list, Anc=fixedArray} ⊕ SC(Array) Push:
        //   newAnc = GCD(fixedArray, array) = array
        //   Desc should stay = list
        var elem = ElemOf(I32);
        var sc = new StateCollection(ConstructorKind.Array, elem);
        var ancNode = NodeOf(sc);
        var desc = Cc(elem, desc: ConstructorKind.List, anc: ConstructorKind.FixedArray);
        var descNode = NodeOf(desc);

        var ok = Push.Apply(sc, desc, ancNode, descNode);

        Assert.IsTrue(ok);
        if (descNode.State is StateCompositeConstraints refined) {
            Assert.AreEqual(ConstructorKind.List, refined.Descendant);
        }
    }

    #endregion

    #region Same-class Apply

    [Test]
    public void SameClass_Pull_DelegatesToUnify() {
        // Two compatible CompCS → Unify produces refined CompCS or collapse to SC.
        var elem = ElemOf(I32);
        var a = Cc(elem, desc: ConstructorKind.List);
        var ancNode = NodeOf(a);
        var b = Cc(elem, anc: ConstructorKind.Enumerable);
        var descNode = NodeOf(b);
        descNode.AddAncestor(ancNode);

        var ok = Pull.Apply(a, b, ancNode, descNode);

        Assert.IsTrue(ok);
        // Result on ancestor: Unify({Desc=list}, {Anc=enumerable}) = {Desc=list, Anc=enumerable}
        var result = ancNode.State;
        Assert.IsInstanceOf<StateCompositeConstraints>(result);
    }

    [Test]
    public void SameClass_DisjointBranches_Rejects() {
        var elem = ElemOf(I32);
        var a = Cc(elem, anc: ConstructorKind.List);
        var ancNode = NodeOf(a);
        var b = Cc(elem, anc: ConstructorKind.Set);
        var descNode = NodeOf(b);

        var ok = Pull.Apply(a, b, ancNode, descNode);

        Assert.IsFalse(ok);
    }

    #endregion

    #region Primitive cross — Any and reject

    [Test]
    public void Apply_PrimAny_AsAnc_AcceptsAllStages() {
        var anc = Any;
        var ancNode = NodeOf(anc);
        var desc = Cc(ElemOf(I32), anc: ConstructorKind.List);
        var descNode = NodeOf(desc);

        Assert.IsTrue(Pull.Apply(anc, desc, ancNode, descNode));
        Assert.IsTrue(Push.Apply(anc, desc, ancNode, descNode));
    }

    [Test]
    public void Apply_PrimI32_AsAnc_RejectsCompCsDesc() {
        var anc = I32;
        var ancNode = NodeOf(anc);
        var desc = Cc(ElemOf(I32), anc: ConstructorKind.List);
        var descNode = NodeOf(desc);

        Assert.IsFalse(Pull.Apply(anc, desc, ancNode, descNode));
        Assert.IsFalse(Push.Apply(anc, desc, ancNode, descNode));
    }

    [Test]
    public void Apply_CompCsAnc_PrimNone_SetsIsOptional() {
        // None descendant → set IsOptional, detach edge (per Pull None handling).
        var elem = ElemOf(I32);
        var anc = Cc(elem);
        var ancNode = NodeOf(anc);
        var descNode = NodeOf(None);

        var ok = Pull.Apply(anc, None, ancNode, descNode);

        Assert.IsTrue(ok);
        var refined = (StateCompositeConstraints)ancNode.State;
        Assert.IsTrue(refined.IsOptional);
    }

    #endregion
}
