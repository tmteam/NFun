using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors {

internal class CannotSetStateSyntaxNodeException: TicException{
    public TicNode Node { get; } 
    public ITicNodeState State { get; }
    public CannotSetStateSyntaxNodeException(TicNode node, ITicNodeState state) : base($"Node {node} cannot has state {state}") {
        Node = node;
        State = state;
    }
}

}