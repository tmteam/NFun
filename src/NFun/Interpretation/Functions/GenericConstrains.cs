using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.Interpretation.Functions;

using static StatePrimitive;

public readonly struct GenericConstrains {
    public readonly StatePrimitive Ancestor;
    public readonly StatePrimitive Descendant;
    public readonly bool IsComparable;
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

    public override string ToString() {
        if (Ancestor == null && Descendant == null && !HasStructDescendant && IsComparable)
            return "<>";
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
        = new(Real, I16, false);

    public static readonly GenericConstrains Numbers
        = new(Real, null, false);

    public static GenericConstrains FromTicConstrains(ConstraintsState constraintsState)
        => new(constraintsState.Ancestor, constraintsState.Descendant as StatePrimitive, constraintsState.IsComparable);

    internal static GenericConstrains WithStructDescendant(FunnyType structType)
        => new(null, null, false, structType);

    private GenericConstrains(
        StatePrimitive ancestor = null, StatePrimitive descendant = null,
        bool isComparable = false, FunnyType structDescendant = default) {
        Ancestor = ancestor;
        Descendant = descendant;
        IsComparable = isComparable;
        StructDescendant = structDescendant;
    }
}
