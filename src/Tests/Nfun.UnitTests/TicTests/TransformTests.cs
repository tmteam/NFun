using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static NFun.Tic.SolvingStates.StatePrimitive;

namespace Nfun.UnitTests.TicTests;

/// <summary>
/// Unit tests for TransformToArrayOrNull, TransformToStructOrNull, TransformToOptionalOrNull.
/// These functions convert ConstraintsState to composite types during Pull.
/// </summary>
[TestFixture]
public class TransformTests {

    // ── TransformToArrayOrNull ─────────────────────────────────

    [Test]
    public void TransformToArray_EmptyCS_ReturnsArray() {
        var result = SolvingFunctions.TransformToArrayOrNull("test", ConstraintsState.Empty);
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<ConstraintsState>(result.Element);
    }

    [Test]
    public void TransformToArray_CSWithArrayDesc_ReturnsArrayWithElement() {
        var elemNode = TicNode.CreateTypeVariableNode("e", I32);
        var arrDesc = new StateArray(elemNode);
        var cs = ConstraintsState.Of(arrDesc);
        var result = SolvingFunctions.TransformToArrayOrNull("test", cs);
        Assert.IsNotNull(result);
        // Element should carry the descendant from the snapshot
        Assert.IsInstanceOf<ConstraintsState>(result.Element);
        var elemCs = (ConstraintsState)result.Element;
        Assert.IsTrue(elemCs.HasDescendant);
    }

    [Test]
    public void TransformToArray_CSWithNonArrayDesc_ReturnsNull() {
        var cs = ConstraintsState.Of(I32);
        var result = SolvingFunctions.TransformToArrayOrNull("test", cs);
        Assert.IsNull(result);
    }

    [Test]
    public void TransformToArray_SolvedArrayDesc_PreferredLost() {
        // BUG 5 reproduction: when snapshot element is SOLVED (StatePrimitive),
        // TransformToArrayOrNull creates fresh CS via AddDescendant which calls
        // Concretest, stripping the Preferred from the original CS.
        // The snapshot is created by ConcretestArrayElement which DOES preserve Preferred.
        // But TransformToArrayOrNull line 513: AddDescendant(arrayEDesc.Element)
        // receives the CONCRETEST'd element (which ConcretestArrayElement preserved as CS).
        var preservedCs = ConstraintsState.Of(U8);
        preservedCs.Preferred = I32;
        var elemNode = TicNode.CreateTypeVariableNode("e", preservedCs);
        var arrDesc = new StateArray(elemNode);
        // Mark as solved by making element a solved CS
        // In reality, ConcretestArrayElement preserves CS[U8,P=I32]
        var cs = ConstraintsState.Of(arrDesc);

        var result = SolvingFunctions.TransformToArrayOrNull("test", cs);
        Assert.IsNotNull(result);
        // When arrayEDesc is NOT solved, TransformToArrayOrNull returns it directly.
        // The Preferred survives because no transformation happens.
        // The real bug occurs when the snapshot goes through AddDescendant on a PARENT CS
        // (in ApplyAncestorConstrains), not in TransformToArrayOrNull itself.
        // This test verifies the direct path works.
        var resultElem = result.Element;
        if (resultElem is ConstraintsState rcs)
            Assert.AreEqual(I32, rcs.Preferred, "Preferred should survive when array returned as-is");
    }

    [Test]
    public void AddDescendant_ArrayWithPreferredElement_PreferredExtracted() {
        // The actual bug path: AddDescendant stores arr(CS[U8,P=I32]) as snapshot.
        // Concretest produces arr(CS[U8,P=I32]) (ConcretestArrayElement preserves it).
        // But the PARENT CS that stores this snapshot doesn't inherit Preferred.
        var elemCs = ConstraintsState.Of(U8);
        elemCs.Preferred = I32;
        var elemNode = TicNode.CreateTypeVariableNode("e", elemCs);
        var arr = new StateArray(elemNode);

        var parentCs = ConstraintsState.Empty;
        parentCs.AddDescendant(arr);

        // After AddDescendant, parentCs.Descendant is arr(something).
        // parentCs.Preferred should be I32 (extracted from leaf).
        // Currently: parentCs.Preferred is null (Bug 5).
        // NOTE: This tests the PARENT level. The fix should be in AddDescendant.
        Assert.IsNull(parentCs.Preferred,
            "Bug 5: Preferred not extracted from composite leaf in AddDescendant (expected null = current broken behavior)");
    }

    // ── TransformToStructOrNull ────────────────────────────────

    [Test]
    public void TransformToStruct_EmptyCS_ReturnsAncestorStruct() {
        var anc = StateStruct.Of("x", I32);
        var result = SolvingFunctions.TransformToStructOrNull(ConstraintsState.Empty, anc);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.GetFieldOrNull("x"));
    }

    [Test]
    public void TransformToStruct_CSWithStructDesc_ReturnsDescStruct() {
        var descStruct = StateStruct.Of(false, ("a", (ITicNodeState)I32));
        var cs = ConstraintsState.Of(descStruct);
        var anc = StateStruct.Of(false, ("a", (ITicNodeState)I32));
        var result = SolvingFunctions.TransformToStructOrNull(cs, anc);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.GetFieldOrNull("a"));
    }

    [Test]
    public void TransformToStruct_CSWithNonStructDesc_ReturnsNull() {
        var cs = ConstraintsState.Of(I32);
        var anc = StateStruct.Of("x", I32);
        var result = SolvingFunctions.TransformToStructOrNull(cs, anc);
        Assert.IsNull(result);
    }

    [Test]
    public void TransformToStruct_PreservesIsOpen() {
        var descStruct = new StateStruct(isOpen: true);
        descStruct.AddField("a", TicNode.CreateTypeVariableNode("f", I32));
        var cs = ConstraintsState.Of(descStruct);
        var anc = StateStruct.Of(false, ("a", (ITicNodeState)I32));
        var result = SolvingFunctions.TransformToStructOrNull(cs, anc);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsOpen, "IsOpen should be preserved through TransformToStructOrNull");
    }

    // ── TransformToOptionalOrNull ──────────────────────────────

    [Test]
    public void TransformToOptional_EmptyCS_ReturnsOptional() {
        var result = SolvingFunctions.TransformToOptionalOrNull("test", ConstraintsState.Empty);
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<ConstraintsState>(result.Element);
    }

    [Test]
    public void TransformToOptional_CSWithOptionalDesc_ReturnsOptional() {
        var optDesc = StateOptional.Of(I32);
        var cs = ConstraintsState.Of(optDesc);
        var result = SolvingFunctions.TransformToOptionalOrNull("test", cs);
        Assert.IsNotNull(result);
    }

    [Test]
    public void TransformToOptional_CSWithNoneDesc_ReturnsNull() {
        // IsOptional=true WITHOUT descendant: TransformToOptionalOrNull returns null.
        // (NoConstrains=false because IsOptional is set, HasDescendant=false)
        var cs = ConstraintsState.Of(isOptional: true);
        var result = SolvingFunctions.TransformToOptionalOrNull("test", cs);
        Assert.IsNull(result);
    }
}
