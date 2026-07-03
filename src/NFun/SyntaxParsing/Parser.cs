using System;
using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Top-level NFun parser. Reads a token flow and produces a <see cref="SyntaxTree"/>.
///
/// Two modes:
///   • <see cref="Mode.Expression"/> — classic short-form: equations, anonymous
///     output, `f(x) = expr` user functions, typed inputs.
///   • <see cref="Mode.Lang"/> — indent-based statement mode (Statements.md): adds
///     `fun … :` block definitions, control flow, mutable state, attributes.
///
/// Mode-specific top-level dispatch lives here. Reusable sub-grammars are extracted
/// into helper classes — they know nothing about Parser state:
///   • <see cref="TypeDeclarationParser"/>     — `type Name = …` (both modes)
///   • <see cref="FunctionDefinitionParser"/>  — `f(x)=body` and `fun f(x): block`
///   • <see cref="StatementParser"/>           — lang statement-level grammar: statements,
///                                              blocks, assignments, control flow, attributes
///   • <see cref="ExpressionParser"/>          — expression-level grammar (binary chains,
///                                              atoms, lambdas, struct/array literals)
/// </summary>
public class Parser {
    public enum Mode { Expression, Lang }

    public const string AnonymousEquationId = "out";

    public static SyntaxTree Parse(TokFlow flow)     => new Parser(flow, Mode.Expression).Run();
    public static SyntaxTree ParseLang(TokFlow flow) => new Parser(flow, Mode.Lang).Run();

    private readonly TokFlow _flow;
    private readonly Mode _mode;
    private readonly List<ISyntaxNode> _nodes = new();
    private readonly List<string> _equationNames = new();

    // Per-iteration state, set at the top of each top-level loop body.
    private bool _hasAnonymousEquation;
    private bool _startOfTheLine;
    private int _exprStartPosition;
    private FunnyAttribute[] _attributes;

    // Lang-mode only: synthetic id counter for side-effect-only top-level statements
    // (for/while/print/top-level if without else). They are auto-wrapped as
    // `__stmt_N__ = stmt` equations so downstream stages handle them uniformly.
    private int _langStmtCounter;

    private Parser(TokFlow flow, Mode mode) {
        _flow = flow;
        _mode = mode;
    }

    private SyntaxTree Run() {
        // In lang mode, NewLines outside brackets are statement terminators
        // and must stop binary-operator chains (BugHunt-stmt #66).
        if (_mode == Mode.Lang)
            _flow.RespectNewLines = true;

        while (true)
        {
            _flow.SkipNewLines();
            if (_flow.IsDoneOrEof()) break;

            if (_mode == Mode.Lang)
                ReadLangTopLevel();
            else
                ReadExpressionTopLevel();
        }

        // In lang mode, when multiple top-level value-bearing expressions auto-wrapped
        // to `out`, only the LAST one should keep the `out` name — earlier ones are
        // statements (their value is discarded). Without this, a sequence like
        //
        //   a = list(1,2,3)
        //   a.add(4)            # value-bearing → was: `out = a.add(4)` (none)
        //   out = a             # explicit out
        //
        // produces two `out` equations whose types LCA to `list<int>?` (the mutator's
        // none leaks Optional onto the real out). Bug hunt round 3 #12 / #13.
        // Earlier auto-wrapped `out` bindings are renamed to `__stmt_N__` (the same
        // sink used by IsValueBearingStatement=false statements).
        if (_mode == Mode.Lang)
            DemoteEarlierAutoWrappedOutBindings();

        var tree = new SyntaxTree(_nodes.ToArray());

        // Context-sensitive validation: `return` only inside functions,
        // `break`/`continue` only inside loops.
        if (_mode == Mode.Lang)
            LangContextValidator.Validate(tree);

        return tree;
    }

    private void DemoteEarlierAutoWrappedOutBindings() {
        // Find the LAST out equation (auto-wrapped or explicit) — that's the
        // real `out`. Any auto-wrapped `out` before it is a discarded statement.
        int lastOutIndex = -1;
        for (int i = _nodes.Count - 1; i >= 0; i--) {
            if (_nodes[i] is EquationSyntaxNode eq && eq.Id == AnonymousEquationId) {
                lastOutIndex = i;
                break;
            }
        }
        if (lastOutIndex < 0) return;
        for (int i = 0; i < lastOutIndex; i++) {
            if (_nodes[i] is EquationSyntaxNode eq
                && eq.IsAutoWrapped
                && eq.Id == AnonymousEquationId) {
                var renamed = SyntaxNodeFactory.Equation(
                    $"__stmt_{_langStmtCounter++}__",
                    eq.Expression,
                    eq.Interval.Start,
                    Array.Empty<FunnyAttribute>());
                renamed.IsAutoWrapped = true;
                _nodes[i] = renamed;
            }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // Expression-mode top-level
    // ───────────────────────────────────────────────────────────────

    private void ReadExpressionTopLevel() {
        _attributes = _flow.ReadAttributes();
        _startOfTheLine = _flow.IsStartOfTheLine();
        _exprStartPosition = _flow.Current.Start;

        // type keyword — parse named type declaration
        if (_flow.IsCurrent(TokType.TypeKeyword))
        {
            _nodes.Add(TypeDeclarationParser.Parse(_flow));
            return;
        }

        var e = ExpressionParser.ReadNodeOrNull(_flow) ??
                throw Errors.UnknownValueAtStartOfExpression(_exprStartPosition, _flow.Current);

        if (e is TypedVarDefSyntaxNode typed)
        {
            if (_flow.IsCurrent(TokType.Def))
                ReadEquation(typed, typed.Id);
            else
                ReadInputVariableSpecification(typed);
        }
        else if (_flow.IsCurrent(TokType.Def) || _flow.IsCurrent(TokType.Colon) || _flow.IsCurrent(TokType.Arrow))
        {
            if (e is NamedIdSyntaxNode variable)
                ReadEquation(variable, variable.Id);
            //Fun call can be used as fun definition
            else if (e is FunCallSyntaxNode fun && !fun.IsOperator)
                ReadUserFunction(fun);
            // Indexed-write `arr[i] = v` is a lang-mode statement form. In
            // expression-mode the parser doesn't have a statement layer, so it
            // falls through to here as `[](arr, i)` followed by `=` — give a
            // routed error rather than the generic "expression before definition".
            else if (e is FunCallSyntaxNode opCall
                     && opCall.IsOperator
                     && opCall.Id == Functions.CoreFunNames.GetElementName)
                throw Errors.IndexedWriteRequiresLangMode(_exprStartPosition, e, _flow.Current);
            else
                throw Errors.ExpressionBeforeTheDefinition(_exprStartPosition, e, _flow.Current);
        }
        else
            ReadAnonymousEquation(e);
    }

    /// <summary>
    /// Read input type specification.
    /// Like `i:int`.
    /// </summary>
    private void ReadInputVariableSpecification(TypedVarDefSyntaxNode typed)
        => _nodes.Add(SyntaxNodeFactory.VarDefinition(typed, _attributes));

    /// <summary>
    /// Read anonymous equation. Throws if at least one other equation exists.
    /// </summary>
    private void ReadAnonymousEquation(ISyntaxNode e) {
        if (_equationNames.Any())
        {
            if (_startOfTheLine && _hasAnonymousEquation)
                throw Errors.OnlyOneAnonymousExpressionAllowed(_exprStartPosition, e, _flow.Current);
            else
                throw Errors.UnexpectedExpression(e);
        }

        if (!_startOfTheLine)
            throw Errors.AnonymousExpressionHasToStartFromNewLine(_exprStartPosition, e, _flow.Current);

        var equation = SyntaxNodeFactory.Equation(AnonymousEquationId, e, _exprStartPosition, _attributes);
        _hasAnonymousEquation = true;
        _equationNames.Add(equation.Id);
        _nodes.Add(equation);
    }

    /// <summary>
    /// Read user function (expression-mode short form): `y(a,b,c:int):real = ...`.
    /// Delegates the actual signature/body building to <see cref="FunctionDefinitionParser"/>.
    /// </summary>
    private void ReadUserFunction(FunCallSyntaxNode fun) {
        if (!_startOfTheLine)
            throw Errors.FunctionDefinitionHasToStartFromNewLine(_exprStartPosition, fun, _flow.Current);
        if (_attributes.Length > 0)
            throw Errors.AttributeOnFunction(fun);

        _nodes.Add(FunctionDefinitionParser.FromCall(fun, _flow, _exprStartPosition));
    }

    /// <summary>
    /// Read named equation: `y = 1 + x`.
    /// </summary>
    private void ReadEquation(ISyntaxNode equationHeader, string id) {
        if (_hasAnonymousEquation)
            throw Errors.UnexpectedExpression(_nodes.OfType<EquationSyntaxNode>().Single());
        if (!_startOfTheLine)
            throw Errors.DefinitionHasToStartFromNewLine(_exprStartPosition, equationHeader, _flow.Current);

        _flow.MoveNext();
        var equation = ReadEquationBody(id);

        if (equationHeader is TypedVarDefSyntaxNode typed)
            equation.TypeSpecificationOrNull = typed;

        _nodes.Add(equation);
        _equationNames.Add(equation.Id);
    }

    private EquationSyntaxNode ReadEquationBody(string id) {
        _flow.SkipNewLines();
        var start = _flow.CurrentTokenFinishPosition;
        var exNode = ExpressionParser.ReadNodeOrNull(_flow);
        if (exNode == null)
            throw Errors.VarExpressionIsMissed(start, id, _flow.Current);
        return SyntaxNodeFactory.Equation(id, exNode, start, _attributes);
    }

    // ───────────────────────────────────────────────────────────────
    // Lang-mode top-level
    // ───────────────────────────────────────────────────────────────

    private void ReadLangTopLevel() {
        // Optional @Annotations before `fun` definitions
        if (_flow.IsCurrent(TokType.MetaInfo)) {
            var attrs = StatementParser.ParseAttributes(_flow);
            _flow.SkipNewLines();
            if (_flow.IsCurrent(TokType.Fun)) {
                var funcNode = FunctionDefinitionParser.FromFunKeyword(_flow);
                if (funcNode is UserFunctionDefinitionSyntaxNode ufd)
                    ufd.Attributes = attrs.ToArray();
                _nodes.Add(funcNode);
            } else {
                // Attributes before non-function — accept but discard for now
                var stmt = StatementParser.ParseStatement(_flow);
                _nodes.Add(WrapLangTopLevelStatement(stmt));
            }
        }
        else if (_flow.IsCurrent(TokType.Fun)) {
            _nodes.Add(FunctionDefinitionParser.FromFunKeyword(_flow));
        }
        else if (_flow.IsCurrent(TokType.TypeKeyword)) {
            _nodes.Add(TypeDeclarationParser.Parse(_flow));
        }
        else {
            var stmt = StatementParser.ParseStatement(_flow);
            // Top-level `id:type` input declaration (Basics.md §Input variables L166-175):
            // `i:int\n y = i+1`. Statement mode is an extension of expression mode
            // (Statements.md L1-3) — wrap as VarDefinition (matching expression-mode shape)
            // instead of auto-wrapping as an equation that would later trip
            // ExpressionBuilderVisitor's "not an expression" guard. (BugHunt-stmt #61.)
            if (stmt is TypedVarDefSyntaxNode typedDef) {
                _nodes.Add(SyntaxNodeFactory.VarDefinition(typedDef, Array.Empty<FunnyAttribute>()));
            } else {
                _nodes.Add(WrapLangTopLevelStatement(stmt));
            }
        }
        StatementParser.RequireStatementTerminator(_flow);
    }

    /// <summary>
    /// Wrap a bare top-level statement as a synthetic equation for RuntimeBuilder compatibility.
    /// Value-bearing expressions get the canonical `out` name (matches expression mode,
    /// Basics.md §Outputs); pure statements (for/while, top-level if/when without else,
    /// print, return, break/continue) get an internal `__stmt_N__` name that is later
    /// treated as non-output (BugHunt-stmt #23/#24).
    /// </summary>
    private ISyntaxNode WrapLangTopLevelStatement(ISyntaxNode stmt) {
        if (stmt is EquationSyntaxNode || stmt is UserFunctionDefinitionSyntaxNode)
            return stmt;

        bool isValueBearing = IsValueBearingStatement(stmt);
        var equationId = isValueBearing
            ? AnonymousEquationId
            : $"__stmt_{_langStmtCounter++}__";
        var eq = SyntaxNodeFactory.Equation(
            equationId, stmt, stmt.Interval.Start,
            Array.Empty<FunnyAttribute>());
        eq.IsAutoWrapped = true;
        return eq;
    }

    /// <summary>
    /// Statements that have no value at the top level — side-effect-only constructs
    /// shouldn't surface as outputs. Anything else (literal, call, identifier, binary op,
    /// ternary if-expr, struct init, lambda, …) carries a value and gets bound to the
    /// canonical `out` name.
    /// </summary>
    private static bool IsValueBearingStatement(ISyntaxNode node) => node switch {
        ForSyntaxNode => false,
        WhileSyntaxNode => false,
        IfBlockSyntaxNode => false,
        // Multi-line `if cond: ...` without an explicit `else` is parsed as
        // IfThenElseSyntaxNode with an auto-inserted DefaultValueSyntaxNode else.
        // It's the statement form (no value), so route through __stmt_N__ instead
        // of clobbering `out` (BugHunt-stmt #43). Check the IsAutoInsertedElse flag —
        // a user-written `else default` is a real expression that DOES bear a value.
        // (MR11Bug2.)
        IfThenElseSyntaxNode ite
            when ite.ElseExpr is DefaultValueSyntaxNode { IsAutoInsertedElse: true } => false,
        WhenSyntaxNode w when w.ElseBody == null => false,
        TryBlockSyntaxNode => false,
        FieldAssignmentSyntaxNode => false,
        IndexedAssignmentSyntaxNode => false,
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
}
