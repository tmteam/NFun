using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

class SolvingFunctionsTest {
    [Test]
    public void MergeInplace_TwoConstrains_ReturnsMerged() {
        var a = CreateNode("a", ConstrainsState.Of(I16, Real));
        var b = CreateNode("b", ConstrainsState.Of(I24, Real));
        SolvingFunctions.MergeInplace(a, b);
        Assert.AreEqual(ConstrainsState.Of(I24, Real), a.State);
        Assert.AreEqual(new StateRefTo(a), b.State);
    }

    [Test]
    public void MergeInplace_TwoPrimitives_Throws() {
        var a = CreateNode("a", I16);
        var b = CreateNode("b", I32);
        Assert.Catch(() => SolvingFunctions.MergeInplace(a, b));
    }

    [Test]
    public void MergeInplace_TwoReferencedPrimitives_Throws() {
        var a = CreateNode("a", I16);
        var b = CreateNode("b", I32);
        var refA = CreateNode("a", new StateRefTo(a));
        var refB = CreateNode("b", new StateRefTo(b));

        Assert.Catch(() => SolvingFunctions.MergeInplace(refA, refB));
    }

    [Test]
    public void MergeInplace_ConstrainsAndPrimitive_ReturnsPrimitive() {
        var a = CreateNode("a", ConstrainsState.Of(U16, Real));
        var b = CreateNode("b", U32);
        SolvingFunctions.MergeInplace(a, b);
        Assert.AreEqual(U32, a.State);
        Assert.AreEqual(U32, b.State);
    }

    [Test]
    public void MergeInplace_PrimitiveAndConstrains_ReturnsPrimitive() {
        var a = CreateNode("a", ConstrainsState.Of(I16, Real));
        var b = CreateNode("b", I64);
        SolvingFunctions.MergeInplace(b, a);
        Assert.AreEqual(I64, a.State);
        Assert.AreEqual(I64, b.State);
    }

    [Test]
    public void MergeInplace_WhereSecondaryIsReferenced_ReturnsOrigin() {
        var a = CreateNode("a", ConstrainsState.Of(I16, Real));
        var refToA = CreateNode("b", new StateRefTo(a));
        SolvingFunctions.MergeInplace(a, refToA);
        Assert.AreEqual(ConstrainsState.Of(I16, Real), a.State);
        Assert.AreEqual(new StateRefTo(a), refToA.State);
    }

    [TestCase(0, 1, 2)]
    [TestCase(0, 2, 1)]
    [TestCase(1, 2, 0)]
    [TestCase(1, 0, 2)]
    [TestCase(2, 1, 0)]
    [TestCase(2, 0, 1)]
    public void MergeGroup_WithCycle_ReturnsSingle(params int[] order) {
        //a[i32,r]
        //b[i24,r]
        //r ==> a
        var a = CreateNode("a", ConstrainsState.Of(I32, Real));
        var b = CreateNode("b", ConstrainsState.Of(I24, Real));
        var r = CreateNode("r", new StateRefTo(a));
        var group = new TicNode[3];
        group[order[0]] = a;
        group[order[1]] = b;
        group[order[2]] = r;

        var merged = SolvingFunctions.MergeGroup(group);
        Assert.AreNotEqual(r, merged);
        Assert.AreEqual(ConstrainsState.Of(I32, Real), merged.State);
    }

    [TestCase(0, 1, 2)]
    [TestCase(0, 2, 1)]
    [TestCase(1, 2, 0)]
    [TestCase(1, 0, 2)]
    [TestCase(2, 1, 0)]
    [TestCase(2, 0, 1)]
    public void MergeGroup_WithCycle_AncestorsAreCorrect(params int[] order) {
        //Arrange:

        //a[i32,r]
        //b[i24,r]
        //r ==> a
        var a = CreateNode("a", ConstrainsState.Of(I32, Real));
        var b = CreateNode("b", ConstrainsState.Of(I24, Real));
        var r = CreateNode("r", new StateRefTo(a));

        var anc1 = CreateNode("anc1");
        var anc2 = CreateNode("anc2");
        var anc3 = CreateNode("anc3");
        a.AddAncestor(anc1);
        b.AddAncestor(anc2);
        r.AddAncestor(anc3);
        // Shuffle group order
        var group = new TicNode[3];
        group[order[0]] = a;
        group[order[1]] = b;
        group[order[2]] = r;

        //Act:
        var merged = SolvingFunctions.MergeGroup(group);

        //Assert:
        //All non main node have to loose all ancestors
        foreach (var nonRef in new[] { a, b, r }.Where(i => i != merged))
            Assert.IsEmpty(nonRef.Ancestors);
        //All ancestors move to main node
        Assert.AreEqual(3, merged.Ancestors.Count);
        Assert.Contains(anc1, merged.Ancestors.ToArray());
        Assert.Contains(anc2, merged.Ancestors.ToArray());
        Assert.Contains(anc3, merged.Ancestors.ToArray());
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MergeGroup_WithSmallCycle_ReturnsSingle(bool reversedOrder) {
        //a[i32,r]
        //r ==> a
        var a = CreateNode("a", ConstrainsState.Of(I32, Real));
        var r = CreateNode("r", new StateRefTo(a));
        var merged = SolvingFunctions.MergeGroup(reversedOrder ? new[] { r, a } : new[] { a, r });
        Assert.AreEqual(a, merged);
        Assert.AreEqual(r.State, new StateRefTo(merged));
        Assert.AreEqual(ConstrainsState.Of(I32, Real), merged.State);
    }

    private static TicNode CreateNode(string name, ITicNodeState state = null)
        => TicNode.CreateNamedNode(name, state ?? ConstrainsState.Empty);

    [Test]
    public void GetMergedStateOrNull_TwoSamePrimitives() {
        var res = SolvingFunctions.GetMergedStateOrNull(I32, I32);
        Assert.AreEqual(res, I32);
    }

    [Test]
    public void GetMergedStateOrNull_PrimitiveAndEmptyConstrains() {
        var res = SolvingFunctions.GetMergedStateOrNull(I32, ConstrainsState.Empty);
        Assert.AreEqual(res, I32);
    }

    [Test]
    public void GetMergedStateOrNull_ConstrainsAndPrimitive_ReturnsPrimitive() {
        var a = ConstrainsState.Of(U16, Real);
        var b = I32;
        var merged = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.AreEqual(I32, merged);
    }

    [Test]
    public void GetMergedStateOrNull_EmptyConstrainsAndPrimitive() {
        var res = SolvingFunctions.GetMergedStateOrNull(ConstrainsState.Empty, I32);
        Assert.AreEqual(res, I32);
    }

    [Test]
    public void GetMergedStateOrNull_PrimitiveAndConstrainsThatFit() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            I32,
            ConstrainsState.Of(U24, I48));
        Assert.AreEqual(res, I32);
    }

    [Test]
    public void GetMergedStateOrNull_ConstrainsAndPrimitiveThatFit() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            I64,
            ConstrainsState.Of(I16, Real));
        Assert.AreEqual(res, I64);
    }

    [Test]
    public void GetMergedStateOrNull_ConstrainsThatFitAndPrimitive() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            ConstrainsState.Of(U24, I48),
            I32);
        Assert.AreEqual(res, I32);
    }

    [Test]
    public void GetMergedStateOrNull_TwoSameConcreteArrays() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            StateArray.Of(I32),
            StateArray.Of(I32));
        Assert.AreEqual(res, StateArray.Of(I32));
    }

    [Test]
    public void GetMergedStateOrNull_TwoSameConcreteStructs() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            new StateStruct("a", TicNode.CreateTypeVariableNode(I32), false),
            new StateStruct("a", TicNode.CreateTypeVariableNode(I32), false));

        Assert.AreEqual(res, new StateStruct("a", TicNode.CreateTypeVariableNode(I32), false));
    }


    [Test]
    public void GetMergedStateOrNull_TwoConcreteStructsWithDifferentFields() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            new StateStruct(
                new Dictionary<string, TicNode> {
                    { "i", TicNode.CreateTypeVariableNode(I32) },
                    { "r", TicNode.CreateTypeVariableNode(Real) }
                }, false
            ),
            new StateStruct(
                new Dictionary<string, TicNode> {
                    { "r", TicNode.CreateTypeVariableNode(Real) },
                    { "b", TicNode.CreateTypeVariableNode(Bool) }
                }, false));
        var expected = new StateStruct(
            new Dictionary<string, TicNode> {
                { "i", TicNode.CreateTypeVariableNode(I32) },
                { "r", TicNode.CreateTypeVariableNode(Real) },
                { "b", TicNode.CreateTypeVariableNode(Bool) }
            }, false);

        Assert.AreEqual(res, expected);
    }

    [Test]
    public void GetMergedStateOrNull_EmptyAndNonEmptyStruct() {
        var nonEmpty = new StateStruct(
            new Dictionary<string, TicNode> {
                { "i", TicNode.CreateTypeVariableNode(I32) },
                { "r", TicNode.CreateTypeVariableNode(Real) }
            }, false
        );

        var res = SolvingFunctions.GetMergedStateOrNull(
            nonEmpty,
            new StateStruct());
        var expected = new StateStruct(
            new Dictionary<string, TicNode> {
                { "i", TicNode.CreateTypeVariableNode(I32) },
                { "r", TicNode.CreateTypeVariableNode(Real) }
            }, false);

        Assert.AreEqual(res, expected);

        var invertedres = SolvingFunctions.GetMergedStateOrNull(
            new StateStruct(), nonEmpty);
        Assert.AreEqual(res, invertedres);
    }

    [Test]
    public void GetMergedStateOrNull_TwoNonEmpty() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            new StateStruct(),
            new StateStruct());
        Assert.AreEqual(res, new StateStruct());
    }


    #region obviousFailed

    [Test]
    public void GetMergedState_PrimitiveAndConstrainsThatNotFit()
        => AssertGetMergedStateIsNull(I32, ConstrainsState.Of(U24, U48));

    [Test]
    public void GetMergedState_TwoDifferentPrimitivesThrows()
        => AssertGetMergedStateIsNull(I32, Real);

    [Test]
    public void GetMergedState_TwoDifferentConcreteArraysThrows()
        => AssertGetMergedStateIsNull(
            stateA: StateArray.Of(I32),
            stateB: StateArray.Of(Real));

    #endregion


    void AssertGetMergedStateIsNull(ITicNodeState stateA, ITicNodeState stateB) {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(stateA, stateB));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(stateB, stateA));
    }
}
