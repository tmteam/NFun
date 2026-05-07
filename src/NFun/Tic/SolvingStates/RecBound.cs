using System.Collections.Generic;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// F-bound representation as typed ADT: a single slot in <see cref="ConstraintsState"/> carrying
/// body+binders+kind, with dispatch via <see cref="RecBoundKind"/> tag.
///
/// References:
/// - Pierce TAPL §20.2 (iso-recursive types, fold/unfold)
/// - Crary-Harper-Puri '99 PLDI (vector-bound μ-types in TILT)
/// - Amadio-Cardelli '93 §4.2 (coinductive equality)
/// - Wand '87 LICS §4 (type-equation systems)
/// </summary>
public enum RecBoundKind {
    /// <summary>τ = StateStruct{...} — the populated case.</summary>
    StructShape,
    /// <summary>τ = StateOptional(...) — reserved.</summary>
    OptShape,
    /// <summary>τ = StateArray(...) — reserved for forest-like recursion.</summary>
    ArrShape,
    /// <summary>τ = StateFun(...) — reserved for higher-order rec.</summary>
    FunShape,
}

public sealed class RecBound {
    /// <summary>
    /// τ — type-with-hole. Currently always <see cref="StateStruct"/>; reserved kinds may carry
    /// <see cref="StateOptional"/>, <see cref="StateArray"/>, or <see cref="StateFun"/>.
    /// </summary>
    public ITypeState Body { get; private set; }

    /// <summary>
    /// X⃗ — recursion variables. Currently always empty (single implicit binder = the owning
    /// ConstraintsState's TicNode). Empty Binders ≡ no explicit binder ≡ definitionally
    /// equivalent to plain τ. Mutual recursion (Crary-Harper-Puri '99) would populate this.
    /// </summary>
    public IReadOnlyList<TicNode> Binders { get; }

    /// <summary>O(1) dispatch tag. See <see cref="RecBoundKind"/>.</summary>
    public RecBoundKind Kind { get; }

    /// <summary>
    /// True iff fixpoint reached, no free recursion variable. Set by SCC+Kleene driver after
    /// Pull-iteration converges. Currently always true (StructShape bounds enter already closed
    /// via LiftMuTypes).
    /// </summary>
    public bool IsClosed { get; private set; }

    public RecBound(ITypeState body, RecBoundKind kind, IReadOnlyList<TicNode> binders = null, bool isClosed = true) {
        Body = body;
        Kind = kind;
        Binders = binders ?? System.Array.Empty<TicNode>();
        IsClosed = isClosed;
    }

    /// <summary>Convenience constructor for StructShape.</summary>
    public static RecBound OfStruct(StateStruct s) =>
        new(s, RecBoundKind.StructShape);

    /// <summary>
    /// Structural equality on body, dispatched by Kind. Defers to
    /// <see cref="StateStruct.Equals(object)"/> for StructShape (Amadio-Cardelli bisimulation,
    /// cycle-aware via thread-static visited guard).
    /// </summary>
    public override bool Equals(object obj) {
        if (obj is not RecBound other) return false;
        if (Kind != other.Kind) return false;
        return Body.Equals(other.Body);
    }

    public override int GetHashCode() {
        // Defer to body. StateStruct's hash uses field names + arity (cycle-safe).
        return ((int)Kind * 31) ^ (Body?.GetHashCode() ?? 0);
    }

    public override string ToString() => $"RB.{Kind}({Body})";
}
