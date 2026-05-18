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
                // Wrap bare expressions as synthetic equations for RuntimeBuilder compatibility
                if (stmt is not EquationSyntaxNode && stmt is not UserFunctionDefinitionSyntaxNode) {
                    stmt = SyntaxNodeFactory.Equation(
                        $"__stmt_{stmtCounter++}__", stmt, stmt.Interval.Start,
                        System.Array.Empty<FunnyAttribute>());
                }
                nodes.Add(stmt);
            }
        }

        return new SyntaxTree(nodes.ToArray());
    }

    private static ISyntaxNode ParseFunctionDefinition(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'fun'

        if (!flow.MoveIf(TokType.Id, out var firstIdToken))
            throw new FunnyParseException(0, "Expected function name after 'fun'", flow.Current.Interval);

        // Extension function syntax: fun receiver.name(args)
        // Namespace separation controlled by dialect flag at TIC/dispatch level.
        Tok nameToken;
        TypedVarDefSyntaxNode receiverArg = null;
        if (flow.IsCurrent(TokType.Dot)) {
            flow.MoveNext(); // consume '.'
            if (!flow.MoveIf(TokType.Id, out nameToken))
                throw new FunnyParseException(0, "Expected function name after '.'", flow.Current.Interval);
            receiverArg = SyntaxNodeFactory.TypedVar(firstIdToken.Value, TypeSyntax.Empty, firstIdToken.Start, firstIdToken.Finish);
        } else {
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

        // Optionally read return type: -> type
        var outputType = TypeSyntax.Empty;
        if (flow.MoveIf(TokType.Arrow))
            outputType = flow.ReadTypeSyntax();

        // Expect colon
        if (!flow.MoveIf(TokType.Colon))
            throw new FunnyParseException(0, "Expected ':' after function signature", flow.Current.Interval);

        // Expect NewLine then Indent
        if (!flow.MoveIf(TokType.NewLine))
            throw new FunnyParseException(0, "Expected newline after ':'", flow.Current.Interval);
        SkipNewLines(flow); // allow extra blank lines

        // Parse block body
        var body = ParseBlock(flow);

        // Create a FunCallSyntaxNode as the "Head" (reusing existing UserFunctionDefinitionSyntaxNode pattern)
        // The Head is a FunCallSyntaxNode with the function name and arg nodes
        var argNodes = new ISyntaxNode[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
            argNodes[i] = arguments[i];

        var headInterval = new Interval(start, nameToken.Finish);
        var head = new FunCallSyntaxNode(nameToken.Value, argNodes, headInterval, false, false);

        var funcNode = new UserFunctionDefinitionSyntaxNode(arguments, head, body, outputType);
        funcNode.Interval = new Interval(start, body.Interval.Finish);
        return funcNode;
    }

    /// <summary>
    /// Parse named type declaration: type name = {field defs} or type name = alias
    /// Same logic as Parser.ReadTypeDeclaration but static for LangParser.
    /// </summary>
    // Delegates to shared Parser.ParseTypeDeclaration

    private static TypedVarDefSyntaxNode ReadFunArg(TokFlow flow) {
        if (!flow.MoveIf(TokType.Id, out var argId))
            throw new FunnyParseException(0, "Expected argument name", flow.Current.Interval);

        var typeSyntax = TypeSyntax.Empty;
        if (flow.IsCurrent(TokType.Colon)) {
            flow.MoveNext();
            typeSyntax = flow.ReadTypeSyntax();
        }

        return SyntaxNodeFactory.TypedVar(argId.Value, typeSyntax, argId.Start, argId.Finish);
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

    private static ISyntaxNode ParseStatement(TokFlow flow) {
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
            // Arm body: if next is Indent, it's a block; otherwise single expression
            ISyntaxNode armBody;
            if (flow.IsCurrent(TokType.Indent))
                armBody = ParseBlock(flow);
            else
                armBody = SyntaxNodeReader.ReadNodeOrNull(flow);

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

        // Parse elif chain
        while (true) {
            SkipNewLines(flow);
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

        var finish = flow.CurrentTokenFinishPosition;
        // If no else: use none/default as else expression for TIC compatibility
        if (elseBody == null)
            elseBody = SyntaxNodeFactory.DefaultValue(new Interval(finish, finish));
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
}
