namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Law tests for debt #12 — S axis (StructBound) transport inside the algebra operators
/// (Algebra.md §Транспорт осей; Algebra_LCA.md / Algebra_GCD.md / Algebra_Merge.md):
///   ∨ (LCA):    S = S₁ ∩ S₂ — field-name intersection, LCA on common field types,
///               a missing bound absorbs to "no bound";
///   ∧ (GCD):    S = S₁ ∪ S₂ — GcdBound field union;
///   ⊓ (Merge):  S = S₁ ∪ S₂ — same union, plus three-way (D, A, S) non-emptiness
///               and S-discharge gating of the collapse rules.
/// Ownerless GcdBound rule: self-referential positions keep node identity (no rewire);
/// ownership transfer stays with stage callers that alias merged nodes.
/// </summary>
public class AlgebraStructBoundLawsTest {

    private static ConstraintsState Cs(
        ITicNodeState desc = null, StatePrimitive anc = null,
        bool cmp = false, bool opt = false, StateStruct bound = null) {
        var cs = ConstraintsState.Of(desc, anc, cmp, opt);
        cs.StructBound = bound;
        return cs;
    }

    private static StateStruct BoundOf(ITicNodeState s) =>
        (s as ConstraintsState)?.StructBound;

    // ================================================================
    // ∨ (LCA): S = S₁ ∩ S₂
    // ================================================================

    [Test]
    public void Lca_BothBounds_DisjointFieldNames_ResultHasNoBound() {
        var a = Cs(bound: StateStruct.Of("left", I32));
        var b = Cs(bound: StateStruct.Of("right", I32));
        var result = ((ITicNodeState)a).Lca(b);
        Assert.IsNull(BoundOf(result), "S₁ ∩ S₂ with disjoint names is empty — no bound");
    }

    [Test]
    public void Lca_BothBounds_CommonField_TypesJoinViaLca() {
        // S₁ = {v:I32, w:Bool}, S₂ = {v:Real, z:Char} → S = {v: LCA(I32,Real)=Real}
        var a = Cs(bound: StateStruct.Of(("v", I32), ("w", Bool)));
        var b = Cs(bound: StateStruct.Of(("v", Real), ("z", Char)));
        var result = ((ITicNodeState)a).Lca(b);
        var s = BoundOf(result);
        Assert.IsNotNull(s);
        Assert.AreEqual(1, s.FieldsCount);
        Assert.AreEqual(Real, s.GetFieldOrNull("v").GetNonReference().State,
            "common field joins covariantly: LCA(I32, Real) = Real");
    }

    [Test]
    public void Lca_OneSidedBound_AbsorbsToNoBound_BothOrders() {
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        var plain = Cs();
        Assert.IsNull(BoundOf(((ITicNodeState)bounded).Lca(plain)),
            "missing bound on the right absorbs: join is the weaker constraint");
        Assert.IsNull(BoundOf(((ITicNodeState)plain).Lca(bounded)),
            "missing bound on the left absorbs symmetrically");
    }

    [Test]
    public void Lca_SameBoundReference_Idempotent() {
        var s = StateStruct.Of("v", I32);
        var result = ((ITicNodeState)Cs(bound: s)).Lca(Cs(bound: s));
        var bound = BoundOf(result);
        Assert.IsNotNull(bound, "S ∩ S = S — the bound must survive the join");
        Assert.AreEqual(1, bound.FieldsCount);
        Assert.AreEqual(I32, bound.GetFieldOrNull("v").GetNonReference().State,
            "S ∩ S = S (value equality; the reference fast path is an implementation detail)");
    }

    [Test]
    public void Lca_CommutativeOnSAxis() {
        var a = Cs(bound: StateStruct.Of(("v", I32), ("w", Bool)));
        var b = Cs(bound: StateStruct.Of(("v", Real), ("w", Bool)));
        var ab = BoundOf(((ITicNodeState)a).Lca(b));
        var ba = BoundOf(((ITicNodeState)b).Lca(a));
        Assert.IsNotNull(ab);
        Assert.IsNotNull(ba);
        Assert.AreEqual(ab.FieldsCount, ba.FieldsCount);
        foreach (var (name, node) in ab.Fields)
            Assert.AreEqual(
                node.GetNonReference().State,
                ba.GetFieldOrNull(name).GetNonReference().State,
                $"field {name} must join order-independently");
    }

    [Test]
    public void Lca_SurvivingBound_BlocksSolvedCompositeCollapse() {
        // Without S the join of two identical solved-struct descendants collapses to the
        // bare struct; a surviving S must keep the constraint form (the bound is still
        // an obligation on the node).
        var solved = StateStruct.Of("v", I32);
        var s = StateStruct.Of("v", I32);
        var a = Cs(desc: solved, bound: s);
        var b = Cs(desc: solved, bound: s);
        var result = ((ITicNodeState)a).Lca(b);
        Assert.IsInstanceOf<ConstraintsState>(result,
            "a surviving S blocks the collapse-to-solved-type shortcut");
        Assert.AreSame(s, BoundOf(result));
    }

    [Test]
    public void Lca_RecursionVariablePosition_DroppedFromIntersection() {
        // A field whose state chain reaches a CS carrying its own bound (μ-back-edge)
        // cannot be joined by a pure operator — it is dropped (sound weakening for ∨).
        var ownerA = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var backEdgeA = TicNode.CreateInvisibleNode(new StateRefTo(ownerA));
        var boundA = new StateStruct(
            new System.Collections.Generic.Dictionary<string, TicNode> { { "next", backEdgeA } },
            isFrozen: true);
        ((ConstraintsState)ownerA.State).StructBound = boundA;

        var boundB = StateStruct.Of("next", I32);
        var result = ((ITicNodeState)ownerA.State).Lca(Cs(bound: boundB));
        Assert.IsNull(BoundOf(result),
            "the only common field is a μ-position — dropped; empty intersection = no bound");
    }

    // ================================================================
    // ∧ (GCD): S = S₁ ∪ S₂
    // ================================================================

    [Test]
    public void Gcd_BothBounds_FieldUnion() {
        var a = Cs(bound: StateStruct.Of("left", I32));
        var b = Cs(bound: StateStruct.Of("right", I32));
        var result = ((ITicNodeState)a).Gcd(b);
        var s = BoundOf(result);
        Assert.IsNotNull(s, "meet of two bounds is their field union");
        Assert.AreEqual(2, s.FieldsCount);
        Assert.IsNotNull(s.GetFieldOrNull("left"));
        Assert.IsNotNull(s.GetFieldOrNull("right"));
    }

    [Test]
    public void Gcd_OneSidedBound_SurvivesNeutralOtherSide_BothOrders() {
        var s = StateStruct.Of("v", I32);
        var bounded = Cs(bound: s);
        var neutral = Cs();
        Assert.AreSame(s, BoundOf(((ITicNodeState)bounded).Gcd(neutral)),
            "meet accumulates obligations: one-sided S is kept");
        Assert.AreSame(s, BoundOf(((ITicNodeState)neutral).Gcd(bounded)),
            "S survival is order-independent");
    }

    [Test]
    public void Gcd_CommutativeOnSAxis() {
        var a = Cs(bound: StateStruct.Of(("v", I32), ("w", Bool)));
        var b = Cs(bound: StateStruct.Of(("v", Real), ("z", Char)));
        var ab = BoundOf(((ITicNodeState)a).Gcd(b));
        var ba = BoundOf(((ITicNodeState)b).Gcd(a));
        Assert.IsNotNull(ab);
        Assert.IsNotNull(ba);
        Assert.AreEqual(3, ab.FieldsCount);
        Assert.AreEqual(3, ba.FieldsCount);
        foreach (var (name, node) in ab.Fields)
            Assert.AreEqual(
                node.GetNonReference().State,
                ba.GetFieldOrNull(name).GetNonReference().State,
                $"field {name} must meet order-independently (shared: GCD(I32,Real)=I32)");
    }

    [Test]
    public void Gcd_BoundVsAny_ReturnsBoundConstraint() {
        var s = StateStruct.Of("v", I32);
        var result = ((ITicNodeState)Cs(bound: s)).Gcd(Any);
        Assert.AreSame(s, BoundOf(result), "Any is the identity of ∧ — the bound survives whole");
    }

    [Test]
    public void Gcd_BoundVsNonAnyPrimitive_ReturnsNull() {
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        Assert.IsNull(((ITicNodeState)bounded).Gcd(Bool),
            "no struct satisfier exists under a primitive");
    }

    [Test]
    public void Gcd_BoundVsPrimitiveAncestorCs_ReturnsNull() {
        // Gcd(CS{S}, CS[∅..Real]): satisfier must be a struct AND ≤ Real — empty.
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        var primitiveBounded = Cs(anc: Real);
        Assert.IsNull(((ITicNodeState)bounded).Gcd(primitiveBounded));
        Assert.IsNull(((ITicNodeState)primitiveBounded).Gcd(bounded), "symmetric");
    }

    [Test]
    public void Gcd_BoundVsSolvedStruct_AbsorbsViaUnion() {
        // struct-≤ IS the F-bound predicate: meet(S, T) = GcdBound(T, S).
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        var solved = StateStruct.Of(("v", I32), ("label", Char));
        var result = ((ITicNodeState)bounded).Gcd(solved);
        var st = result as StateStruct;
        Assert.IsNotNull(st, "solved struct absorbs the bound");
        Assert.AreEqual(2, st.FieldsCount);
        Assert.IsNotNull(st.GetFieldOrNull("v"));
        Assert.IsNotNull(st.GetFieldOrNull("label"));
    }

    [Test]
    public void Gcd_BoundVsArray_ReturnsNull() {
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        Assert.IsNull(((ITicNodeState)bounded).Gcd(StateArray.Of(I32)),
            "different constructors — a struct bound admits no array satisfier");
    }

    // ================================================================
    // ⊓ (Merge): S = S₁ ∪ S₂ + three-way (D, A, S) non-emptiness
    // ================================================================

    [Test]
    public void Merge_OneSidedBound_Survives_BothOrders() {
        // THE debt-#12 latent bug: MergeOrNull used to drop S silently.
        var s = StateStruct.Of("v", I32);
        var bounded = Cs(bound: s);
        var plain = Cs();
        var ab = bounded.MergeOrNull(plain);
        var ba = plain.MergeOrNull(bounded);
        Assert.AreSame(s, BoundOf(ab), "S must survive ⊓ (the old silent drop is the bug)");
        Assert.AreSame(s, BoundOf(ba), "S survival is order-independent");
    }

    [Test]
    public void Merge_BothBounds_FieldUnion() {
        var a = Cs(bound: StateStruct.Of("left", I32));
        var b = Cs(bound: StateStruct.Of("right", I32));
        var result = a.MergeOrNull(b);
        var s = BoundOf(result);
        Assert.IsNotNull(s);
        Assert.AreEqual(2, s.FieldsCount);
        Assert.IsNotNull(s.GetFieldOrNull("left"));
        Assert.IsNotNull(s.GetFieldOrNull("right"));
    }

    [Test]
    public void Merge_SharedBoundField_MeetsPerGcdBound() {
        // Shared field types meet (narrower): GCD(I32, Real) = I32 — GcdBound semantics.
        var a = Cs(bound: StateStruct.Of("v", I32));
        var b = Cs(bound: StateStruct.Of("v", Real));
        var s = BoundOf(a.MergeOrNull(b));
        Assert.IsNotNull(s);
        Assert.AreEqual(I32, s.GetFieldOrNull("v").GetNonReference().State);
    }

    [Test]
    public void Merge_CommutativeOnSAxis() {
        var a = Cs(bound: StateStruct.Of(("v", I32), ("w", Bool)));
        var b = Cs(bound: StateStruct.Of(("v", Real), ("z", Char)));
        var ab = BoundOf(a.MergeOrNull(b));
        var ba = BoundOf(b.MergeOrNull(a));
        Assert.IsNotNull(ab);
        Assert.IsNotNull(ba);
        Assert.AreEqual(ab.FieldsCount, ba.FieldsCount);
        foreach (var (name, node) in ab.Fields)
            Assert.AreEqual(
                node.GetNonReference().State,
                ba.GetFieldOrNull(name).GetNonReference().State,
                $"field {name} must merge order-independently");
    }

    [Test]
    public void Merge_BoundVsNonAnyPrimitiveAncestor_ReturnsNull() {
        // Three-way (D, A, S): a struct bound with a non-Any primitive upper bound is empty.
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        var primitiveBounded = Cs(anc: Real);
        Assert.IsNull(bounded.MergeOrNull(primitiveBounded));
        Assert.IsNull(primitiveBounded.MergeOrNull(bounded), "symmetric");
    }

    [Test]
    public void Merge_BoundVsComparable_ReturnsNull() {
        var bounded = Cs(bound: StateStruct.Of("v", I32));
        Assert.IsNull(bounded.MergeOrNull(Cs(cmp: true)), "structs are never Comparable");
    }

    [Test]
    public void Merge_PointCollapseWithBound_ReturnsNull() {
        // [I32..∅]{S} ⊓ [∅..I32] would collapse to the point I32 — a primitive point never
        // satisfies a struct bound, so Sat is empty (per satisfier semantics).
        var bounded = Cs(desc: I32, bound: StateStruct.Of("v", I32));
        var upper = Cs(anc: I32);
        Assert.IsNull(bounded.MergeOrNull(upper));
        Assert.IsNull(upper.MergeOrNull(bounded), "symmetric");
    }

    [Test]
    public void Merge_SolvedStructDescendant_FittingBound_CollapsesAndDischarges() {
        // [D..∅, S] with solved struct D that fits S → collapse to D (S discharged).
        var d = StateStruct.Of(("v", I32), ("label", Char));
        var bounded = Cs(desc: d, bound: StateStruct.Of("v", I32));
        var result = bounded.MergeOrNull(Cs());
        Assert.IsInstanceOf<StateStruct>(result,
            "D fits S — collapse pins the node to D, discharging the bound");
        Assert.AreSame(d, result);
    }

    [Test]
    public void Merge_SolvedStructDescendant_NotFittingBoundByType_KeepsConstraintForm() {
        // D = {v:Bool} vs S = {v:I32}: names match (three-way check passes) but the
        // covariant field check fails — collapse must NOT discharge; constraint form kept.
        var d = StateStruct.Of("v", Bool);
        var s = StateStruct.Of("v", I32);
        var bounded = Cs(desc: d, bound: s);
        var result = bounded.MergeOrNull(Cs());
        Assert.IsInstanceOf<ConstraintsState>(result,
            "unfitting D must not silently discharge S via collapse");
        Assert.AreSame(s, BoundOf(result));
    }

    [Test]
    public void Merge_SolvedStructDescendant_MissingBoundField_ReturnsNull() {
        // D = {w} vs S = {v}: every T ≥ D has fields ⊆ fields(D), so no satisfier can
        // provide v — Sat is empty (three-way check).
        var bounded = Cs(desc: StateStruct.Of("w", I32), bound: StateStruct.Of("v", I32));
        Assert.IsNull(bounded.MergeOrNull(Cs()));
    }

    [Test]
    public void Merge_SelfReferentialBound_PreservesBackEdgeIdentity() {
        // Ownerless-mode law: a unique bound field carrying a μ-back-edge keeps its ORIGINAL
        // node (no rewire) — the stage's node aliasing transfers ownership later.
        var owner = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var backEdge = TicNode.CreateInvisibleNode(new StateRefTo(owner));
        var boundA = new StateStruct(
            new System.Collections.Generic.Dictionary<string, TicNode> { { "next", backEdge } },
            isFrozen: true);
        ((ConstraintsState)owner.State).StructBound = boundA;

        var other = Cs(bound: StateStruct.Of("v", I32));
        var s = BoundOf(((ConstraintsState)owner.State).MergeOrNull(other));
        Assert.IsNotNull(s);
        Assert.AreEqual(2, s.FieldsCount);
        Assert.AreSame(backEdge, s.GetFieldOrNull("next"),
            "self-referential position keeps node identity through ownerless GcdBound");
    }

    [Test]
    public void Merge_SharedSelfReferentialField_KeepsSelfRefSide() {
        // Shared field: μ-position on one side, concrete on the other — the self-referential
        // side wins (GcdBound's instantiation rule), preserving its node.
        var owner = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var backEdge = TicNode.CreateInvisibleNode(new StateRefTo(owner));
        var boundA = new StateStruct(
            new System.Collections.Generic.Dictionary<string, TicNode> { { "next", backEdge } },
            isFrozen: true);
        ((ConstraintsState)owner.State).StructBound = boundA;

        var other = Cs(bound: StateStruct.Of("next", I32));
        var s = BoundOf(((ConstraintsState)owner.State).MergeOrNull(other));
        Assert.IsNotNull(s);
        Assert.AreSame(backEdge, s.GetFieldOrNull("next"),
            "shared μ-position keeps the self-referential side's node");
    }

    [Test]
    public void Unify_BoundSurvivesUnification() {
        // Unify(CS,CS) ≝ Merge — the S axis must survive it like opt/Preferred do.
        var s = StateStruct.Of("v", I32);
        var result = ((ITicNodeState)Cs(bound: s)).UnifyOrNull(Cs());
        Assert.AreSame(s, BoundOf(result));
    }

    // ================================================================
    // Cycle-merge reproduction (the latent bug of debt #12)
    // ================================================================

    [Test]
    public void MergeGroup_CycleWithBoundCarrier_PreservesStructBound() {
        // Toposort cycle merge routes through GetMergedStateOrNull(CS,CS) → MergeOrNull,
        // which silently dropped S before the fix — the F-bound vanished from merged
        // recursion variables.
        var s = StateStruct.Of("v", I32);
        var a = TicNode.CreateTypeVariableNode("a", Cs(bound: s));
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Of(desc: StateStruct.Of("v", I32)));
        a.AddAncestor(b);
        b.AddAncestor(a);

        var main = SolvingFunctions.MergeGroup(new[] { a, b });

        var merged = main.GetNonReference().State;
        // The merged state either keeps the constraint form with S, or collapsed to a
        // solved struct that discharges S — both preserve the bound's obligation.
        if (merged is ConstraintsState cs)
            Assert.IsTrue(cs.HasStructBound, $"cycle merge must not drop S; got {merged}");
        else
            Assert.IsInstanceOf<StateStruct>(merged,
                $"cycle merge may discharge S only into a fitting solved struct; got {merged}");
    }

    [Test]
    public void MergeGroup_CycleBoundCarrierWithEmptyPeer_KeepsBound() {
        var s = StateStruct.Of(("v", I32), ("next", Any));
        var a = TicNode.CreateTypeVariableNode("a", Cs(bound: s));
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Empty);
        a.AddAncestor(b);
        b.AddAncestor(a);

        var main = SolvingFunctions.MergeGroup(new[] { a, b });
        var cs = main.GetNonReference().State as ConstraintsState;
        Assert.IsNotNull(cs, "merge of a bound carrier with an empty CS stays a CS");
        Assert.AreSame(s, cs.StructBound, "one-sided S keeps reference identity");
    }

    [Test]
    public void GetMergedState_StructVsBoundCarrierCs_AbsorbsBound() {
        // The struct×CS arm of the cycle merge: a solved struct meeting CS{S} must absorb
        // the bound's required fields (GcdBound meet) instead of dropping them.
        var solid = StateStruct.Of(("v", I32), ("label", Char));
        var carrier = Cs(bound: StateStruct.Of("v", I32));
        var merged = SolvingFunctions.GetMergedStateOrNull(solid, carrier);
        var st = merged as StateStruct;
        Assert.IsNotNull(st, $"expected struct, got {merged}");
        Assert.IsNotNull(st.GetFieldOrNull("v"));
        Assert.IsNotNull(st.GetFieldOrNull("label"));
    }

    [Test]
    public void GetMergedState_StructMissingBoundField_AbsorbsRequiredField() {
        // GcdBound union semantics (same as the Pull stage's Apply(CS{S}, Struct)): the
        // bound's required field is materialized on the merged struct rather than rejected —
        // rejection is GcdFrozenAndOpen's job and applies only to frozen-vs-open pairs.
        var solid = StateStruct.Of("w", I32);
        var carrier = Cs(bound: StateStruct.Of("v", I32));
        var merged = SolvingFunctions.GetMergedStateOrNull(solid, carrier) as StateStruct;
        Assert.IsNotNull(merged);
        Assert.IsNotNull(merged.GetFieldOrNull("w"));
        Assert.IsNotNull(merged.GetFieldOrNull("v"), "bound's required field materialized");
    }

    // ================================================================
    // Coinductive context (AlgebraCycleContext) — hostile μ-shapes that cycle ACROSS
    // the family's bridges (struct-∨ → CS∨CS → S-axis join; Unify → Merge →
    // AddDescendant → ∨). The explicit context must discharge the re-entry; a missed
    // bridge diverges (stack overflow) here.
    // ================================================================

    [Test]
    public void Lca_MutuallyRecursiveBounds_ThroughStructLayer_Terminates() {
        // The bounds' common field is a STRUCT layer — ContainsRecursionVariable does not
        // descend struct layers, so the μ-position drop does NOT fire and the join recurses
        // across the bridges: IntersectBoundsOrNull(SA, SB) → field ∨ → struct∨struct →
        // inner field Unify ≝ Merge → GcdBound S-union (memoized) — every hop must carry
        // the assumption set; a missed bridge diverges here.
        var csA = Cs();
        var csB = Cs();
        var boundA = StateStruct.Of("v", StateStruct.Of("w", csA));
        var boundB = StateStruct.Of("v", StateStruct.Of("w", csB));
        csA.StructBound = boundA; // close the μ-knots
        csB.StructBound = boundB;

        var result = ((ITicNodeState)csA).Lca(csB);

        var s = BoundOf(result);
        Assert.IsNotNull(s, "outer field v is joinable — the bound survives");
        Assert.IsNotNull(s.GetFieldOrNull("v"));
        var vState = s.GetFieldOrNull("v").GetNonReference().State as StateStruct;
        Assert.IsNotNull(vState, "v joins as a struct layer");
        var wState = (vState.GetFieldOrNull("w").GetNonReference().State as ConstraintsState);
        Assert.IsNotNull(wState, "w stays an unsolved CS");
        Assert.IsNotNull(wState.StructBound,
            "inner field pair routes through Unify ≝ Merge: S = S₁ ∪ S₂ (GcdBound union, "
            + "μ-positions keep node identity) — the union bound survives on w");
        Assert.IsNotNull(wState.StructBound.GetFieldOrNull("v"),
            "the union carries the bounds' required field");
    }

    [Test]
    public void Lca_NamedMuKnot_ReenteredThroughMergeDescendantJoin_Terminates() {
        // t = {next: CS[desc = RefTo(t-node)]} — a REAL μ-knot (cyclic node graph).
        // Lca(t, t') enters the named-∨ arm, the field CS∨CS goes through Unify → Merge →
        // AddDescendant → Lca(t, t') again. The named hypothesis must survive the
        // Merge/AddDescendant hop (the bridge the removed ThreadStatic covered ambiently).
        StateStruct MakeKnot() {
            var owner = TicNode.CreateTypeVariableNode("t", ConstraintsState.Empty);
            var t = StateStruct.Of("next", Cs(desc: new StateRefTo(owner)));
            t.TypeName = "t";
            owner.State = t;
            return t;
        }

        var tA = MakeKnot();
        var tB = MakeKnot();
        var result = ((ITicNodeState)tA).Lca(tB);

        var st = result as StateStruct;
        Assert.IsNotNull(st, $"expected struct, got {result}");
        Assert.AreEqual("t", st.TypeName, "named identity survives the join");
        Assert.IsNotNull(st.GetFieldOrNull("next"));
    }

    [Test]
    public void Gcd_NamedMuKnot_ReenteredThroughFieldMeet_Terminates() {
        // Same knot through the ∧ family: struct-∧ named arm → field Gcd → CS ancestors /
        // descendants → named struct pair again.
        StateStruct MakeKnot() {
            var owner = TicNode.CreateTypeVariableNode("t", ConstraintsState.Empty);
            var t = StateStruct.Of("next", Cs(desc: new StateRefTo(owner)));
            t.TypeName = "t";
            owner.State = t;
            return t;
        }

        var tA = MakeKnot();
        var tB = MakeKnot();
        var result = ((ITicNodeState)tA).Gcd(tB);

        var st = result as StateStruct;
        Assert.IsNotNull(st, $"expected struct, got {result}");
        Assert.AreEqual("t", st.TypeName);
    }

    // ================================================================
    // FitStructBound: per-field covariant recursion (Algebra_Fit §2 п.4).
    // A composite concrete bound field must be checked recursively —
    // NOT degenerate to `≤c Any` (shallow accept-everything).
    // ================================================================

    [Test]
    public void Fit_CompositeBoundField_MismatchedElement_DoesNotFit() {
        // S = {items: arr(I32)}, T = {items: arr(arr(Char))} — same field name, same
        // outer constructor, incompatible elements: T.items ≰c S.items → T ∉ Sat(S).
        var cs = Cs(bound: StateStruct.Of("items", StateArray.Of(I32)));
        var candidate = StateStruct.Of("items", StateArray.Of(StateArray.Of(Char)));
        Assert.IsFalse(cs.CanBeConvertedTo(candidate));
    }

    [Test]
    public void Fit_CompositeBoundField_ShapeMismatch_DoesNotFit() {
        // S = {items: arr(I32)}, T = {items: I32} — primitive field cannot satisfy
        // a composite bound field.
        var cs = Cs(bound: StateStruct.Of("items", StateArray.Of(I32)));
        var candidate = StateStruct.Of("items", I32);
        Assert.IsFalse(cs.CanBeConvertedTo(candidate));
    }

    [Test]
    public void Fit_CompositeBoundField_ExactMatch_Fits() {
        var cs = Cs(bound: StateStruct.Of("items", StateArray.Of(I32)));
        var candidate = StateStruct.Of("items", StateArray.Of(I32));
        Assert.IsTrue(cs.CanBeConvertedTo(candidate));
    }

    [Test]
    public void Fit_CompositeBoundField_CovariantElement_Fits() {
        // arr is covariant: arr(U8) ≤c arr(I32) because U8 ≤c I32.
        var cs = Cs(bound: StateStruct.Of("items", StateArray.Of(I32)));
        var candidate = StateStruct.Of("items", StateArray.Of(U8));
        Assert.IsTrue(cs.CanBeConvertedTo(candidate));
    }
}
