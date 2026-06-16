using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

/// <summary>
/// 2-dimentional visitor implement some operation
/// for all possible combinations of states
///
/// like
///   Primitive vs Primitive
///   Primitive vs Constrains
///   Primitive vs Composite
///   Constrains vs Primitive
///   Constrains vs Constrains
/// etc..
///
/// Reference states are excluded (all of them references for non reference state)
///
/// This visitor implemented for Push, Pull and Destruction stages of tic solving
/// </summary>
public interface IStateFunction {
    bool Apply(
        StatePrimitive ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StatePrimitive ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StatePrimitive ancestor,
        ICompositeState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ConstraintsState ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ConstraintsState ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ConstraintsState ancestor,
        ICompositeState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ICompositeState ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ICompositeState ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StateArray ancestor,
        StateArray descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StateFun ancestor,
        StateFun descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StateStruct ancestor,
        StateStruct descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StateOptional ancestor,
        StateOptional descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    // StateMap deleted — map dispatch flows through the single-arg
    // StateCollection cell below (Constructor = ConstructorKind.Map).

    /// <summary>
    /// Unified Apply for all single-arg invariant collections (list, fixedArray,
    /// array, set, future queue/stack). Discriminated by <see cref="StateCollection.Constructor"/>
    /// — cross-kind pairs reject (uniform invariance).
    /// </summary>
    bool Apply(
        StateCollection ancestor,
        StateCollection descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>
    /// Cross-family subtyping <c>StateCollection(List) ≤ StateArray</c> per Stage 0
    /// collections hierarchy. Lets lang-mode <c>list&lt;T&gt;</c> values flow into
    /// the ee-mode <c>T[]</c> argument positions used by existing LINQ generic
    /// functions without per-function overloads. Other ConstructorKinds reject.
    /// </summary>
    bool Apply(
        StateArray ancestor,
        StateCollection descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>
    /// Reverse cross-family direction: legacy ee-mode <c>StateArray</c> values
    /// flow into a lang-mode <c>StateCollection</c> slot (e.g. existing LINQ
    /// returns <c>T[]</c>, assigned into a <c>placed:int[]</c> = <c>array&lt;int&gt;</c>
    /// slot in lang). Conversion is via <c>VarTypeConverter</c> at the call
    /// site. Other ConstructorKinds reject.
    /// </summary>
    bool Apply(
        StateCollection ancestor,
        StateArray descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    // ─────────────────────────────────────────────────────────────────────
    // Stage C.3 — StateCompositeConstraints Apply cells.
    // CompCS is an interval state over ConstructorLattice (peer of ConstraintsState).
    // Per Specs/Tic/Algebra_CompositeConstraints.md §4: Layer-2 directional commit.

    /// <summary>§4 same-class: delegate to UnifyOrNull (symmetric).</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>§4 CompCS as ancestor, primitive as descendant. Any = no-op; non-Any reject.</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>§4 reverse: primitive as ancestor, CompCS as descendant. Any = no-op; non-Any reject.</summary>
    bool Apply(
        StatePrimitive ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>§4 CompCS anc, ConstraintsState desc. Coerce CS to CompCS-view if CS.Desc is composite.</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>§4 reverse: CS anc, CompCS desc.</summary>
    bool Apply(
        ConstraintsState ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>
    /// §4.1 Forward Pull/Push/Destruction with StateCollection / StateArray / StateOptional descendant.
    /// StateFun / StateStruct descendants reject explicitly.
    /// </summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        ICompositeState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>
    /// §4.1 Reverse: composite ancestor, CompCS descendant.
    /// </summary>
    bool Apply(
        ICompositeState ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);
}
