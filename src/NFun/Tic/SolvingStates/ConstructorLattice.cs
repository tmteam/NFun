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
        var p = new ConstructorKind[8];
        p[(int)ConstructorKind.Any]        = ConstructorKind.Any;
        p[(int)ConstructorKind.Enumerable] = ConstructorKind.Any;
        p[(int)ConstructorKind.FixedArray] = ConstructorKind.Enumerable;
        p[(int)ConstructorKind.Array]      = ConstructorKind.FixedArray;
        p[(int)ConstructorKind.List]       = ConstructorKind.Array;
        p[(int)ConstructorKind.Set]        = ConstructorKind.Enumerable;
        p[(int)ConstructorKind.Map]        = ConstructorKind.Enumerable;
        // Mutable is a typeclass marker, not a lattice node — its place in the
        // parent map is technically Enumerable (any mutable kind is also
        // Enumerable). Direct subtype checks use `IsSubtypeOf` overridden below.
        p[(int)ConstructorKind.Mutable]    = ConstructorKind.Enumerable;
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
        // Mutable as a typeclass marker: LCA(Mutable, K) follows the typeclass
        // membership, not the lattice parent chain. `Mutable < Enumerable` is
        // recorded in Parent for the chain walk's benefit but the Lca call
        // would otherwise wrongly collapse `Mutable ⊓ List` to Enumerable.
        if (a == ConstructorKind.Mutable) return IsMutable(b) ? ConstructorKind.Mutable : ConstructorKind.Enumerable;
        if (b == ConstructorKind.Mutable) return IsMutable(a) ? ConstructorKind.Mutable : ConstructorKind.Enumerable;

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
        // Mutable typeclass — predicate-based, not lattice-based.
        if (parent == ConstructorKind.Mutable) return IsMutable(child);
        // A Mutable-bound child fits any Enumerable parent (Mutable < Enumerable
        // by construction); other parents reject it (we don't have concrete
        // values of kind Mutable — only constraints on it).
        if (child == ConstructorKind.Mutable)
            return parent == ConstructorKind.Enumerable || parent == ConstructorKind.Mutable;
        var cur = child;
        while (cur != ConstructorKind.Any) {
            if (cur == parent) return true;
            cur = Parent[(int)cur];
        }
        return false;
    }

    /// <summary>
    /// True iff the constructor admits write operations (`clear`, `add`,
    /// `remove`, …). Read-only kinds (FixedArray, ee-mode legacy) return false.
    /// </summary>
    public static bool IsMutable(ConstructorKind kind) => kind switch {
        ConstructorKind.List       => true,
        ConstructorKind.Array      => true,
        ConstructorKind.Set        => true,
        // FixedArray is immutable, Enumerable is abstract, Map is read-only via
        // this surface (key/value mutation comes through a separate API), Any is
        // not a concrete kind. Mutable itself is also "not a concrete kind".
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
        ConstructorKind.Mutable    => ConstructorKind.List,
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
        kind == ConstructorKind.Enumerable || kind == ConstructorKind.Mutable;

    /// <summary>
    /// True iff the <c>Concretest</c> rule must descend further when the type
    /// resolves to this constructor unbound. Both <see cref="ConstructorKind.Enumerable"/>
    /// and <see cref="ConstructorKind.FixedArray"/> qualify — they have at
    /// least one concrete descendant in the lattice (FixedArray → Array,
    /// Enumerable → List by <see cref="Concretest"/>).
    /// </summary>
    public static bool RequiresConcretestDescent(ConstructorKind kind) =>
        kind == ConstructorKind.Enumerable
        || kind == ConstructorKind.FixedArray
        || kind == ConstructorKind.Mutable;

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
        ConstructorKind.Mutable    => Variance.Invariant,
        _ => throw new ArgumentOutOfRangeException(nameof(kind),
            $"{kind} is not a parameterised constructor"),
    };
}
