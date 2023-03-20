using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;

namespace NFun.ParseErrors;

internal static partial class Errors {

    internal static FunnyParseException ImpossibleCast(FunnyType from, FunnyType to, Interval interval) => new(
        710, $"Unable to cast from {from} to {to}. Possible recursive type definition", interval);

    internal static FunnyParseException VariousIfElementTypes(IfThenElseSyntaxNode ifThenElse) {
        var allExpressions = ifThenElse.Ifs
                                       .Select(i => i.Expression)
                                       .Append(ifThenElse.ElseExpr)
                                       .ToArray();

        //Search first failed interval
        Interval failedInterval = ifThenElse.Interval;

        //Lca defined only in TI. It is kind of hack
        var hmTypes = allExpressions.Select(a => a.OutputType.ConvertToTicType()).ToArray();

        return new(
            713, $"'If-else expressions contains different type. " +
                 $"Specify toAny() cast if the result should be of 'any' type. " +
                 $"Actual types: {string.Join(",", hmTypes.Select(m => m.Description))}",
            failedInterval);
    }

    internal static FunnyParseException VariousArrayElementTypes(ArraySyntaxNode arraySyntaxNode) =>
        new(716, "'Various array element types. " +
                 $"{arraySyntaxNode.OutputType} = [{string.Join(",", arraySyntaxNode.Expressions.Select(e => e.OutputType))}]", arraySyntaxNode.Interval);

    internal static FunnyParseException TranslateTicError(TicException ticException, ISyntaxNode rootToSearch, GraphBuilder graph) {
        var allTicNodes = graph.GetNodes();
        return ticException switch {
                   TicIncompatibleAncestorSyntaxNodeException syntaxNodeEx => TranslateIncompatibleAncestorError(rootToSearch, syntaxNodeEx, allTicNodes),
                   TicCannotSetStateSyntaxNodeException stateException     => TranslateStateError(ticException, rootToSearch, stateException),
                   TicRecursiveTypeDefinitionException e                   => TranslateRecursiveTypeDefinitionError(rootToSearch, e, allTicNodes),
                   TicInvalidFunctionalVariableSignature signature         => TranlsateInvalidFunctionalVarError(rootToSearch, signature, allTicNodes),
                   TicNodeIsNotAFunctionalVariableException notFun         => TranslateIsNotAFunctionalVar(rootToSearch,notFun,allTicNodes),
                   _                                                       => GeneralTypeError(799, ticException, rootToSearch)
               };
    }

    private static FunnyParseException TranslateIncompatibleAncestorError(ISyntaxNode rootToSearch, TicIncompatibleAncestorSyntaxNodeException syntaxNodeEx, TicNode[] allTicNodes) {
        var ticAncestor = syntaxNodeEx.Ancestor;
        var ticDescendant = syntaxNodeEx.Descendant;

        var error = GetAncestorToDescendantErrorOrNull(rootToSearch, ticAncestor, ticDescendant);
        if (error != null)
            return error;

        var ancestor = FindConcreteTicNodeForGenericOrNull(ticAncestor, allTicNodes);
        var descendant = FindConcreteTicNodeForGenericOrNull(ticDescendant, allTicNodes);

        var error2 = GetAncestorToDescendantErrorOrNull(rootToSearch, ancestor, descendant);
        if (error2 != null)
            return error2;

        return new(719, "There's an error somewhere in the types used (but I can't figure out exactly where)", rootToSearch.Interval);
    }


    private static FunnyParseException TranslateStateError(TicException ticException, ISyntaxNode rootToSearch, TicCannotSetStateSyntaxNodeException stateException) {
        var path = rootToSearch.FindSyntaxNodePath(stateException.Node.Name);

        if (path.Count != 0)
        {
            var failed = path.Dequeue();
            if (path.TryPeek(out var parent))
            {
                if (parent is FunCallSyntaxNode functionCall)
                    return InvalidFunctionArgument(failed, functionCall, stateException.State, stateException.Node);

                if (parent is StructFieldAccessSyntaxNode f)
                {
                    if (failed is GenericIntSyntaxNode || failed is ConstantSyntaxNode)
                        return new(722, $"Invalid syntax. Element '{failed.ToShortText()}' has no fields. What did you mean?", f.Interval);
                    return new(725, $"Element '{failed.ToShortText()}' has no fields. What did you mean?", f.Interval);
                }
            }
        }

        return GeneralTypeError(798, ticException, rootToSearch);
    }

    private static FunnyParseException TranslateRecursiveTypeDefinitionError(ISyntaxNode rootToSearch, TicRecursiveTypeDefinitionException e, TicNode[] allTicNodes) {
        var cycle = new List<ISyntaxNode>();

        foreach (var ticNode in e.Nodes)
        {
            var path = rootToSearch.FindSyntaxNodePath(ticNode.Name).FirstOrDefault();
            if (path != null)
                cycle.Add(path);
            else
            {
                var referenced = FindConcreteTicNodeForGenericOrNull(ticNode, allTicNodes);
                path = rootToSearch.FindSyntaxNodePath(referenced?.Name).FirstOrDefault();
                if (path != null)
                    cycle.Add(path);
            }
        }

        if (cycle.IsEmpty())
            return new FunnyParseException(728, $"Recursive type definition", rootToSearch.Interval);
        else
        {
            var firstElement = cycle[0];
            if (cycle.Count == 1)
                return new(731, $"Recursive type definition of '{firstElement.ToShortText()}'", firstElement.Interval);
            else
                return new(734, $"Recursive type definition of '{firstElement.ToShortText()}'. Cycle: ${String.Join("->", cycle.Select(desc => desc.ToShortText()))}", firstElement.Interval);
        }
    }

    private static FunnyParseException TranlsateInvalidFunctionalVarError(ISyntaxNode rootToSearch, TicInvalidFunctionalVariableSignature signature, TicNode[] allTicNodes) {
        var syntaxNode = FindSyntaxNodeOrNull(rootToSearch, signature.FuncNode, allTicNodes);
        var interval = (syntaxNode ?? rootToSearch).Interval;
        var msg = signature.StateFun.Args.Count() switch {
                      0 => $"Invalid functional variable signature: cannot use function without arguments here",
                      1 => $"Invalid functional variable signature: cannot use function with 1 argument here",
                      _ => $"Invalid functional variable signature: cannot use function with {signature.StateFun.Args.Count()} args count here",
                  };
        return new(737, msg, interval);
    }

    private static FunnyParseException TranslateIsNotAFunctionalVar(ISyntaxNode rootToSearch, TicNodeIsNotAFunctionalVariableException e, TicNode[] allTicNodes) {
        var node = FindSyntaxNodeOrNull(rootToSearch, e.Node, allTicNodes);

        if (node == null)
            return new(738, $"Node is not a function or functional variable but it was called", rootToSearch.Interval);
        else
            return new(738, $"`{node.ToShortText()}` is not a function or functional variable", node.Interval);
    }

    private static ISyntaxNode FindSyntaxNodeOrNull(ISyntaxNode rootToSearch, TicNode node, TicNode[] allTicNodes) {
        var syntaxNode = rootToSearch.FindSyntaxNodePath(node.Name).FirstOrDefault();
        if (syntaxNode != null)
            return syntaxNode;

        var concrete = FindConcreteTicNodeForGenericOrNull(node, allTicNodes);
        if(concrete!=null)
            return rootToSearch.FindSyntaxNodePath(concrete.Name).FirstOrDefault();
        return null;
    }


    private static FunnyParseException GetAncestorToDescendantErrorOrNull(ISyntaxNode rootToSearch, TicNode ticAncestorOrNull, TicNode ticDescendantOrNull) {
        var ancestorPath = rootToSearch.FindSyntaxNodePath(ticAncestorOrNull?.Name);
        var descedantPath = rootToSearch.FindSyntaxNodePath(ticDescendantOrNull?.Name);

        var ancestor = ancestorPath.FirstOrDefault();
        var desc = descedantPath.FirstOrDefault();

        if (desc == null && ancestor == null)
            return null;

        if (desc != null && ancestor != null)
        {
            if (descedantPath.Contains(ancestor) && ancestor is EquationSyntaxNode eq)
            {
                var start = Math.Min(eq.Expression.Interval.Start, eq.Interval.Start);
                var finish = Math.Max(eq.Expression.Interval.Start, eq.Interval.Start);
                return new(740, $"Variable '{eq.Id}' cannot be initialized with type constrains '{GetDescription(ticDescendantOrNull)}' by expression '{desc.ToShortText()}'", start, finish);
            }

            return desc switch {
                       ConstantSyntaxNode c
                           => new(743, $"Constant '{c.Value}' cannot be used here due invalid type", desc.Interval),
                       GenericIntSyntaxNode gint
                           => new(746, $"Constant '{gint.Value}' cannot be used here due invalid type", desc.Interval),
                       TypedVarDefSyntaxNode tyvardef when ancestor is NamedIdSyntaxNode id && tyvardef.Id == id.Id
                           => new(749, $"Variable '{id.Id}' cannot be used here due invalid type", ancestor.Interval),
                       VarDefinitionSyntaxNode vardef when ancestor is NamedIdSyntaxNode id && vardef.Id == id.Id
                           => new(752, $"Variable '{id.Id}' cannot be used here due invalid type", ancestor.Interval),
                       NamedIdSyntaxNode nid when ancestor is NamedIdSyntaxNode id && nid.Id == id.Id
                           => new(752, $"Variable '{id.Id}' cannot be used here due invalid type", ancestor.Interval),
                       _ => new(755, $"Expression `{desc.ToShortText()}` cannot be used here due to type mismatch", ancestor.Interval)
                   };
        }

        if (ancestorPath.Count > 1 && ancestorPath.ElementAt(1) is FunCallSyntaxNode ancFunc)
            return new(758, $"'{ancestor.ToShortText()}' cannot be used as an argument of '{ancFunc.Id}'", ancestor.Interval);

        if (descedantPath.Count > 1 && descedantPath.ElementAt(1) is FunCallSyntaxNode descFunc)
            return InvalidFunctionArgument(desc, descFunc, ticAncestorOrNull?.State);

        return desc switch {
                   null => new(761, $"Seems like expression `{ancestor.ToShortText()}` cannot be used here", ancestor.Interval),
                   NamedIdSyntaxNode id => new(
                       763, $"'the type '{GetDescription(ticDescendantOrNull)}' of '{id}' is not suitable for use here. Do something about it! ",
                       id.Interval),
                   AnonymFunctionSyntaxNode => new(765, $"Rule signature `{desc.ToShortText()}` cannot be used here", desc.Interval),
                   FunCallSyntaxNode        => new(767, $"Seems like function `{desc.ToShortText()}` cannot be used here as its return type does not fit", desc.Interval),
                   ConstantSyntaxNode       => new(769, $"Seems like constant `{desc.ToShortText()}` cannot be used here", desc.Interval),
                   GenericIntSyntaxNode     => new(771, $"Seems like integer constant `{desc.ToShortText()}` cannot be used here", desc.Interval),
                   StructInitSyntaxNode     => new(773, $"Seems like struct `{desc.ToShortText()}` cannot be used here", desc.Interval),
                   ArraySyntaxNode          => new(775, $"Seems like array `{desc.ToShortText()}` cannot be used here", desc.Interval),
                   _                        => new(777, $"Seems like expression `{desc.ToShortText()}` cannot be used here", desc.Interval),
               };

    }
    private static TicNode FindConcreteTicNodeForGenericOrNull(TicNode node, TicNode[] allTicNodes) {
        if (node == null)
            return null;
        var nonrefOrigin = node.GetNonReferenceSafeOrNull();
        if (nonrefOrigin == null)
            return null;
        foreach (var ticNode in allTicNodes)
        {
            var nonReference = ticNode.GetNonReferenceSafeOrNull();
            if (nonReference == null)
            {
                //cycle appears
            }
            else if (nonReference == nonrefOrigin)
                return nonReference;
            else if (nonReference.State is ICompositeState st && st.Members.Contains(nonrefOrigin))
                return nonReference;
            else if (nonReference.State is StateRefTo refto && refto.Node == node)
                return nonReference;
        }

        return null;
    }

    private static FunnyParseException InvalidFunctionArgument(ISyntaxNode failed, FunCallSyntaxNode functionCall, ITicNodeState failedState = null, TicNode stateExceptionNode = null)
    {
        var argNum = functionCall.Args.IndexOf(failed);
        var argumentType = functionCall.FunctionSignature.ArgTypes[argNum];

        if (functionCall.IsOperator)
            return new(780,
                $"Invalid operator call argument: {Signature(functionCall.FunctionSignature)}. Expected: {argumentType}" +
                (stateExceptionNode == null ? "" : $", but was: {ToNFunString(stateExceptionNode.State)}"),
                failed.Interval);
        else
            return new(783,
                $"Invalid function call argument: {Signature(functionCall.FunctionSignature)}. Expected: {argumentType}" +
                (stateExceptionNode == null ? "" : $", but was: {ToNFunString(stateExceptionNode.State)}"),
                failed.Interval);
    }

    private static FunnyParseException GeneralTypeError(int id, TicException ticException, ISyntaxNode rootToSearch)
        => new(id, $"Types cannot be solved: {ticException.Message} ", rootToSearch.Interval);

    private static string GetDescription(TicNode node) {
        var concrete = node.GetNonReferenceSafeOrNull();
        return concrete == null
            ? "recursive type"
            : TicTypesConverter.Concrete.Convert(concrete.State).ToString();
    }

    private static string ToNFunString(ITicNodeState state)
    {
        var concrete = state.GetNonReferenceSafeOrNull();
        return TicTypesConverter.Concrete.Convert(concrete).ToString();
    }

    private static TicNode GetNonReferenceSafeOrNull(this  TicNode ticNode) {
        var result = ticNode;
        var visited = new HashSet<TicNode>();
        while (true)
        {
            if (result.State is StateRefTo r)
            {
                if (!visited.Add(result))
                    return null;
                result = r.Node;
            }
            else
                return result;
        }
    }

    private static ITicNodeState GetNonReferenceSafeOrNull(this  ITicNodeState state) {
        var result = state;
        var visited = new HashSet<ITicNodeState>();
        while (true)
        {
            if (result is StateRefTo r)
            {
                if (!visited.Add(result))
                    return null;
                result = r.Node.State;
            }
            else
                return result;
        }
    }
}
