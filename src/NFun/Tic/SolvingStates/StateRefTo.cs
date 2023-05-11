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

    public string PrintState(int depth) {
        if (depth > 100)
            return "ref(...REQ...)";
        return $"ref({Node.State.PrintState(depth + 1)})";
    }

    public string StateDescription => PrintState(0);

    public bool CanBePessimisticConvertedTo(StatePrimitive primitive) =>
        primitive.Equals(StatePrimitive.Any);

    public override bool Equals(object obj) {
        if (obj is not StateRefTo refTo)
            return false;

        return Node.Equals(refTo.Node);
    }
}
