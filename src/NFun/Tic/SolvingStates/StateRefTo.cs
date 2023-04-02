namespace NFun.Tic.SolvingStates;

public class StateRefTo : ITicNodeState {
    public StateRefTo(TicNode node) => Node = node;
    public bool IsSolved => false;
    public bool IsMutable => !IsSolved;
    public ITicNodeState GetNonReference() => Node.GetNonReference().State;
    public ITicNodeState Element => Node.State;
    public TicNode Node { get; }
    public override string ToString() => $"ref({Node.Name})";
    public string Description => Node.Name.ToString();

    public override bool Equals(object obj) {
        if (obj is not StateRefTo refTo)
            return false;

        return Node.Equals(refTo.Node);
    }
}
