using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

/// <summary>
/// Unit tests for <see cref="ConstraintsState"/> invariants — the "interval" representation
/// of a not-yet-resolved type variable. Theoretical model: a ConstraintsState holds
///   - a Descendant (lower bound, optional)
///   - an Ancestor (upper bound, primitive only)
///   - flags: IsOptional, IsComparable, RecursiveBound
///   - Preferred (metadata for resolution-time defaulting)
///
/// Foundation invariants:
///   - IsMutable = true (constraints can absorb further info)
///   - IsSolved = false (until resolved at Destruction)
///   - NoConstrains: convenient predicate "this is empty / vacuous"
///   - HasDescendant / HasAncestor: presence checks
///   - IntersectIntervalsOrNull: (D1,A1) ∩ (D2,A2) returns a CS with merged bounds or null
///   - IntervalIsNonEmpty: D ≤ A check (constraint satisfiability)
///   - CanBeConvertedTo: "does this constraint accept type T?"
///
/// These tests cover boundary conditions for Bug E (None vs Optional skip), Bug C
/// (row poly), Bug M (coalesce compatibility), and various Pull/Push interactions.
/// </summary>
class BugRegression_ConstraintsStateInvariantsTests {

    // ─── Empty constraint state ───

    [Test]
    public void Empty_IsNoConstrains() =>
        Assert.IsTrue(ConstraintsState.Empty.NoConstrains);

    [Test]
    public void Empty_HasNoAncestor() =>
        Assert.IsFalse(ConstraintsState.Empty.HasAncestor);

    [Test]
    public void Empty_HasNoDescendant() =>
        Assert.IsFalse(ConstraintsState.Empty.HasDescendant);

    [Test]
    public void Empty_NotOptional() =>
        Assert.IsFalse(ConstraintsState.Empty.IsOptional);

    [Test]
    public void Empty_NotComparable() =>
        Assert.IsFalse(ConstraintsState.Empty.IsComparable);

    [Test]
    public void Empty_HasNoStructBound() =>
        Assert.IsNull(ConstraintsState.Empty.StructBound);

    [Test]
    public void Empty_HasNoRecursiveBound() =>
        Assert.IsNull(ConstraintsState.Empty.RecursiveBound);

    [Test]
    public void Empty_IsNotSolved() {
        // Constraints are by construction not yet solved — that's their point.
        Assert.IsFalse(ConstraintsState.Empty.IsSolved);
    }

    [Test]
    public void Empty_IsMutable() {
        Assert.IsTrue(ConstraintsState.Empty.IsMutable);
    }

    // ─── Single-bound constructions ───

    [Test]
    public void WithDescendantOnly_HasDescNoAnc() {
        var c = ConstraintsState.Of(desc: I32);
        Assert.IsTrue(c.HasDescendant);
        Assert.IsFalse(c.HasAncestor);
        Assert.IsFalse(c.NoConstrains);
    }

    [Test]
    public void WithAncestorOnly_HasAncNoDesc() {
        var c = ConstraintsState.Of(anc: Real);
        Assert.IsTrue(c.HasAncestor);
        Assert.IsFalse(c.HasDescendant);
        Assert.IsFalse(c.NoConstrains);
    }

    [Test]
    public void WithBothBounds_HasBoth() {
        var c = ConstraintsState.Of(desc: I16, anc: Real);
        Assert.IsTrue(c.HasDescendant);
        Assert.IsTrue(c.HasAncestor);
    }

    // ─── Boundary intervals ───
    // Interval = [Descendant .. Ancestor]. A primitive T fits iff Descendant ≤ T ≤ Ancestor.

    [Test]
    public void CanBeConvertedTo_TypeWithinInterval_True() {
        // [I16..Real] — I32 fits.
        var c = ConstraintsState.Of(I16, Real);
        Assert.IsTrue(c.CanBeConvertedTo(I32));
    }

    [Test]
    public void CanBeConvertedTo_TypeAtAncestor_True() {
        // [I16..Real] — Real fits (upper boundary inclusive).
        var c = ConstraintsState.Of(I16, Real);
        Assert.IsTrue(c.CanBeConvertedTo(Real));
    }

    [Test]
    public void CanBeConvertedTo_TypeAtDescendant_True() {
        // [I16..Real] — I16 fits (lower boundary inclusive).
        var c = ConstraintsState.Of(I16, Real);
        Assert.IsTrue(c.CanBeConvertedTo(I16));
    }

    [Test]
    public void CanBeConvertedTo_TypeBelowDescendant_False() {
        // [I32..Real] — I16 is below the lower bound.
        var c = ConstraintsState.Of(I32, Real);
        Assert.IsFalse(c.CanBeConvertedTo(I16));
    }

    [Test]
    public void CanBeConvertedTo_TypeAboveAncestor_False() {
        // [I16..I24] — Real is above the upper bound.
        var c = ConstraintsState.Of(I16, I24);
        Assert.IsFalse(c.CanBeConvertedTo(Real));
    }

    [Test]
    public void CanBeConvertedTo_Empty_AcceptsAnyType() {
        // Empty constraint — accepts everything (vacuous).
        var c = ConstraintsState.Empty;
        Assert.IsTrue(c.CanBeConvertedTo(I32));
        Assert.IsTrue(c.CanBeConvertedTo(Real));
        Assert.IsTrue(c.CanBeConvertedTo(Bool));
        Assert.IsTrue(c.CanBeConvertedTo(Any));
    }

    // ─── IntervalIsNonEmpty: satisfiability check ───

    [Test]
    public void IntervalIsNonEmpty_BothNull_True() {
        // No constraint = trivially satisfiable.
        Assert.IsTrue(ConstraintsState.Empty.IntervalIsNonEmpty());
    }

    [Test]
    public void IntervalIsNonEmpty_DescOnly_True() {
        // [I32..∞] — satisfiable by any T ≥ I32.
        Assert.IsTrue(ConstraintsState.Of(desc: I32).IntervalIsNonEmpty());
    }

    [Test]
    public void IntervalIsNonEmpty_AncOnly_True() {
        // [-∞..Real] — satisfiable by any T ≤ Real.
        Assert.IsTrue(ConstraintsState.Of(anc: Real).IntervalIsNonEmpty());
    }

    [Test]
    public void IntervalIsNonEmpty_DescBelowAnc_True() {
        // [I16..Real] — D=I16, A=Real, I16 ≤ Real → satisfiable.
        Assert.IsTrue(ConstraintsState.Of(I16, Real).IntervalIsNonEmpty());
    }

    [Test]
    public void IntervalIsNonEmpty_DescEqualsAnc_True() {
        // [I32..I32] — singleton.
        Assert.IsTrue(ConstraintsState.Of(I32, I32).IntervalIsNonEmpty());
    }

    // ─── IntersectIntervalsOrNull: pair intersection ───

    [Test]
    public void Intersect_TwoEmpty_ReturnsEmpty() {
        var res = ConstraintsState.Empty.IntersectIntervalsOrNull(ConstraintsState.Empty);
        Assert.IsNotNull(res);
        Assert.IsTrue(res.NoConstrains);
    }

    [Test]
    public void Intersect_OneEmpty_OneNarrow_ReturnsNarrow() {
        var narrow = ConstraintsState.Of(I32, Real);
        var res = ConstraintsState.Empty.IntersectIntervalsOrNull(narrow);
        Assert.IsNotNull(res);
        Assert.IsTrue(res.HasDescendant);
    }

    [Test]
    public void Intersect_DisjointIntervals_ReturnsNull() {
        // [I16..I24] ∩ [U32..U48] — disjoint integer families: no type satisfies both
        // constraints. IntersectIntervalsOrNull must return null per its name (OrNull
        // = "or null when failure"). Previously this returned a degenerate interval
        // [I48..U16] (where Descendant is wider than Ancestor) — mathematical garbage
        // packaged as a non-null result, requiring callers to remember
        // IntervalIsNonEmpty as a separate check. Found via bug-regression unit-test
        // investigation of finding #3 — API name lied about the contract.
        var a = ConstraintsState.Of(I16, I24);
        var b = ConstraintsState.Of(U32, U48);
        Assert.IsNull(a.IntersectIntervalsOrNull(b),
            "Disjoint integer ranges must produce null (not a degenerate interval)");
    }

    [Test]
    public void Intersect_SymmetricOnDisjoint_BothNull() {
        // Symmetry contract: empty intersection from either direction.
        var a = ConstraintsState.Of(I16, I24);
        var b = ConstraintsState.Of(U32, U48);
        Assert.IsNull(a.IntersectIntervalsOrNull(b));
        Assert.IsNull(b.IntersectIntervalsOrNull(a));
    }

    [Test]
    public void Intersect_DescriptionInvariant_NullIffEmptyInterval() {
        // Stronger contract assertion: result == null IFF the resulting interval is
        // unsatisfiable. Either the intersection contains some type (non-null,
        // IntervalIsNonEmpty=true), or it's empty (null).
        var pairs = new[] {
            (ConstraintsState.Of(I16, I24), ConstraintsState.Of(I16, I24)),   // identical → non-empty
            (ConstraintsState.Of(I16, Real), ConstraintsState.Of(I24, I48)),  // nested → non-empty
            (ConstraintsState.Of(I16, I24), ConstraintsState.Of(U32, U48)),   // disjoint → null
            (ConstraintsState.Empty,        ConstraintsState.Of(I32, Real)),  // empty + narrow → non-empty
        };
        foreach (var (a, b) in pairs) {
            var res = a.IntersectIntervalsOrNull(b);
            if (res == null) {
                // Null result → caller knows it's unsatisfiable.
                continue;
            }
            Assert.IsTrue(res.IntervalIsNonEmpty(),
                $"Non-null result must have satisfiable interval; got {res} for ({a},{b})");
        }
    }

    [Test]
    public void Intersect_NestedIntervals_ReturnsInner() {
        // [I16..Real] ∩ [I24..I48] — inner is more constrained.
        var outer = ConstraintsState.Of(I16, Real);
        var inner = ConstraintsState.Of(I24, I48);
        var res = outer.IntersectIntervalsOrNull(inner);
        Assert.IsNotNull(res);
    }

    [Test]
    public void Intersect_IsSymmetric_BothEmpty() {
        Assert.AreEqual(
            ConstraintsState.Empty.IntersectIntervalsOrNull(ConstraintsState.Empty) != null,
            ConstraintsState.Empty.IntersectIntervalsOrNull(ConstraintsState.Empty) != null);
    }

    [Test]
    public void Intersect_IsSymmetric_DisjointReturnsNull() {
        var a = ConstraintsState.Of(I16, I24);
        var b = ConstraintsState.Of(U32, U48);
        Assert.AreEqual(
            a.IntersectIntervalsOrNull(b) != null,
            b.IntersectIntervalsOrNull(a) != null);
    }

    // ─── IsOptional flag ───
    // The IsOptional flag indicates "this variable accepts Optional types via implicit lift".
    // Set during Pull when None propagation reaches the variable.

    [Test]
    public void IsOptional_DefaultsFalse() =>
        Assert.IsFalse(ConstraintsState.Empty.IsOptional);

    // ─── IsComparable flag ───
    // Set when the variable participates in a comparable position (e.g., used with `<`, `>`).

    [Test]
    public void IsComparable_DefaultsFalse() =>
        Assert.IsFalse(ConstraintsState.Empty.IsComparable);

    // ─── Preferred metadata ───
    // Preferred carries provenance (e.g. "this was an int literal", "this was a hex literal")
    // for resolution-time defaulting. Not used by interval algebra directly.

    [Test]
    public void Preferred_DefaultsNull() =>
        Assert.IsNull(ConstraintsState.Empty.Preferred);

    [Test]
    public void Preferred_Settable() {
        var c = ConstraintsState.Of();
        c.Preferred = I32;
        Assert.AreSame(I32, c.Preferred);
    }

    // ─── GetCopy: structural deep copy invariants ───

    [Test]
    public void GetCopy_PreservesBounds() {
        var orig = ConstraintsState.Of(I16, Real);
        var copy = orig.GetCopy();
        Assert.AreNotSame(orig, copy);
        Assert.IsTrue(copy.HasDescendant);
        Assert.IsTrue(copy.HasAncestor);
    }

    [Test]
    public void GetCopy_PreservesPreferred() {
        var orig = ConstraintsState.Of();
        orig.Preferred = I32;
        var copy = orig.GetCopy();
        Assert.AreSame(I32, copy.Preferred);
    }

    [Test]
    public void GetCopy_PreservesIsComparable() {
        var orig = ConstraintsState.Of();
        orig.IsComparable = true;
        var copy = orig.GetCopy();
        Assert.IsTrue(copy.IsComparable);
    }
}
