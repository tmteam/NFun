namespace NFun.Tic.Tests.UnitTests;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Property-style law tests for the TIC algebra operators, closing debt #20
/// (test-coverage holes of the algebra laws):
///  * the primitive LCA/GCD tables are checked EXHAUSTIVELY over the full 23-point
///    lattice against an independent brute-force LUB/GLB oracle,
///  * LCA/GCD monotonicity, lattice absorption, GCD null-propagation,
///  * Ref-transparency of both operators,
///  * Concretest/Abstractest idempotence and Fit-compatibility,
///  * LCA+Concretest law 4 of the umbrella spec (T ∨ CS = T ∨ ↓CS).
/// Style follows AlgebraInvariantsTest / AlgebraGcdFitLawsTest.
/// </summary>
public class AlgebraLatticeLawsTest {

    // Full 23-point primitive lattice, incl. the abstract mid-points and None.
    private static readonly StatePrimitive[] Lattice = {
        Any, Char, Bool, Ip,
        Real, F32, I96, I64, I48, I32, I24, I16, I12, I8,
        U64, U48, U32, U24, U16, U12, U8, U4,
        None,
    };

    /// <summary>
    /// Independent subtyping oracle `a ≤ b` — derived from the first principles of the
    /// lattice (Specs/Tic/TicTypeSystem.md), NOT from the LcaMap/GcdMap closure formulas:
    ///   * Any is the top; None ≤ Any only; Char/Bool/Ip relate only to self and Any.
    ///   * Real is the numeric top; F32 sits below Real and above every integer;
    ///     I96 is the integer top.
    ///   * I_n ≤ I_k iff n ≤ k;  U_m ≤ U_j iff m ≤ j.
    ///   * U_m ≤ I_n iff n ≥ m + 1 (a sign bit is required); I_n ≤ U_m — never.
    ///   * U4 has nominal width 7 (the common subset of I8 and U8).
    /// </summary>
    private static bool Leq(StatePrimitive a, StatePrimitive b) {
        if (a.Equals(b)) return true;
        if (b.Equals(Any)) return true;
        if (a.Equals(Any)) return false;
        if (a.Equals(None) || b.Equals(None)) return false;
        if (!a.IsNumeric || !b.IsNumeric) return false; // Char/Bool/Ip are incomparable
        if (b.Equals(Real)) return true;
        if (a.Equals(Real)) return false;
        if (b.Equals(F32)) return true;  // every integer ≤ F32
        if (a.Equals(F32)) return false;
        if (b.Equals(I96)) return true;  // every integer ≤ I96
        if (a.Equals(I96)) return false;

        var (aSigned, aWidth) = SignAndWidth(a);
        var (bSigned, bWidth) = SignAndWidth(b);
        if (aSigned == bSigned) return aWidth <= bWidth;
        if (!aSigned && bSigned) return bWidth >= aWidth + 1;
        return false; // signed never fits into unsigned
    }

    private static (bool signed, int width) SignAndWidth(StatePrimitive p) =>
        p.Name switch {
            PrimitiveTypeName.I64 => (true, 64),
            PrimitiveTypeName.I48 => (true, 48),
            PrimitiveTypeName.I32 => (true, 32),
            PrimitiveTypeName.I24 => (true, 24),
            PrimitiveTypeName.I16 => (true, 16),
            PrimitiveTypeName.I12 => (true, 12),
            PrimitiveTypeName.I8 => (true, 8),
            PrimitiveTypeName.U64 => (false, 64),
            PrimitiveTypeName.U48 => (false, 48),
            PrimitiveTypeName.U32 => (false, 32),
            PrimitiveTypeName.U24 => (false, 24),
            PrimitiveTypeName.U16 => (false, 16),
            PrimitiveTypeName.U12 => (false, 12),
            PrimitiveTypeName.U8 => (false, 8),
            PrimitiveTypeName.U4 => (false, 7),
            _ => throw new System.InvalidOperationException($"{p} is not a fixed-width integer")
        };

    // ================================================================
    // Exhaustive primitive tables vs the independent oracle (23×23)
    // ================================================================

    [Test]
    public void Order_MatchesIndependentOracle() {
        // CanBePessimisticConvertedTo must coincide with the first-principles order.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
            Assert.AreEqual(Leq(a, b), a.CanBePessimisticConvertedTo(b),
                $"Order disagreement: {a} ≤ {b} must be {Leq(a, b)}");
    }

    [Test]
    public void Lca_PrimitiveTable_IsLeastUpperBound() {
        // LcaMap must be THE least upper bound of the independent order, on all 23² pairs.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            var upper = Lattice.Where(c => Leq(a, c) && Leq(b, c)).ToList();
            Assert.IsNotEmpty(upper, $"Lattice defect: no common upper bound for ({a}, {b})");
            var minimal = upper.Where(c => upper.All(o => !Leq(o, c) || o.Equals(c))).ToList();
            Assert.AreEqual(1, minimal.Count,
                $"Lattice defect: LUB({a}, {b}) is not unique: {string.Join(", ", minimal)}");
            Assert.AreEqual(minimal[0], a.GetLastCommonPrimitiveAncestor(b),
                $"LCA({a}, {b}) must be the least upper bound {minimal[0]}");
        }
    }

    [Test]
    public void Gcd_PrimitiveTable_IsGreatestLowerBound() {
        // GcdMap must be THE greatest lower bound where one exists, and null otherwise.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            var lower = Lattice.Where(c => Leq(c, a) && Leq(c, b)).ToList();
            var gcd = a.GetFirstCommonDescendantOrNull(b);
            if (lower.Count == 0)
            {
                Assert.IsNull(gcd, $"GCD({a}, {b}) must be null: no common lower bound exists");
                continue;
            }
            var maximal = lower.Where(c => lower.All(o => !Leq(c, o) || o.Equals(c))).ToList();
            Assert.AreEqual(1, maximal.Count,
                $"Lattice defect: GLB({a}, {b}) is not unique: {string.Join(", ", maximal)}");
            Assert.AreEqual(maximal[0], gcd,
                $"GCD({a}, {b}) must be the greatest lower bound {maximal[0]}");
        }
    }

    // ================================================================
    // Monotonicity: A ≤ B ⟹ A ∨ C ≤ B ∨ C and A ∧ C ≤ B ∧ C  (23³ sweep)
    // ================================================================

    [Test]
    public void Lca_Monotone_FullLattice() {
        // State-level ∨ (None joins through the Optional axis), ≤ is FitsInto.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            if (!((ITicNodeState)a).FitsInto(b)) continue;
            foreach (var c in Lattice)
            {
                var ac = ((ITicNodeState)a).Lca(c);
                var bc = ((ITicNodeState)b).Lca(c);
                Assert.IsTrue(ac.FitsInto(bc),
                    $"LCA monotonicity violated: {a} ≤ {b} but ({a} ∨ {c}) = {ac} ≰ ({b} ∨ {c}) = {bc}");
            }
        }
    }

    [Test]
    public void Gcd_Monotone_FullLattice() {
        // A ≤ B ⟹ (A ∧ C ≠ null ⟹ B ∧ C ≠ null ∧ A∧C ≤ B∧C):
        // A∧C is a common lower bound of B and C, so the meet with B must exist and dominate it.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            if (!((ITicNodeState)a).FitsInto(b)) continue;
            foreach (var c in Lattice)
            {
                var ac = ((ITicNodeState)a).Gcd(c);
                if (ac == null) continue;
                var bc = ((ITicNodeState)b).Gcd(c);
                Assert.IsNotNull(bc,
                    $"GCD monotonicity violated: {a} ≤ {b}, {a} ∧ {c} = {ac} but {b} ∧ {c} is null");
                Assert.IsTrue(ac.FitsInto(bc),
                    $"GCD monotonicity violated: {a} ≤ {b} but ({a} ∧ {c}) = {ac} ≰ ({b} ∧ {c}) = {bc}");
            }
        }
    }

    // ================================================================
    // Lattice absorption: A ∨ (A ∧ B) = A and A ∧ (A ∨ B) = A  (23² sweep)
    // ================================================================

    [Test]
    public void Absorption_LcaOverGcd_FullLattice() {
        // A ∨ (A ∧ B) = A. Pairs where ∧ is undefined are skipped — and the skip set
        // must be EXACTLY the pairs with no common lower bound in the independent order
        // (the cross-family nulls: Char/Bool/Ip/None vs anything unrelated).
        int skipped = 0;
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            var gcd = ((ITicNodeState)a).Gcd(b);
            var oracleHasGlb = Lattice.Any(c => Leq(c, a) && Leq(c, b));
            Assert.AreEqual(oracleHasGlb, gcd != null,
                $"GCD({a}, {b}) definedness must match the oracle (common lower bound exists: {oracleHasGlb})");
            if (gcd == null)
            {
                skipped++;
                continue;
            }
            var absorbed = ((ITicNodeState)a).Lca(gcd);
            Assert.AreEqual(a, absorbed,
                $"Absorption violated: {a} ∨ ({a} ∧ {b}) = {a} ∨ {gcd} = {absorbed}, expected {a}");
        }
        var expectedSkips = Lattice.Sum(
            a => Lattice.Count(b => !Lattice.Any(c => Leq(c, a) && Leq(c, b))));
        Assert.AreEqual(expectedSkips, skipped, "Skip set must match the known cross-family nulls");
    }

    [Test]
    public void Absorption_GcdOverLca_FullLattice() {
        // A ∧ (A ∨ B) = A. The join always exists, and A is a lower bound of it,
        // so the meet is always defined — no skips on this direction.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            var lca = ((ITicNodeState)a).Lca(b);
            var absorbed = ((ITicNodeState)a).Gcd(lca);
            Assert.AreEqual(a, absorbed,
                $"Absorption violated: {a} ∧ ({a} ∨ {b}) = {a} ∧ {lca} = {absorbed}, expected {a}");
        }
    }

    // ================================================================
    // GCD null-propagation: A ∧ B = null ⟹ ∀C ≤ A: C ∧ B = null  (23³ sweep)
    // ================================================================

    [Test]
    public void Gcd_NullPropagation_FullLattice() {
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        {
            if (((ITicNodeState)a).Gcd(b) != null) continue;
            foreach (var c in Lattice)
            {
                if (!((ITicNodeState)c).FitsInto(a)) continue;
                Assert.IsNull(((ITicNodeState)c).Gcd(b),
                    $"Null-propagation violated: {a} ∧ {b} = null, {c} ≤ {a}, but {c} ∧ {b} ≠ null");
            }
        }
    }

    // ================================================================
    // GCD associativity on the full primitive lattice (where defined)
    // ================================================================

    [Test]
    public void Gcd_Associativity_Primitives_FullLattice() {
        // (A ∧ B) ∧ C = A ∧ (B ∧ C) — when both intermediate meets are defined.
        foreach (var a in Lattice)
        foreach (var b in Lattice)
        foreach (var c in Lattice)
        {
            var ab = ((ITicNodeState)a).Gcd(b);
            var bc = ((ITicNodeState)b).Gcd(c);
            if (ab == null || bc == null) continue;
            var ab_c = ab.Gcd(c);
            var a_bc = ((ITicNodeState)a).Gcd(bc);
            Assert.AreEqual(ab_c, a_bc,
                $"GCD associativity violated for ({a}, {b}, {c}): ({ab})∧{c}={ab_c} but {a}∧({bc})={a_bc}");
        }
    }

    // ================================================================
    // Ref-transparency: Ref(A) ∘ B = A ∘ B for ∨ and ∧  (T ∪ C sample)
    // ================================================================

    private static ConstraintsState Cs(
        ITicNodeState desc = null, StatePrimitive anc = null,
        bool cmp = false, bool opt = false, StatePrimitive preferred = null) {
        var cs = ConstraintsState.Of(desc, anc, cmp, opt);
        cs.Preferred = preferred;
        return cs;
    }

    private static ITicNodeState Ref(ITicNodeState state) =>
        new StateRefTo(TicNode.CreateInvisibleNode(state));

    // T ∪ C sample: primitives, CS shapes (flags/Preferred), composites with CS innards.
    private static IEnumerable<ITicNodeState> StateZoo() {
        yield return U8;
        yield return I32;
        yield return I64;
        yield return F32;
        yield return Real;
        yield return Char;
        yield return Bool;
        yield return Any;
        yield return None;
        yield return Cs();
        yield return Cs(desc: U8);
        yield return Cs(anc: Real);
        yield return Cs(desc: U8, anc: Real);
        yield return Cs(desc: I16, anc: I64, cmp: true);
        yield return Cs(desc: U8, anc: Real, preferred: I32);
        yield return Cs(desc: U8, opt: true);
        yield return StateArray.Of(I32);
        yield return StateArray.Of(Cs(desc: U8, anc: Real));
        yield return StateStruct.Of("a", I32);
        yield return StateStruct.Of("a", Cs(desc: I16, anc: I64));
        yield return StateFun.Of(new ITicNodeState[] { I32 }, Real);
        yield return StateFun.Of(new ITicNodeState[] { Cs(anc: Real) }, Cs(desc: U8));
        yield return StateOptional.Of(I32);
        yield return StateOptional.Of(Cs(desc: U8, anc: Real));
    }

    // Results may contain fresh invisible nodes (Fun/Struct rebuilds), and
    // StateFun.Equals falls back to node identity on mutable args — compare the
    // printed structure instead.
    private static string Print(ITicNodeState s) => s == null ? "<null>" : s.StateDescription;

    [Test]
    public void Lca_RefTransparent() {
        foreach (var a in StateZoo())
        foreach (var b in StateZoo())
        {
            var plain = Print(a.Lca(b));
            Assert.AreEqual(plain, Print(Ref(a).Lca(b)),
                $"Ref-transparency violated: Ref({Print(a)}) ∨ {Print(b)}");
            Assert.AreEqual(plain, Print(a.Lca(Ref(b))),
                $"Ref-transparency violated: {Print(a)} ∨ Ref({Print(b)})");
            Assert.AreEqual(plain, Print(Ref(Ref(a)).Lca(Ref(b))),
                $"Ref-transparency violated: Ref(Ref({Print(a)})) ∨ Ref({Print(b)})");
        }
    }

    [Test]
    public void Gcd_RefTransparent() {
        foreach (var a in StateZoo())
        foreach (var b in StateZoo())
        {
            var plain = Print(a.Gcd(b));
            Assert.AreEqual(plain, Print(Ref(a).Gcd(b)),
                $"Ref-transparency violated: Ref({Print(a)}) ∧ {Print(b)}");
            Assert.AreEqual(plain, Print(a.Gcd(Ref(b))),
                $"Ref-transparency violated: {Print(a)} ∧ Ref({Print(b)})");
            Assert.AreEqual(plain, Print(Ref(Ref(a)).Gcd(Ref(b))),
                $"Ref-transparency violated: Ref(Ref({Print(a)})) ∧ Ref({Print(b)})");
        }
    }

    // ================================================================
    // Concretest / Abstractest: idempotence  ↓↓ = ↓, ↑↑ = ↑
    // ================================================================

    // CS shapes per the C fragment incl. flags/Preferred and optional-flag forms (Rule B).
    private static IEnumerable<ConstraintsState> CsShapes() {
        yield return Cs();
        yield return Cs(desc: U8);
        yield return Cs(anc: Real);
        yield return Cs(desc: U8, anc: Real);
        yield return Cs(desc: I16, anc: I64);
        yield return Cs(cmp: true);
        yield return Cs(desc: U8, anc: Real, cmp: true);
        yield return Cs(desc: I16, preferred: I32);
        yield return Cs(desc: U8, anc: Real, preferred: I32);
        yield return Cs(opt: true);
        yield return Cs(desc: U8, opt: true);
        yield return Cs(anc: Real, opt: true);
        yield return Cs(desc: U8, anc: Real, opt: true);
        yield return Cs(desc: U8, anc: Real, opt: true, preferred: I32);
        // Debt #19 (pure ↓): the exact shapes of the two extracted resolution arms —
        // optional-with-hint (no ancestor) and hint-carrying array elements at depth.
        yield return Cs(desc: U8, opt: true, preferred: I32);
        yield return Cs(desc: StateArray.Of(I32));
        yield return Cs(desc: StateArray.Of(Cs(desc: U8)), opt: true);
        yield return Cs(desc: StateArray.Of(Cs(desc: U8, preferred: I32)));
        yield return Cs(desc: StateArray.Of(Cs(desc: U8, anc: Real, preferred: I32)), opt: true);
    }

    private static IEnumerable<ITicNodeState> ProjectionZoo() {
        foreach (var p in Lattice) yield return p;
        foreach (var cs in CsShapes()) yield return cs;
        yield return StateArray.Of(I32);
        yield return StateArray.Of(Cs(desc: U8, anc: Real));
        yield return StateArray.Of(Cs(desc: U8, preferred: I32));
        yield return StateArray.Of(Cs(desc: U8, anc: Real, opt: true, preferred: I32));
        yield return StateArray.Of(StateArray.Of(Cs(desc: U8, preferred: I32)));
        yield return StateArray.Of(StateArray.Of(Cs()));
        yield return StateStruct.Of("a", I32);
        yield return StateStruct.Of("a", Cs(desc: I16, anc: I64));
        yield return StateFun.Of(new ITicNodeState[] { I32 }, Real);
        yield return StateFun.Of(new ITicNodeState[] { Cs(anc: Real) }, Cs(desc: U8));
        yield return StateOptional.Of(I32);
        yield return StateOptional.Of(StateArray.Of(I32));
        yield return StateOptional.Of(Cs(desc: U8, anc: Real));
        yield return Ref(Cs(desc: U8, anc: Real));
    }

    [Test]
    public void Concretest_Idempotent() {
        foreach (var s in ProjectionZoo())
        {
            var once = s.Concretest();
            var twice = once.Concretest();
            Assert.AreEqual(Print(once), Print(twice),
                $"↓↓ ≠ ↓ for {Print(s)}: ↓ = {Print(once)}, ↓↓ = {Print(twice)}");
        }
    }

    [Test]
    public void Abstractest_Idempotent() {
        foreach (var s in ProjectionZoo())
        {
            var once = s.Abstractest();
            var twice = once.Abstractest();
            Assert.AreEqual(Print(once), Print(twice),
                $"↑↑ ≠ ↑ for {Print(s)}: ↑ = {Print(once)}, ↑↑ = {Print(twice)}");
        }
    }

    // ================================================================
    // Fit-compatibility of the projections: ↓CS Fit CS, ↑CS Fit CS
    // ================================================================

    // `↓CS Fit CS` (Algebra_Concretest.md) — verified over the WHOLE C fragment since the
    // #16 predicate unification (the two previously-refuted sub-fragments are covered by
    // the dedicated tests below and included in the full sweep here).
    [Test]
    public void Concretest_FitsBackInto_Cs() {
        foreach (var cs in CsShapes())
        {
            var down = ((ITicNodeState)cs).Concretest();
            Assert.IsTrue(down.FitsInto(cs),
                $"↓CS Fit CS violated: ↓{Print(cs)} = {Print(down)} does not fit back");
        }
    }

    [Test]
    // Un-ignored 2026-07-09 (debt #16 closure): the unsolved-target ancestor cell of the
    // authoritative predicate is now existential — Fit(⊥, [∅..A]) = Fit(⊥, A) = true
    // (a desc-less unsolved target can still narrow under A; with its own ancestor A_t the
    // obligation is GCD(A_t, A) ≠ ∅).
    public void Concretest_FitsBackInto_AncestorOnlyCs() {
        foreach (var cs in new[] { Cs(anc: Real), Cs(anc: I64), Cs(anc: Real, opt: true) })
        {
            var down = ((ITicNodeState)cs).Concretest();
            Assert.IsTrue(down.FitsInto(cs),
                $"↓CS Fit CS violated: ↓{Print(cs)} = {Print(down)} does not fit back");
        }
    }

    [Test]
    // Un-ignored 2026-07-09 (debt #16 closure): the authoritative predicate carries the
    // Optional axis (opt(X) ≤ C? ⟺ X satisfies the plain cells) and the primitive-desc arm
    // of CanBeFitConverted got the same `D ≤ opt(X) ⟺ D ≤ X` lift the composite arm had.
    public void Concretest_FitsBackInto_OptionalPrimitiveDescCs() {
        foreach (var cs in new[] {
                     Cs(desc: U8, opt: true),
                     Cs(desc: U8, anc: Real, opt: true),
                     Cs(desc: U8, anc: Real, opt: true, preferred: I32),
                 })
        {
            var down = ((ITicNodeState)cs).Concretest();
            Assert.IsTrue(down.FitsInto(cs),
                $"↓CS Fit CS violated on the optional axis: ↓{Print(cs)} = {Print(down)} does not fit back");
        }
    }

    [Test]
    public void Abstractest_FitsBackInto_NonOptionalNonCmpCs() {
        // Spec domain: C non-optional, non-comparable (Algebra_Abstractest.md).
        foreach (var cs in CsShapes().Where(c => !c.IsOptional && !c.IsComparable))
        {
            var up = ((ITicNodeState)cs).Abstractest();
            Assert.IsTrue(up.FitsInto(cs),
                $"↑CS Fit CS violated: ↑{Print(cs)} = {Print(up)} does not fit back");
        }
    }

    [Test]
    public void BoundsOrder_ConcretestLeqAbstractest_NonOptionalCs() {
        // ↓CS ≤ ↑CS on non-optional CS (on the optional axis the law is FALSE by design:
        // ↑ drops the opt axis while ↓ keeps it — a paired contract with Destruction).
        foreach (var cs in CsShapes().Where(c => !c.IsOptional))
        {
            var down = ((ITicNodeState)cs).Concretest();
            var up = ((ITicNodeState)cs).Abstractest();
            Assert.IsTrue(down.FitsInto(up),
                $"↓CS ≤ ↑CS violated: ↓{Print(cs)} = {Print(down)} ≰ ↑ = {Print(up)}");
        }
    }

    // ================================================================
    // Umbrella law 4: T ∨ CS = T ∨ ↓CS  (C: non-optional, Preferred-free)
    // ================================================================

    private static IEnumerable<ITicNodeState> SolvedTypes() {
        yield return U8;
        yield return I32;
        yield return I64;
        yield return F32;
        yield return Real;
        yield return Char;
        yield return Bool;
        yield return Any;
        yield return None;
        yield return StateArray.Of(I32);
        yield return StateArray.Of(Char);
        yield return StateStruct.Of("a", I32);
        yield return StateFun.Of(new ITicNodeState[] { I32 }, Real);
        yield return StateOptional.Of(I32);
    }

    // Hint detection at any depth — law 4's domain is Preferred-free INCLUDING nested
    // element hints: LCA transports hints through the join (keeps the CS[D, pref]
    // carrier) while pure ↓ strips them, so the forms differ (Algebra.md law-4 scope note).
    private static bool ContainsHint(ITicNodeState s) =>
        s switch {
            ConstraintsState cs => cs.Preferred != null || (cs.HasDescendant && ContainsHint(cs.Descendant)),
            StateArray arr => ContainsHint(arr.Element),
            StateOptional opt => ContainsHint(opt.Element),
            StateRefTo r => ContainsHint(r.Element),
            _ => false
        };

    // ================================================================
    // Monotonicity of the projections: A ≤ B ⟹ ↓A ≤ ↓B and ↑A ≤ ↑B
    // (debt #20 remainder; Algebra_Concretest.md / Algebra_Abstractest.md)
    //
    // Domain: SOLVED types T, where FitsInto restricted to T×T is the subtyping
    // order. There the law is order-preservation of the identity projections
    // (↓T = T, ↑T = T incl. the Fun-duality: on solved args ↑/↓ are identity too).
    //
    // On the C fragment the law is REFUTED under ≤ = FitsInto — see the two
    // [Ignore]-pinned sweeps below: Fit(X, C) is the SATISFACTION relation
    // (X ∈ Sat(C)), not the information order on states, so it cannot serve
    // as the ≤ of the monotonicity law ("в подходящем смысле для C" from the
    // spec stays unfulfilled until the algebra defines a Sat-inclusion order).
    // ================================================================

    // Fully-solved zoo: the 23-point lattice plus solved composites, incl.
    // fun-variance pairs and nested array/optional shapes. No CS at any depth.
    private static IEnumerable<ITicNodeState> SolvedZoo() {
        foreach (var p in Lattice) yield return p;
        yield return StateArray.Of(U8);
        yield return StateArray.Of(I32);
        yield return StateArray.Of(Real);
        yield return StateArray.Of(Char);
        yield return StateArray.Of(StateArray.Of(I32));
        yield return StateStruct.Of("a", I32);
        yield return StateStruct.Of("a", Real);
        yield return StateFun.Of(new ITicNodeState[] { I32 }, Real);
        yield return StateFun.Of(new ITicNodeState[] { Real }, Real);
        yield return StateFun.Of(new ITicNodeState[] { Real }, I32);
        yield return StateOptional.Of(I32);
        yield return StateOptional.Of(Real);
        yield return StateOptional.Of(StateArray.Of(I32));
    }

    [Test]
    public void Concretest_Monotone_SolvedTypes() {
        foreach (var a in SolvedZoo())
        foreach (var b in SolvedZoo())
        {
            if (!a.FitsInto(b)) continue;
            var da = a.Concretest();
            var db = b.Concretest();
            Assert.IsTrue(da.FitsInto(db),
                $"↓-monotonicity violated: {Print(a)} ≤ {Print(b)} but ↓A = {Print(da)} ≰ ↓B = {Print(db)}");
        }
    }

    [Test]
    public void Abstractest_Monotone_SolvedTypes() {
        foreach (var a in SolvedZoo())
        foreach (var b in SolvedZoo())
        {
            if (!a.FitsInto(b)) continue;
            var ua = a.Abstractest();
            var ub = b.Abstractest();
            Assert.IsTrue(ua.FitsInto(ub),
                $"↑-monotonicity violated: {Print(a)} ≤ {Print(b)} but ↑A = {Print(ua)} ≰ ↑B = {Print(ub)}");
        }
    }

    [Test]
    [Ignore("REFUTED (2026-07-10, debt #20 remainder). ↓-monotonicity under ≤ = FitsInto is FALSE " +
            "on every sub-fragment that touches C (323 counterexamples over the ProjectionZoo sweep, " +
            "CS at some depth on either side). Two classes: (1) C on the right — Re Fit [U8..Re] " +
            "holds (satisfaction), but ↓Re = Re ≰ U8 = ↓[U8..Re]; inherent to the choice of ≤: " +
            "Fit(X, C) is the satisfaction relation X ∈ Sat(C), not an information order — ↓ " +
            "projects C to its LOWER bound, so any satisfier strictly above D refutes the law. " +
            "(2) opt-flagged C on the left — [U8..]? Fit Re holds, but ↓ keeps the opt axis: " +
            "↓[U8..]? = opt(U8) ≰ Re (paired ↓/Destruction contract, see BoundsOrder scope note). " +
            "On T×T (where Fit IS the subtyping order) the law holds — see " +
            "Concretest_Monotone_SolvedTypes. A meaningful C-monotonicity needs a Sat-inclusion " +
            "order predicate, which the algebra does not define. See Algebra_Concretest.md §Законы.")]
    public void Concretest_Monotone_CsFragment() {
        foreach (var a in ProjectionZoo())
        foreach (var b in ProjectionZoo())
        {
            if (!a.FitsInto(b)) continue;
            var da = a.Concretest();
            var db = b.Concretest();
            Assert.IsTrue(da.FitsInto(db),
                $"↓-monotonicity violated: {Print(a)} ≤ {Print(b)} but ↓A = {Print(da)} ≰ ↓B = {Print(db)}");
        }
    }

    [Test]
    [Ignore("REFUTED (2026-07-10, debt #20 remainder). ↑-monotonicity under ≤ = FitsInto is FALSE " +
            "on every sub-fragment that touches C (369 counterexamples over the ProjectionZoo sweep, " +
            "CS at some depth on either side). Minimal counterexample: [..] Fit Ch holds (an " +
            "unconstrained CS can still narrow to Ch — satisfaction), but ↑[..] = Any ≰ Ch = ↑Ch. " +
            "Dual of the ↓-refutation: ↑ projects C to its UPPER bound (Any when unconstrained), so " +
            "any satisfiable target below Any refutes the law. The optional axis adds its own class: " +
            "None Fit [..Re]? but ↑ drops the opt axis (↑[..Re]? = Re) and None ≰ Re — the (↓,↑) " +
            "pair on the opt axis is only correct jointly with Destruction (paired contract, see " +
            "BoundsOrder scope note). On T×T the law holds — see Abstractest_Monotone_SolvedTypes. " +
            "See Algebra_Abstractest.md §Законы.")]
    public void Abstractest_Monotone_CsFragment() {
        foreach (var a in ProjectionZoo())
        foreach (var b in ProjectionZoo())
        {
            if (!a.FitsInto(b)) continue;
            var ua = a.Abstractest();
            var ub = b.Abstractest();
            Assert.IsTrue(ua.FitsInto(ub),
                $"↑-monotonicity violated: {Print(a)} ≤ {Print(b)} but ↑A = {Print(ua)} ≰ ↑B = {Print(ub)}");
        }
    }

    [Test]
    public void Lca_ConcretestProjection_Law4() {
        // T ∨ CS = T ∨ ↓CS on the law's domain (non-optional, hint-free at any depth).
        var csShapes = CsShapes().Where(c => !c.IsOptional && !ContainsHint(c));
        foreach (var cs in csShapes)
        foreach (var t in SolvedTypes())
        {
            var joinCs = t.Lca(cs);
            var joinDown = t.Lca(((ITicNodeState)cs).Concretest());
            Assert.AreEqual(Print(joinDown), Print(joinCs),
                $"Law 4 violated: {Print(t)} ∨ {Print(cs)} = {Print(joinCs)}, " +
                $"but {Print(t)} ∨ ↓CS = {Print(joinDown)}");
        }
    }
}
