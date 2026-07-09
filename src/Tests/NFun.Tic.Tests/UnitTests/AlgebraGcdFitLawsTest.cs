namespace NFun.Tic.Tests.UnitTests;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Law tests for the algebra fixes of debt items:
///  #17 — GCD commutativity on the C fragment (neutral [D..∅] CS),
///  #18 — GCD Any-arm is identity (Any ∧ x = x, even for unsolved innards),
///  #15 — Fit comparable cell is conjunctive (cmp does not cancel the descendant check),
/// plus the Merge/Fit absorption law on the fixed comparable cells.
/// Style follows AlgebraInvariantsTest (Nfun.UnitTests).
/// </summary>
public class AlgebraGcdFitLawsTest {

    private static ConstraintsState Cs(
        ITicNodeState desc = null, StatePrimitive anc = null,
        bool cmp = false, bool opt = false, StatePrimitive preferred = null) {
        var cs = ConstraintsState.Of(desc, anc, cmp, opt);
        cs.Preferred = preferred;
        return cs;
    }

    // CS shapes: with/without ancestor, flags, Preferred — the C fragment sample.
    private static IEnumerable<ConstraintsState> CsShapes() {
        yield return Cs();                                      // [∅..∅] — neutral
        yield return Cs(desc: U8);                              // [U8..∅] — neutral with desc
        yield return Cs(desc: I16, preferred: I32);             // neutral + Preferred
        yield return Cs(desc: U8, opt: true);                   // neutral + IsOptional
        yield return Cs(cmp: true);                             // neutral + IsComparable
        yield return Cs(anc: Real);                             // [∅..Real]
        yield return Cs(desc: U8, anc: Real);                   // [U8..Real]
        yield return Cs(desc: I16, anc: I64);                   // [I16..I64]
        yield return Cs(desc: U8, anc: Real, cmp: true);        // bounded + cmp
        yield return Cs(desc: U8, anc: Real, preferred: I32);   // bounded + Preferred
        yield return Cs(anc: I64, opt: true);                   // bounded + IsOptional
    }

    private static IEnumerable<ITicNodeState> SolvedTypes() {
        yield return U8;
        yield return I32;
        yield return I64;
        yield return Real;
        yield return Bool;
        yield return Char;
        yield return Any;
        yield return None;
        yield return StateArray.Of(I32);
        yield return StateArray.Of(Char);
        yield return StateStruct.Of("a", I32);
        yield return StateFun.Of(new ITicNodeState[] { I32 }, Real);
        yield return StateOptional.Of(I32);
    }

    private static readonly StatePrimitive[] Primitives = {
        U8, U12, U16, U24, U32, U48, U64,
        I8, I12, I16, I24, I32, I48, I64, I96,
        F32, Real, Char, Bool, Ip, Any, None,
    };

    // A no-ancestor CS contributes no upper bound to the meet — it is a representative
    // of the ⊤ class on the C fragment.
    private static bool IsNeutralCs(ITicNodeState s) => s is ConstraintsState { HasAncestor: false };

    // ================================================================
    // #17 — GCD commutativity including the C fragment
    // ================================================================

    [Test]
    public void Gcd_Commutative_IncludingConstraintsFragment() {
        // A ∧ B = B ∧ A over solved types AND constraint states.
        // The only quotient: when BOTH operands are neutral ([D..∅]), each is a
        // representative of the same ⊤ class (no upper-bound contribution), and the
        // operator returns the other operand — commutativity holds up to the ⊤ class.
        var all = CsShapes().Cast<ITicNodeState>().Concat(SolvedTypes()).ToArray();
        foreach (var a in all)
        foreach (var b in all)
        {
            var ab = a.Gcd(b);
            var ba = b.Gcd(a);
            if (IsNeutralCs(a) && IsNeutralCs(b))
            {
                Assert.IsTrue(IsNeutralCs(ab) && IsNeutralCs(ba),
                    $"GCD of two neutral CS must stay in the neutral (⊤) class: " +
                    $"Gcd({a}, {b})={ab}, Gcd({b}, {a})={ba}");
                continue;
            }
            Assert.AreEqual(ab, ba,
                $"GCD commutativity violated: Gcd({a}, {b})={ab} but Gcd({b}, {a})={ba}");
        }
    }

    [Test]
    public void Gcd_NeutralCs_ReturnsOtherOperandWhole_BothOrders() {
        // #17 pin: [D₁..A₁] ∧ [D₂..∅] = [D₁..A₁] — the non-neutral operand survives
        // WHOLE (Desc, flags, Preferred), in BOTH orders. Before the fix the left-to-right
        // order collapsed the bounded CS to its Ancestor (Real), losing Desc/cmp/Preferred.
        var bounded = Cs(desc: U8, anc: Real, cmp: true, preferred: I32);
        var neutral = Cs(desc: I16);

        Assert.AreEqual(bounded, bounded.Gcd(neutral),
            "[D₁..A₁] ∧ [D₂..∅] must return the bounded CS whole");
        Assert.AreEqual(bounded, neutral.Gcd(bounded),
            "[D₂..∅] ∧ [D₁..A₁] must return the bounded CS whole");
    }

    [Test]
    public void Gcd_NeutralCs_IsIdentity_OnSolvedTypes() {
        // [D..∅] ∧ T = T in BOTH orders, for every solved T.
        var neutrals = CsShapes().Where(IsNeutralCs).ToArray();
        foreach (var n in neutrals)
        foreach (var t in SolvedTypes())
        {
            Assert.AreEqual(t, ((ITicNodeState)n).Gcd(t),
                $"Gcd({n}, {t}) must be {t}");
            Assert.AreEqual(t, t.Gcd(n),
                $"Gcd({t}, {n}) must be {t}");
        }
    }

    [Test]
    public void Gcd_BoundedCs_EntersMeetThroughAncestorOnly() {
        // [D..A] ∧ T = A ∧ T in BOTH orders — a bounded CS contributes exactly its
        // Ancestor to the meet (Desc and flags are node obligations, not an upper bound).
        var boundeds = CsShapes().Where(c => c.HasAncestor).ToArray();
        foreach (var cs in boundeds)
        foreach (var t in SolvedTypes())
        {
            var expected = ((ITicNodeState)cs.Ancestor).Gcd(t);
            Assert.AreEqual(expected, ((ITicNodeState)cs).Gcd(t),
                $"Gcd({cs}, {t}) must equal Gcd({cs.Ancestor}, {t})={expected}");
            Assert.AreEqual(expected, t.Gcd(cs),
                $"Gcd({t}, {cs}) must equal Gcd({t}, {cs.Ancestor})={expected}");
        }
    }

    // ================================================================
    // #18 — GCD Any-arm identity
    // ================================================================

    [Test]
    public void Gcd_AnyIsIdentity_UnsolvedComposites() {
        // Any ∧ x = x — identity, INCLUDING composites with unsolved innards.
        // Before the fix the Any-arm returned x.Abstractest(), which differs from x
        // exactly on unsolved components (e.g. arr([U8..Real]) became arr(Real)).
        var unsolvedComposites = new ITicNodeState[] {
            StateArray.Of(Cs(desc: U8, anc: Real)),
            StateArray.Of(Cs()),
            StateArray.Of(StateArray.Of(Cs(desc: I32))),
            StateStruct.Of("a", Cs(desc: I16, anc: I64)),
            StateFun.Of(new ITicNodeState[] { Cs(anc: Real) }, Cs(desc: U8)),
            StateOptional.Of(Cs(desc: U8, anc: Real)),
        };
        foreach (var x in unsolvedComposites)
        {
            Assert.AreEqual(x, x.Gcd(Any), $"Gcd({x}, Any) must be {x}");
            Assert.AreEqual(x, ((ITicNodeState)Any).Gcd(x), $"Gcd(Any, {x}) must be {x}");
        }
    }

    // ================================================================
    // #15 — Fit comparable cell is conjunctive
    // ================================================================

    [Test]
    public void FitsInto_ComparableCell_DescendantViolation_False() {
        // A ≤ [D..A′, cmp] ⟺ A ∈ Comparable ∧ D ≤c A ∧ A ≤c A′.
        // U8 is comparable but I64 ≰c U8 — must be false (was true before the fix).
        Assert.IsFalse(U8.FitsInto(Cs(desc: I64, cmp: true)),
            "FitsInto(U8, [I64.., cmp]) must be false: I64 ≰c U8");
    }

    [Test]
    public void FitsInto_ComparableCell_InInterval_True() {
        var cell = Cs(desc: I64, cmp: true);
        Assert.IsTrue(I64.FitsInto(cell), "I64 ≤ [I64.., cmp]");
        Assert.IsTrue(Real.FitsInto(cell), "Real ≤ [I64.., cmp]");
        Assert.IsTrue(I32.FitsInto(Cs(desc: U8, anc: Real, cmp: true)), "I32 ≤ [U8..Real, cmp]");
        Assert.IsTrue(Char.FitsInto(Cs(cmp: true)), "Char is comparable");
        Assert.IsTrue(((ITicNodeState)StateArray.Of(Char)).FitsInto(Cs(cmp: true)),
            "arr(char) is the comparable composite");
    }

    [Test]
    public void FitsInto_NonComparable_IntoComparableCell_False() {
        var cmpCell = Cs(cmp: true);
        Assert.IsFalse(Bool.FitsInto(cmpCell), "Bool is not comparable");
        Assert.IsFalse(((ITicNodeState)StateStruct.Of("a", I32)).FitsInto(cmpCell),
            "struct is not comparable");
        Assert.IsFalse(((ITicNodeState)StateArray.Of(I32)).FitsInto(cmpCell),
            "arr(i32) is not comparable");
        Assert.IsFalse(((ITicNodeState)StateOptional.Of(I32)).FitsInto(cmpCell),
            "optional is not comparable");
        Assert.IsFalse(I32.FitsInto(Cs(desc: U8, anc: I16, cmp: true)),
            "I32 exceeds the ancestor I16");
    }

    [Test]
    public void FitsInto_ComparableCell_ConjunctiveRule_AllPrimitives() {
        // Property form of the fixed cell: for every primitive P,
        // P ≤ [I64..Real, cmp] ⟺ P.IsComparable ∧ I64 ≤c P ∧ P ≤c Real.
        var cell = Cs(desc: I64, anc: Real, cmp: true);
        foreach (var p in Primitives)
        {
            var expected = p.IsComparable
                           && I64.CanBePessimisticConvertedTo(p)
                           && p.CanBePessimisticConvertedTo(Real);
            Assert.AreEqual(expected, p.FitsInto(cell),
                $"FitsInto({p}, {cell}) must be {expected}");
        }
    }

    // ================================================================
    // Merge/Fit absorption on the fixed comparable cells
    // ================================================================

    [Test]
    public void MergeFit_Absorption_ComparableCells() {
        // Absorption law (Merge M5): x ≤ (a ⊓ b) ⟺ x ≤ a ∧ x ≤ b.
        // Broken before #15: with a = [I64..∅], b = [cmp], x = U8 the right side was
        // false (I64 ≰c U8) while x ≤ (a⊓b) = x ≤ [I64.., cmp] returned true.
        var cellPairs = new (ConstraintsState a, ConstraintsState b)[] {
            (Cs(desc: I64), Cs(cmp: true)),
            (Cs(desc: U8, anc: Real), Cs(cmp: true)),
            (Cs(desc: Char), Cs(cmp: true)),
            (Cs(desc: U8, cmp: true), Cs(anc: Real)),
            (Cs(desc: I16, anc: I64, cmp: true), Cs(desc: U8, anc: Real)),
            (Cs(cmp: true), Cs(cmp: true)),
        };
        var targets = new ITicNodeState[] {
            U8, I16, I32, I64, Real, Char, Bool, Any,
            StateArray.Of(Char), StateArray.Of(I32), StateOptional.Of(I32),
        };
        foreach (var (a, b) in cellPairs)
        {
            var merged = a.MergeOrNull(b);
            Assert.IsNotNull(merged, $"{a} ⊓ {b} must be defined for this cell set");
            foreach (var x in targets)
            {
                var fitsMerged = x.FitsInto(merged);
                var fitsBoth = x.FitsInto(a) && x.FitsInto(b);
                Assert.AreEqual(fitsBoth, fitsMerged,
                    $"Absorption violated for x={x}, a={a}, b={b}, a⊓b={merged}: " +
                    $"x≤(a⊓b)={fitsMerged} but (x≤a ∧ x≤b)={fitsBoth}");
            }
        }
    }
}
