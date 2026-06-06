namespace NFun.Tic.SolvingStates;

/// <summary>
/// Shared algebra for the invariant single-argument <see cref="StateCollection"/>
/// (covers list, fixedArray, array, set, queue, stack via ConstructorKind data
/// discrimination — see <see cref="StateCollection"/>).
///
/// Centralises the cycle-guarded equality protocol and the invariant-LCA rule
/// (same-element → self; mismatch → <see cref="StatePrimitive.Any"/>) so that
/// <see cref="StateCollection"/> stays a thin shell over the per-instance data.
///
/// Composition, not inheritance — the helper is invoked by static call, the
/// state class is not coupled to it via a base contract.
/// </summary>
internal static class InvariantSingleArgComposite {

    /// <summary>
    /// Cycle-guarded structural equality of two single-arg invariant composites.
    /// Caller has already verified the class + Constructor match
    /// (<c>obj is StateCollection other &amp;&amp; other.Constructor == Constructor</c>)
    /// and now wants element-equality with a coinductive guard for recursive types
    /// (e.g. <c>forest = {kids: list&lt;forest&gt;}</c>).
    /// </summary>
    /// <param name="elementNode">Self's element node — its <c>VisitMark</c> is mutated.</param>
    /// <param name="otherElement">The other instance's element state.</param>
    /// <param name="cycleGuardMark">Class-specific guard constant. Distinct values
    /// for distinct classes prevent false equality when mutually-recursive types
    /// of different shapes meet.</param>
    public static bool EqualsWithCycleGuard(TicNode elementNode, ITicNodeState otherElement, int cycleGuardMark) {
        if (elementNode.VisitMark == cycleGuardMark) return true;
        var prev = elementNode.VisitMark;
        elementNode.VisitMark = cycleGuardMark;
        try { return otherElement.Equals(elementNode.State); }
        finally { elementNode.VisitMark = prev; }
    }

    /// <summary>
    /// LCA rule for invariant single-arg composites of the SAME class.
    /// Caller has already verified the class match. Returns:
    ///   • <c>self</c> when both elements are concrete types and equal,
    ///   • <see cref="StatePrimitive.Any"/> when both are concrete but unequal
    ///     (uniform-invariance rule — no element widening),
    ///   • <c>null</c> when either side is not yet resolved to a concrete
    ///     <see cref="ITypeState"/> (deferred until concretisation).
    /// </summary>
    public static ITypeState LcaInvariantOrAny(
        ITicNodeState selfElement, ITicNodeState otherElement, StateComposite self) {
        if (selfElement is not ITypeState a)  return null;
        if (otherElement is not ITypeState b) return null;
        return a.Equals(b) ? (ITypeState)self : StatePrimitive.Any;
    }
}
