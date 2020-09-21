using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Tic.Errors
{
    public class ImcompatibleAncestorSyntaxNodeException : TicException
    {
        public int SyntaxNodeId { get; }
        public ITicNodeState Ancestor { get; }
        public ITicNodeState Descendant { get; }

        public ImcompatibleAncestorSyntaxNodeException(int syntaxNodeId, ITicNodeState ancestor, ITicNodeState descendant): base($"Incompatible ancestor {ancestor}=>{descendant} at node {syntaxNodeId}")
        {
            SyntaxNodeId = syntaxNodeId;
            Ancestor = ancestor;
            Descendant = descendant;
        }
    }
}