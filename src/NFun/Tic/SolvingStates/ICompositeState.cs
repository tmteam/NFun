using System.Collections.Generic;

namespace NFun.Tic.SolvingStates;

public interface ITypeState : ITicNodeState {
    ITypeState GetLastCommonAncestorOrNull(ITypeState otherType);
    bool CanBeImplicitlyConvertedTo(StatePrimitive type);
}

public interface ICompositeState : ITypeState {
    ICompositeState GetNonReferenced();
    /// <summary>
    /// State of any Member node is 'RefTo'
    /// </summary>
    bool HasAnyReferenceMember { get; }
    IEnumerable<TicNode> Members { get; }
    IEnumerable<TicNode> AllLeafTypes { get; }
}

public interface ITicNodeState {
    /// <summary>
    /// This type is not solved or can be changed
    /// </summary>
    bool IsMutable { get; }

    /// <summary>
    /// This type and all dependent type are solved.
    /// </summary>
    bool IsSolved { get; }

    string Description { get; }
}
