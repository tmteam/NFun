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

    /// <summary>Apply for unified single-arg invariant collections.
    /// Cross-kind pairs reject.</summary>
    bool Apply(
        StateCollection ancestor,
        StateCollection descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>Cross-family subtyping: Array-branch StateCollection ≤ StateArray.
    /// Other ConstructorKinds reject.</summary>
    bool Apply(
        StateArray ancestor,
        StateCollection descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>Reverse cross-family direction: StateArray ≤ Array-branch StateCollection.
    /// Conversion via <c>VarTypeConverter</c>. Other ConstructorKinds reject.</summary>
    bool Apply(
        StateCollection ancestor,
        StateArray descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    // StateCompositeConstraints cells.
    // See specs_tic/Algebra/CompositeConstraints.md §4.

    /// <summary>Same-class: delegate to UnifyOrNull.</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>CompCS anc × primitive desc. Any = no-op; non-Any reject.</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>Primitive anc × CompCS desc. Any = no-op; non-Any reject.</summary>
    bool Apply(
        StatePrimitive ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>CompCS anc × CS desc. Coerce CS to CompCS-view if CS.Desc is composite.</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        ConstraintsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>CS anc × CompCS desc.</summary>
    bool Apply(
        ConstraintsState ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>CompCS anc × ICompositeState desc. StateFun/StateStruct reject.</summary>
    bool Apply(
        StateCompositeConstraints ancestor,
        ICompositeState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    /// <summary>ICompositeState anc × CompCS desc.</summary>
    bool Apply(
        ICompositeState ancestor,
        StateCompositeConstraints descendant,
        TicNode ancestorNode,
        TicNode descendantNode);
}
