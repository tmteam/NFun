using System.Linq;
using NFun.Exceptions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.Errors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;

namespace NFun.ParseErrors {

internal static partial class Errors {
    internal static FunnyParseException TypesNotSolved(ISyntaxNode syntaxNode) => new(
        710, $"Types cannot be solved ", syntaxNode.Interval);


    internal static FunnyParseException FunctionIsNotSolved(UserFunctionDefinitionSyntaxNode function) => new(
        713, $"Cannot calculate types for function '{function.Head}'. Check the expressions and/or add types to arguments/return", function.Interval);

    internal static FunnyParseException TranslateTicError(TicException ticException, ISyntaxNode syntaxNodeToSearch) {
        if (ticException is IncompatibleAncestorSyntaxNodeException syntaxNodeEx)
        {
            var concreteNode =
                SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, syntaxNodeEx.SyntaxNodeId);
            if (concreteNode != null)
                return new(
                    716, $"Types cannot be solved: {ticException.Message} ",
                    concreteNode.Interval);
        }
        else if (ticException is IncompatibleAncestorNamedNodeException namedNodeEx)
        {
            var concreteNode =
                SyntaxTreeDeepFieldSearch.FindVarDefinitionOrNull(syntaxNodeToSearch, namedNodeEx.NodeName);
            if (concreteNode != null)
                return new(
                    719, $"Types cannot be solved: {ticException.Message} ", concreteNode.Interval);
        }
        else if (ticException is RecursiveTypeDefinitionException e)
        {
            foreach (var nodeName in e.NodeNames)
            {
                var concreteNode =
                    SyntaxTreeDeepFieldSearch.FindVarDefinitionOrNull(syntaxNodeToSearch, nodeName);
                if (concreteNode != null)
                {
                    return new(
                        722, $"Recursive type definition: {string.Join("->", e.NodeNames)} ", concreteNode.Interval);
                }
            }

            foreach (var nodeId in e.NodeIds)
            {
                var concreteNode = SyntaxTreeDeepFieldSearch.FindNodeByOrderNumOrNull(syntaxNodeToSearch, nodeId);
                if (concreteNode != null)
                    return new(
                        725, $"Recursive type definition detected", concreteNode.Interval);
            }
        }

        return TypesNotSolved(syntaxNodeToSearch);
    }

    internal static FunnyParseException ImpossibleCast(FunnyType from, FunnyType to, Interval interval) => new(
        731, $"Unable to cast from {from} to {to}. Possible recursive type definition", interval);


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
            734, $"'If-else expressions contains different type. " +
                 $"Specify toAny() cast if the result should be of 'any' type. " +
                 $"Actual types: {string.Join(",", hmTypes.Select(m => m.Description))}",
            failedInterval);
    }

    internal static FunnyParseException VariousArrayElementTypes(ArraySyntaxNode arraySyntaxNode) =>
        new(
            737, $"'Various array element types. " +
                 $"{arraySyntaxNode.OutputType} = [{string.Join(",", arraySyntaxNode.Expressions.Select(e => e.OutputType))}]", arraySyntaxNode.Interval);


}

}