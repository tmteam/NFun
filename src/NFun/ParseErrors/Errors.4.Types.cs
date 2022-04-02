using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;

namespace NFun.ParseErrors {

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
        var hmTypes = allExpressions.Select(a => a.OutputType.ConvertToTiType()).ToArray();

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
                   IncompatibleAncestorSyntaxNodeException syntaxNodeEx => TranslateIncompatibleAncestorError(rootToSearch, syntaxNodeEx, allTicNodes),
                   CannotSetStateSyntaxNodeException stateException     => TranslateStateError(ticException, rootToSearch, stateException),
                   RecursiveTypeDefinitionException e                   => TranslateRecursiveTypeDefinitionError(rootToSearch, e, allTicNodes),
                   TicInvalidFunctionalVarableSignature signature       => TranlsateInvalidFunctionalVarError(rootToSearch, signature, allTicNodes),
                   _                                                    => GeneralTypeError(799, ticException, rootToSearch)
               };
    }

    private static FunnyParseException TranslateIncompatibleAncestorError(ISyntaxNode rootToSearch, IncompatibleAncestorSyntaxNodeException syntaxNodeEx, TicNode[] allTicNodes) {
        var ticAncestor = syntaxNodeEx.Ancestor;
        var ticDescendant = syntaxNodeEx.Descendant;

        var error = GetAncestorToDescendantErrorOrNull(rootToSearch, ticAncestor, ticDescendant);
        if (error != null)
            return error;

        var ancestor = FindConcreteNodeForGenericOrNull(ticAncestor, allTicNodes);
        var descendant = FindConcreteNodeForGenericOrNull(ticDescendant, allTicNodes);

        var error2 = GetAncestorToDescendantErrorOrNull(rootToSearch, ancestor, descendant);
        if (error2 != null)
            return error2;

        return new(719, "There's an error somewhere in the types used (but I can't figure out exactly where)", rootToSearch.Interval);
    }


    private static FunnyParseException TranslateStateError(TicException ticException, ISyntaxNode rootToSearch, CannotSetStateSyntaxNodeException stateException) {
        var path = rootToSearch.FindNodePath(stateException.Node.Name);

        if (path.Count != 0)
        {
            var failed = path.Dequeue();
            if (path.TryPeek(out var parent))
            {
                if (parent is FunCallSyntaxNode functionCall)
                    return InvalidFunctionArgument(failed, functionCall, stateException.State);

                if (parent is StructFieldAccessSyntaxNode f)
                {
                    if (failed is GenericIntSyntaxNode || failed is ConstantSyntaxNode)
                        return new(722, $"Ivalid syntax. Element '{GetDescription(failed)}' has no fields. What did you mean?", f.Interval);
                    return new(725, $"Element '{GetDescription(failed)}' has no fields. What did you mean?", f.Interval);
                }
            }
        }

        return GeneralTypeError(798, ticException, rootToSearch);
    }

    private static FunnyParseException TranslateRecursiveTypeDefinitionError(ISyntaxNode rootToSearch, RecursiveTypeDefinitionException e, TicNode[] allTicNodes) {
        var cycle = new List<ISyntaxNode>();

        foreach (var ticNode in e.Nodes)
        {
            var path = rootToSearch.FindNodePath(ticNode.Name).FirstOrDefault();
            if (path != null)
                cycle.Add(path);
            else
            {
                var referenced = FindConcreteNodeForGenericOrNull(ticNode, allTicNodes);
                path = rootToSearch.FindNodePath(referenced?.Name).FirstOrDefault();
                if (path != null)
                    cycle.Add(path);
            }
        }

        if (cycle.IsEmpty())
            return new FunnyParseException(728, $"Recursive type definition", rootToSearch.Interval);
        else
        {
            var firstelement = cycle[0];
            if (cycle.Count == 1)
                return new(731, $"Recursive type definition of '{GetDescription(firstelement)}'", firstelement.Interval);
            else
                return new(734, $"Recursive type definition of '{GetDescription(firstelement)}'. Cycle: ${String.Join("->", cycle.Select(GetDescription))}", firstelement.Interval);
        }
    }

    private static FunnyParseException TranlsateInvalidFunctionalVarError(ISyntaxNode rootToSearch, TicInvalidFunctionalVarableSignature signature, TicNode[] allTicNodes) {
        var ticNode = FindConcreteNodeForGenericOrNull(signature.FuncNode, allTicNodes);
        var node = rootToSearch.FindNodePath(ticNode?.Name).FirstOrDefault();
        var interval = (node ?? rootToSearch).Interval;
        return new FunnyParseException(737, $"Invalid functional variable signature: cannot use function with {signature.StateFun.Args.Count()} args count here", interval);
    }



    private static FunnyParseException GetAncestorToDescendantErrorOrNull(ISyntaxNode rootToSearch, TicNode ticAncestorOrNull, TicNode ticDescendantOrNull) {
        var ancestorPath = rootToSearch.FindNodePath(ticAncestorOrNull?.Name);
        var descedantPath = rootToSearch.FindNodePath(ticDescendantOrNull?.Name);

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
                return new(740, $"Variable {eq.Id} cannot be initialized with type constrains '{GetDescription(ticDescendantOrNull)}' of expression '{GetDescription(desc)}'", start, finish);
            }

            return desc switch {
                       ConstantSyntaxNode c
                           => new(743, $"Constant '{c.Value}' cannot be used here due invalid type", desc.Interval),
                       GenericIntSyntaxNode gint
                           => new(746, $"Constant '{gint.Value}' cannot be used here due invalid type", desc.Interval),
                       TypedVarDefSyntaxNode tyvardef when ancestor is NamedIdSyntaxNode id && tyvardef.Id == id.Id
                           => new(749, $"Variable '{id.Id}' cannot be used here due invalid type", ancestor.Interval),
                       VarDefinitionSyntaxNode vardef when ancestor is NamedIdSyntaxNode idd && vardef.Id == idd.Id
                           => new(752, $"Variable '{idd.Id}' cannot be used here due invalid type", ancestor.Interval),
                       _ => new(755, $"Expression {GetDescription(desc)} cannot be used here due to type mismatch", ancestor.Interval)
                   };
        }

        if (ancestorPath.Count > 1 && ancestorPath.ElementAt(1) is FunCallSyntaxNode ancFunc)
            return new(758, $"'{GetDescription(ancestor)}' cannot be used as an argument of '{ancFunc.Id}'", ancestor.Interval);

        if (descedantPath.Count > 1 && descedantPath.ElementAt(1) is FunCallSyntaxNode descFunc)
            return InvalidFunctionArgument(desc, descFunc, ticAncestorOrNull?.State);

        return desc switch {
                   null => new(761, $"Seems like expression {GetDescription(ancestor)} cannot be used here", ancestor.Interval),
                   NamedIdSyntaxNode id => new(
                       763, $"'the type '{GetDescription(ticDescendantOrNull)}' of '{id}' is not suitable for use here. Do something about it! ",
                       id.Interval),
                   AnonymFunctionSyntaxNode => new(765, $"Rule signature {GetDescription(desc)} cannot be used here", desc.Interval),
                   FunCallSyntaxNode        => new(767, $"Seems like function {GetDescription(desc)} cannot be used here as its return type does not fit", desc.Interval),
                   ConstantSyntaxNode       => new(769, $"Seems like constant {GetDescription(desc)} cannot be used here", desc.Interval),
                   GenericIntSyntaxNode     => new(771, $"Seems like integer constant {GetDescription(desc)} cannot be used here", desc.Interval),
                   StructInitSyntaxNode     => new(773, $"Seems like struct {GetDescription(desc)} cannot be used here", desc.Interval),
                   ArraySyntaxNode          => new(775, $"Seems like array {GetDescription(desc)} cannot be used here", desc.Interval),
                   _                        => new(777, $"Seems like expression {GetDescription(desc)} cannot be used here", desc.Interval),
               };

    }
    private static TicNode FindConcreteNodeForGenericOrNull(TicNode node, TicNode[] allTicNodes) {
        foreach (var ticNode in allTicNodes)
        {
            var nonReference = ticNode.GetNonReferenceSafeOrNull();
            if (nonReference == null)
            {
                //cycle appears
            }
            else if (nonReference == node)
                return nonReference;
            else if (nonReference.State is ICompositeState st && st.Members.Contains(node))
                return nonReference;
            else if (nonReference.State is StateRefTo refto && refto.Node == node)
                return nonReference;
        }

        return null;
    }

    private static FunnyParseException InvalidFunctionArgument(ISyntaxNode failed, FunCallSyntaxNode functionCall, ITicNodeState failedState = null) {
        var argNum = functionCall.Args.IndexOf(failed);

        if (functionCall.IsOperator)
            return new(780, $"Invalid operator argument: {functionCall.Id}({string.Join(",", functionCall.FunctionSignature.ArgTypes)})", failed.Interval);
        else
            return new(783, "Invalid function argument: " +
                            $"{functionCall.Id}({string.Join(",", functionCall.FunctionSignature.ArgTypes)})->{functionCall.FunctionSignature.ReturnType}. " +
                            (failedState == null ? "" : $"Expected: {functionCall.FunctionSignature.ArgTypes[argNum]}, but was: {failedState}"),
                failed.Interval);
    }

    private static FunnyParseException GeneralTypeError(int id, TicException ticException, ISyntaxNode rootToSearch)
        => new(id, $"Types cannot be solved: {ticException.Message} ", rootToSearch.Interval);

    private static string GetDescription(ISyntaxNode desc) => desc.Accept(new ShortDescritpionVisitor());
    private static string GetDescription(TicNode node) {
        var concrete = node.GetNonReferenceSafeOrNull();
        return concrete == null
            ? "reqursive type"
            : TicTypesConverter.Concrete.Convert(concrete.State).ToString();
    }

}

}