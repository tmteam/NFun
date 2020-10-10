namespace NFun.Tic.SolvingStates
{
    public class StateRefTo: ITicNodeState
    {
        public StateRefTo(TicNode node)
        {
            Node = node;
        }

        public ITicNodeState GetNonReference() => Node.GetNonReference().State;
        public ITicNodeState Element => Node.State; 
        public TicNode Node { get; }
        public override string ToString() => $"ref({Node.Name})";
        public string Description => Node.Name.ToString();

        public bool ApplyDescendant(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode) => false;
        

        public bool Apply(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode, StatePrimitive ancestor)
            => false;

        public bool Apply(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode, ConstrainsState ancestor)
            => false;

        public bool Apply(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode, ICompositeState ancestor)
            => false;
    }
}