namespace NFun.UnitTests.TicTests.Stages;

using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.Tic.Stages;
using NUnit.Framework;
using static SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

/// <summary>
/// Atomic unit tests for PushConstraintsFunctions — Optional-related overloads.
///
/// Push direction: ancestor → descendant (narrows descendant constraints).
/// Each test creates minimal nodes, calls Apply directly, and checks state changes.
/// </summary>
public class PushOptionalTest {
    private static readonly PushConstraintsFunctions Push = (PushConstraintsFunctions)PushConstraintsFunctions.Singleton;

    // ═══════════════════════════════════════════════════════════════
    // Apply(ICompositeState(StateOptional) anc, StatePrimitive desc)
    //
    // opt(T) → primitive:
    //   - None ≤ opt(T): no constraint on T
    //   - Value ≤ opt(T): redirect to element
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Push_Optional_None_NoRedirect() {
        // opt(I32) → None:  None ≤ opt(T) is always valid, no action needed
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var descNode = Node("desc", None);

        var result = Push.Apply(ancOpt, None, ancNode, descNode);

        Assert.IsTrue(result);
        // desc should NOT get element as ancestor
        Assert.IsFalse(HasAncestor(descNode, ancOpt.ElementNode));
    }

    [Test]
    public void Push_Optional_Primitive_RedirectsToElement() {
        // opt(I32) → U8:  U8 ≤ opt(I32) via lift, so push U8 toward element
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var descNode = Node("desc", U8);

        var result = Push.Apply(ancOpt, U8, ancNode, descNode);

        Assert.IsTrue(result);
        // desc should get element node as ancestor (for push constraint propagation)
        Assert.IsTrue(HasAncestor(descNode, ancOpt.ElementNode));
    }

    [Test]
    public void Push_Optional_IncompatiblePrimitive_Throws() {
        // opt(Bool) → I32:  PushConstraints detects I32 ≰ Bool and throws
        var ancOpt = StateOptional.Of(Bool);
        var ancNode = Node("anc", ancOpt);
        var descNode = Node("desc", I32);

        Assert.Catch(() => Push.Apply(ancOpt, I32, ancNode, descNode));
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ICompositeState(StateOptional) anc, ConstraintsState desc)
    //
    // opt(T) → constraints desc:
    //   - If desc can become Optional (has opt descendant) → transform
    //   - If desc is primitive/empty constraints → implicit lift to element
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Push_Optional_EmptyConstraints_TransformsToOptional() {
        // opt(I32) → C[]  → TransformToOptionalOrNull returns opt(C[]),
        // desc becomes Optional with elements connected
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Empty;
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Push.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(descNode.State);
        var descOpt = (StateOptional)descNode.State;
        Assert.IsTrue(HasAncestor(descOpt.ElementNode, ancOpt.ElementNode));
    }

    [Test]
    public void Push_Optional_ConstraintsWithOptDesc_TransformsToOptional() {
        // opt(I32) → C[opt(U8)..]  → desc becomes opt(element)
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(Optional(U8));
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Push.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(descNode.State);
    }

    [Test]
    public void Push_Optional_ConstraintsWithNoneDesc_NoLift() {
        // opt(I32) → C[None..]  → None desc means no redirect to element
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(None);
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Push.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsFalse(HasAncestor(descNode, ancNode));
        // None desc → should NOT redirect to element
        Assert.IsFalse(HasAncestor(descNode, ancOpt.ElementNode));
    }

    [Test]
    public void Push_Optional_ConstraintsWithPrimDesc_ImplicitLift() {
        // opt(Real) → C[I32..]  → primitive desc, implicit lift to element
        var ancOpt = StateOptional.Of(Real);
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(I32);
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        var result = Push.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsFalse(HasAncestor(descNode, ancNode));
        Assert.IsTrue(HasAncestor(descNode, ancOpt.ElementNode));
    }

    [Test]
    public void Push_Optional_ConstraintsWithUnsolvedStructDesc_WrapsInOptional() {
        // opt(struct{a:C[]}) → C[struct{a:C[I32..]}..]
        // Map+?.a scenario: lambda param has unsolved struct desc from generic T
        // and opt(struct) ancestor from safe field access.
        // Only unsolved structs are wrapped (solved ones come from literals).
        var ancOpt = StateOptional.Of(Struct("a", ConstraintsState.Empty));
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(Struct("a", Constrains(I32)));
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        Assert.IsFalse(((StateStruct)desc.Descendant).IsSolved, "struct desc should NOT be solved");

        var result = Push.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<StateOptional>(descNode.State,
            $"Descendant with unsolved struct desc and Optional ancestor should be wrapped in Optional, got {descNode.State.GetType().Name}: {descNode.State}");
    }

    [Test]
    public void Push_Optional_ConstraintsWithSolvedStructDesc_DoesNotWrap() {
        // opt(struct{a:I32}) → C[struct{a:I32}..]  where struct has solved fields
        // This is the x?.name case on a non-optional struct literal.
        // Solved structs should NOT be wrapped — they come from concrete values.
        var ancOpt = StateOptional.Of(Struct("a", I32));
        var ancNode = Node("anc", ancOpt);
        var desc = ConstraintsState.Of(Struct("a", I32));
        var descNode = Node("desc", desc);
        descNode.AddAncestor(ancNode);

        Assert.IsTrue(((StateStruct)desc.Descendant).IsSolved, "struct desc should be solved");

        var result = Push.Apply(ancOpt, desc, ancNode, descNode);

        Assert.IsTrue(result);
        // Should NOT be wrapped — implicit lift instead
        Assert.IsNotInstanceOf<StateOptional>(descNode.State,
            $"Solved struct desc should NOT be wrapped in Optional");
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(StateOptional anc, StateOptional desc)
    //
    // opt(A) → opt(B): covariant — push element constraints
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Push_Optional_Optional_PushesElements() {
        // opt(I32) → opt(C[])  → push element constraints
        var ancOpt = StateOptional.Of(I32);
        var ancNode = Node("anc", ancOpt);
        var descOpt = StateOptional.Of(ConstraintsState.Empty);
        var descNode = Node("desc", descOpt);

        var result = Push.Apply(ancOpt, descOpt, ancNode, descNode);

        Assert.IsTrue(result);
        // PushConstraints is called recursively on element nodes
        // We can't easily verify the recursive call, but result should be true
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ConstraintsState anc, StatePrimitive desc) — sanity
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Push_Constraints_Primitive_Compatible() {
        // C[..Real] → I32:  I32 ≤ Real → true
        var anc = ConstraintsState.Of(null, Real);
        var ancNode = Node("anc", anc);
        var descNode = Node("desc", I32);

        var result = Push.Apply(anc, I32, ancNode, descNode);

        Assert.IsTrue(result);
    }

    [Test]
    public void Push_Constraints_Primitive_Incompatible() {
        // C[..Bool] → I32:  I32 ≰ Bool → false
        var anc = ConstraintsState.Of(null, Bool);
        var ancNode = Node("anc", anc);
        var descNode = Node("desc", I32);

        var result = Push.Apply(anc, I32, ancNode, descNode);

        Assert.IsFalse(result);
    }

    [Test]
    public void Push_ConstraintsNoAncestor_Primitive_ReturnsTrue() {
        // C[] → I32:  no ancestor constraint → always OK
        var anc = ConstraintsState.Empty;
        var ancNode = Node("anc", anc);
        var descNode = Node("desc", I32);

        var result = Push.Apply(anc, I32, ancNode, descNode);

        Assert.IsTrue(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(ConstraintsState anc, ICompositeState(StateOptional) desc)
    //
    // When ancestor has non-Any ancestor bound → false
    // When ancestor has no bound → true
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Push_ConstraintsWithAncestor_Optional_ReturnsFalse() {
        // C[..I32] → opt(U8):  I32 is not Any → false
        var anc = ConstraintsState.Of(null, I32);
        var ancNode = Node("anc", anc);
        var descOpt = StateOptional.Of(U8);
        var descNode = Node("desc", descOpt);

        var result = Push.Apply(anc, descOpt, ancNode, descNode);

        Assert.IsFalse(result);
    }

    [Test]
    public void Push_ConstraintsWithAny_Optional_ReturnsTrue() {
        // C[..Any] → opt(U8):  Any is OK → true
        var anc = ConstraintsState.Of(null, Any);
        var ancNode = Node("anc", anc);
        var descOpt = StateOptional.Of(U8);
        var descNode = Node("desc", descOpt);

        var result = Push.Apply(anc, descOpt, ancNode, descNode);

        Assert.IsTrue(result);
    }

    [Test]
    public void Push_EmptyConstraints_Optional_ReturnsTrue() {
        // C[] → opt(U8):  no ancestor → true
        var anc = ConstraintsState.Empty;
        var ancNode = Node("anc", anc);
        var descOpt = StateOptional.Of(U8);
        var descNode = Node("desc", descOpt);

        var result = Push.Apply(anc, descOpt, ancNode, descNode);

        Assert.IsTrue(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply(StatePrimitive anc, ConstraintsState desc) — sanity
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Push_Primitive_Constraints_AddsAncestor() {
        // Real → C[]  → desc becomes C[..Real]
        var ancNode = Node("anc", Real);
        var desc = ConstraintsState.Empty;
        var descNode = Node("desc", desc);

        var result = Push.Apply(Real, desc, ancNode, descNode);

        Assert.IsTrue(result);
        if (descNode.State is ConstraintsState cs)
        {
            Assert.IsTrue(cs.HasAncestor);
            Assert.AreEqual(Real, cs.Ancestor);
        }
    }

    [Test]
    public void Push_Primitive_Constraints_Incompatible_ReturnsFalse() {
        // Bool → C[I32..]  → I32 ≰ Bool → false
        var ancNode = Node("anc", Bool);
        var desc = ConstraintsState.Of(I32);
        var descNode = Node("desc", desc);

        var result = Push.Apply(Bool, desc, ancNode, descNode);

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
