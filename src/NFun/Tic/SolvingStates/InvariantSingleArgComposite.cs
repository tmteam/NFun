namespace NFun.Tic.SolvingStates;

/// <summary>Cycle-guarded equality + invariant LCA shared by single-arg
/// <see cref="StateCollection"/>s.</summary>
internal static class InvariantSingleArgComposite {

    /// <summary>Element-equality with coinductive cycle guard for recursive shapes. Caller
    /// must have verified class + Constructor match. <paramref name="cycleGuardMark"/> must be
    /// class-specific so mutually-recursive distinct shapes don't false-positive.</summary>
    public static bool EqualsWithCycleGuard(TicNode elementNode, ITicNodeState otherElement, int cycleGuardMark) {
        if (elementNode.VisitMark == cycleGuardMark) return true;
        var prev = elementNode.VisitMark;
        elementNode.VisitMark = cycleGuardMark;
        try { return otherElement.Equals(elementNode.State); }
        finally { elementNode.VisitMark = prev; }
    }

    /// <summary>Invariant LCA for same-class composites: equal→self, unequal→Any,
    /// unresolved→null (deferred).</summary>
    public static ITypeState LcaInvariantOrAny(
        ITicNodeState selfElement, ITicNodeState otherElement, StateComposite self) {
        if (selfElement is not ITypeState a)  return null;
        if (otherElement is not ITypeState b) return null;
        return a.Equals(b) ? (ITypeState)self : StatePrimitive.Any;
    }
}
