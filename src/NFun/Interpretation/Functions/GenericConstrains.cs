using NFun.Tic.SolvingStates;

namespace NFun.Interpretation.Functions;

using static StatePrimitive;

public readonly struct GenericConstrains {
    public readonly StatePrimitive Ancestor;
    public readonly StatePrimitive Descendant;
    public readonly bool IsComparable;

    public override string ToString() {
        if (Ancestor == null && Descendant == null && IsComparable)
            return "<>";
        return $"[{Descendant}..{Ancestor}]" + (IsComparable ? "<>" : "");
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

    public static GenericConstrains FromTicConstrains(ConstrainsState constrainsState)
        => new(constrainsState.Ancestor, constrainsState.Descendant as StatePrimitive, constrainsState.IsComparable);

    private GenericConstrains(
        StatePrimitive ancestor = null, StatePrimitive descendant = null,
        bool isComparable = false) {
        Ancestor = ancestor;
        Descendant = descendant;
        IsComparable = isComparable;
    }
}
