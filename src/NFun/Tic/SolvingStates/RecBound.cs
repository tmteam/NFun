namespace NFun.Tic.SolvingStates;

/// <summary>
/// F-bound representation: a single slot in <see cref="ConstraintsState"/> carrying the bound
/// body. Currently only struct-shape bounds exist (μX.{...}); reserving capacity for OptShape /
/// ArrShape / FunShape is removed per "no design for hypothetical future requirements". Pending
/// task GH #127 eliminates this wrapper in favor of self-referencing StateStruct.
///
/// References:
/// - Pierce TAPL §20.2 (iso-recursive types, fold/unfold)
/// - Amadio-Cardelli '93 §4.2 (coinductive equality)
/// </summary>
public sealed class RecBound {
    /// <summary>τ — bound shape; today always <see cref="StateStruct"/>.</summary>
    public ITypeState Body { get; }

    private RecBound(ITypeState body) {
        Body = body;
    }

    public static RecBound OfStruct(StateStruct s) => new(s);

    /// <summary>
    /// Structural equality on body. Defers to <see cref="StateStruct.Equals(object)"/>
    /// (Amadio-Cardelli bisimulation, cycle-aware via thread-static visited guard).
    /// </summary>
    public override bool Equals(object obj) =>
        obj is RecBound other && Body.Equals(other.Body);

    /// <summary>StateStruct's hash uses field names + arity (cycle-safe).</summary>
    public override int GetHashCode() => Body?.GetHashCode() ?? 0;

    public override string ToString() => $"RB({Body})";
}
