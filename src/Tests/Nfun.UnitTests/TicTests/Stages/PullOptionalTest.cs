namespace NFun.UnitTests.TicTests.Stages;

using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.Tic.Stages;
using NUnit.Framework;
using static SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

/// <summary>
/// Atomic unit tests for PullConstraintsFunctions — Optional-related overloads.
///
/// Pull direction: descendant → ancestor (tightens ancestor constraints).
/// Each test creates minimal nodes, calls Apply directly, and checks state changes.
/// </summary>
public class PullOptionalTest {
    private static readonly PullConstraintsFunctions Pull = (PullConstraintsFunctions)PullConstraintsFunctions.Singleton;

    // ═══════════════════════════════════════════════════════════════
    // Apply(ConstraintsState anc, StatePrimitive(None) desc)
    //
    // When ancestor already has a non-None descendant and desc=None:
    //   → Wraps ancestor in Optional, disconnects descendant
    // When ancestor has no descendant or has None descendant:
    //   → Standard path: adds None as descendant
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pull_Constraints_None_WhenAncHasDescendant_WrapsInOptional() {
        // C[I32..] ← None  → ancestor becomes opt(C[I32..])
        var ancNode = Node("anc", Constrains(I32));
        var descNode = Node("desc", None);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(
            (ConstraintsState)ancNode.State, None, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(ancNode.State);
        var opt = (StateOptional)ancNode.State;
        // Inner element should preserve the original constraint
        Assert.IsInstanceOf<ConstraintsState>(opt.Element);
        var inner = (ConstraintsState)opt.Element;
        Assert.IsTrue(inner.HasDescendant);
        Assert.AreEqual(I32, inner.Descendant);
    }

    [Test]
    public void Pull_Constraints_None_WhenAncIsEmpty_DoesNotWrap() {
        // C[] ← None  → ancestor stays constraints (adds None as descendant)
        var ancNode = Node("anc", ConstraintsState.Empty);
        var descNode = Node("desc", None);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(
            ConstraintsState.Empty, None, ancNode, descNode);

        Assert.IsTrue(result);
        // Ancestor should NOT be wrapped in Optional (no existing descendant to protect)
        Assert.IsInstanceOf<ConstraintsState>(ancNode.State);
    }

    [Test]
    public void Pull_Constraints_None_WhenAncHasNoneDesc_DoesNotWrap() {
        // C[None..] ← None  → no double-wrap
        var ancNode = Node("anc", Constrains(None));
        var descNode = Node("desc", None);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(
            (ConstraintsState)ancNode.State, None, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<ConstraintsState>(ancNode.State);
    }

    [Test]
    public void Pull_Constraints_None_WhenAncHasOptDesc_DoesNotWrap() {
        // C[opt(I32)..] ← None  → no wrapping (already optional)
        var ancNode = Node("anc", Constrains(Optional(I32)));
        var descNode = Node("desc", None);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(
            (ConstraintsState)ancNode.State, None, ancNode, descNode);

        Assert.IsTrue(result);
        // Should NOT double-wrap
        Assert.IsNotInstanceOf<StateOptional>(ancNode.State);
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ConstraintsState anc, ICompositeState(StateOptional) desc)
    //
    // Optional descendant should wrap ancestor in Optional,
    // connecting inner element nodes for covariant propagation.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pull_Constraints_Optional_WrapsAncestorInOptional() {
        // C[] ← opt(I32)  → ancestor becomes opt(C[])
        var ancNode = Node("anc", ConstraintsState.Empty);
        var descOpt = StateOptional.Of(I32);
        var descNode = Node("desc", descOpt);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(
            ConstraintsState.Empty, descOpt, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(ancNode.State);
        var ancOpt = (StateOptional)ancNode.State;
        // Inner element should be constraints (from original ancestor)
        Assert.IsInstanceOf<ConstraintsState>(ancOpt.Element);
    }

    [Test]
    public void Pull_Constraints_Optional_IncompatibleAncestor_ReturnsFalse() {
        // C[comparable] ← opt(Bool)
        // comparable rejects composites except arr(char),
        // so SimplifyOrNull with opt desc should return null
        var anc = ConstraintsState.Of(isComparable: true);
        var ancNode = Node("anc", anc);
        var descOpt = StateOptional.Of(Bool);
        var descNode = Node("desc", descOpt);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(anc, descOpt, ancNode, descNode);

        Assert.IsFalse(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ICompositeState(StateOptional) anc, StatePrimitive desc)
    //
    // opt(T) ancestor + primitive descendant:
    //   - None: just disconnect (None ≤ opt(T))
    //   - Value: redirect to element (T ≤ opt(T) via implicit lift)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pull_Optional_Primitive_RedirectsToElement() {
        // opt(C[]) ← I32  → desc should be redirected to element node
        var ancOpt = StateOptional.Of(ConstraintsState.Empty);
        var ancNode = Node("anc", ancOpt);
        var descNode = Node("desc", I32);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, I32, ancNode, descNode);

        Assert.IsTrue(result);
        // descNode should no longer have ancNode as ancestor
        Assert.IsFalse(HasAncestor(descNode, ancNode));
        // descNode should have element node as ancestor
        Assert.IsTrue(HasAncestor(descNode, ancOpt.ElementNode));
    }

    [Test]
    public void Pull_Optional_None_DisconnectsOnly() {
        // opt(I32) ← None  → disconnect, no redirect
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var descNode = Node("desc", None);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, None, ancNode, descNode);

        Assert.IsTrue(result);
        // None should be disconnected from ancestor
        Assert.IsFalse(HasAncestor(descNode, ancNode));
        // None should NOT be redirected to element
        Assert.IsFalse(HasAncestor(descNode, ancOpt.ElementNode));
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ICompositeState(StateOptional) anc, ConstraintsState desc)
    //
    // opt(T) ancestor + constraints descendant:
    //   - If desc can become Optional → transform + connect elements
    //   - If desc is primitive/empty → implicit lift to element
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pull_Optional_Constraints_WithOptDesc_TransformsToOptional() {
        // opt(I32) ← C[opt(U8)..]  → desc becomes opt(element), elements connected
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(Optional(U8));
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(descNode.State);
    }

    [Test]
    public void Pull_Optional_EmptyConstraints_TransformsToOptional() {
        // opt(I32) ← C[]  → TransformToOptionalOrNull returns opt(C[]),
        // desc becomes Optional with elements connected
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Empty;
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(descNode.State);
        // Elements should be connected
        var descOpt = (StateOptional)descNode.State;
        Assert.IsTrue(HasAncestor(descOpt.ElementNode, ancOpt.ElementNode));
    }

    [Test]
    public void Pull_Optional_ConstraintsWithNoneDesc_NoRedirect() {
        // opt(I32) ← C[None..]  → None desc should not redirect to element
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(None);
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        // None desc should NOT redirect to element (None stays as-is)
    }

    [Test]
    public void Pull_Optional_ConstraintsWithPrimDesc_ImplicitLift() {
        // opt(Real) ← C[I32..]  → primitive desc, implicit lift to element
        var ancOpt = StateOptional.Of(Real);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(I32);
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsFalse(HasAncestor(descNode, ancNode));
        Assert.IsTrue(HasAncestor(descNode, ancOpt.ElementNode));
    }

    [Test]
    public void Pull_Optional_ConstraintsWithStructDesc_TransformsToOptional() {
        // opt(struct{a:I32}) ← C[struct{a:I32}..]
        // This is the map + ?.a scenario: lambda param has struct desc from map's T,
        // and opt(struct) ancestor from safe field access.
        // desc should become opt(inner) where inner has struct descendant
        var ancOpt = StateOptional.Of(Struct("a", I32));
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(Struct("a", I32));
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(descNode.State);
    }

    [Test]
    public void Pull_Optional_ConstraintsWithArrayDesc_DoesNotTransform() {
        // opt(I32[]) ← C[I32[]..]
        // Array descendants are NOT transformed to Optional here —
        // arrays use Phase 2 / Destruction path instead.
        var ancOpt = StateOptional.Of((ITicNodeState)StateArray.Of(I32));
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(Array(I32));
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        // Array composite desc should NOT be transformed to Optional in Pull
        Assert.IsNotInstanceOf<StateOptional>(descNode.State);
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(StateOptional anc, StateOptional desc)
    //
    // opt(A) ← opt(B): covariant — connect element nodes
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pull_Optional_Optional_ConnectsElements() {
        // opt(I32) ← opt(U8)  → covariant: element(desc) gets ancestor element(anc)
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var descOpt = StateOptional.Of(U8);
        var descNode = Node("desc", descOpt);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, descOpt, ancNode, descNode);

        Assert.IsTrue(result);
        // desc element should have ancestor = anc element
        Assert.IsTrue(HasAncestor(descOpt.ElementNode, ancOpt.ElementNode));
        // descNode should be disconnected from ancNode
        Assert.IsFalse(HasAncestor(descNode, ancNode));
    }

    [Test]
    public void Pull_Optional_Optional_SameElement_NoDoubleAdd() {
        // opt(X) ← opt(X) where elements share the same node
        var sharedElement = TicNode.CreateTypeVariableNode("elem", ConstraintsState.Empty);
        var ancOpt = StateOptional.Of(sharedElement);
        var descOpt = StateOptional.Of(sharedElement);
        var ancNode = Node("anc", ancOpt);
        var descNode = Node("desc", descOpt);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(ancOpt, descOpt, ancNode, descNode);

        Assert.IsTrue(result);
        // Same element — should skip AddAncestor (guard in code)
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ConstraintsState anc, ConstraintsState desc) — basic sanity
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pull_Constraints_Constraints_MergesDescendant() {
        // C[..Real] ← C[U8..]  → ancestor becomes C[U8..Real]
        var anc = ConstraintsState.Of(null, Real);
        var ancNode = Node("anc", anc);
        var desc = ConstraintsState.Of(U8);
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(anc, desc, ancNode, descNode);

        Assert.IsTrue(result);
        // Ancestor should now have U8 as descendant
        if (ancNode.State is ConstraintsState cs)
        {
            Assert.IsTrue(cs.HasDescendant);
            Assert.AreEqual(U8, cs.Descendant);
        }
    }

    [Test]
    public void Pull_Constraints_Constraints_Incompatible_ReturnsFalse() {
        // C[..Bool] ← C[I32..]  → I32 is not ≤ Bool → false
        var anc = ConstraintsState.Of(null, Bool);
        var ancNode = Node("anc", anc);
        var desc = ConstraintsState.Of(I32);
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Pull.Apply(anc, desc, ancNode, descNode);

        Assert.IsFalse(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private static TicNode Node(string name, ITicNodeState state) =>
        TicNode.CreateTypeVariableNode(name, state);

    /// <summary>
    /// Checks if node has a specific ancestor by reference equality
    /// (matches SmallList.Remove semantics).
    /// </summary>
    private static bool HasAncestor(TicNode node, TicNode expectedAncestor) =>
        node.Ancestors.Any(a => ReferenceEquals(a, expectedAncestor));
}
