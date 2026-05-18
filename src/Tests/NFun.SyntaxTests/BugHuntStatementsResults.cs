using System;
using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated statement-mode (lang) bug hunting (300 iterations
/// across 3 agents: SIMPLE_AND_TRICKY, HELL_AND_NESTED, EDGE_AND_CREATIVE).
/// 26 unique confirmed bugs after de-duplication. All marked [Ignore] until
/// fixed; expected behavior per Statements.md / Basics.md.
/// </summary>
public class BugHuntStatementsResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    private static FunnyRuntime BuildLang(string script) => Funny.Hardcore.BuildLang(script);

    // ═══════════════════════════════════════════════════════════════════════
    // CRITICAL — silent data loss / crash
    // ═══════════════════════════════════════════════════════════════════════

    // Bug #1: Recursive `fun` body's local variables share a single
    // VariableSource slot across stack frames. Fixed by snapshot-on-entry /
    // restore-on-exit of local sources in ConcreteRecursiveUserFunction.
    [Test]
    public void StmtBug1_RecursiveFn_LocalsClobbered() {
        var rt = BuildLang(
            "fun f(n):\n" +
            "    if n <= 0: return 1\n" +
            "    a = f(n - 1)\n" +
            "    b = f(n - 1)\n" +
            "    return a + b\n" +
            "y = f(2)");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test]
    public void StmtBug2_TreeDepth_LocalsClobbered() {
        var rt = Funny.Hardcore
            .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildLang(
                "type tree = {v: int, l: tree? = none, r: tree? = none}\n" +
                "fun depth(t: tree?) -> int:\n" +
                "    if t == none: return 0\n" +
                "    a = depth(t!.l)\n" +
                "    b = depth(t!.r)\n" +
                "    return 1 + max(a, b)\n" +
                "y = depth(tree{v=1, l=tree{v=2, l=tree{v=4}}, r=tree{v=5}})");
        rt.Run();
        Assert.AreEqual(3, Convert.ToInt32(rt["y"].Value));
    }

    // Bug #3: Named-type constructor inside a multi-line `fun` body was
    // skipped by NamedTypeElaborator (its ElaborateChildren switch had no
    // case for BlockSyntaxNode / ReturnSyntaxNode / IfBlockSyntaxNode and
    // friends), so it survived to ExpressionBuilder which threw
    // NFunImpossibleException. Fixed by adding statement-mode container
    // cases to the elaborator.
    [Test]
    public void StmtBug3_NamedTypeCtorInFunBody_Crash() {
        var rt = Funny.Hardcore
            .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .BuildLang(
                "type t = {x: int}\n" +
                "fun make(n):\n" +
                "    return t{x = n}\n" +
                "y = make(5)");
        Assert.DoesNotThrow(() => rt.Run());
    }

    // Bug #4/#5: Top-level `return`/`break`/`continue` were accepted by the
    // parser and the internal signal object (ReturnSignal / BreakSignal /
    // ContinueSignal) leaked as an output value. Fixed by LangContextValidator
    // running after parsing: `return` requires being inside a function body,
    // `break`/`continue` require being inside a loop body.
    [Test]
    public void StmtBug4_TopLevelReturn_LeaksSignal() {
        Assert.Throws<FunnyParseException>(() => BuildLang("return 42"));
    }

    [Test]
    public void StmtBug5_TopLevelBreak_LeaksSignal() {
        Assert.Throws<FunnyParseException>(() => BuildLang("y = 1\nbreak"));
    }

    [Test]
    public void StmtBug5_TopLevelContinue_LeaksSignal() {
        Assert.Throws<FunnyParseException>(() => BuildLang("continue"));
    }

    [Test]
    public void StmtBug5_BreakInsideIf_NotInLoop() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "if true:\n" +
            "    break"));
    }

    // Bug #6: Variables declared inside a block (with no else) used to leak
    // to the enclosing scope. Fixed by ExpressionBuilder cleanup that removes
    // output variables introduced inside an if-with-no-real-else and inside
    // for/while/when/try block bodies. If-with-real-else preserves the
    // common `result = x else: result = -x; return result` idiom (both
    // branches reuse the same VariableSource).
    [Test]
    public void StmtBug6_ScopeLeak_IfBlock_SpecExample() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f():\n" +
            "    if true:\n" +
            "        local = 1\n" +
            "    return local\n" +
            "y = f()"));
    }

    // Bug #7: `for` iterator no longer leaks. The for-body opens a fresh
    // scope; iterator + body locals are removed on exit. (At top-level
    // scripts the leaked name resolves as a script input rather than an
    // error — this is the documented script-level "implicit input" behaviour;
    // strict rejection happens inside function bodies.)
    [Test]
    public void StmtBug7_ScopeLeak_ForIterator_InsideFunction() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f():\n" +
            "    for item in [1,2,3]:\n" +
            "        z = item\n" +
            "    return item\n" +
            "y = f()"));
    }

    // Bug #8: Field assignment to an undeclared variable used to crash with
    // NullReferenceException. Now produces a graceful FunnyRuntimeException
    // identifying the missing variable. (Parser still accepts the script —
    // the var becomes an implicit input shaped like the struct it's being
    // assigned to — but execution rejects the null target cleanly.)
    [Test]
    public void StmtBug8_FieldAssignUndefinedVar_GracefulError() {
        var rt = BuildLang("unknownVar.x = 1");
        Assert.Throws<FunnyRuntimeException>(() => rt.Run());
    }

    // Bug #9: `e` from `catch e:` no longer leaks usefully — it does still
    // resolve as an implicit input at script-level (parser permits it), but
    // reading a field on the null input now throws a graceful
    // FunnyRuntimeException instead of NullReferenceException.
    [Test]
    public void StmtBug9_CatchVarLeaks_GracefulError() {
        var rt = BuildLang(
            "x = try:\n" +
            "    oops('bad')\n" +
            "catch e:\n" +
            "    0\n" +
            "y = e.message");
        Assert.Throws<FunnyRuntimeException>(() => rt.Run());
    }

    // Bug #10: Function body with two `return` branches of incompatible
    // types (`return 1` and `return 'hi'`) crashed with InvalidCastException.
    // Fixed by binding every ReturnSyntaxNode's expression type to the
    // enclosing function's return slot in TIC — all return paths unify via
    // LCA so a mixed return-type function now produces an `Any`-typed result
    // instead of crashing.
    [Test]
    public void StmtBug10_MixedReturnTypes_AnyUnified() {
        var rt = BuildLang(
            "fun mixed(b):\n" +
            "    if b: return 1\n" +
            "    return 'hi'\n" +
            "y = mixed(true)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void StmtBug10_MixedReturnTypes_FalseBranchOk() {
        var rt = BuildLang(
            "fun mixed(b):\n" +
            "    if b: return 1\n" +
            "    return 'hi'\n" +
            "y = mixed(false)");
        rt.Run();
        Assert.AreEqual("hi", rt["y"].Value.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MODERATE — wrong value, missing validation, missing feature
    // ═══════════════════════════════════════════════════════════════════════

    // Bug #11: Multi-line `fun` body that falls off the end without a
    // `return` now returns `none` (Statements.md §Functions/Multi-line).
    // Fix in ConcreteUserFunction.Calc — if Expression is a BlockExpressionNode
    // and no ReturnSignal surfaced, return FunnyNone instead of the body's
    // last expression value. Expression-form `f(x) = expr` is unchanged.
    [Test]
    public void StmtBug11_FunNoReturn_ReturnsNone() {
        var rt = BuildLang(
            "fun mute(x):\n" +
            "    z = x * 2\n" +
            "y = mute(5)");
        rt.Run();
        // After the StmtBug41/42/46 fix, fall-off is reflected in the type
        // system: y's type is Optional(Int32). The Optional output converter
        // surfaces FunnyNone as CLR `null`.
        Assert.IsNull(rt["y"].Value);
    }

    // Bug #12: Function declared `-> int` with a path that has no `return`
    // silently produces the default int value (0). Should be a compile error
    // (fun signature claims int but actually returns none).
    [Test]
    public void StmtBug12_AnnotatedReturnType_MissingReturn_SilentDefault() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f(b: bool) -> int:\n" +
            "    if b: return 1\n" +
            "y = f(false)"));
    }

    [Test]
    public void StmtBug12_AnnotatedReturnType_BothPathsReturn_Ok() {
        var rt = BuildLang(
            "fun f(b: bool) -> int:\n" +
            "    if b: return 1\n" +
            "    return 2\n" +
            "y = f(false)");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    [Test]
    public void StmtBug12_AnnotatedOptionalReturnType_MissingPath_Ok() {
        // Declaring `-> int?` accepts a missing-return path — the optional
        // type lets `none` legitimately appear as the fallback.
        Assert.DoesNotThrow(() => Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildLang(
                "fun f(b: bool) -> int?:\n" +
                "    if b: return 1\n" +
                "y = f(false)"));
    }

    // Bug #13: `if` used as expression without `else` is accepted; produces
    // the type-default of the branch value when the condition is false.
    // Spec §if/elif/else line 169: "As an expression `if` requires `else`."
    [Test]
    public void StmtBug13_IfAsExpression_MissingElse() {
        Assert.Throws<FunnyParseException>(() => BuildLang("x = 5\ny = if x > 0: 1"));
    }

    // Bug #14: `when` used as expression without `else` is accepted; returns
    // `none` when no arm matches. Spec §when: "As expression — requires `else`."
    [Test]
    public void StmtBug14_WhenAsExpression_MissingElse() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "x = 5\n" +
            "y = when x:\n" +
            "    1: 'one'"));
    }

    // Bug #15: Extension function (`fun x.double():`) callable via direct
    // call `double(21)`. Spec §Extension line 112: "callable only via piped
    // syntax." Breaks the dual-namespace promise of extension form.
    [Test]
    public void StmtBug15_ExtensionFn_CallableDirect() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun x.double():\n" +
            "    return x * 2\n" +
            "y = double(21)"));
    }

    [Test]
    public void StmtBug15_ExtensionFn_PipedStillWorks() {
        var rt = BuildLang(
            "fun x.double():\n" +
            "    return x * 2\n" +
            "y = 21.double()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    // Bug #16: Spec's lambda syntax `fun: expr` (implicit `it`) does not parse.
    // Statements.md §Lambdas line 139: "`fun: expr` for implicit `it`..."
    // Only `rule it * 2` works.
    [Test]
    public void StmtBug16_FunLambda_ImplicitIt_NotParsed() {
        var rt = BuildLang("y = [1,2,3].map(fun: it * 2)");
        rt.Run();
        Assert.AreEqual(new[] { 2, 4, 6 }, rt["y"].Value);
    }

    // Bug #17: Spec's lambda syntax `fun(args): expr` does not parse either.
    // Statements.md §Lambdas line 139: "`fun(args): expr` for named parameters".
    [Test]
    public void StmtBug17_FunLambda_NamedArgs_NotParsed() {
        var rt = BuildLang("y = [1,2,3].fold(0, fun(acc, x): acc + x)");
        rt.Run();
        Assert.AreEqual(6, rt["y"].Value);
    }

    // Bug #18: Typed-receiver extension `fun x:T.method()` does not parse.
    // The spec example in Statements.md §Extension uses this form.
    [Test]
    public void StmtBug18_TypedReceiverExtension_NotParsed() {
        var rt = BuildLang(
            "fun x:real.tripple() -> real:\n" +
            "    return x * 3\n" +
            "y = 21.tripple()");
        rt.Run();
        Assert.AreEqual(63, Convert.ToInt32(rt["y"].Value));
    }

    // Bug #19: Spec example `value = x ?? return none` fails to parse.
    // Statements.md §return-as-expression lines 254-258 shows this as the
    // canonical early-exit pattern.
    [Test]
    public void StmtBug19_CoalesceReturnNone_NotParsed() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildLang(
                "fun process(x:int?) -> int?:\n" +
                "    value = x ?? return none\n" +
                "    return value + 1\n" +
                "y = process(5)");
        rt.Run();
        Assert.AreEqual(6, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void StmtBug19_CoalesceReturnNone_NonePath() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildLang(
                "fun process(x:int?) -> int?:\n" +
                "    value = x ?? return none\n" +
                "    return value + 1\n" +
                "y = process(none)");
        rt.Run();
        // process(none) takes the `?? return none` branch — the resulting
        // optional carries no value. The CLR representation is null (the
        // converter exposes Optional<T>.None as null at the boundary).
        Assert.IsNull(rt["y"].Value);
    }

    // Bug #20: Optional return-type not lifted to T? when one branch returns
    // `none`. `result` typed as `Real` while holding `none` — type/value
    // mismatch. With explicit `-> real?` annotation it's correct.
    [Test]
    public void StmtBug20_OptionalReturnNotLifted() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildLang(
                "fun safeDiv(a, b):\n" +
                "    if b == 0: return none\n" +
                "    return a / b\n" +
                "y = safeDiv(10, 0)");
        rt.Run();
        // Should be Real? (Optional<Real>), not Real
        Assert.AreEqual(FunnyType.OptionalOf(FunnyType.Real), rt["y"].Type);
    }

    // Bug #22: Spec example for indented try/catch (Statements.md lines 281-285)
    // with `catch:` indented INSIDE `x = try:` fails to parse. Only the
    // alternative layout (catch at column 0, body indented) works.
    [Test]
    public void StmtBug22_TryCatch_SpecLayout_NotParsed() {
        var rt = BuildLang(
            "x = try:\n" +
            "        oops('fail')\n" +
            "    catch:\n" +
            "        0\n" +
            "y = x");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void StmtBug22_IfElse_SpecLayout_Parses() {
        var rt = BuildLang(
            "x = if true:\n" +
            "        1\n" +
            "    else:\n" +
            "        2");
        rt.Run();
        Assert.AreEqual(1, rt["x"].Value);
    }

    [Test]
    public void StmtBug22_When_SpecLayout_Parses() {
        var rt = BuildLang(
            "y = when 1:\n" +
            "        1: 'one'\n" +
            "        2: 'two'\n" +
            "    else: 'other'");
        rt.Run();
        Assert.AreEqual("one", rt["y"].Value.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MINOR — cosmetic / borderline behavior
    // ═══════════════════════════════════════════════════════════════════════

    // Bug #23: Anonymous output in lang mode is named `__stmt_0__` instead of
    // `out` (the name expression mode uses per Basics.md). Internal name
    // leaks to script consumers.
    [Test]
    public void StmtBug23_AnonymousOutputName_InLang() {
        var rt = BuildLang("2 + 3");
        rt.Run();
        // Expression mode produces `out`; lang mode now matches.
        Assert.IsNotNull(rt["out"]);
        Assert.AreEqual(5, rt["out"].Value);
    }

    // Bug #24: Statement-level `for` / `while` loops produce phantom
    // `__stmt_N__:none = none` entries in the output variable list — clutter
    // for clients consuming outputs.
    [Test]
    public void StmtBug24_StatementLoops_PhantomNoneOutputs() {
        var rt = BuildLang(
            "total = 0\n" +
            "for x in [1,2,3]:\n" +
            "    total += x");
        rt.Run();
        // Only `total` should be an output, no `__stmt_0__:none`.
        int statementOutputs = 0;
        foreach (var v in rt.Variables)
            if (v.Name.StartsWith("__stmt_")) statementOutputs++;
        Assert.AreEqual(0, statementOutputs);
    }

    // Bug #25: Reassignment to fully-incompatible type widens to `Any`
    // silently. Spec §Reassignment line 54: "Reassigning a value that doesn't
    // widen into a single type is a parse error." `Any` swallowing any pair
    // makes the constraint vacuous.
    [Test]
    public void StmtBug25_Reassign_IncompatibleTypeWidensToAny() {
        // x:int then x:text — should be a parse error, not Any.
        Assert.Throws<FunnyParseException>(() => BuildLang("x = 1\nx = 'hello'"));
    }

    [Test]
    public void StmtBug25_Reassign_ExplicitAnyAllowed() {
        // Explicit `:any` annotation makes widening intentional — must still work.
        var rt = BuildLang("x:any = 1\nx = 'hello'");
        rt.Run();
        Assert.AreEqual("hello", rt["x"].Value.ToString());
    }

    [Test]
    public void StmtBug25_Reassign_NumericWidening_Allowed() {
        // int → real is a legitimate single-type widen — must NOT be rejected.
        var rt = BuildLang("x = 1\nx = 2.5");
        rt.Run();
        Assert.AreEqual(2.5, rt["x"].Value);
    }

    // Bug #26: Single-line `fun f(): 42` form not supported. Statements.md
    // §Blocks line 9 says single-line and multi-line block forms "appear
    // everywhere a block is expected (function body, …)".
    [Test]
    public void StmtBug26_SingleLineFunBody_NotParsed() {
        var rt = BuildLang("fun f(): 42\ny = f()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void StmtBug26_SingleLineFunBody_WithArgs() {
        var rt = BuildLang("fun double(x): x * 2\ny = double(21)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }
}
