﻿ using System.Collections.Generic;

namespace NFun.Tic.SolvingStates
{
    public interface IType : IState
    {
        bool IsSolved { get; }
        IType GetLastCommonAncestorOrNull(IType otherType);
        bool CanBeImplicitlyConvertedTo(Primitive type);
    }
    public interface ICompositeType : IType
    {
        ICompositeType GetNonReferenced();
        /// <summary>
        /// State of any Member node is 'RefTo'
        /// </summary>
        bool HasAnyReferenceMember { get; }
        IEnumerable<SolvingNode> Members { get; }
        IEnumerable<SolvingNode> AllLeafTypes { get; }
    }
    public interface IState
    {
        string Description { get; }
    }
   
}