using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.Interpretation.Functions;

using static StatePrimitive;

public readonly struct GenericConstrains {
    public readonly StatePrimitive Ancestor;
    public readonly StatePrimitive Descendant;
    public readonly bool IsComparable;
    /// <summary>
    /// Preferred type for this generic (e.g. Int32 for integer literals).
    /// Propagated from TIC ConstraintsState so that generic functions
    /// whose type variables are not constrained by call-site arguments
    /// still resolve to the preferred type rather than the widest (ancestor).
    /// </summary>
    public readonly StatePrimitive Preferred;
    /// <summary>
    /// Struct descendant constraint as a FunnyType (lang-level type).
    /// When set, the generic type must be a struct with at least these fields.
    /// The struct fields may reference other generics via FunnyType.Generic(i).
    /// This preserves struct field constraints for generic user functions,
    /// allowing callers to pass structs with additional fields beyond
    /// what the function body accesses.
    /// </summary>
    public readonly FunnyType StructDescendant;

    public bool HasStructDescendant => StructDescendant.BaseType == BaseFunnyType.Struct;

    /// <summary>
    /// F-bounded structural upper bound.
    /// `T &lt;: {fᵢ: state}` — covariant width subtyping; the bound's field
    /// types may include <see cref="FunnyType.Generic"/>(i) referring to the
    /// owning generic itself (F-bound self-reference). Distinct from
    /// <see cref="StructDescendant"/> which carries open-row width-poly
    /// semantics ("at least these fields"). They are MUTUALLY EXCLUSIVE —
    /// a single GenericConstrains never has both.
    ///
    /// Established by <c>LiftMuTypes</c> for cycle-rescued opt-sourced
    /// recursive structs whose registry lookup did not yield a unique
    /// TypeName. Call-site dispatch checks structural Fit:
    /// <c>candidate &lt;: bound iff candidate is a struct, Fields(candidate) ⊇ Fields(bound),
    /// and pointwise covariant ≤ on shared fields, with self-RefTo recursion
    /// resolved coinductively (Amadio–Cardelli equirecursive subtyping).</c>
    /// </summary>
    public readonly FunnyType StructBound;

    public bool HasStructBound => StructBound.BaseType == BaseFunnyType.Struct;

    /// <summary>
    /// Composite-lattice constraint — Stage C carrier for <c>Enumerable&lt;T&gt;</c> /
    /// <c>IndexedRead&lt;T&gt;</c> / <c>IndexedMutable&lt;T&gt;</c> typeclass signatures.
    /// When set, signals that the generic's TIC node should be emitted as
    /// <see cref="StateCompositeConstraints"/> with <c>Ancestor = CompositeAncestor</c>.
    /// <para>The discriminator carries only the upper bound (cap). Lower bound is null
    /// (unconstrained) for all current use cases — call-site Pull refines Desc.</para>
    /// <para>Mutually exclusive with <see cref="StructDescendant"/> /
    /// <see cref="StructBound"/>: composite-shape and struct-shape don't coexist on
    /// a single generic.</para>
    /// </summary>
    public readonly ConstructorKind? CompositeAncestor;

    public bool HasCompositeAncestor => CompositeAncestor.HasValue;

    public override string ToString() {
        if (Ancestor == null && Descendant == null && !HasStructDescendant && !HasStructBound && !HasCompositeAncestor && IsComparable)
            return "<>";
        if (HasStructBound) return $"⊆{StructBound}";
        if (HasCompositeAncestor) return $"⊆{CompositeAncestor.Value}";
        var desc = HasStructDescendant ? StructDescendant.ToString() : Descendant?.ToString();
        var suffix = IsComparable ? "<>" : "";
        return $"[{desc}..{Ancestor}]" + suffix;
    }

    public static readonly GenericConstrains Comparable = new(null, null, true);

    public static readonly GenericConstrains Any
        = new(null, null, false);

    public static readonly GenericConstrains Arithmetical
        = new(Real, U24, false);

    public static readonly GenericConstrains Integers
        = new(I96, null, false);

    public static readonly GenericConstrains Integers3264
        = new(I96, U24, false);

    public static readonly GenericConstrains Integers32
        = new(I48, null, false);

    public static readonly GenericConstrains SignedNumber
        = new(Real, I8, false);

    /// <summary>IEEE float family constraint: <c>F32 ≤ T ≤ Real</c>.</summary>
    public static readonly GenericConstrains Floats
        = new(Real, F32, false);

    public static readonly GenericConstrains Numbers
        = new(Real, null, false);

    public static GenericConstrains FromTicConstrains(ConstraintsState constraintsState)
        => new(constraintsState.Ancestor, constraintsState.Descendant as StatePrimitive,
            constraintsState.IsComparable, preferred: constraintsState.Preferred);

    internal static GenericConstrains WithStructDescendant(FunnyType structType)
        => new(null, null, false, structType);

    /// <summary>
    /// F-bounded structural upper bound. Mutually exclusive with
    /// <see cref="StructDescendant"/>. <paramref name="structType"/> MUST have
    /// <c>BaseType == Struct</c> (peers may include <see cref="FunnyType.Generic"/>(i)
    /// for self-references).
    /// </summary>
    internal static GenericConstrains WithStructBound(FunnyType structType)
        => new(null, null, false, default, null, structType);

    // ─────────────────────────────────────────────────────────────────────
    // Stage C — composite-lattice constraint factories.

    /// <summary>
    /// <c>Enumerable&lt;T&gt;</c> constraint — top of the ConstructorLattice.
    /// Used by LINQ-style functions: <c>count</c>, <c>map</c>, <c>filter</c>,
    /// <c>any</c>, <c>fold</c>, etc. Accepts any concrete collection kind.
    /// </summary>
    public static readonly GenericConstrains Enumerable
        = new(null, null, false, default, null, default, ConstructorKind.Enumerable);

    /// <summary>
    /// <c>IndexedRead&lt;T&gt;</c> constraint — <c>Anc ≤ FixedArray</c> in the lattice.
    /// Used by random-access read functions: <c>get</c>, <c>slice</c>, <c>chunk</c>.
    /// </summary>
    public static readonly GenericConstrains IndexedRead
        = new(null, null, false, default, null, default, ConstructorKind.FixedArray);

    /// <summary>
    /// <c>IndexedMutable&lt;T&gt;</c> constraint — <c>Anc ≤ Array</c> in the lattice.
    /// Used by indexed assignment <c>a[i] = v</c> and other mutating-by-index
    /// operations. Closes tech-debt #14 when wired at <c>TicSetupVisitor:2465-2468</c>.
    /// </summary>
    public static readonly GenericConstrains IndexedMutable
        = new(null, null, false, default, null, default, ConstructorKind.Array);

    private GenericConstrains(
        StatePrimitive ancestor = null, StatePrimitive descendant = null,
        bool isComparable = false, FunnyType structDescendant = default,
        StatePrimitive preferred = null,
        FunnyType structBound = default,
        ConstructorKind? compositeAncestor = null) {
        Ancestor = ancestor;
        Descendant = descendant;
        IsComparable = isComparable;
        StructDescendant = structDescendant;
        Preferred = preferred;
        StructBound = structBound;
        CompositeAncestor = compositeAncestor;
        #if DEBUG
        // Sanity: StructBound and StructDescendant carry different semantics
        // (F-bound vs open-row); a single constraint never has both.
        System.Diagnostics.Debug.Assert(
            !(StructDescendant.BaseType == BaseFunnyType.Struct
              && StructBound.BaseType == BaseFunnyType.Struct),
            "GenericConstrains: StructBound and StructDescendant must not coexist");
        // CompositeAncestor mutually exclusive with struct constraints (composite vs struct shape).
        System.Diagnostics.Debug.Assert(
            !(CompositeAncestor.HasValue
              && (StructDescendant.BaseType == BaseFunnyType.Struct
                  || StructBound.BaseType == BaseFunnyType.Struct)),
            "GenericConstrains: CompositeAncestor and Struct* must not coexist");
        #endif
    }
}
