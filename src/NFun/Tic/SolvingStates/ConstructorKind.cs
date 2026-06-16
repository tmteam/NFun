namespace NFun.Tic.SolvingStates;

/// <summary>
/// Discriminator for composite collection types in TIC.
///
/// Two abstract members (<see cref="Enumerable"/>, <see cref="FixedArray"/>)
/// cannot be instantiated by the TIC algebra — they exist only as constraint
/// upper bounds in function signatures (`xs: Enumerable&lt;T&gt;`). When a
/// generic constraint resolves to one of them at finalize time, the
/// <c>Concretest</c> rule descends to a concrete member (Enumerable → List,
/// FixedArray → Array).
///
/// Concrete single-arg members (List/FixedArray/Array/Set, future Queue/Stack)
/// are carried as data on the unified <see cref="StateCollection"/> class — they
/// no longer have per-class C# subtypes. <see cref="Map"/> ALSO uses
/// <see cref="StateCollection"/>: Map's element node holds a frozen
/// <c>{key: K, value: V}</c> pair-struct, so map is uniformly an
/// <c>Enumerable&lt;{key, value}&gt;</c> at the algebra level and shares all
/// Apply cells / merge / LCA paths with the other single-arg collections.
///
/// <see cref="Any"/> is the universal top — returned by
/// <see cref="ConstructorLattice"/> when two unrelated constructors meet.
/// Distinct from <see cref="StatePrimitive.Any"/>; this is a constructor-
/// level value, not a type-state.
///
/// The legacy <c>StateArray</c> (ee-mode immutable, covariant) does NOT
/// have a member here — it stays outside the new lattice to preserve
/// expression-mode semantics unchanged.
/// </summary>
public enum ConstructorKind : byte {
    /// <summary>Universal top — no common constructor.</summary>
    Any        = 0,

    /// <summary>Abstract: iteration-only. Not directly instantiable; used as a constraint.</summary>
    Enumerable = 1,

    /// <summary>Abstract: + indexed read. Concretely instantiable via the <c>fixedArray(...)</c> factory.</summary>
    FixedArray = 2,

    /// <summary>Concrete: + indexed write (mutable fixed-size). Lang-mode <c>int[]</c>.</summary>
    Array      = 3,

    /// <summary>Concrete: + add/remove/clear (mutable growable).</summary>
    List       = 4,

    /// <summary>Concrete: hash-based, unordered, no duplicates.</summary>
    Set        = 5,

    /// <summary>Concrete: hash-based key→value mapping.</summary>
    Map        = 6,
}

/// <summary>
/// Variance of a single type argument of a <see cref="StateComposite"/>.
///
/// All lang-mode collections are <see cref="Invariant"/> in their element type
/// per the Stage 0 design decision (Specs/Collections.md §Design constraints).
/// We do NOT implement variance-climbing during LCA — the algorithm is simpler
/// at the cost of LCA(list&lt;int&gt;, list&lt;text&gt;) collapsing to Any.
///
/// <see cref="Covariant"/> exists for completeness (legacy <c>StateArray</c>
/// in ee-mode behaves covariantly) but no new <see cref="StateComposite"/>
/// subclass uses it.
/// </summary>
public enum Variance : byte {
    Invariant = 0,
    Covariant = 1,
}
