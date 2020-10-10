using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public interface IStateCombinationFunctions
    {
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