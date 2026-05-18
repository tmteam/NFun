using NFun.Exceptions;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing;

/// <summary>
/// Post-parse validator for statement-mode constructs that require lexical context:
/// <list type="bullet">
/// <item><c>return</c> must be inside a function body (named function or lambda).</item>
/// <item><c>break</c> / <c>continue</c> must be inside a loop body (<c>for</c> / <c>while</c>).</item>
/// <item><c>if</c> / <c>when</c> used as an expression (RHS of an equation /
///       inside a call / return value) must have an <c>else</c> clause.</item>
/// </list>
/// Without this pass, <c>return 42</c> at top level was accepted by the parser and the
/// evaluator leaked the internal <c>ReturnSignal</c> object as the script's output value
/// (BugHuntStatementsResults #4/#5/#13/#14).
/// </summary>
internal static class LangContextValidator {
    public static void Validate(SyntaxTree tree) {
        foreach (var node in tree.Nodes)
        {
            // A user-written named equation (`y = …`) is an expression position —
            // an `if`/`when` directly there must have an `else`. Auto-wrapped
            // equations (parser-synthesised around bare statements) are
            // statement positions and exempt: their if/when may legitimately
            // lack an else.
            if (node is EquationSyntaxNode eq && !eq.IsAutoWrapped)
                ValidateExpressionPosition(eq.Expression);

            Walk(node, inFunction: false, inLoop: false);
        }
    }

    // Flow analysis: does every control-flow path through `node` exit via
    // `return`? Used to enforce the contract that an annotated non-optional
    // return type means the function actually returns on all paths.
    private static bool DefinitelyReturns(ISyntaxNode node) {
        switch (node) {
            case ReturnSyntaxNode: return true;
            case BlockSyntaxNode block:
                foreach (var stmt in block.Statements)
                    if (DefinitelyReturns(stmt)) return true;
                return false;
            case IfThenElseSyntaxNode ite:
                // The else slot holds DefaultValueSyntaxNode when no `else` was
                // written — that path falls through.
                if (ite.ElseExpr is DefaultValueSyntaxNode) return false;
                foreach (var arm in ite.Ifs)
                    if (!DefinitelyReturns(arm.Expression)) return false;
                return DefinitelyReturns(ite.ElseExpr);
            case IfBlockSyntaxNode ib:
                if (ib.ElseBody == null) return false;
                foreach (var arm in ib.Ifs)
                    if (!DefinitelyReturns(arm.Expression)) return false;
                return DefinitelyReturns(ib.ElseBody);
            case WhenSyntaxNode w:
                if (w.ElseBody == null) return false;
                foreach (var arm in w.Arms)
                    if (!DefinitelyReturns(arm.Body)) return false;
                return DefinitelyReturns(w.ElseBody);
            default:
                return false;
        }
    }

    private static string TypeSyntaxText(TypeSyntax t) => t switch {
        TypeSyntax.Named n => n.Name,
        TypeSyntax.ArrayOf a => TypeSyntaxText(a.Element) + "[]",
        TypeSyntax.OptionalOf o => TypeSyntaxText(o.Element) + "?",
        _ => t.ToString()
    };

    private static void ValidateExpressionPosition(ISyntaxNode expr) {
        switch (expr) {
            case IfThenElseSyntaxNode ite when ite.ElseExpr is DefaultValueSyntaxNode:
                throw new FunnyParseException(0,
                    "'if' used as an expression requires an 'else' clause",
                    ite.Interval);
            case WhenSyntaxNode whenNode when whenNode.ElseBody == null:
                throw new FunnyParseException(0,
                    "'when' used as an expression requires an 'else' clause",
                    whenNode.Interval);
        }
    }

    private static void Walk(ISyntaxNode node, bool inFunction, bool inLoop) {
        switch (node) {
            case ReturnSyntaxNode ret:
                if (!inFunction)
                    throw new FunnyParseException(0,
                        "'return' is only valid inside a function body",
                        ret.Interval);
                if (ret.Expression != null) {
                    ValidateExpressionPosition(ret.Expression);
                    Walk(ret.Expression, inFunction, inLoop);
                }
                return;

            // Nested equation (e.g. `y = if cond: x` inside a fun body). Same
            // expression-position contract as top-level: if/when on the RHS must
            // have an else (BugHunt-stmt #30/#31). Auto-wrapped equations from
            // bare statements are exempt — they live in statement position.
            case EquationSyntaxNode eq:
                if (!eq.IsAutoWrapped)
                    ValidateExpressionPosition(eq.Expression);
                Walk(eq.Expression, inFunction, inLoop);
                return;

            case BreakSyntaxNode br:
                if (!inLoop)
                    throw new FunnyParseException(0,
                        "'break' is only valid inside a loop body",
                        br.Interval);
                return;

            case ContinueSyntaxNode co:
                if (!inLoop)
                    throw new FunnyParseException(0,
                        "'continue' is only valid inside a loop body",
                        co.Interval);
                return;

            // Entering a function body resets the loop context — break/continue
            // in an inner function should not escape to an enclosing loop. return
            // inside an inner function returns from THAT function, not the outer.
            case UserFunctionDefinitionSyntaxNode fn:
                // Spec contract check: a function with an annotated non-optional
                // return type must return on every path through its body. Without
                // this, `fun f() -> int:\n  if b: return 1` silently produces
                // `none` for the false branch — a type/value mismatch.
                if (fn.Body is BlockSyntaxNode block
                    && fn.ReturnTypeSyntax is not TypeSyntax.EmptyType
                    && fn.ReturnTypeSyntax is not TypeSyntax.OptionalOf
                    && !DefinitelyReturns(block))
                    throw new FunnyParseException(0,
                        $"Function '{fn.Id}' declares return type "
                        + $"'{TypeSyntaxText(fn.ReturnTypeSyntax)}' but not every path returns. "
                        + "Add a final 'return' or make the return type optional.",
                        fn.Interval);
                Walk(fn.Body, inFunction: true, inLoop: false);
                foreach (var arg in fn.Args)
                    if (arg.HasDefault) Walk(arg.DefaultValue, inFunction, inLoop);
                return;

            case AnonymFunctionSyntaxNode anon:
                Walk(anon.Body, inFunction: true, inLoop: false);
                return;

            case SuperAnonymFunctionSyntaxNode saf:
                Walk(saf.Body, inFunction: true, inLoop: false);
                return;

            // Entering a loop body opens the loop context. The collection
            // (or condition) is evaluated in the enclosing context.
            case ForSyntaxNode forNode:
                Walk(forNode.Collection, inFunction, inLoop);
                Walk(forNode.Body, inFunction, inLoop: true);
                return;

            case WhileSyntaxNode whileNode:
                Walk(whileNode.Condition, inFunction, inLoop);
                Walk(whileNode.Body, inFunction, inLoop: true);
                return;

            // Container nodes: walk children unchanged.
            default:
                foreach (var child in node.Children)
                    Walk(child, inFunction, inLoop);
                return;
        }
    }
}
