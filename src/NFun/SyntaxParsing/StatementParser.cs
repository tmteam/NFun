using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing;

/// <summary>
/// Lang-mode statement-level parser. One file = one grammar layer (statements).
///
/// Contents (top to bottom):
///   • Dispatcher        — <see cref="ParseStatement"/>, <see cref="ParseBlock"/>,
///                         <see cref="ParseSingleLineBody"/>, <see cref="ReadExpressionOrIfBlock"/>,
///                         <see cref="RequireStatementTerminator"/>.
///   • Assignments       — `=`, compound `+=`/`-=`/…, field assignment desugaring.
///   • Lookahead helpers — `IsFollowedByParenthesis`, <see cref="IsColonStyleIf"/>,
///                         <see cref="IsMultiLineTry"/>.
///   • Control flow      — `for`, `while`, `when`, `return`, `print`, multi-line `if`,
///                         multi-line `try/catch/anyway`.
///   • Attributes        — `@Annotation(args)` for top-level statements.
///
/// Recursion: control-flow constructs invoke <see cref="ParseBlock"/> /
/// <see cref="ParseSingleLineBody"/> for their bodies, which in turn re-enter
/// <see cref="ParseStatement"/>. Depth is bounded by the source's block nesting.
/// </summary>
internal static class StatementParser {

    // ───────────────────────────────────────────────────────────────
    // Dispatcher
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Read one statement. May yield any ISyntaxNode shape (equation, expression,
    /// loop, when, return, etc.). Caller is responsible for the statement separator.
    /// </summary>
    internal static ISyntaxNode ParseStatement(TokFlow flow) {
        flow.SkipNewLines();

        if (flow.IsCurrent(TokType.For))    return ParseForLoop(flow);
        if (flow.IsCurrent(TokType.While))  return ParseWhileLoop(flow);
        if (flow.IsCurrent(TokType.When))   return ParseWhen(flow);

        if (flow.IsCurrent(TokType.Break)) {
            var start = flow.Current.Start;
            flow.MoveNext();
            return SyntaxNodeFactory.Break(new Interval(start, flow.CurrentTokenFinishPosition));
        }

        if (flow.IsCurrent(TokType.Continue)) {
            var start = flow.Current.Start;
            flow.MoveNext();
            return SyntaxNodeFactory.Continue(new Interval(start, flow.CurrentTokenFinishPosition));
        }

        if (flow.IsCurrent(TokType.Return)) return ParseReturn(flow);

        // Print (contextual keyword — 'print' is still an identifier for function calls like print(42))
        if (flow.IsCurrent(TokType.Id) && flow.Current.Value == "print" && !IsFollowedByParenthesis(flow))
            return ParsePrint(flow);

        if (flow.IsCurrent(TokType.If) && IsColonStyleIf(flow))
            return ParseIfBlock(flow);

        if (flow.IsCurrent(TokType.Try) && IsMultiLineTry(flow))
            return ParseTryBlock(flow);

        // Read expression
        var stmtStart = flow.Current.Start;
        var node = ExpressionParser.ReadNodeOrNull(flow);
        if (node == null)
            throw new FunnyParseException(0, "Expected statement", flow.Current.Interval);

        // Expression-style user function definition: `f(args) = body`
        // (also accepts `f(args):retType = body` / `f(args) -> retType = body`).
        // Shared with expression mode for full feature parity (typed args, defaults,
        // params, keyword-only, return type annotation).
        if (node is FunCallSyntaxNode funCall && !funCall.IsOperator
            && (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon) || flow.IsCurrent(TokType.Arrow))) {
            return FunctionDefinitionParser.FromCall(funCall, flow, stmtStart);
        }

        // Assignment: expr followed by `=`
        if (flow.IsCurrent(TokType.Def))
            return ParseAssignment(flow, node);

        // Compound assignment: +=, -=, *=, /=, %=, //=
        var compoundOp = TryGetCompoundOperator(flow);
        if (compoundOp != null)
            return ParseCompoundAssignment(flow, node, compoundOp);

        return node;
    }

    /// <summary>
    /// Read an indented block: INDENT statement (NewLine|;)* DEDENT.
    /// Returns a <see cref="BlockSyntaxNode"/> wrapping all statements.
    /// </summary>
    internal static BlockSyntaxNode ParseBlock(TokFlow flow) {
        var start = flow.CurrentTokenStartPosition;

        if (!flow.MoveIf(TokType.Indent))
            throw Errors.IndentExpected(flow.Current);

        var statements = new List<ISyntaxNode>();
        while (!flow.IsCurrent(TokType.Dedent) && !flow.IsDoneOrEof()) {
            flow.SkipNewLines();
            if (flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()) break;
            var stmt = ParseStatement(flow);
            if (stmt != null)
                statements.Add(stmt);
            // Statement must be followed by a separator (newline / `;` / Dedent)
            // before the next one. (StmtBug73.)
            RequireStatementTerminator(flow, TokType.Dedent);
            flow.SkipNewLines();
        }

        var finish = flow.CurrentTokenFinishPosition;
        if (flow.IsCurrent(TokType.Dedent))
            flow.MoveNext();

        return SyntaxNodeFactory.Block(statements, new Interval(start, finish));
    }

    /// <summary>
    /// Wrap a single statement (the body of `if c: stmt`, `for x in xs: stmt`, etc.)
    /// in a 1-statement <see cref="BlockSyntaxNode"/> for uniform downstream handling.
    /// </summary>
    internal static BlockSyntaxNode ParseSingleLineBody(TokFlow flow) {
        var stmt = ParseStatement(flow);
        if (stmt == null)
            throw new FunnyParseException(0, "Expected expression after ':'", flow.Current.Interval);
        return SyntaxNodeFactory.Block(
            new List<ISyntaxNode> { stmt },
            stmt.Interval);
    }

    /// <summary>
    /// RHS of an assignment, or `return` operand. Accepts multi-line `if`/`when`/`try`
    /// blocks so that `result = if cond: …` works; otherwise delegates to
    /// <see cref="ExpressionParser"/>.
    /// </summary>
    internal static ISyntaxNode ReadExpressionOrIfBlock(TokFlow flow) {
        if (flow.IsCurrent(TokType.If) && IsColonStyleIf(flow))
            return ParseIfBlock(flow);
        if (flow.IsCurrent(TokType.When))
            return ParseWhen(flow);
        if (flow.IsCurrent(TokType.Try) && IsMultiLineTry(flow))
            return ParseTryBlock(flow);
        return ExpressionParser.ReadNodeOrNull(flow);
    }

    /// <summary>
    /// Enforce per Basics.md §Nfun script (L52): "Each of these elements begins with a
    /// new line. In this case, symbol `;` is the full equivalent of a line break."
    /// After parsing a statement, the next token must be a separator (NewLine / EOF /
    /// caller-provided block-end). Anything else — typically the start of an
    /// un-separated next statement like `y = 5 z = 6` — is a parse error. (StmtBug73.)
    /// </summary>
    internal static void RequireStatementTerminator(TokFlow flow, params TokType[] blockEnds) {
        // The previous statement may have consumed its own trailing NewLine
        // (e.g. equation body via ExpressionParser leaves the cursor on the
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

    // ───────────────────────────────────────────────────────────────
    // Assignments
    // ───────────────────────────────────────────────────────────────

    private static ISyntaxNode ParseAssignment(TokFlow flow, ISyntaxNode lhs) {
        // Field assignment: s.field = expr → s = FieldAssignment(s, "field", expr)
        if (lhs is StructFieldAccessSyntaxNode fieldAccess) {
            var (varName, fieldName) = ExtractFieldTarget(fieldAccess);
            flow.MoveNext(); // consume '='
            var valueExpr = ReadExpressionOrIfBlock(flow);
            if (valueExpr == null)
                throw new FunnyParseException(0,
                    $"Expected expression after '=' in field assignment to '{varName}.{fieldName}'",
                    flow.Current.Interval);
            var source = new NamedIdSyntaxNode(varName, fieldAccess.Source.Interval);
            var fieldAssign = SyntaxNodeFactory.FieldAssignment(varName, fieldName, source, valueExpr,
                new Interval(fieldAccess.Interval.Start, valueExpr.Interval.Finish));
            return SyntaxNodeFactory.Equation(varName, fieldAssign, fieldAccess.Interval.Start,
                Array.Empty<FunnyAttribute>());
        }

        if (lhs is NamedIdSyntaxNode named) {
            flow.MoveNext(); // consume '='
            var body = ReadExpressionOrIfBlock(flow);
            if (body == null)
                throw new FunnyParseException(0,
                    $"Expected expression after '=' in assignment to '{named.Id}'",
                    flow.Current.Interval);
            return SyntaxNodeFactory.Equation(named.Id, body, named.Interval.Start,
                Array.Empty<FunnyAttribute>());
        }

        if (lhs is TypedVarDefSyntaxNode typed) {
            flow.MoveNext(); // consume '='
            var body = ReadExpressionOrIfBlock(flow);
            if (body == null)
                throw new FunnyParseException(0,
                    $"Expected expression after '=' in assignment to '{typed.Id}'",
                    flow.Current.Interval);
            var eq = SyntaxNodeFactory.Equation(typed.Id, body, typed.Interval.Start,
                Array.Empty<FunnyAttribute>());
            eq.TypeSpecificationOrNull = typed;
            return eq;
        }

        throw new FunnyParseException(0, "Left side of '=' must be an identifier", lhs.Interval);
    }

    private static ISyntaxNode ParseCompoundAssignment(TokFlow flow, ISyntaxNode lhs, string compoundOp) {
        // Field compound assignment: s.field += expr → s = FieldAssignment(s, "field", s.field + expr)
        if (lhs is StructFieldAccessSyntaxNode fieldAccess) {
            var (varName, fieldName) = ExtractFieldTarget(fieldAccess);
            flow.MoveNext(); // consume compound operator
            var rhs = ReadExpressionOrIfBlock(flow);
            if (rhs == null)
                throw new FunnyParseException(0,
                    $"Expected expression after compound assignment to '{varName}.{fieldName}'",
                    flow.Current.Interval);
            // Desugar: s.a += expr → s.a = s.a + expr → s = FieldAssignment(s, "a", s.a + expr)
            var readSource = new NamedIdSyntaxNode(varName, fieldAccess.Source.Interval);
            var fieldRead = new StructFieldAccessSyntaxNode(readSource, fieldName, fieldAccess.Interval);
            var binOp = SyntaxNodeFactory.BinOperatorCall(compoundOp, fieldRead, rhs);
            var source = new NamedIdSyntaxNode(varName, fieldAccess.Source.Interval);
            var fieldAssign = SyntaxNodeFactory.FieldAssignment(varName, fieldName, source, binOp,
                new Interval(fieldAccess.Interval.Start, rhs.Interval.Finish));
            return SyntaxNodeFactory.Equation(varName, fieldAssign, fieldAccess.Interval.Start,
                Array.Empty<FunnyAttribute>());
        }

        if (lhs is NamedIdSyntaxNode named) {
            flow.MoveNext(); // consume compound operator
            var rhs = ReadExpressionOrIfBlock(flow);
            if (rhs == null)
                throw new FunnyParseException(0,
                    $"Expected expression after compound assignment to '{named.Id}'",
                    flow.Current.Interval);
            // Desugar: x += expr → x = x + expr
            var varRef = new NamedIdSyntaxNode(named.Id, named.Interval);
            var binOp = SyntaxNodeFactory.BinOperatorCall(compoundOp, varRef, rhs);
            return SyntaxNodeFactory.Equation(named.Id, binOp, named.Interval.Start,
                Array.Empty<FunnyAttribute>());
        }

        throw new FunnyParseException(0, "Left side of compound assignment must be an identifier", lhs.Interval);
    }

    /// <summary>
    /// If the current token is a compound assignment operator, returns the corresponding
    /// binary operator name. Otherwise null.
    /// </summary>
    private static string TryGetCompoundOperator(TokFlow flow) {
        if (flow.IsCurrent(TokType.PlusDef))   return CoreFunNames.Add;
        if (flow.IsCurrent(TokType.MinusDef))  return CoreFunNames.Substract;
        if (flow.IsCurrent(TokType.MulDef))    return CoreFunNames.Multiply;
        if (flow.IsCurrent(TokType.DivDef))    return CoreFunNames.DivideReal;
        if (flow.IsCurrent(TokType.ModDef))    return CoreFunNames.Remainder;
        if (flow.IsCurrent(TokType.IntDivDef)) return CoreFunNames.DivideInt;
        return null;
    }

    /// <summary>
    /// Extract (varName, fieldName) from a single-level field access `s.field`.
    /// Nested chains (`a.b.c`) are not supported as assignment targets.
    /// </summary>
    private static (string varName, string fieldName) ExtractFieldTarget(StructFieldAccessSyntaxNode fieldAccess) {
        if (fieldAccess.Source is NamedIdSyntaxNode varNode)
            return (varNode.Id, fieldAccess.FieldName);
        throw new FunnyParseException(0,
            "Field assignment target must be variable.field (nested field assignment not yet supported)",
            fieldAccess.Interval);
    }

    // ───────────────────────────────────────────────────────────────
    // Lookahead helpers
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// True if the current token is followed by '(' (lookahead). Used to distinguish
    /// statement-form `print expr` from the function-call form `print(expr)`.
    /// </summary>
    private static bool IsFollowedByParenthesis(TokFlow flow) {
        var saved = flow.CurrentTokenPosition;
        flow.MoveNext();
        var result = flow.IsCurrent(TokType.ParenthObr);
        flow.Move(saved);
        return result;
    }

    /// <summary>
    /// True if the current `if` token starts a colon-style block — i.e. there is a `:`
    /// before any newline/dedent at the same paren-depth. Used to choose between
    /// block-form parsing here and expression-form `if a then b else c` in
    /// <see cref="ExpressionParser"/>. Does not consume tokens.
    /// </summary>
    internal static bool IsColonStyleIf(TokFlow flow) {
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
    /// True if the current `try` token starts a multi-line block — i.e. `try` is followed
    /// by `:` and then a newline. Single-line `try expr catch expr` is handled by
    /// <see cref="ExpressionParser"/>. Does not consume tokens.
    /// </summary>
    internal static bool IsMultiLineTry(TokFlow flow) {
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

    // ───────────────────────────────────────────────────────────────
    // Control flow constructs
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// `for iter in collection:` — block or single-line body.
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
        var collection = ExpressionParser.ReadNodeOrNull(flow);
        flow.SuppressTypeAnnotation = savedSuppress;

        if (collection == null)
            throw Errors.ForCollectionExpected(flow.Current);

        if (!flow.MoveIf(TokType.Colon))
            throw Errors.ColonExpectedAfterStatement(flow.Current, "for");

        ISyntaxNode body;
        if (flow.IsCurrent(TokType.NewLine)) {
            flow.SkipNewLines();
            body = ParseBlock(flow);
        } else {
            body = ParseSingleLineBody(flow);
        }

        return SyntaxNodeFactory.For(
            iteratorTok.Value, collection, body,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// `while condition:` — block or single-line body.
    /// </summary>
    private static ISyntaxNode ParseWhileLoop(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'while'

        var savedSuppress = flow.SuppressTypeAnnotation;
        flow.SuppressTypeAnnotation = true;
        var condition = ExpressionParser.ReadNodeOrNull(flow);
        flow.SuppressTypeAnnotation = savedSuppress;

        if (condition == null)
            throw Errors.WhileConditionExpected(flow.Current);

        if (!flow.MoveIf(TokType.Colon))
            throw Errors.ColonExpectedAfterStatement(flow.Current, "while");

        ISyntaxNode body;
        if (flow.IsCurrent(TokType.NewLine)) {
            flow.SkipNewLines();
            body = ParseBlock(flow);
        } else {
            body = ParseSingleLineBody(flow);
        }

        return SyntaxNodeFactory.While(
            condition, body,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// Pattern matching:
    ///   `when subject:` — value-based (matches when subject == armValue)
    ///   `when:`         — condition-based (first arm whose condition is true)
    /// Else clause optional in statement form; required in expression form (enforced by builder).
    /// </summary>
    private static ISyntaxNode ParseWhen(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'when'

        ISyntaxNode subject = null;

        // Condition-based (when:) vs value-based (when subject:)
        if (!flow.IsCurrent(TokType.Colon)) {
            var savedSuppress = flow.SuppressTypeAnnotation;
            flow.SuppressTypeAnnotation = true;
            subject = ExpressionParser.ReadNodeOrNull(flow);
            flow.SuppressTypeAnnotation = savedSuppress;
        }

        // Colon after subject is optional: both `when x:` and `when x` (followed by newline) are valid
        flow.MoveIf(TokType.Colon);
        flow.SkipNewLines();

        var arms = new List<WhenArmSyntaxNode>();
        ISyntaxNode elseBody = null;

        bool isIndented = flow.MoveIf(TokType.Indent);

        while (true) {
            flow.SkipNewLines();

            // End conditions
            if (isIndented && (flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()))
                break;
            if (!isIndented && (flow.IsCurrent(TokType.NewLine) || flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()))
                break;

            var armStart = flow.CurrentTokenStartPosition;

            // 'else' arm
            if (flow.IsCurrent(TokType.Else)) {
                flow.MoveNext(); // consume 'else'
                if (!flow.MoveIf(TokType.Colon))
                    throw Errors.ColonExpectedAfterStatement(flow.Current, "else");
                flow.SkipNewLines();
                if (flow.IsCurrent(TokType.Indent))
                    elseBody = ParseBlock(flow);
                else
                    elseBody = ExpressionParser.ReadNodeOrNull(flow);
                break;
            }

            // Arm condition / value
            var savedSuppress = flow.SuppressTypeAnnotation;
            flow.SuppressTypeAnnotation = true;
            var armCondition = ExpressionParser.ReadNodeOrNull(flow);
            flow.SuppressTypeAnnotation = savedSuppress;

            if (armCondition == null)
                throw Errors.WhenArmConditionExpected(flow.Current);

            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "when arm");

            flow.SkipNewLines();
            // Arm body: block (if Indent), else single statement — same shape as `if c: stmt`
            ISyntaxNode armBody;
            if (flow.IsCurrent(TokType.Indent))
                armBody = ParseBlock(flow);
            else
                armBody = ParseSingleLineBody(flow);

            if (armBody == null)
                throw Errors.WhenArmBodyExpected(flow.Current);

            arms.Add(SyntaxNodeFactory.WhenArm(
                armCondition, armBody, armStart, flow.CurrentTokenFinishPosition));
            flow.SkipNewLines();
        }

        if (isIndented && !flow.MoveIf(TokType.Dedent)) {
            if (!flow.IsDoneOrEof())
                throw Errors.DedentExpected(flow.Current);
        }

        // Spec layout: `else:` may appear AFTER the dedent when arms sit on a deeper
        // indent than the originating `when` keyword's continuation column.
        flow.SkipNewLines();
        if (elseBody == null && flow.IsCurrent(TokType.Else)) {
            flow.MoveNext();
            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "else");
            flow.SkipNewLines();
            if (flow.IsCurrent(TokType.Indent))
                elseBody = ParseBlock(flow);
            else
                elseBody = ExpressionParser.ReadNodeOrNull(flow);
        }

        return SyntaxNodeFactory.When(
            subject, arms.ToArray(), elseBody,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// `return [expression]` — bare `return` yields none.
    /// </summary>
    private static ISyntaxNode ParseReturn(TokFlow flow) {
        var retStart = flow.Current.Start;
        flow.MoveNext(); // consume 'return'

        if (flow.IsCurrent(TokType.NewLine) || flow.IsCurrent(TokType.Dedent) || flow.IsDoneOrEof()) {
            return SyntaxNodeFactory.Return(null, new Interval(retStart, flow.CurrentTokenFinishPosition));
        }

        var expr = ReadExpressionOrIfBlock(flow);
        if (expr == null)
            throw new FunnyParseException(0, "Expected expression after 'return'", flow.Current.Interval);
        return SyntaxNodeFactory.Return(expr, new Interval(retStart, expr.Interval.Finish));
    }

    /// <summary>
    /// `print expression` (statement form). The function-call form `print(args)` is handled
    /// by ExpressionParser as a regular call.
    /// </summary>
    private static ISyntaxNode ParsePrint(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'print'

        var expr = ExpressionParser.ReadNodeOrNull(flow);
        if (expr == null)
            throw Errors.PrintExpressionExpected(flow.Current);

        return SyntaxNodeFactory.Print(expr,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    /// <summary>
    /// Multi-line `if condition:` / `elif condition:` / `else:` block.
    /// </summary>
    private static ISyntaxNode ParseIfBlock(TokFlow flow) {
        var start = flow.CurrentTokenStartPosition;
        var cases = new List<IfCaseSyntaxNode>();

        if (!flow.MoveIf(TokType.If))
            throw new FunnyParseException(0, "Expected 'if'", flow.Current.Interval);

        cases.Add(ParseIfBranch(flow));

        // Single-line first branch followed by elif/else on a deeper-indent line
        // (`label = if c: a\n    elif d: b\n    else: c`) pushed an INDENT token
        // after the branch body. Track it so we match a balancing DEDENT before returning.
        int continuationIndents = 0;

        while (true) {
            flow.SkipNewLines();
            while (flow.IsCurrent(TokType.Indent)) {
                flow.MoveNext();
                continuationIndents++;
                flow.SkipNewLines();
            }
            if (!flow.IsCurrent(TokType.Elif))
                break;
            flow.MoveNext(); // consume elif
            cases.Add(ParseIfBranch(flow));
        }

        ISyntaxNode elseBody = null;
        if (flow.IsCurrent(TokType.Else)) {
            flow.MoveNext(); // consume else
            if (!flow.MoveIf(TokType.Colon))
                throw new FunnyParseException(0, "Expected ':' after 'else'", flow.Current.Interval);
            if (flow.IsCurrent(TokType.NewLine)) {
                flow.MoveNext();
                flow.SkipNewLines();
                elseBody = ParseBlock(flow);
            } else {
                elseBody = ParseSingleLineBody(flow);
            }
        }

        while (continuationIndents > 0) {
            flow.SkipNewLines();
            if (flow.IsCurrent(TokType.Dedent))
                flow.MoveNext();
            continuationIndents--;
        }

        var finish = flow.CurrentTokenFinishPosition;
        // No else: insert sentinel for TIC compatibility, marked auto-inserted
        // so validators can distinguish from a user-written `else default`.
        if (elseBody == null)
            elseBody = new DefaultValueSyntaxNode(new Interval(finish, finish))
                       { IsAutoInsertedElse = true };
        return SyntaxNodeFactory.IfElse(cases.ToArray(), elseBody, start, finish);
    }

    /// <summary>
    /// Parse a single if/elif branch: condition Colon (NewLine Indent block Dedent | single-line stmt).
    /// The if/elif keyword has already been consumed.
    /// </summary>
    private static IfCaseSyntaxNode ParseIfBranch(TokFlow flow) {
        var branchStart = flow.CurrentTokenStartPosition;

        var prevSuppress = flow.SuppressTypeAnnotation;
        flow.SuppressTypeAnnotation = true;
        var condition = ExpressionParser.ReadNodeOrNull(flow);
        flow.SuppressTypeAnnotation = prevSuppress;
        if (condition == null)
            throw new FunnyParseException(0, "Expected condition after 'if'/'elif'", flow.Current.Interval);

        if (!flow.MoveIf(TokType.Colon))
            throw new FunnyParseException(0, "Expected ':' after if/elif condition", flow.Current.Interval);

        ISyntaxNode body;
        if (flow.IsCurrent(TokType.NewLine)) {
            flow.MoveNext();
            flow.SkipNewLines();
            body = ParseBlock(flow);
        } else {
            body = ParseSingleLineBody(flow);
        }

        return SyntaxNodeFactory.IfCase(condition, body, branchStart, body.Interval.Finish);
    }

    /// <summary>
    /// Multi-line `try: ... catch [e]: ... anyway: ...`. Either catch or anyway must be present.
    /// </summary>
    private static ISyntaxNode ParseTryBlock(TokFlow flow) {
        var start = flow.Current.Start;
        flow.MoveNext(); // consume 'try'

        if (!flow.MoveIf(TokType.Colon))
            throw Errors.ColonExpectedAfterStatement(flow.Current, "try");

        flow.SkipNewLines();
        var tryBody = ParseBlock(flow);

        ISyntaxNode catchBody = null;
        string errorVarName = null;
        ISyntaxNode anywayBody = null;

        flow.SkipNewLines();

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

            flow.SkipNewLines();
            catchBody = ParseBlock(flow);
            flow.SkipNewLines();
        }

        // Optional anyway (finally)
        if (flow.IsCurrent(TokType.Anyway)) {
            flow.MoveNext();
            if (!flow.MoveIf(TokType.Colon))
                throw Errors.ColonExpectedAfterStatement(flow.Current, "anyway");
            flow.SkipNewLines();
            anywayBody = ParseBlock(flow);
        }

        if (catchBody == null && anywayBody == null)
            throw Errors.TryCatchOrAnywayExpected(start, flow.CurrentTokenFinishPosition);

        return SyntaxNodeFactory.TryBlock(
            tryBody, catchBody, errorVarName, anywayBody,
            new Interval(start, flow.CurrentTokenFinishPosition));
    }

    // ───────────────────────────────────────────────────────────────
    // Attributes
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Read one-or-more chained `@Annotation` / `@Annotation(arg1, arg2)` attributes.
    /// Stops at the next non-attribute token.
    /// </summary>
    internal static List<FunnyAttribute> ParseAttributes(TokFlow flow) {
        var attrs = new List<FunnyAttribute>();
        while (flow.IsCurrent(TokType.MetaInfo)) {
            flow.MoveNext(); // consume @
            if (!flow.MoveIf(TokType.Id, out var nameToken))
                throw new FunnyParseException(0, "Expected attribute name after '@'", flow.Current.Interval);

            var values = new List<object>();
            if (flow.MoveIf(TokType.ParenthObr)) {
                if (!flow.IsCurrent(TokType.ParenthCbr)) {
                    values.Add(ReadAttributeConstant(flow));
                    while (flow.MoveIf(TokType.Sep)) {
                        values.Add(ReadAttributeConstant(flow));
                    }
                }
                if (!flow.MoveIf(TokType.ParenthCbr))
                    throw new FunnyParseException(0, "Expected ')' after attribute arguments", flow.Current.Interval);
            }

            attrs.Add(new FunnyAttribute(nameToken.Value, values.ToArray()));
            flow.SkipNewLines();
        }
        return attrs;
    }

    /// <summary>Read a constant value used as an attribute argument: int, real, text, bool, none, negative number.</summary>
    private static object ReadAttributeConstant(TokFlow flow) {
        if (flow.MoveIf(TokType.True)) return true;
        if (flow.MoveIf(TokType.False)) return false;
        if (flow.MoveIf(TokType.None)) return FunnyNone.Instance;
        if (flow.MoveIf(TokType.IntNumber, out var intTok))
            return long.TryParse(intTok.Value, out var l) ? l : intTok.Value;
        if (flow.MoveIf(TokType.RealNumber, out var realTok))
            return double.TryParse(realTok.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : realTok.Value;
        if (flow.MoveIf(TokType.Text, out var textTok))
            return textTok.Value;
        if (flow.MoveIf(TokType.Minus)) {
            if (flow.MoveIf(TokType.IntNumber, out var negInt))
                return long.TryParse(negInt.Value, out var nl) ? -nl : negInt.Value;
            if (flow.MoveIf(TokType.RealNumber, out var negReal))
                return double.TryParse(negReal.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var nd) ? -nd : negReal.Value;
        }
        throw new FunnyParseException(0, $"Expected constant value in attribute, got '{flow.Current.Value}'", flow.Current.Interval);
    }
}
