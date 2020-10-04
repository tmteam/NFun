using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors
{
    public class IncompatibleAncestorNamedNodeException : TicException
    {
        public string NodeName { get; }
        public ITicNodeState Ancestor { get; }
        public ITicNodeState Descendant { get; }

        public IncompatibleAncestorNamedNodeException(string nodeName, ITicNodeState ancestor, ITicNodeState descendant) : base($"Incompatible ancestor {ancestor}=>{descendant} at node {nodeName}")
        {
            NodeName = nodeName;
            Ancestor = ancestor;
            Descendant = descendant;
        }
    }
}