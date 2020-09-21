using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Tic.Errors
{
    public class ImcompatibleAncestorNamedNodeException : TicException
    {
        public string NodeName { get; }
        public ITicNodeState Ancestor { get; }
        public ITicNodeState Descendant { get; }

        public ImcompatibleAncestorNamedNodeException(string nodeName, ITicNodeState ancestor, ITicNodeState descendant) : base($"Incompatible ancestor {ancestor}=>{descendant} at node {nodeName}")
        {
            NodeName = nodeName;
            Ancestor = ancestor;
            Descendant = descendant;
        }
    }
}