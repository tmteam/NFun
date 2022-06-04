using System;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Errors; 

internal static class TicErrors {
    public static Exception IncompatibleNodes(TicNode ancestor, TicNode descendant) 
        => new IncompatibleAncestorSyntaxNodeException(ancestor, descendant);

    public static Exception IncompatibleTypes(TicNode ancestor, TicNode descendant)
        => IncompatibleNodes(ancestor, descendant);

    public static Exception InvalidFunctionalVariableSignature(TicNode funcNode) 
        => new TicInvalidFunctionalVariableSignature(funcNode, funcNode.State as StateFun);

    public static Exception CannotMerge(TicNode a, TicNode b)
        => IncompatibleNodes(a, b);

    public static Exception RecursiveTypeDefinition(TicNode[] group) => 
        new RecursiveTypeDefinitionException(group);

    public static Exception CannotSetState(TicNode node, ITicNodeState b) => new CannotSetStateSyntaxNodeException(node, b);
}