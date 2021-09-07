using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests {

class SolvingFunctionsTest {
    [Test]
    public void MergeInplace_TwoConstrains_ReturnsMerged() {
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.I16, StatePrimitive.Real));
        var b = CreateNode("b", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));
        SolvingFunctions.MergeInplace(a, b);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real), a.State);
        Assert.AreEqual(new StateRefTo(a), b.State);
    }

    [Test]
    public void MergeInplace_TwoPrimitives_Throws() {
        var a = CreateNode("a", StatePrimitive.I16);
        var b = CreateNode("b", StatePrimitive.I32);
        Assert.Catch(() => SolvingFunctions.MergeInplace(a, b));
    }

    [Test]
    public void MergeInplace_TwoReferencedPrimitives_Throws() {
        var a = CreateNode("a", StatePrimitive.I16);
        var b = CreateNode("b", StatePrimitive.I32);
        var refA = CreateNode("a", new StateRefTo(a));
        var refB = CreateNode("b", new StateRefTo(b));

        Assert.Catch(() => SolvingFunctions.MergeInplace(refA, refB));
    }

    [Test]
    public void MergeInplace_ConstrainsAndPrimitive_ReturnsPrimitive() {
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.U16, StatePrimitive.Real));
        var b = CreateNode("b", StatePrimitive.U32);
        SolvingFunctions.MergeInplace(a, b);
        Assert.AreEqual(StatePrimitive.U32, a.State);
        Assert.AreEqual(StatePrimitive.U32, b.State);
    }

    [Test]
    public void MergeInplace_PrimitiveAndConstrains_ReturnsPrimitive() {
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.I16, StatePrimitive.Real));
        var b = CreateNode("b", StatePrimitive.I64);
        SolvingFunctions.MergeInplace(b, a);
        Assert.AreEqual(StatePrimitive.I64, a.State);
        Assert.AreEqual(StatePrimitive.I64, b.State);
    }

    [Test]
    public void MergeInplace_WhereSecondaryIsReferenced_ReturnsOrigin() {
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.I16, StatePrimitive.Real));
        var refToA = CreateNode("b", new StateRefTo(a));
        SolvingFunctions.MergeInplace(a, refToA);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I16, StatePrimitive.Real), a.State);
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
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
        var b = CreateNode("b", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));
        var r = CreateNode("r", new StateRefTo(a));
        var group = new TicNode[3];
        group[order[0]] = a;
        group[order[1]] = b;
        group[order[2]] = r;

        var merged = SolvingFunctions.MergeGroup(group);
        Assert.AreNotEqual(r, merged);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), merged.State);
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
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
        var b = CreateNode("b", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));
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
        var a = CreateNode("a", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
        var r = CreateNode("r", new StateRefTo(a));
        var merged = SolvingFunctions.MergeGroup(reversedOrder ? new[] { r, a } : new[] { a, r });
        Assert.AreEqual(a, merged);
        Assert.AreEqual(r.State, new StateRefTo(merged));
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), merged.State);
    }

    private static TicNode CreateNode(string name, ITicNodeState state = null)
        => TicNode.CreateNamedNode(name, state ?? new ConstrainsState());

    [Test]
    public void GetMergedStateOrNull_TwoSamePrimitives() {
        var res = SolvingFunctions.GetMergedStateOrNull(StatePrimitive.I32, StatePrimitive.I32);
        Assert.AreEqual(res, StatePrimitive.I32);
    }

    [Test]
    public void GetMergedStateOrNull_PrimitiveAndEmptyConstrains() {
        var res = SolvingFunctions.GetMergedStateOrNull(StatePrimitive.I32, new ConstrainsState());
        Assert.AreEqual(res, StatePrimitive.I32);
    }

    [Test]
    public void GetMergedStateOrNull_ConstrainsAndPrimitive_ReturnsPrimitive() {
        var a = new ConstrainsState(StatePrimitive.U16, StatePrimitive.Real);
        var b = StatePrimitive.I32;
        var merged = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.AreEqual(StatePrimitive.I32, merged);
    }

    [Test]
    public void GetMergedStateOrNull_EmptyConstrainsAndPrimitive() {
        var res = SolvingFunctions.GetMergedStateOrNull(new ConstrainsState(), StatePrimitive.I32);
        Assert.AreEqual(res, StatePrimitive.I32);
    }

    [Test]
    public void GetMergedStateOrNull_PrimitiveAndConstrainsThatFit() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            StatePrimitive.I32,
            new ConstrainsState(StatePrimitive.U24, StatePrimitive.I48));
        Assert.AreEqual(res, StatePrimitive.I32);
    }

    [Test]
    public void GetMergedStateOrNull_ConstrainsAndPrimitiveThatFit() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            StatePrimitive.I64,
            new ConstrainsState(StatePrimitive.I16, StatePrimitive.Real));
        Assert.AreEqual(res, StatePrimitive.I64);
    }

    [Test]
    public void GetMergedStateOrNull_ConstrainsThatFitAndPrimitive() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            new ConstrainsState(StatePrimitive.U24, StatePrimitive.I48),
            StatePrimitive.I32);
        Assert.AreEqual(res, StatePrimitive.I32);
    }

    [Test]
    public void GetMergedStateOrNull_TwoSameConcreteArrays() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            StateArray.Of(StatePrimitive.I32),
            StateArray.Of(StatePrimitive.I32));
        Assert.AreEqual(res, StateArray.Of(StatePrimitive.I32));
    }

    [Test]
    public void GetMergedStateOrNull_TwoSameConcreteStructs() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            new StateStruct("a", TicNode.CreateTypeVariableNode(StatePrimitive.I32)),
            new StateStruct("a", TicNode.CreateTypeVariableNode(StatePrimitive.I32)));

        Assert.AreEqual(res, new StateStruct("a", TicNode.CreateTypeVariableNode(StatePrimitive.I32)));
    }


    [Test]
    public void GetMergedStateOrNull_TwoConcreteStructsWithDifferentFields() {
        var res = SolvingFunctions.GetMergedStateOrNull(
            new StateStruct(
                new Dictionary<string, TicNode> {
                    { "i", TicNode.CreateTypeVariableNode(StatePrimitive.I32) },
                    { "r", TicNode.CreateTypeVariableNode(StatePrimitive.Real) }
                }
            ),
            new StateStruct(
                new Dictionary<string, TicNode> {
                    { "r", TicNode.CreateTypeVariableNode(StatePrimitive.Real) },
                    { "b", TicNode.CreateTypeVariableNode(StatePrimitive.Bool) }
                }));
        var expected = new StateStruct(
            new Dictionary<string, TicNode> {
                { "i", TicNode.CreateTypeVariableNode(StatePrimitive.I32) },
                { "r", TicNode.CreateTypeVariableNode(StatePrimitive.Real) },
                { "b", TicNode.CreateTypeVariableNode(StatePrimitive.Bool) }
            });

        Assert.AreEqual(res, expected);
    }

    [Test]
    public void GetMergedStateOrNull_EmptyAndNonEmptyStruct() {
        var nonEmpty = new StateStruct(
            new Dictionary<string, TicNode> {
                { "i", TicNode.CreateTypeVariableNode(StatePrimitive.I32) },
                { "r", TicNode.CreateTypeVariableNode(StatePrimitive.Real) }
            }
        );

        var res = SolvingFunctions.GetMergedStateOrNull(
            nonEmpty,
            new StateStruct());
        var expected = new StateStruct(
            new Dictionary<string, TicNode> {
                { "i", TicNode.CreateTypeVariableNode(StatePrimitive.I32) },
                { "r", TicNode.CreateTypeVariableNode(StatePrimitive.Real) }
            });

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
        => AssertGetMergedStateIsNull(StatePrimitive.I32, new ConstrainsState(StatePrimitive.U24, StatePrimitive.U48));

    [Test]
    public void GetMergedState_TwoDifferentPrimitivesThrows()
        => AssertGetMergedStateIsNull(StatePrimitive.I32, StatePrimitive.Real);

    [Test]
    public void GetMergedState_TwoDifferentConcreteArraysThrows()
        => AssertGetMergedStateIsNull(
            stateA: StateArray.Of(StatePrimitive.I32),
            stateB: StateArray.Of(StatePrimitive.Real));

    #endregion


    void AssertGetMergedStateIsNull(ITicNodeState stateA, ITicNodeState stateB) {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(stateA, stateB));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(stateB, stateA));
    }
}

}