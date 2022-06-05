using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors;

internal class TicNodeIsNotAFunctionalVariableException: TicException{
    public TicNode Node { get; } 
    public ITicNodeState State { get; }
    public TicNodeIsNotAFunctionalVariableException(TicNode node, ITicNodeState state) 
        : base($"Node {node} cannot has state {state} as it is not a functional variable or function") {
        Node = node;
        State = state;
    }
}