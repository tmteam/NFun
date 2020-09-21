 using System.Collections.Generic;

namespace NFun.Tic.SolvingStates
{
    public interface ITypeState : ITicNodeState
    {
        bool IsSolved { get; }
        ITypeState GetLastCommonAncestorOrNull(ITypeState otherType);
        bool CanBeImplicitlyConvertedTo(StatePrimitive type);
    }
    public interface ICompositeTypeState : ITypeState
    {
        ICompositeTypeState GetNonReferenced();
        /// <summary>
        /// State of any Member node is 'RefTo'
        /// </summary>
        bool HasAnyReferenceMember { get; }
        IEnumerable<TicNode> Members { get; }
        IEnumerable<TicNode> AllLeafTypes { get; }
    }
    public interface ITicNodeState
    {
        string Description { get; }
    }
   
}