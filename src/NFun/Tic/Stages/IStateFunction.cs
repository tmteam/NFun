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
}
