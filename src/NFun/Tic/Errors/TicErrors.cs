using System;
using NFun.Tic.SolvingStates;
using System.Diagnostics;

namespace NFun.Tic.Errors; 

internal static class TicErrors {
    public static Exception IncompatibleNodes(TicNode ancestor, TicNode descendant) {
        TraceLog.WriteLine($"  TIC ERROR IncompatibleNodes: anc={ancestor.Name}:{ancestor.State} desc={descendant.Name}:{descendant.State}");
        TraceLog.WriteLine($"    Stack: {Environment.StackTrace}");
        return new TicIncompatibleAncestorSyntaxNodeException(ancestor, descendant);
    }

    public static Exception IncompatibleTypes(TicNode ancestor, TicNode descendant)
        => IncompatibleNodes(ancestor, descendant);

    public static Exception InvalidFunctionalVariableSignature(TicNode funcNode) 
        => new TicInvalidFunctionalVariableSignature(funcNode, funcNode.State as StateFun);

    public static Exception CannotMerge(TicNode a, TicNode b)
        => IncompatibleNodes(a, b);

    public static Exception RecursiveTypeDefinition(TicNode[] group) => 
        new TicRecursiveTypeDefinitionException(group);

    public static Exception CannotSetState(TicNode node, ITicNodeState b) => 
        new TicCannotSetStateSyntaxNodeException(node, b);
    
    public static Exception IsNotAFunctionalVariableOrFunction(TicNode node, ITicNodeState b) => 
        new TicNodeIsNotAFunctionalVariableException(node, b);
}