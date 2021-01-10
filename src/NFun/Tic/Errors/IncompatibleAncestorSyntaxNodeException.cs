using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors
{
    public class IncompatibleAncestorSyntaxNodeException : TicException
    {
        public int SyntaxNodeId { get; }
        public ITicNodeState Ancestor { get; }
        public ITicNodeState Descendant { get; }

        public IncompatibleAncestorSyntaxNodeException(int syntaxNodeId, ITicNodeState ancestor, ITicNodeState descendant)
            : base($"Incompatible ancestor {ancestor}=>{descendant} at node {syntaxNodeId}")
        {
            SyntaxNodeId = syntaxNodeId;
            Ancestor = ancestor;
            Descendant = descendant;
        }
    }
}