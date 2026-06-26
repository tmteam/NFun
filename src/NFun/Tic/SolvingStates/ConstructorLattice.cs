using System;

namespace NFun.Tic.SolvingStates;

/// <summary>LCA / GCD / subtype on the <see cref="ConstructorKind"/> lattice.
/// See specs_tic/TicTypeSystem.md §ConstructorLattice for the hierarchy.</summary>
public static class ConstructorLattice {

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

    public static ConstructorKind Lca(ConstructorKind a, ConstructorKind b) {
        if (a == b) return a;
        if (a == ConstructorKind.Any || b == ConstructorKind.Any) return ConstructorKind.Any;
        // Bitset is `int` (32 bits) — switch to `long` if ConstructorKind grows past 32 members.
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

    /// <summary>GCD of two constructors, or null when they lie in disjoint sub-trees.</summary>
    public static ConstructorKind? Gcd(ConstructorKind a, ConstructorKind b) {
        if (a == b) return a;
        if (a == ConstructorKind.Any) return b;
        if (b == ConstructorKind.Any) return a;
        if (IsSubtypeOf(a, b)) return a;
        if (IsSubtypeOf(b, a)) return b;
        return null;
    }

    /// <summary>True iff <paramref name="child"/> &lt;: <paramref name="parent"/> in the lattice
    /// (or equal).</summary>
    public static bool IsSubtypeOf(ConstructorKind child, ConstructorKind parent) {
        if (parent == ConstructorKind.Any) return true;
        var cur = child;
        while (cur != ConstructorKind.Any) {
            if (cur == parent) return true;
            cur = Parent[(int)cur];
        }
        return false;
    }

    /// <summary>True iff the constructor supports `.clear()`. Array is excluded because its
    /// length is fixed (element-wise mutable, not clearable).</summary>
    public static bool IsClearable(ConstructorKind kind) => kind switch {
        ConstructorKind.List => true,
        ConstructorKind.Set  => true,
        ConstructorKind.Map  => true,
        _ => false,
    };

    /// <summary>Preferred concrete instantiation of an abstract constructor — see
    /// specs_tic/TicTypeSystem.md §Concretest.</summary>
    public static ConstructorKind Concretest(ConstructorKind kind) => kind switch {
        ConstructorKind.Enumerable => ConstructorKind.List,
        ConstructorKind.FixedArray => ConstructorKind.FixedArray,
        _ => kind,
    };

    /// <summary>True iff the constructor exists only as a constraint upper bound (no factory).
    /// Distinct from <see cref="RequiresConcretestDescent"/>.</summary>
    public static bool IsConstraintOnly(ConstructorKind kind) =>
        kind == ConstructorKind.Enumerable;

    /// <summary>True iff Concretest must descend further from this constructor.</summary>
    public static bool RequiresConcretestDescent(ConstructorKind kind) =>
        kind == ConstructorKind.Enumerable
        || kind == ConstructorKind.FixedArray;

    /// <summary>Variance of the element argument. All lattice constructors are invariant;
    /// the legacy <c>StateArray</c> is not part of this lattice.</summary>
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
