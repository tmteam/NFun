using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages {

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
public interface IStateCombination2dimensionalVisitor {
    bool Apply(
        StatePrimitive ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StatePrimitive ancestor,
        ConstrainsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        StatePrimitive ancestor,
        ICompositeState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ConstrainsState ancestor,
        StatePrimitive descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ConstrainsState ancestor,
        ConstrainsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ConstrainsState ancestor,
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
        ConstrainsState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);

    bool Apply(
        ICompositeState ancestor,
        ICompositeState descendant,
        TicNode ancestorNode,
        TicNode descendantNode);
}

}