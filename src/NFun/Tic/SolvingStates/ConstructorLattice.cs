using System;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// LCA / GCD / subtype operations on the <see cref="ConstructorKind"/> lattice.
///
/// Hierarchy (subtype on the right):
/// <code>
///   Any
///    └── Enumerable
///         ├── FixedArray
///         │    └── Array
///         │         └── List
///         ├── Set
///         └── Map
/// </code>
///
/// The lattice is small (7 nodes) and shape-fixed — no need for a precomputed
/// table. Operations are pure ordinal lookups against a hand-written
/// <c>parent</c> map. O(depth) climb, depth ≤ 4.
///
/// All operations are pure functions of their inputs — no dialect dependency,
/// no global state, no allocation. Confluence of TIC's algebraic operators
/// (per <c>Specs/Tic/Algebra.md</c>) survives the addition.
/// </summary>
public static class ConstructorLattice {

    /// <summary>
    /// Direct supertype of each constructor (or <see cref="ConstructorKind.Any"/>
    /// for the top).
    /// </summary>
    private static readonly ConstructorKind[] Parent = BuildParentMap();

    private static ConstructorKind[] BuildParentMap() {
        var p = new ConstructorKind[7];
        p[(int)ConstructorKind.Any]        = ConstructorKind.Any;
        p[(int)ConstructorKind.Enumerable] = ConstructorKind.Any;
        p[(int)ConstructorKind.FixedArray] = ConstructorKind.Enumerable;
        p[(int)ConstructorKind.Array]      = ConstructorKind.FixedArray;
        p[(int)ConstructorKind.List]       = ConstructorKind.Array;
        p[(int)ConstructorKind.Set]        = ConstructorKind.Enumerable;
        p[(int)ConstructorKind.Map]        = ConstructorKind.Enumerable;
        return p;
    }

    /// <summary>
    /// Least common ancestor of two constructors.
    ///
    /// Climb both paths to the top, then find the deepest shared ancestor.
    /// <c>Lca(List, Set) = Enumerable</c>, <c>Lca(List, Array) = Array</c>,
    /// <c>Lca(Set, Map) = Enumerable</c>, <c>Lca(Any, anything) = Any</c>.
    /// </summary>
    public static ConstructorKind Lca(ConstructorKind a, ConstructorKind b) {
        if (a == b) return a;
        if (a == ConstructorKind.Any || b == ConstructorKind.Any) return ConstructorKind.Any;
        // Walk a's chain to root, mark each node. Then walk b's chain, returning the first match.
        // Lattice depth is bounded by 4; use a stackalloc-free bitset.
        // Bitset is `int` (32 bits) — adding more than 32 ConstructorKind members would
        // silently corrupt the mask. Adjust this method to `long` if ConstructorKind grows
        // past 32 members.
        int aMask = 0;
        var cur = a;
        while (true) {
            System.Diagnostics.Debug.Assert((int)cur < 32, "ConstructorKind ordinals must fit in a 32-bit mask");
            aMask |= 1 << (int)cur;
            if (cur == ConstructorKind.Any) break;
            cur = Parent[(int)cur];
        }
        cur = b;
        while (true) {
            if ((aMask & (1 << (int)cur)) != 0) return cur;
            if (cur == ConstructorKind.Any) return ConstructorKind.Any;
            cur = Parent[(int)cur];
        }
    }

    /// <summary>
    /// Greatest common descendant of two constructors, or null if none exists.
    ///
    /// Symmetric to <see cref="Lca"/>. Used when intersecting two constraints
    /// (Stage 2+ generic typeclass match). For Stage 1 it exists to round out
    /// the lattice API; few call-sites need it before Stage 4.
    ///
    /// Returns null when the constructors lie in disjoint sub-trees
    /// (e.g. <c>Gcd(Set, List)</c> — no common descendant).
    /// </summary>
    public static ConstructorKind? Gcd(ConstructorKind a, ConstructorKind b) {
        if (a == b) return a;
        if (a == ConstructorKind.Any) return b;
        if (b == ConstructorKind.Any) return a;
        if (IsSubtypeOf(a, b)) return a;
        if (IsSubtypeOf(b, a)) return b;
        return null;
    }

    /// <summary>
    /// True if <paramref name="child"/> &lt;: <paramref name="parent"/> in the
    /// constructor lattice (or they are equal).
    ///
    /// Used by typeclass-constraint satisfaction: <c>IsSubtypeOf(List, Enumerable) = true</c>
    /// means a <c>List&lt;T&gt;</c> argument satisfies an <c>Enumerable&lt;T&gt;</c>
    /// parameter constraint.
    /// </summary>
    public static bool IsSubtypeOf(ConstructorKind child, ConstructorKind parent) {
        if (parent == ConstructorKind.Any) return true;
        var cur = child;
        while (cur != ConstructorKind.Any) {
            if (cur == parent) return true;
            cur = Parent[(int)cur];
        }
        return false;
    }

    /// <summary>
    /// True iff the constructor supports `.clear()` — dropping ALL elements
    /// (which changes the length). Satisfied by <see cref="ConstructorKind.List"/>,
    /// <see cref="ConstructorKind.Set"/>, <see cref="ConstructorKind.Map"/>.
    /// **Excludes <see cref="ConstructorKind.Array"/>** — Array IS mutable
    /// element-wise (`a[i] = v`) but its length is fixed, so `clear` doesn't
    /// apply (would surface as a runtime error if accepted at TIC). Also
    /// excludes <see cref="ConstructorKind.FixedArray"/> (immutable), the
    /// legacy ee-mode <c>StateArray</c>, and abstract caps.
    ///
    /// <para>Conceptual note: Clearable ⊂ "anything mutable". Array is
    /// mutable but not clearable. NFun's only collection-level typeclass is
    /// Clearable; a broader "Mutable" typeclass isn't currently used.</para>
    /// </summary>
    public static bool IsClearable(ConstructorKind kind) => kind switch {
        ConstructorKind.List => true,
        ConstructorKind.Set  => true,
        ConstructorKind.Map  => true,
        // Array: NOT clearable — length is fixed (Array is the "indexed
        // mutable, fixed length" cap). Caller should use a different cap
        // (e.g. ConstructorKind.Array via IndexedMutable) for indexed write.
        // FixedArray / ee-mode StateArray: immutable, no clear at all.
        // Enumerable / Any: abstract caps, not concrete kinds.
        _ => false,
    };

    /// <summary>
    /// Map an abstract constructor to its preferred concrete instantiation.
    /// Used by the <c>Concretest</c> algebraic operator when a constraint
    /// resolves to <see cref="ConstructorKind.Enumerable"/> or
    /// <see cref="ConstructorKind.FixedArray"/>.
    ///
    /// Dialect-independent at the TIC level (per spec §TIC implementation sketch);
    /// the lang vs ee default for the literal <c>[1,2,3]</c> happens at the parser,
    /// where the parser hard-codes which state class the literal binds to.
    /// </summary>
    public static ConstructorKind Concretest(ConstructorKind kind) => kind switch {
        ConstructorKind.Enumerable => ConstructorKind.List,
        ConstructorKind.FixedArray => ConstructorKind.FixedArray,
        _ => kind,
    };

    /// <summary>
    /// True iff the constructor exists only as a constraint upper bound and
    /// has no factory / no literal that produces it. Currently
    /// <see cref="ConstructorKind.Enumerable"/> only.
    ///
    /// FixedArray is NOT constraint-only — it is concretely instantiable via
    /// <c>fixedArray(1,2,3)</c>. Use <see cref="RequiresConcretestDescent"/>
    /// for the related but distinct "must descend during resolution" check.
    /// </summary>
    public static bool IsConstraintOnly(ConstructorKind kind) =>
        kind == ConstructorKind.Enumerable;

    /// <summary>
    /// True iff the <c>Concretest</c> rule must descend further when the type
    /// resolves to this constructor unbound. Both <see cref="ConstructorKind.Enumerable"/>
    /// and <see cref="ConstructorKind.FixedArray"/> qualify — they have at
    /// least one concrete descendant in the lattice (FixedArray → Array,
    /// Enumerable → List by <see cref="Concretest"/>).
    /// </summary>
    public static bool RequiresConcretestDescent(ConstructorKind kind) =>
        kind == ConstructorKind.Enumerable
        || kind == ConstructorKind.FixedArray;

    /// <summary>
    /// Variance of the (single) element argument for each concrete constructor.
    /// All new collection constructors are <see cref="Variance.Invariant"/>.
    /// The legacy <c>StateArray</c> is covariant but is NOT in this lattice.
    /// </summary>
    public static Variance ElementVariance(ConstructorKind kind) => kind switch {
        ConstructorKind.FixedArray => Variance.Invariant,
        ConstructorKind.Array      => Variance.Invariant,
        ConstructorKind.List       => Variance.Invariant,
        ConstructorKind.Set        => Variance.Invariant,
        ConstructorKind.Map        => Variance.Invariant,
        ConstructorKind.Enumerable => Variance.Invariant,
        _ => throw new ArgumentOutOfRangeException(nameof(kind),
            $"{kind} is not a parameterised constructor"),
    };
}
