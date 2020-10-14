 using System.Collections.Generic;
 using NFun.Tic.Stages;

 namespace NFun.Tic.SolvingStates
{
    public interface ITypeState : ITicNodeState
    {
        ITypeState GetLastCommonAncestorOrNull(ITypeState otherType);
        bool CanBeImplicitlyConvertedTo(StatePrimitive type);
    }
    public interface ICompositeState : ITypeState
    {
        ICompositeState GetNonReferenced();
        /// <summary>
        /// State of any Member node is 'RefTo'
        /// </summary>
        bool HasAnyReferenceMember { get; }
        IEnumerable<TicNode> Members { get; }
        IEnumerable<TicNode> AllLeafTypes { get; }
    }
    public interface ITicNodeState
    {
        bool IsSolved { get; }
        string Description { get; }
        bool ApplyDescendant(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode);
        bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, StatePrimitive ancestor);
        bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, ConstrainsState ancestor);
        bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, ICompositeState ancestor);
    }
}