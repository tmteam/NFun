namespace NFun.Tic.SolvingStates;

/// <summary>Composite-collection constructor discriminator. See Specs/Collections.md and
/// specs_tic/TicTypeSystem.md §ConstructorLattice. Abstract members (Enumerable, FixedArray)
/// appear only as signature upper bounds. <see cref="Any"/> is the lattice top, distinct
/// from <see cref="StatePrimitive.Any"/>. Legacy <c>StateArray</c> is outside this lattice.</summary>
public enum ConstructorKind : byte {
    /// <summary>Lattice top.</summary>
    Any        = 0,
    Enumerable = 1,
    FixedArray = 2,
    Array      = 3,
    List       = 4,
    Set        = 5,
    Map        = 6,
}

/// <summary>Variance of a single <see cref="StateComposite"/> argument.
/// See Specs/Collections.md §Design constraints. No variance-climbing in LCA.</summary>
public enum Variance : byte {
    Invariant = 0,
    Covariant = 1,
}
