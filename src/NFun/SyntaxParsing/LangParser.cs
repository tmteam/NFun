using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Statement-level parser for NFun-Lang v4 (indent-based language mode).
/// Handles for, while, when, break, continue, return, print, multiline if/elif/else, multiline try/catch/anyway.
/// Delegates expression parsing to SyntaxNodeReader.
/// </summary>
internal static class LangParser {
    public static SyntaxTree Parse(TokFlow flow) {
        var nodes = new List<ISyntaxNode>();
        int stmtCounter = 0;
        // In lang mode, NewLines outside brackets are statement terminators
        // and must stop binary-operator chains (BugHunt-stmt #66). Set the
        // flag for the whole parse so SyntaxNodeReader's chain readers honor it.
        flow.RespectNewLines = true;

        while (true) {
            SkipNewLines(flow);
            if (flow.IsDoneOrEof()) break;

            // Parse @Annotations before fun definitions
            if (flow.IsCurrent(TokType.MetaInfo)) {
                var attrs = ParseAttributes(flow);
                SkipNewLines(flow);
                if (flow.IsCurrent(TokType.Fun)) {
                    var funcNode = ParseFunctionDefinition(flow);
                    if (funcNode is UserFunctionDefinitionSyntaxNode ufd)
                        ufd.Attributes = attrs.ToArray();
                    nodes.Add(funcNode);
                } else {
                    // Attributes before non-function — ignore for now
                    var stmt = ParseStatement(flow);
                    nodes.Add(stmt);
                }
            }
            else if (flow.IsCurrent(TokType.Fun)) {
                nodes.Add(ParseFunctionDefinition(flow));
            }
            else if (flow.IsCurrent(TokType.TypeKeyword)) {
                nodes.Add(Parser.ParseTypeDeclaration(flow));
            }
            else {
                // Try to parse statement (equation or expression)
                var stmt = ParseStatement(flow);
                // Top-level `id:type` input declaration (Basics.md §Input variables
                // L166-175): `i:int\n y = i+1`. Statement mode is an extension of
                // expression mode (Statements.md L1-3) — wrap as VarDefinition
                // (matching expression-mode shape) instead of auto-wrapping as an
                // equation that would later trip ExpressionBuilderVisitor's
                // "not an expression" guard (BugHunt-stmt #61).
                if (stmt is TypedVarDefSyntaxNode typedDef) {
                    nodes.Add(SyntaxNodeFactory.VarDefinition(typedDef, System.Array.Empty<FunnyAttribute>()));
                    continue;
                }
                // Wrap bare expressions as synthetic equations for RuntimeBuilder compatibility.
                // Value-bearing expressions get the canonical `out` name (matches
                // expression mode, Basics.md §Outputs); pure statements (for/while/
                // top-level if/when without else) get an internal `__stmt_N__` name
                // that is later treated as non-output (BugHunt-stmt #23/#24).
                if (stmt is not EquationSyntaxNode && stmt is not UserFunctionDefinitionSyntaxNode) {
                    bool isValueBearing = IsValueBearingStatement(stmt);
                    var equationId = isValueBearing
                        ? Parser.AnonymousEquationId
                        : $"__stmt_{stmtCounter++}__";
                    var eq = SyntaxNodeFactory.Equation(
                        equationId, stmt, stmt.Interval.Start,
                        System.Array.Empty<FunnyAttribute>());
                    eq.IsAutoWrapped = true;
                    stmt = eq;
                }
                nodes.Add(stmt);
            }
            RequireStatementTerminator(flow);
        }

        var tree = new SyntaxTree(nodes.ToArray());
        // Context-sensitive validation: `return` only inside functions,
        // `break`/`continue` only inside loops.
        LangContextValidator.Validate(tree);
        return tree;
    }

    // Statements that have no value at the top level — they're side-effect-only
    // constructs and shouldn't surface as outputs. Anything else (literal, call,
    // identifier, binary op, ternary if-expr, struct init, lambda, …) carries a
    // value and gets bound to the canonical `out` name.
    private static bool IsValueBearingStatement(ISyntaxNode node) => node switch {
        ForSyntaxNode => false,
        WhileSyntaxNode => false,
        IfBlockSyntaxNode => false,
        // Multi-line `if cond: ...` without an explicit `else` is parsed as
        // IfThenElseSyntaxNode with an auto-inserted DefaultValueSyntaxNode
        // else. It's the statement form (no value), so route it through
        // __stmt_N__ instead of clobbering `out` (BugHunt-stmt #43).
        // Check the IsAutoInsertedElse flag — a user-written `else default`
        // is a real expression that DOES bear a value. (MR11Bug2.)
        IfThenElseSyntaxNode ite
            when ite.ElseExpr is DefaultValueSyntaxNode { IsAutoInsertedElse: true } => false,
        WhenSyntaxNode w when w.ElseBody == null => false,
        TryBlockSyntaxNode => false,
        FieldAssignmentSyntaxNode => false,
        PrintSyntaxNode => false,
        // `print(args)` parses as a FunCallSyntaxNode because the lang-mode
        // print-statement form only fires when print is NOT followed by '('.
        // Either form is fire-and-forget — don't clobber `out` (BugHunt-stmt #72).
        FunCallSyntaxNode fcn when fcn.Id == "print" => false,
        ReturnSyntaxNode => false,
        BreakSyntaxNode => false,
        ContinueSyntaxNode => false,
        _ => true,
    };

    private static ISyntaxNode ParseFunctionDefinition(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'fun'

        if (!flow.MoveIf(TokType.Id, out var firstIdToken))
            throw new FunnyParseException(0, "Expected function name after 'fun'", flow.Current.Interval);

        // Extension function syntax: `fun receiver.name(args)` or
        // `fun receiver:Type.name(args)` (Statements.md §Extension). The
        // typed-receiver form lets the user pin the receiver's type just like
        // any other argument annotation. Namespace separation is controlled
        // by a dialect flag at TIC/dispatch level.
        Tok nameToken;
        TypedVarDefSyntaxNode receiverArg = null;
        var receiverType = TypeSyntax.Empty;
        if (flow.IsCurrent(TokType.Colon)) {
            flow.MoveNext(); // consume ':'
            receiverType = flow.ReadTypeSyntax();
        }
        if (flow.IsCurrent(TokType.Dot)) {
            flow.MoveNext(); // consume '.'
            if (!flow.MoveIf(TokType.Id, out nameToken))
                throw new FunnyParseException(0, "Expected function name after '.'", flow.Current.Interval);
            receiverArg = SyntaxNodeFactory.TypedVar(firstIdToken.Value, receiverType, firstIdToken.Start, firstIdToken.Finish);
        } else {
            if (receiverType is not TypeSyntax.EmptyType)
                throw new FunnyParseException(0, "Receiver type annotation requires '.method' — `fun x:T.method()`", flow.Current.Interval);
            nameToken = firstIdToken;
        }

        // Read argument list in parentheses
        var arguments = new List<TypedVarDefSyntaxNode>();
        if (receiverArg != null)
            arguments.Add(receiverArg);

        if (!flow.MoveIf(TokType.ParenthObr))
            throw new FunnyParseException(0, $"Expected '(' after function name '{nameToken.Value}'", flow.Current.Interval);

        if (!flow.IsCurrent(TokType.ParenthCbr)) {
            // Read first arg
            arguments.Add(ReadFunArg(flow));
            while (flow.MoveIf(TokType.Sep)) {
                arguments.Add(ReadFunArg(flow));
            }
        }

        if (!flow.MoveIf(TokType.ParenthCbr))
            throw new FunnyParseException(0, "Expected ')' after function arguments", flow.Current.Interval);

        // Normalize argument layout to match the classic-form's:
        // [positionals..., paramsArg, keyword-only...].
        // Args declared AFTER `...params` (must have defaults) become
        // keyword-only. Without this, TicSetupVisitor's binding loop
        // (line 241-end) computes paramsIndex at the params arg's declared
        // position — for `fun f(...xs, b=10)` paramsIndex=0, b at index 1.
        // A call `f(1,2,3)` then assigns result[0]=1, then overwrites it
        // with extraArgs=[2,3] for the params slot. `1` is silently dropped.
        // (StmtBug76.)
        arguments = NormalizePostSpreadKeywordOnly(arguments, nameToken);

        // Validate: no required positional after defaults (mirrors Parser.cs L198-209).
        // Params and keyword-only legitimately follow defaults — stop at the first.
        bool seenDefault = false;
        foreach (var arg in arguments) {
            if (arg.IsParams || arg.IsKeywordOnly) break;
            if (arg.HasDefault) seenDefault = true;
            else if (seenDefault) throw Errors.RequiredArgAfterDefault(nameToken.Value, arg);
        }

        // Optionally read return type: -> type
        var outputType = TypeSyntax.Empty;
        if (flow.MoveIf(TokType.Arrow))
            outputType = flow.ReadTypeSyntax();

        // Expect colon
        if (!flow.MoveIf(TokType.Colon))
            throw new FunnyParseException(0, "Expected ':' after function signature", flow.Current.Interval);

        // Single-line form: `fun f(): expr`  (Statements.md §Blocks — a block
        // shape can be a single expression). Body is the expression directly,
        // not a BlockExpressionNode — preserves implicit-return semantics and
        // sidesteps the #11 "block-without-return ⇒ none" rule.
        ISyntaxNode body;
        if (!flow.IsCurrent(TokType.NewLine))
        {
            body = SyntaxNodeReader.ReadNodeOrNull(flow)
                   ?? throw new FunnyParseException(0, "Expected function body after ':'", flow.Current.Interval);
        }
        else
        {
            flow.MoveNext(); // consume NewLine
            SkipNewLines(flow); // allow extra blank lines
            body = ParseBlock(flow);
        }

        // Create a FunCallSyntaxNode as the "Head" (reusing existing UserFunctionDefinitionSyntaxNode pattern)
        // The Head is a FunCallSyntaxNode with the function name and arg nodes
        var argNodes = new ISyntaxNode[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
            argNodes[i] = arguments[i];

        var headInterval = new Interval(start, nameToken.Finish);
        // Mark extension definitions as IsPipeForward so dispatch routes them
        // through the extension namespace (Spec §Extension — callable only via
        // piped syntax). receiverArg != null is the structural signal that this
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
    /// Parse named type declaration: type name = {field defs} or type name = alias
    /// Same logic as Parser.ReadTypeDeclaration but static for LangParser.
    /// </summary>
    // Delegates to shared Parser.ParseTypeDeclaration

    /// <summary>
    /// Mirror of <c>Parser.cs</c> classic-form argument layout: regular positional
    /// args first, then the params (`...xs`), then keyword-only args (anything
    /// declared AFTER params with a default). Without this normalisation, the
    /// fun-form parser left args in declaration order — the binding loop in
    /// <c>TicSetupVisitor</c> then computed <c>paramsIndex</c> from the original
    /// declaration position, which for <c>fun f(...xs, b=10)</c> is 0 — causing
    /// the first positional argument of every call to be overwritten by the
    /// params-array expansion. (StmtBug76.)
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
                // Args after `...params` must have defaults and become keyword-only.
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
        // Layout: [regular..., params, keyword-only...]
        var result = new List<TypedVarDefSyntaxNode>(regular.Count + 1 + keywordOnly.Count);
        result.AddRange(regular);
        result.Add(paramsArg);
        result.AddRange(keywordOnly);
        return result;
    }

    private static TypedVarDefSyntaxNode ReadFunArg(TokFlow flow) {
        // `...xs` varargs prefix — symmetry with short-form `f(...xs) = …`
        // (BugHunt-stmt #45). Same isParams flag is set; downstream
        // FindFunctionDependenciesVisitor/Parser/TIC honor it as before.
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
        // the same shapes for symmetry (BugHunt-stmt #39).
        ISyntaxNode defaultValue = null;
        if (flow.IsCurrent(TokType.Def)) {
            flow.MoveNext(); // consume '='
            defaultValue = SyntaxNodeReader.ReadNodeOrNull(flow)
                ?? throw new FunnyParseException(0,
                    $"Expected default value expression after '=' for argument '{argId.Value}'",
                    flow.Current.Interval);
        }

        return SyntaxNodeFactory.TypedVar(
            argId.Value, typeSyntax,
            spreadStart >= 0 ? spreadStart : argId.Start, argId.Finish,
            defaultValue, isParams: isParams);
    }

    internal static BlockSyntaxNode ParseBlock(TokFlow flow) {
        var start = flow.CurrentTokenStartPosition;

        if (!flow.MoveIf(TokType.Indent))
            throw Errors.IndentExpected(flow.Current);

        var statements = new List<ISyntaxNode>();
        while (!flow.IsCurrent(TokType.Dedent) && !flow.IsDoneOrEof()) {
            SkipNewLines(flow);
            if (flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()) break;
            var stmt = ParseStatement(flow);
            if (stmt != null)
                statements.Add(stmt);
            // Statement must be followed by a separator (newline / `;` / Dedent)
            // before the next one. (StmtBug73.)
            RequireStatementTerminator(flow, TokType.Dedent);
            // Consume trailing newlines after statement
            SkipNewLines(flow);
        }

        var finish = flow.CurrentTokenFinishPosition;
        if (flow.IsCurrent(TokType.Dedent))
            flow.MoveNext(); // consume Dedent

        if (statements.Count == 1)
            return SyntaxNodeFactory.Block(statements, new Interval(start, finish));
        return SyntaxNodeFactory.Block(statements, new Interval(start, finish));
    }

    internal static ISyntaxNode ParseStatement(TokFlow flow) {
        SkipNewLines(flow);

        // For loop
        if (flow.IsCurrent(TokType.For))
            return ParseForLoop(flow);

        // While loop
        if (flow.IsCurrent(TokType.While))
            return ParseWhileLoop(flow);

        // When (pattern matching)
        if (flow.IsCurrent(TokType.When))
            return ParseWhen(flow);

        // Break
        if (flow.IsCurrent(TokType.Break)) {
            var start = flow.Current.Start;
            flow.MoveNext();
            return SyntaxNodeFactory.Break(new Interval(start, flow.CurrentTokenFinishPosition));
        }

        // Continue
        if (flow.IsCurrent(TokType.Continue)) {
            var start = flow.Current.Start;
            flow.MoveNext();
            return SyntaxNodeFactory.Continue(new Interval(start, flow.CurrentTokenFinishPosition));
        }

        // Return statement
        if (flow.IsCurrent(TokType.Return))
            return ParseReturn(flow);

        // Print (contextual keyword — 'print' is still an identifier for function calls like print(42))
        if (flow.IsCurrent(TokType.Id) && flow.Current.Value == "print" && !IsFollowedByParenthesis(flow))
            return ParsePrint(flow);

        // Multi-line if/elif/else block
        if (flow.IsCurrent(TokType.If) && IsColonStyleIf(flow))
            return ParseIfBlock(flow);

        // Multiline try
        if (flow.IsCurrent(TokType.Try) && IsMultiLineTry(flow))
            return ParseTryBlock(flow);

        // Read expression
        var stmtStart = flow.Current.Start;
        var node = SyntaxNodeReader.ReadNodeOrNull(flow);
        if (node == null)
            throw new FunnyParseException(0, "Expected statement", flow.Current.Interval);

        // Expression-style user function definition: `f(args) = body`
        // (also accepts `f(args):retType = body` / `f(args) -> retType = body`).
        // Delegates to the shared parser used by expression mode for full feature parity
        // (typed args, defaults, params, keyword-only, return type annotation).
        if (node is FunCallSyntaxNode funCall && !funCall.IsOperator
            && (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon) || flow.IsCurrent(TokType.Arrow))) {
            return Parser.ParseUserFunctionFromCall(funCall, flow, stmtStart);
        }

        // Check if it's an assignment: expr followed by `=`
        if (flow.IsCurrent(TokType.Def)) {
            // Field assignment: s.field = expr → s = FieldAssignment(s, "field", expr)
            if (node is StructFieldAccessSyntaxNode fieldAccess) {
                var (varName, fieldName) = ExtractFieldTarget(fieldAccess);
                flow.MoveNext(); // consume '='
                var valueExpr = ReadExpressionOrIfBlock(flow);
                if (valueExpr == null)
                    throw new FunnyParseException(0, $"Expected expression after '=' in field assignment to '{varName}.{fieldName}'", flow.Current.Interval);
                var source = new NamedIdSyntaxNode(varName, fieldAccess.Source.Interval);
                var fieldAssign = SyntaxNodeFactory.FieldAssignment(varName, fieldName, source, valueExpr,
                    new Interval(fieldAccess.Interval.Start, valueExpr.Interval.Finish));
                // Wrap as equation: s = <field-assign-expr>
                return SyntaxNodeFactory.Equation(varName, fieldAssign, fieldAccess.Interval.Start, Array.Empty<FunnyAttribute>());
            }

            if (node is NamedIdSyntaxNode named) {
                flow.MoveNext(); // consume '='
                var body = ReadExpressionOrIfBlock(flow);
                if (body == null)
                    throw new FunnyParseException(0, $"Expected expression after '=' in assignment to '{named.Id}'", flow.Current.Interval);
                return SyntaxNodeFactory.Equation(named.Id, body, named.Interval.Start, Array.Empty<FunnyAttribute>());
            }

            if (node is TypedVarDefSyntaxNode typed) {
                flow.MoveNext(); // consume '='
                var body = ReadExpressionOrIfBlock(flow);
                if (body == null)
                    throw new FunnyParseException(0, $"Expected expression after '=' in assignment to '{typed.Id}'", flow.Current.Interval);
                var eq = SyntaxNodeFactory.Equation(typed.Id, body, typed.Interval.Start, Array.Empty<FunnyAttribute>());
                eq.TypeSpecificationOrNull = typed;
                return eq;
            }

            throw new FunnyParseException(0, "Left side of '=' must be an identifier", node.Interval);
        }

        // Check for compound assignment: +=, -=, *=, /=, %=, //=
        var compoundOp = TryGetCompoundOperator(flow);
        if (compoundOp != null) {
            // Field compound assignment: s.field += expr → s = FieldAssignment(s, "field", s.field + expr)
            if (node is StructFieldAccessSyntaxNode fieldAccess) {
                var (varName, fieldName) = ExtractFieldTarget(fieldAccess);
                flow.MoveNext(); // consume compound operator
                var rhs = ReadExpressionOrIfBlock(flow);
                if (rhs == null)
                    throw new FunnyParseException(0, $"Expected expression after compound assignment to '{varName}.{fieldName}'", flow.Current.Interval);
                // Desugar: s.a += expr → s.a = s.a + expr → s = FieldAssignment(s, "a", s.a + expr)
                var readSource = new NamedIdSyntaxNode(varName, fieldAccess.Source.Interval);
                var fieldRead = new StructFieldAccessSyntaxNode(readSource, fieldName,
                    fieldAccess.Interval);
                var binOp = SyntaxNodeFactory.BinOperatorCall(compoundOp, fieldRead, rhs);
                var source = new NamedIdSyntaxNode(varName, fieldAccess.Source.Interval);
                var fieldAssign = SyntaxNodeFactory.FieldAssignment(varName, fieldName, source, binOp,
                    new Interval(fieldAccess.Interval.Start, rhs.Interval.Finish));
                return SyntaxNodeFactory.Equation(varName, fieldAssign, fieldAccess.Interval.Start, Array.Empty<FunnyAttribute>());
            }

            if (node is NamedIdSyntaxNode named) {
                flow.MoveNext(); // consume compound operator
                var rhs = ReadExpressionOrIfBlock(flow);
                if (rhs == null)
                    throw new FunnyParseException(0, $"Expected expression after compound assignment to '{named.Id}'", flow.Current.Interval);
                // Desugar: x += expr → x = x + expr
                var varRef = new NamedIdSyntaxNode(named.Id, named.Interval);
                var binOp = SyntaxNodeFactory.BinOperatorCall(compoundOp, varRef, rhs);
                return SyntaxNodeFactory.Equation(named.Id, binOp, named.Interval.Start, Array.Empty<FunnyAttribute>());
            }
            throw new FunnyParseException(0, "Left side of compound assignment must be an identifier", node.Interval);
        }

        return node;
    }

    /// <summary>
    /// for iter in collection:
    ///     body
    /// </summary>
    private static ISyntaxNode ParseForLoop(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'for'

        if (!flow.MoveIf(TokType.Id, out var iteratorTok))
            throw Errors.ForIteratorNameExpected(flow.Current);

        if (!flow.MoveIf(TokType.In))
            throw Errors.ForInKeywordExpected(flow.Current);

        // Read collection expression with SuppressTypeAnnotation to prevent ':' from being parsed as type
        var savedSuppress = flow.SuppressTypeAnnotation;
        flow.SuppressTypeAnnotation = true;
        var collection = SyntaxNodeReader.ReadNodeOrNull(flow);
        flow.SuppressTypeAnnotation = savedSuppress;

        if (collection == null)
            throw Errors.ForCollectionExpected(flow.Current);

        if (!flow.MoveIf(TokType.Colon))
            throw Errors.ColonExpectedAfterStatement(flow.Current, "for");

        ISyntaxNode body;
        if (flow.IsCurrent(TokType.NewLine)) {
            SkipNewLines(flow);
            body = ParseBlock(flow);
        } else {
            // Single-line form: for x in arr: statement
            body = ParseSingleLineBody(flow);
        }

        return SyntaxNodeFactory.For(
            iteratorTok.Value, collection, body,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// while condition:
    ///     body
    /// </summary>
    private static ISyntaxNode ParseWhileLoop(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'while'

        var savedSuppress = flow.SuppressTypeAnnotation;
        flow.SuppressTypeAnnotation = true;
        var condition = SyntaxNodeReader.ReadNodeOrNull(flow);
        flow.SuppressTypeAnnotation = savedSuppress;

        if (condition == null)
            throw Errors.WhileConditionExpected(flow.Current);

        if (!flow.MoveIf(TokType.Colon))
            throw Errors.ColonExpectedAfterStatement(flow.Current, "while");

        ISyntaxNode body;
        if (flow.IsCurrent(TokType.NewLine)) {
            SkipNewLines(flow);
            body = ParseBlock(flow);
        } else {
            // Single-line form: while cond: statement
            body = ParseSingleLineBody(flow);
        }

        return SyntaxNodeFactory.While(
            condition, body,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// when subject:
    ///     value1: body1
    ///     value2: body2
    ///     else: elseBody
    /// OR condition-based (no subject):
    /// when:
    ///     cond1: body1
    ///     cond2: body2
    ///     else: elseBody
    /// </summary>
    private static ISyntaxNode ParseWhen(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'when'

        ISyntaxNode subject = null;

        // Check if condition-based (when:) or value-based (when subject:)
        if (!flow.IsCurrent(TokType.Colon)) {
            var savedSuppress = flow.SuppressTypeAnnotation;
            flow.SuppressTypeAnnotation = true;
            subject = SyntaxNodeReader.ReadNodeOrNull(flow);
            flow.SuppressTypeAnnotation = savedSuppress;
        }

        // Colon after subject is optional: both `when x:` and `when x` (followed by newline) are valid
        flow.MoveIf(TokType.Colon); // consume optional colon

        SkipNewLines(flow);

        var arms = new List<WhenArmSyntaxNode>();
        ISyntaxNode elseBody = null;

        bool isIndented = flow.MoveIf(TokType.Indent);

        while (true) {
            SkipNewLines(flow);

            // End conditions
            if (isIndented && (flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()))
                break;
            if (!isIndented && (flow.IsCurrent(TokType.NewLine) || flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()))
                break;

            var armStart = flow.CurrentTokenStartPosition;

            // Check for 'else' arm
            if (flow.IsCurrent(TokType.Else)) {
                flow.MoveNext(); // consume 'else'
                if (!flow.MoveIf(TokType.Colon))
                    throw Errors.ColonExpectedAfterStatement(flow.Current, "else");
                SkipNewLines(flow);
                // Else body: if next is Indent, it's a block; otherwise single expression
                if (flow.IsCurrent(TokType.Indent))
                    elseBody = ParseBlock(flow);
                else
                    elseBody = SyntaxNodeReader.ReadNodeOrNull(flow);
                break;
            }

            // Read arm condition/value
            var savedSuppress = flow.SuppressTypeAnnotation;
            flow.SuppressTypeAnnotation = true;
            var armCondition = SyntaxNodeReader.ReadNodeOrNull(flow);
            flow.SuppressTypeAnnotation = savedSuppress;

            if (armCondition == null)
                throw Errors.WhenArmConditionExpected(flow.Current);

            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "when arm");

            SkipNewLines(flow);
            // Arm body: if next is Indent, it's a block; otherwise a single
            // statement (assignment, expression, return, etc.) — same shape that
            // single-line `if c: stmt` accepts. Earlier this used the expression-only
            // reader and rejected `1: result = 100` (BugHunt-stmt #40).
            ISyntaxNode armBody;
            if (flow.IsCurrent(TokType.Indent))
                armBody = ParseBlock(flow);
            else
                armBody = ParseSingleLineBody(flow);

            if (armBody == null)
                throw Errors.WhenArmBodyExpected(flow.Current);

            arms.Add(SyntaxNodeFactory.WhenArm(
                armCondition, armBody, armStart, flow.CurrentTokenFinishPosition));
            SkipNewLines(flow);
        }

        if (isIndented && !flow.MoveIf(TokType.Dedent)) {
            if (!flow.IsDoneOrEof())
                throw Errors.DedentExpected(flow.Current);
        }

        // Spec layout: `else:` may appear AFTER the dedent when the when's
        // arms sit on a deeper indent than the originating `when` keyword's
        // continuation column (see IndentTokenizer continuation-keyword rule).
        // Mirror the try/catch handling: after consuming the arm block's
        // dedent, look one more time for the else continuation.
        SkipNewLines(flow);
        if (elseBody == null && flow.IsCurrent(TokType.Else)) {
            flow.MoveNext(); // consume 'else'
            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "else");
            SkipNewLines(flow);
            if (flow.IsCurrent(TokType.Indent))
                elseBody = ParseBlock(flow);
            else
                elseBody = SyntaxNodeReader.ReadNodeOrNull(flow);
        }

        return SyntaxNodeFactory.When(
            subject, arms.ToArray(), elseBody,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// return [expression]
    /// </summary>
    private static ISyntaxNode ParseReturn(TokFlow flow) {
        var retStart = flow.Current.Start;
        flow.MoveNext(); // consume 'return'

        // return with no expression (end of block)
        if (flow.IsCurrent(TokType.NewLine) || flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()) {
            return SyntaxNodeFactory.Return(null, new Interval(retStart, flow.CurrentTokenFinishPosition));
        }

        var expr = ReadExpressionOrIfBlock(flow);
        if (expr == null)
            throw new FunnyParseException(0, "Expected expression after 'return'", flow.Current.Interval);
        return SyntaxNodeFactory.Return(expr, new Interval(retStart, expr.Interval.Finish));
    }

    /// <summary>
    /// Check if the current token is followed by '(' (lookahead).
    /// Used to distinguish statement-form `print expr` from function-call `print(expr)`.
    /// </summary>
    private static bool IsFollowedByParenthesis(TokFlow flow) {
        var saved = flow.CurrentTokenPosition;
        flow.MoveNext();
        var result = flow.IsCurrent(TokType.ParenthObr);
        flow.Move(saved);
        return result;
    }

    /// <summary>
    /// print expression
    /// </summary>
    private static ISyntaxNode ParsePrint(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'print'

        var expr = SyntaxNodeReader.ReadNodeOrNull(flow);
        if (expr == null)
            throw Errors.PrintExpressionExpected(flow.Current);

        return SyntaxNodeFactory.Print(expr,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// Read either a multi-line if block (if current is `if` + multi-line pattern)
    /// or a regular expression via SyntaxNodeReader.
    /// Used on the RHS of assignments so `result = if cond: ...` works.
    /// </summary>
    private static ISyntaxNode ReadExpressionOrIfBlock(TokFlow flow) {
        if (flow.IsCurrent(TokType.If) && IsColonStyleIf(flow))
            return ParseIfBlock(flow);
        if (flow.IsCurrent(TokType.When))
            return ParseWhen(flow);
        if (flow.IsCurrent(TokType.Try) && IsMultiLineTry(flow))
            return ParseTryBlock(flow);
        return SyntaxNodeReader.ReadNodeOrNull(flow);
    }

    /// <summary>
    /// Check whether current If token uses colon-style syntax (lang mode):
    /// `if expr:` (multi-line or single-line). Returns true if `if expr Colon` pattern found.
    /// Does NOT consume any tokens.
    /// </summary>
    private static bool IsColonStyleIf(TokFlow flow) {
        int saved = flow.CurrentTokenPosition;
        flow.MoveNext(); // skip If

        int depth = 0;
        while (!flow.IsDoneOrEof()) {
            if (flow.IsCurrent(TokType.ParenthObr) || flow.IsCurrent(TokType.ArrOBr) || flow.IsCurrent(TokType.FiObr))
                depth++;
            else if (flow.IsCurrent(TokType.ParenthCbr) || flow.IsCurrent(TokType.ArrCBr) || flow.IsCurrent(TokType.FiCbr))
                depth--;
            else if (depth == 0 && flow.IsCurrent(TokType.Colon)) {
                flow.Move(saved);
                return true;
            }
            else if (flow.IsCurrent(TokType.NewLine) || flow.IsCurrent(TokType.Dedent)) {
                flow.Move(saved);
                return false;
            }
            flow.MoveNext();
        }

        flow.Move(saved);
        return false;
    }

    /// <summary>
    /// Multiline if:
    /// if condition:
    ///     body
    /// elif condition:
    ///     body
    /// else:
    ///     body
    /// </summary>
    private static ISyntaxNode ParseIfBlock(TokFlow flow) {
        var start = flow.CurrentTokenStartPosition;
        var cases = new List<IfCaseSyntaxNode>();

        // Parse first: if condition: block
        if (!flow.MoveIf(TokType.If))
            throw new FunnyParseException(0, "Expected 'if'", flow.Current.Interval);

        cases.Add(ParseIfBranch(flow));

        // Single-line first branch followed by elif/else on a deeper-indent line
        // (`label = if c: a\n    elif d: b\n    else: c`) pushed an INDENT token
        // after the branch body. Track it so we can match a balancing DEDENT
        // before returning.
        int continuationIndents = 0;

        // Parse elif chain
        while (true) {
            SkipNewLines(flow);
            while (flow.IsCurrent(TokType.Indent)) {
                flow.MoveNext();
                continuationIndents++;
                SkipNewLines(flow);
            }
            if (!flow.IsCurrent(TokType.Elif))
                break;
            flow.MoveNext(); // consume elif
            cases.Add(ParseIfBranch(flow));
        }

        // Parse else (optional for statement-form)
        ISyntaxNode elseBody = null;
        if (flow.IsCurrent(TokType.Else)) {
            flow.MoveNext(); // consume else
            if (!flow.MoveIf(TokType.Colon))
                throw new FunnyParseException(0, "Expected ':' after 'else'", flow.Current.Interval);
            if (flow.IsCurrent(TokType.NewLine)) {
                flow.MoveNext(); // consume NewLine
                SkipNewLines(flow);
                elseBody = ParseBlock(flow);
            } else {
                // Single-line form: else: statement
                elseBody = ParseSingleLineBody(flow);
            }
        }

        // Balance any continuation-indent we consumed so the outer parser sees
        // the same dedent depth it expects.
        while (continuationIndents > 0) {
            SkipNewLines(flow);
            if (flow.IsCurrent(TokType.Dedent))
                flow.MoveNext();
            continuationIndents--;
        }

        var finish = flow.CurrentTokenFinishPosition;
        // If no else: use a sentinel DefaultValueSyntaxNode as else expression
        // for TIC compatibility. Marked auto-inserted so validators can
        // distinguish from a user-written `else default`.
        if (elseBody == null)
            elseBody = new DefaultValueSyntaxNode(new Interval(finish, finish))
                       { IsAutoInsertedElse = true };
        return SyntaxNodeFactory.IfElse(cases.ToArray(), elseBody, start, finish);
    }

    /// <summary>
    /// Parse a single if/elif branch: condition Colon NewLine Indent block Dedent
    /// (the if/elif keyword has already been consumed)
    /// </summary>
    private static IfCaseSyntaxNode ParseIfBranch(TokFlow flow) {
        var branchStart = flow.CurrentTokenStartPosition;

        // Suppress type annotation so `id:` at the end of condition doesn't trigger type parsing
        var prevSuppress = flow.SuppressTypeAnnotation;
        flow.SuppressTypeAnnotation = true;
        var condition = SyntaxNodeReader.ReadNodeOrNull(flow);
        flow.SuppressTypeAnnotation = prevSuppress;
        if (condition == null)
            throw new FunnyParseException(0, "Expected condition after 'if'/'elif'", flow.Current.Interval);

        if (!flow.MoveIf(TokType.Colon))
            throw new FunnyParseException(0, "Expected ':' after if/elif condition", flow.Current.Interval);

        ISyntaxNode body;
        if (flow.IsCurrent(TokType.NewLine)) {
            flow.MoveNext(); // consume NewLine
            SkipNewLines(flow);
            body = ParseBlock(flow);
        } else {
            // Single-line form: if cond: statement
            body = ParseSingleLineBody(flow);
        }

        return SyntaxNodeFactory.IfCase(condition, body, branchStart, body.Interval.Finish);
    }

    /// <summary>
    /// Multiline try:
    /// try:
    ///     tryBody
    /// catch:
    ///     catchBody
    /// anyway:
    ///     anywayBody
    /// </summary>
    private static ISyntaxNode ParseTryBlock(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'try'

        if (!flow.MoveIf(TokType.Colon))
            throw Errors.ColonExpectedAfterStatement(flow.Current, "try");

        SkipNewLines(flow);
        var tryBody = ParseBlock(flow);

        ISyntaxNode catchBody = null;
        string errorVarName = null;
        ISyntaxNode anywayBody = null;

        SkipNewLines(flow);

        // Optional catch
        if (flow.IsCurrent(TokType.Catch)) {
            flow.MoveNext(); // consume 'catch'

            // Optional error variable. Two forms:
            //   catch e:        (Statements.md spec form — no parens)
            //   catch (e):      (legacy form — with parens)
            if (flow.MoveIf(TokType.Id, out var errorIdTok)) {
                errorVarName = errorIdTok.Value;
            }
            else if (flow.IsCurrent(TokType.ParenthObr)) {
                flow.MoveNext(); // skip '('
                if (flow.MoveIf(TokType.Id, out var errorIdTok2)) {
                    errorVarName = errorIdTok2.Value;
                    if (!flow.MoveIf(TokType.ParenthCbr))
                        throw Errors.CatchParenthesisCbrExpected(flow.Current);
                } else {
                    throw Errors.CatchErrorVariableExpected(flow.Current);
                }
            }

            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "catch");

            SkipNewLines(flow);
            catchBody = ParseBlock(flow);
            SkipNewLines(flow);
        }

        // Optional anyway (finally)
        if (flow.IsCurrent(TokType.Anyway)) {
            flow.MoveNext(); // consume 'anyway'
            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "anyway");
            SkipNewLines(flow);
            anywayBody = ParseBlock(flow);
        }

        if (catchBody == null && anywayBody == null)
            throw Errors.TryCatchOrAnywayExpected(start, flow.CurrentTokenFinishPosition);

        return SyntaxNodeFactory.TryBlock(
            tryBody, catchBody, errorVarName, anywayBody,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// Check if 'try' is multiline form: try : NewLine
    /// </summary>
    private static bool IsMultiLineTry(TokFlow flow) {
        var savedPos = flow.CurrentTokenPosition;
        flow.MoveNext(); // skip 'try'

        if (flow.IsCurrent(TokType.Colon)) {
            flow.MoveNext();
            var isMultiline = flow.IsCurrent(TokType.NewLine) || flow.IsDoneOrEof();
            flow.Move(savedPos);
            return isMultiline;
        }

        flow.Move(savedPos);
        return false;
    }

    /// <summary>Parse @Annotation or @Annotation(arg1, arg2, ...) — can be multiple.</summary>
    private static List<FunnyAttribute> ParseAttributes(TokFlow flow) {
        var attrs = new List<FunnyAttribute>();
        while (flow.IsCurrent(TokType.MetaInfo)) {
            flow.MoveNext(); // consume @
            if (!flow.MoveIf(TokType.Id, out var nameToken))
                throw new FunnyParseException(0, "Expected attribute name after '@'", flow.Current.Interval);

            var values = new List<object>();
            if (flow.MoveIf(TokType.ParenthObr)) {
                // Parse argument list: @Test(1, 2, 3)
                if (!flow.IsCurrent(TokType.ParenthCbr)) {
                    values.Add(ParseConstantValue(flow));
                    while (flow.MoveIf(TokType.Sep)) {
                        values.Add(ParseConstantValue(flow));
                    }
                }
                if (!flow.MoveIf(TokType.ParenthCbr))
                    throw new FunnyParseException(0, "Expected ')' after attribute arguments", flow.Current.Interval);
            }

            attrs.Add(new FunnyAttribute(nameToken.Value, values.ToArray()));
            SkipNewLines(flow);
        }
        return attrs;
    }

    /// <summary>Parse a constant value in attribute arguments: int, real, text, bool.</summary>
    private static object ParseConstantValue(TokFlow flow) {
        if (flow.MoveIf(TokType.True)) return true;
        if (flow.MoveIf(TokType.False)) return false;
        if (flow.MoveIf(TokType.None)) return Types.FunnyNone.Instance;
        if (flow.MoveIf(TokType.IntNumber, out var intTok))
            return long.TryParse(intTok.Value, out var l) ? l : intTok.Value;
        if (flow.MoveIf(TokType.RealNumber, out var realTok))
            return double.TryParse(realTok.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : realTok.Value;
        if (flow.MoveIf(TokType.Text, out var textTok))
            return textTok.Value;
        if (flow.MoveIf(TokType.Minus)) {
            // Negative number
            if (flow.MoveIf(TokType.IntNumber, out var negInt))
                return long.TryParse(negInt.Value, out var nl) ? -nl : negInt.Value;
            if (flow.MoveIf(TokType.RealNumber, out var negReal))
                return double.TryParse(negReal.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var nd) ? -nd : negReal.Value;
        }
        throw new FunnyParseException(0, $"Expected constant value in attribute, got '{flow.Current.Value}'", flow.Current.Interval);
    }

    /// <summary>
    /// Parse body of a single-line form (after colon, no newline).
    /// Reads a single expression or statement (including assignments).
    /// Wraps result in BlockSyntaxNode for consistency with multi-line form.
    /// </summary>
    private static BlockSyntaxNode ParseSingleLineBody(TokFlow flow) {
        var stmt = ParseStatement(flow);
        if (stmt == null)
            throw new FunnyParseException(0, "Expected expression after ':'", flow.Current.Interval);
        return SyntaxNodeFactory.Block(
            new List<ISyntaxNode> { stmt },
            stmt.Interval);
    }

    /// <summary>
    /// If the current token is a compound assignment operator (+=, -=, *=, /=, %=, //=),
    /// returns the corresponding binary operator name. Otherwise returns null.
    /// </summary>
    private static string TryGetCompoundOperator(TokFlow flow) {
        if (flow.IsCurrent(TokType.PlusDef))  return CoreFunNames.Add;
        if (flow.IsCurrent(TokType.MinusDef)) return CoreFunNames.Substract;
        if (flow.IsCurrent(TokType.MulDef))   return CoreFunNames.Multiply;
        if (flow.IsCurrent(TokType.DivDef))   return CoreFunNames.DivideReal;
        if (flow.IsCurrent(TokType.ModDef))   return CoreFunNames.Remainder;
        if (flow.IsCurrent(TokType.IntDivDef)) return CoreFunNames.DivideInt;
        return null;
    }

    /// <summary>
    /// Extract the variable name and field name from a field access chain.
    /// Currently supports single-level: s.field → ("s", "field").
    /// </summary>
    private static (string varName, string fieldName) ExtractFieldTarget(StructFieldAccessSyntaxNode fieldAccess) {
        if (fieldAccess.Source is NamedIdSyntaxNode varNode)
            return (varNode.Id, fieldAccess.FieldName);
        throw new FunnyParseException(0,
            "Field assignment target must be variable.field (nested field assignment not yet supported)",
            fieldAccess.Interval);
    }

    private static void SkipNewLines(TokFlow flow) {
        while (flow.IsCurrent(TokType.NewLine))
            flow.MoveNext();
    }

    /// <summary>
    /// Enforce per Basics.md §Nfun script (L52): "Each of these elements begins
    /// with a new line. In this case, symbol `;` is the full equivalent of a
    /// line break." After parsing a statement, the next token must be one of:
    /// NewLine (covers `;` — both produce TokType.NewLine in the tokenizer),
    /// EOF, or a block terminator (Dedent for indented blocks). Anything else
    /// — typically a value token marking the start of an un-separated next
    /// statement like `y = 5 z = 6` — is a parse error. (StmtBug73.)
    /// </summary>
    private static void RequireStatementTerminator(TokFlow flow, params TokType[] blockEnds) {
        // The previous statement may have consumed its own trailing NewLine
        // (e.g. equation body via SyntaxNodeReader leaves the cursor on the
        // token AFTER the newline). So both forms count as "separator present":
        //   • current token IS NewLine (separator not yet consumed), or
        //   • PREVIOUS token was NewLine (statement-parser consumed it).
        // EOF and explicit block terminators also OK.
        if (flow.IsCurrent(TokType.NewLine) || flow.IsDoneOrEof())
            return;
        if (flow.CurrentTokenPosition > 0
            && flow.GetTokenAt(flow.CurrentTokenPosition - 1).Type == TokType.NewLine)
            return;
        foreach (var blockEnd in blockEnds)
            if (flow.IsCurrent(blockEnd)) return;
        throw new FunnyParseException(
            0,
            $"Missing statement separator (newline or `;`) before '{flow.Current.Value}'",
            flow.Current.Interval);
    }
}
