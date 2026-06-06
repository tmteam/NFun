using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Parses NFun user function definitions in both forms:
///   • <see cref="FromCall"/>      — `f(args) = body` (expression mode, also accepted in lang mode).
///   • <see cref="FromFunKeyword"/> — `fun f(args): block` (lang mode).
///
/// Shares argument layout normalisation, required-after-default validation,
/// duplicate-keyword-only detection, and default-value parsing across both forms.
/// </summary>
internal static class FunctionDefinitionParser {

    /// <summary>
    /// Build a <see cref="UserFunctionDefinitionSyntaxNode"/> from a FunCallSyntaxNode
    /// in head position. Reads optional `:retType` / `-> retType`, the `=` token, and
    /// the body expression. Used by both expression-mode and lang-mode parsers when
    /// the input shape is `f(x) = expr`.
    /// </summary>
    internal static UserFunctionDefinitionSyntaxNode FromCall(
        FunCallSyntaxNode fun, TokFlow flow, int exprStartPosition) {
        var id = fun.Id;
        if (fun.ParenthesesCount != 0)
            throw Errors.UnexpectedParenthesisOnFunDefinition(fun, exprStartPosition, flow.Previous.Finish);

        var arguments = new List<TypedVarDefSyntaxNode>();
        TypedVarDefSyntaxNode paramsArg = null;
        var keywordOnlyFromPositional = new List<TypedVarDefSyntaxNode>();

        // Positional args from fun.Args (required params + varargs)
        foreach (var headNodeChild in fun.Args)
        {
            TypedVarDefSyntaxNode arg;
            if (headNodeChild is TypedVarDefSyntaxNode varDef)
                arg = varDef;
            else if (headNodeChild is NamedIdSyntaxNode varSyntax)
                arg = SyntaxNodeFactory.TypedVar(
                    varSyntax.Id, TypeSyntax.Empty,
                    headNodeChild.Interval.Start, headNodeChild.Interval.Finish);
            else
                throw Errors.WrongFunctionArgumentDefinition(fun, headNodeChild);

            if (headNodeChild.ParenthesesCount != 0)
                throw Errors.FunctionArgumentDefinitionIsInParenthesis(fun, headNodeChild);

            // Defer params arg — it must be last, after defaults
            if (arg.IsParams)
            {
                if (paramsArg != null)
                    throw Errors.MultipleParams(fun);
                paramsArg = arg;
            }
            else if (paramsArg != null)
            {
                // Arg after ... → keyword-only (must have default)
                if (!arg.HasDefault)
                    throw Errors.KeywordOnlyWithoutDefault(fun, arg.Id, arg.Interval);
                keywordOnlyFromPositional.Add(new TypedVarDefSyntaxNode(
                    arg.Id, arg.TypeSyntax, arg.Interval, arg.DefaultValue, isKeywordOnly: true));
            }
            else
                arguments.Add(arg);
        }

        // Named args BEFORE spread → regular defaults
        var kwStart = fun.KeywordOnlyNamedStartIndex;
        for (int i = 0; i < Math.Min(kwStart, fun.NamedArgs.Length); i++)
        {
            var named = fun.NamedArgs[i];
            arguments.Add(new TypedVarDefSyntaxNode(
                named.Name, TypeSyntax.Empty, named.NameInterval,
                defaultValue: named.Value));
        }

        // Append params
        if (paramsArg != null)
            arguments.Add(paramsArg);

        // Named args AFTER spread → keyword-only (must have defaults)
        for (int i = kwStart; i < fun.NamedArgs.Length; i++)
        {
            var named = fun.NamedArgs[i];
            if (named.Value == null)
                throw Errors.KeywordOnlyWithoutDefault(fun, named.Name, named.NameInterval);
            arguments.Add(new TypedVarDefSyntaxNode(
                named.Name, TypeSyntax.Empty, named.NameInterval,
                defaultValue: named.Value, isKeywordOnly: true));
        }

        // Typed keyword-only args from positional list (e.g., f(...items, sep:text='-'))
        arguments.AddRange(keywordOnlyFromPositional);

        ValidateNoRequiredAfterDefault(arguments, fun);
        ValidateNoDuplicateKeywordOnly(arguments, fun);

        var outputType = TypeSyntax.Empty;
        if (flow.MoveIf(TokType.Colon, out _) || flow.MoveIf(TokType.Arrow, out _))
            outputType = flow.ReadTypeSyntax();

        flow.SkipNewLines();
        if (!flow.MoveIf(TokType.Def, out var def))
            throw Errors.FunDefTokenIsMissed(id, arguments, flow.Current);

        var expression = ExpressionParser.ReadNodeOrNull(flow);
        if (expression == null)
        {
            int finish = flow.Peek?.Finish ?? flow.CurrentTokenFinishPosition;
            throw Errors.FunExpressionIsMissed(id, arguments, new Interval(def.Start, finish));
        }

        return (UserFunctionDefinitionSyntaxNode)SyntaxNodeFactory.UserFunctionDef(arguments, fun, expression, outputType);
    }

    /// <summary>
    /// Parse a `fun name(args): block` definition. Caller positions cursor at the `fun` keyword.
    /// Supports the extension form `fun receiver.name(args):` and `fun receiver:Type.name(args):`.
    /// </summary>
    internal static ISyntaxNode FromFunKeyword(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'fun'

        if (!flow.MoveIf(TokType.Id, out var firstIdToken))
            throw new FunnyParseException(0, "Expected function name after 'fun'", flow.Current.Interval);

        // Extension function: `fun receiver.name(args)` or `fun receiver:Type.name(args)`.
        // The typed-receiver form pins the receiver's type like any other argument annotation.
        Tok nameToken;
        TypedVarDefSyntaxNode receiverArg = null;
        var receiverType = TypeSyntax.Empty;
        if (flow.IsCurrent(TokType.Colon)) {
            flow.MoveNext();
            receiverType = flow.ReadTypeSyntax();
        }
        if (flow.IsCurrent(TokType.Dot)) {
            flow.MoveNext(); // consume '.'
            if (!flow.MoveIf(TokType.Id, out nameToken))
                throw new FunnyParseException(0, "Expected function name after '.'", flow.Current.Interval);
            receiverArg = SyntaxNodeFactory.TypedVar(firstIdToken.Value, receiverType, firstIdToken.Start, firstIdToken.Finish);
        } else {
            if (receiverType is not TypeSyntax.EmptyType)
                throw new FunnyParseException(0,
                    "Receiver type annotation requires '.method' — `fun x:T.method()`",
                    flow.Current.Interval);
            nameToken = firstIdToken;
        }

        // Read argument list in parentheses
        var arguments = new List<TypedVarDefSyntaxNode>();
        if (receiverArg != null)
            arguments.Add(receiverArg);

        if (!flow.MoveIf(TokType.ParenthObr))
            throw new FunnyParseException(0,
                $"Expected '(' after function name '{nameToken.Value}'",
                flow.Current.Interval);

        if (!flow.IsCurrent(TokType.ParenthCbr)) {
            arguments.Add(ReadFunArg(flow));
            while (flow.MoveIf(TokType.Sep)) {
                arguments.Add(ReadFunArg(flow));
            }
        }

        if (!flow.MoveIf(TokType.ParenthCbr))
            throw new FunnyParseException(0, "Expected ')' after function arguments", flow.Current.Interval);

        // Normalise argument layout to match the classic-form's:
        // [positionals..., paramsArg, keyword-only...].
        // Args declared AFTER `...params` (must have defaults) become keyword-only.
        // Without this, TicSetupVisitor's binding loop computes paramsIndex at the
        // params arg's declared position — for `fun f(...xs, b=10)` paramsIndex=0,
        // and b at index 1 would silently overwrite positional[0]. (StmtBug76.)
        arguments = NormalizePostSpreadKeywordOnly(arguments, nameToken);

        ValidateNoRequiredAfterDefault(arguments, nameToken.Value);

        // Optionally read return type: `-> type`
        var outputType = TypeSyntax.Empty;
        if (flow.MoveIf(TokType.Arrow))
            outputType = flow.ReadTypeSyntax();

        // Body opens with `:`
        if (!flow.MoveIf(TokType.Colon))
            throw new FunnyParseException(0, "Expected ':' after function signature", flow.Current.Interval);

        // Single-line form: `fun f(): expr`  (Statements.md §Blocks — a block shape
        // can be a single expression). Body is the expression directly, not a
        // BlockExpressionNode — preserves implicit-return semantics and sidesteps
        // the "block-without-return ⇒ none" rule.
        ISyntaxNode body;
        if (!flow.IsCurrent(TokType.NewLine))
        {
            body = ExpressionParser.ReadNodeOrNull(flow)
                   ?? throw new FunnyParseException(0, "Expected function body after ':'", flow.Current.Interval);
        }
        else
        {
            flow.MoveNext(); // consume NewLine
            flow.SkipNewLines();
            body = StatementParser.ParseBlock(flow);
        }

        var argNodes = new ISyntaxNode[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
            argNodes[i] = arguments[i];

        var headInterval = new Interval(start, nameToken.Finish);
        // Mark extension definitions as IsPipeForward so dispatch routes them
        // through the extension namespace (Statements.md §Extension — callable only
        // via piped syntax). receiverArg != null is the structural signal that this
        // was the `fun x.method()` form.
        var head = new FunCallSyntaxNode(
            nameToken.Value, argNodes, headInterval,
            isPipeForward: receiverArg != null,
            isOperator: false);

        var funcNode = new UserFunctionDefinitionSyntaxNode(arguments, head, body, outputType);
        funcNode.Interval = new Interval(start, body.Interval.Finish);
        return funcNode;
    }

    /// <summary>
    /// Read a single argument from a `fun(args):` signature: optional `...` spread prefix,
    /// identifier, optional `:type`, optional `= default`.
    /// </summary>
    private static TypedVarDefSyntaxNode ReadFunArg(TokFlow flow) {
        // `...xs` varargs prefix — symmetry with short-form `f(...xs) = …`. Same
        // isParams flag; downstream FindFunctionDependenciesVisitor / Parser / TIC
        // honour it as before.
        int spreadStart = -1;
        bool isParams = false;
        if (flow.IsCurrent(TokType.Spread)) {
            spreadStart = flow.Current.Start;
            isParams = true;
            flow.MoveNext();
        }

        if (!flow.MoveIf(TokType.Id, out var argId))
            throw new FunnyParseException(0, "Expected argument name", flow.Current.Interval);

        var typeSyntax = TypeSyntax.Empty;
        if (flow.IsCurrent(TokType.Colon)) {
            flow.MoveNext();
            typeSyntax = flow.ReadTypeSyntax();
        }

        // Default value: `name = expr` or `name:type = expr`. Short-form `name(args) = expr`
        // accepts defaults via named-arg parsing; lang-mode `fun name(args):` must accept
        // the same shapes for symmetry.
        ISyntaxNode defaultValue = null;
        if (flow.IsCurrent(TokType.Def)) {
            flow.MoveNext(); // consume '='
            defaultValue = ExpressionParser.ReadNodeOrNull(flow)
                ?? throw new FunnyParseException(0,
                    $"Expected default value expression after '=' for argument '{argId.Value}'",
                    flow.Current.Interval);
        }

        return SyntaxNodeFactory.TypedVar(
            argId.Value, typeSyntax,
            spreadStart >= 0 ? spreadStart : argId.Start, argId.Finish,
            defaultValue, isParams: isParams);
    }

    /// <summary>
    /// Reorder a fun-form argument list to [positionals..., paramsArg, keyword-only...].
    /// Args declared AFTER `...params` (which must have defaults) are promoted to
    /// keyword-only so TicSetupVisitor's binding loop sees them in the same layout
    /// as the classic short-form parser.
    /// </summary>
    private static List<TypedVarDefSyntaxNode> NormalizePostSpreadKeywordOnly(
        List<TypedVarDefSyntaxNode> declared, Tok nameToken) {
        TypedVarDefSyntaxNode paramsArg = null;
        var regular = new List<TypedVarDefSyntaxNode>(declared.Count);
        var keywordOnly = new List<TypedVarDefSyntaxNode>();
        foreach (var arg in declared) {
            if (arg.IsParams) {
                if (paramsArg != null)
                    throw new FunnyParseException(0,
                        $"Multiple `...` params in '{nameToken.Value}'", arg.Interval);
                paramsArg = arg;
                continue;
            }
            if (paramsArg != null) {
                if (!arg.HasDefault)
                    throw new FunnyParseException(0,
                        $"Argument '{arg.Id}' after `...` must have a default value (keyword-only)",
                        arg.Interval);
                keywordOnly.Add(new TypedVarDefSyntaxNode(
                    arg.Id, arg.TypeSyntax, arg.Interval, arg.DefaultValue,
                    isParams: false, isKeywordOnly: true));
                continue;
            }
            regular.Add(arg);
        }
        if (paramsArg == null) return declared;
        var result = new List<TypedVarDefSyntaxNode>(regular.Count + 1 + keywordOnly.Count);
        result.AddRange(regular);
        result.Add(paramsArg);
        result.AddRange(keywordOnly);
        return result;
    }

    /// <summary>
    /// No required positional arg may follow a defaulted positional arg
    /// (params and keyword-only are separate sections and excluded).
    /// </summary>
    private static void ValidateNoRequiredAfterDefault(
        List<TypedVarDefSyntaxNode> arguments, FunCallSyntaxNode fun) {
        bool seenDefault = false;
        foreach (var arg in arguments)
        {
            if (arg.HasDefault) seenDefault = true;
            else if (arg.IsParams || arg.IsKeywordOnly) break;
            else if (seenDefault) throw Errors.RequiredArgAfterDefault(fun, arg);
        }
    }

    private static void ValidateNoRequiredAfterDefault(
        List<TypedVarDefSyntaxNode> arguments, string functionName) {
        bool seenDefault = false;
        foreach (var arg in arguments) {
            if (arg.IsParams || arg.IsKeywordOnly) break;
            if (arg.HasDefault) seenDefault = true;
            else if (seenDefault) throw Errors.RequiredArgAfterDefault(functionName, arg);
        }
    }

    /// <summary>
    /// No two keyword-only args may share a name (case-insensitive).
    /// </summary>
    private static void ValidateNoDuplicateKeywordOnly(
        List<TypedVarDefSyntaxNode> arguments, FunCallSyntaxNode fun) {
        for (int i = 0; i < arguments.Count; i++)
        {
            if (!arguments[i].IsKeywordOnly) continue;
            for (int j = 0; j < i; j++)
                if (string.Equals(arguments[i].Id, arguments[j].Id, StringComparison.OrdinalIgnoreCase))
                    throw Errors.DuplicateKeywordOnlyArg(fun, arguments[i].Id, arguments[i].Interval);
        }
    }
}
