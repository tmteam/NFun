using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Tic.Errors
{
    public class ImcompatibleAncestorNamedNodeException : TicException
    {
        public string NodeName { get; }
        public IState Ancestor { get; }
        public IState Descendant { get; }

        public ImcompatibleAncestorNamedNodeException(string nodeName, IState ancestor, IState descendant) : base($"Incompatible ancestor {ancestor}=>{descendant} at node {nodeName}")
        {
            NodeName = nodeName;
            Ancestor = ancestor;
            Descendant = descendant;
        }
    }
}