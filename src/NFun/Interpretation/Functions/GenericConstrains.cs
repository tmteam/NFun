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

    public override string ToString() {
        if (Ancestor == null && Descendant == null && !HasStructDescendant && !HasStructBound && IsComparable)
            return "<>";
        if (HasStructBound) return $"⊆{StructBound}";
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

    private GenericConstrains(
        StatePrimitive ancestor = null, StatePrimitive descendant = null,
        bool isComparable = false, FunnyType structDescendant = default,
        StatePrimitive preferred = null,
        FunnyType structBound = default) {
        Ancestor = ancestor;
        Descendant = descendant;
        IsComparable = isComparable;
        StructDescendant = structDescendant;
        Preferred = preferred;
        StructBound = structBound;
        #if DEBUG
        // Sanity: StructBound and StructDescendant carry different semantics
        // (F-bound vs open-row); a single constraint never has both.
        System.Diagnostics.Debug.Assert(
            !(StructDescendant.BaseType == BaseFunnyType.Struct
              && StructBound.BaseType == BaseFunnyType.Struct),
            "GenericConstrains: StructBound and StructDescendant must not coexist");
        #endif
    }
}
