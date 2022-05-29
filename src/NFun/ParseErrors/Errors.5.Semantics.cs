using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.ParseErrors {

internal static partial class Errors {

    #region user function

// ReSharper disable PossibleMultipleEnumeration
    internal static FunnyParseException UnknownVariablesInUserFunction(IEnumerable<VariableExpressionNode> values) {
        if (values.Count() == 1)
            return new(810, $"Unknown variable \"{values.First()}\"", values.First().Interval);
        return new(
            813, $"Unknown variables \"{string.Join(", ", values)}\"", values.First().Interval);
    }

    internal static FunnyParseException UserFunctionWithSameNameAlreadyDeclared(UserFunctionDefinitionSyntaxNode userFun) => new(
        816, $"User function {Signature(userFun.Id, userFun.Args)} already declared with same name", userFun.Head.Interval.Start, userFun.Body.Interval.Finish);

    internal static FunnyParseException FunctionArgumentDuplicates(UserFunctionDefinitionSyntaxNode lexFunction, TypedVarDefSyntaxNode lexFunctionArg) => new(
        819, $"'Argument name '{lexFunctionArg.Id}' duplicates at  {Signature(lexFunction.Id, lexFunction.Args)} ", lexFunction.Head.Interval);

    internal static FunnyParseException ComplexRecursion(UserFunctionDefinitionSyntaxNode[] functionSolveOrder) => new(
        822, $"Complex recursion found: {string.Join("->", functionSolveOrder.Select(s => s.Id + "(..)"))} ", functionSolveOrder.First().Interval);

    #endregion


    #region structs

    internal static FunnyParseException FieldNotExists(string name, Interval interval) => new(
        828, $"Access to non exist field {name}", interval);

    internal static FunnyParseException FieldIsMissed(string name, Interval interval) => new(
        831, $"Field {name} is missed in struct", interval);

    internal static FunnyParseException EmptyStructsAreNotSupported(Interval interval) => new(
        834, $"Struct has to have at least one field", interval);

    #endregion


    #region rules

    internal static FunnyParseException CannotUseSuperAnonymousVariableHere(Interval interval) => new(
        840, "'it' variable can be used only as arguments in rules", interval);

    internal static FunnyParseException AnonymousFunctionArgumentDuplicates(FunArgumentExpressionNode argNode, ISyntaxNode funDefinition) => new(
        843, $"'Argument name '{argNode.Name}' of anonymous fun duplicates ", argNode.Interval);

    internal static FunnyParseException AnonymousFunctionArgumentConflictsWithOuterScope(string argName, Interval defInterval) => new(
        846, $"'Argument name '{argName}' of anonymous fun conflicts with outer scope variable. It is denied for your safety.", defInterval);

    #endregion


    #region misc

    internal static FunnyParseException UnexpectedExpression(ISyntaxNode lexNode) => new(
        852, $"Unexpected expression {ToShortText(lexNode)}", lexNode.Interval);

    internal static FunnyParseException OnlyOneAnonymousExpressionAllowed(int exprStart, ISyntaxNode lexNode, Tok flowCurrent) => new(
        855, $"Only one anonymous equation allowed", exprStart, flowCurrent.Finish);

    internal static FunnyParseException OutputNameWithDifferentCase(string id, Interval interval) => new(
        858, $"{id}<-  output name is same to name  {id}", interval);

    internal static FunnyParseException InputNameWithDifferentCase(string id, string actualName, Interval interval) => new(
        861, $"Input name '{id}' differs from the input name '{actualName}' only in case", interval);

    internal static FunnyParseException InterpolationExpressionIsMissing(ISyntaxNode lastNode) => new(
        864, $"Interpolation expression is missing{Nl} Example: 'before {{...}} after' ", lastNode.Interval);

    internal static FunnyParseException FunctionNameAndVariableNameConflict(VariableUsages usages) => new(
        867, $"Function with name: {usages.Source.Name} can not be used in expression because it's name conflict with function that exists in scope. Declare input variable",
        usages.Source.TypeSpecificationIntervalOrNull ?? usages.Usages.FirstOrDefault()?.Interval ?? Interval.Empty);

    internal static FunnyParseException FunctionNotFoundForHiOrderUsage(FunCallSyntaxNode node, IFunctionDictionary functions) {
        var candidates = functions.SearchAllFunctionsIgnoreCase(node.Id, node.Args.Length);
        var msg = new StringBuilder($"Function '{node.Id}({string.Join(",", node.Args.Select(_ => "_"))})' is not found. ");
        if (candidates.Any())
        {
            var candidate = candidates.First();
            msg.Append(
                $"\r\nDid you mean function '{TypeHelper.GetFunSignature(candidate.Name, candidate.ReturnType, candidate.ArgTypes)}' ?");
        }

        var interval = node.IsPipeForward 
            ? new Interval(node.Args[0].Interval.Finish, node.Args[0].Interval.Finish + node.Id.Length + 1) 
            : new Interval(node.Interval.Start, node.Interval.Start + node.Id.Length);
        
        return new(870, msg.ToString(), interval);
    }

    internal static FunnyParseException CannotUseOutputValueBeforeItIsDeclared(VariableUsages usages) {
        var interval = (usages.Usages.FirstOrDefault()?.Interval) ??
                       usages.Source.TypeSpecificationIntervalOrNull ?? Interval.Empty;

        return new(
            873, $"Cannot use output value '{usages.Source.Name}' before it is declared'", interval);
    }

    internal static FunnyParseException VariableIsDeclaredAfterUsing(VariableUsages usages) => new(
        876, $"Variable '{usages.Source.Name}' used before it is declared'", usages.Usages.First().Interval);

    internal static FunnyParseException VariableIsAlreadyDeclared(string nodeId, Interval nodeInterval) => new(
        879, $"Variable {nodeId} is already declared", nodeInterval);

    #endregion

}

}