namespace NFun.Tic.SolvingStates;

using System.Collections.Generic;

public interface ITypeState : ITicNodeState {
    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType);
}

public interface ICompositeState : ITypeState {
    public ICompositeState GetNonReferenced();
    /// <summary>
    /// State of any Member node is 'RefTo'
    /// </summary>
    public bool HasAnyReferenceMember { get; }

    public int MemberCount { get; }
    public TicNode GetMember(int index);
    public IEnumerable<TicNode> Members { get; }
    public IEnumerable<TicNode> AllLeafTypes { get; }
}

public interface ITicNodeState {
    /// <summary>
    /// This type is not solved or can be changed
    /// </summary>
    public bool IsMutable { get; }

    /// <summary>
    /// This type and all dependent type are solved.
    /// </summary>
    public bool IsSolved { get; }

    public string Description { get; }

    public string PrintState(int depth);
    public string StateDescription { get; }

    public bool CanBePessimisticConvertedTo(StatePrimitive primitive);
}
