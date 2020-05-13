using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Tic.Errors
{
    public class ImcompatibleAncestorSyntaxNodeException : TicException
    {
        public int SyntaxNodeId { get; }
        public IState Ancestor { get; }
        public IState Descendant { get; }

        public ImcompatibleAncestorSyntaxNodeException(int syntaxNodeId, IState ancestor, IState descendant): base($"Incompatible ancestor {ancestor}=>{descendant} at node {syntaxNodeId}")
        {
            SyntaxNodeId = syntaxNodeId;
            Ancestor = ancestor;
            Descendant = descendant;
        }
    }
}