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
/// Round 6 of automated statement-mode (lang) bug hunting.
/// 12 unique confirmed bugs after de-duplication (3 agents × ~100 iterations:
/// SIMPLE_AND_TRICKY, HELL_AND_NESTED, EDGE_AND_CREATIVE). All marked
/// [Ignore] until fixed; expected behaviour per Statements.md / Basics.md /
/// IndentRules.md / Optionals.md / NamedTypes.md.
/// </summary>
public class BugHuntStatementsResults_Round6 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    private static FunnyRuntime BuildLang(string script) => Funny.Hardcore.BuildLang(script);

    // ───────────────────────────────────────────────────────────────
    // StmtBug73 — Missing statement separator silently parses as two
    //   statements: `y = 5 z = 6` accepted (should require `\n` or `;`
    //   per Basics.md §Nfun script L52). Reproduces at top level and
    //   inside fn bodies.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug73: missing statement separator (`y = 5 z = 6`) silently parses")]
    public void StmtBug73_MissingSeparator_SilentlyAccepted() {
        Assert.Throws<FunnyParseException>(() => BuildLang("y = 5 z = 6"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug74 — Type-annotation `y:int` AFTER an existing `y = 5`
    //   crashes with raw .NET InvalidOperationException "Sequence
    //   contains no matching element" from LINQ. Should be a clean
    //   FunnyParseException (analogous to FU879 "already declared").
    //   Reproduces for any primitive or composite type-annotation.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug74: y:T after `y=...` crashes with raw LINQ InvalidOperationException")]
    public void StmtBug74_TypeAnnotationAfterInit_LinqCrash() {
        Assert.Throws<FunnyParseException>(() => BuildLang("y = 5\ny:int"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug75 — Mixed return-types with one return being the function
    //   parameter (untyped/generic) crashes at runtime with raw
    //   InvalidCastException. StmtBug10's LCA-across-return-paths fix
    //   handles literal/literal but misses literal/parameter pairings —
    //   TIC infers return type from the literal branch only, the param
    //   branch then violates that type.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug75: fn with `return literal` and `return param` paths crashes (incomplete StmtBug10)")]
    public void StmtBug75_MixedReturn_LiteralVsParam_CastCrash() {
        // Either TIC widens to Any (and `out:Any = 5`) OR a clean
        // FunnyParseException — not a raw .NET InvalidCastException.
        var rt = BuildLang(
            "fun f(x):\n" +
            "    if x == 0: return 'a'\n" +
            "    return x\n" +
            "out = f(5)");
        Assert.DoesNotThrow(() => rt.Run());
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug76 — `fun f(...xs, b=10):` form silently drops one
    //   positional argument when a keyword-only param follows varargs.
    //   `fun f(...xs, b=10): f(1,2,3)` makes xs = [2,3] (loses 1).
    //   Classic-form `f(...xs, b=10) = ...` handles this correctly.
    //   CRITICAL: silent data loss in user-facing function dispatch.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug76: fun(...xs, b=N) silently drops one positional arg")]
    public void StmtBug76_FunFormVarargsKwarg_DropsPositional() {
        var rt = BuildLang(
            "fun f(...xs, b=10):\n" +
            "    return b + xs.sum()\n" +
            "out = f(1, 2, 3)");
        rt.Run();
        Assert.AreEqual(16, rt["out"].Value); // 10 + 1 + 2 + 3
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug77 — `fun:` implicit-`it` lambda rejects a block body.
    //   `fun(args):` block body works (Round 5 #62 fix); `fun:` block
    //   body still emits FU520 "Anonymous fun body is missing".
    //   Per Statements.md §Lambdas L141: "The body may itself be a
    //   block." applies to both forms.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug77: `fun:` implicit-`it` lambda rejects block body")]
    public void StmtBug77_FunImplicitIt_BlockBody_Rejected() {
        Assert.DoesNotThrow(() => BuildLang(
            "y = [1,2,3].map(fun:\n" +
            "    return it * 2)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug78 — Leading binary operator on next line inside `()` /
    //   `[]` brackets fails to continue the expression. Trailing-op
    //   form works (`1 +\n  2`); leading-op form rejects with FU606
    //   "expression is missed before '+'". Per IndentRules.md §8
    //   L130-147: "Indentation inside brackets is ignored (free-form)."
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug78: leading binary op after NL inside () rejected")]
    public void StmtBug78_NlBeforeLeadingOp_InsideBrackets_Rejected() {
        Assert.DoesNotThrow(() => BuildLang(
            "y = (\n" +
            "  1\n" +
            "  + 2\n" +
            ")"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug79 — `fun` form accepts required parameter AFTER a
    //   default-valued parameter, silently. Classic `f(a, b=10, c) =`
    //   form correctly rejects with FU420. Per Basics.md §Default
    //   values L539: "Required args must come before defaults."
    //   Inconsistent across the two definition forms.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug79: `fun` form accepts required-after-default param (classic form rejects)")]
    public void StmtBug79_FunForm_RequiredAfterDefault_NoValidation() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun outer(a, b=10, c):\n" +
            "    return a + b + c\n" +
            "out = outer(1, 2, 3)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug80 — `fun f(): try: ... catch e: return e.message` causes
    //   TIC stack overflow when the catch's return expression yields
    //   Char[] from `e.message`. Infinite recursion through
    //   StagesExtension.WrapDescendantInOptional → Invoke → InvokeCore
    //   → WrapDescendantInOptional. Spec example
    //   (Statements.md §Error handling L287-291) shows
    //   `catch e:\n  log(e.message)`.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug80: `catch e: return e.message` triggers TIC stack overflow")]
    public void StmtBug80_CatchE_ReturnEMessage_StackOverflow() {
        Assert.DoesNotThrow(() => BuildLang(
            "fun f():\n" +
            "    try:\n" +
            "        oops('x')\n" +
            "    catch e:\n" +
            "        return e.message\n" +
            "out = f()"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug81 — Function body whose last statement is a try/catch
    //   where both branches `return` concrete values has its inferred
    //   return type spuriously widened to Optional. Same family as
    //   StmtBug69 (return + unreachable widens) — the always-exits
    //   detector doesn't recognise TryBlockSyntaxNode as a terminator
    //   even when every branch returns.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug81: try/catch with both-branch-returns widens fn return to Optional")]
    public void StmtBug81_TryCatchBothReturn_WidensToOptional() {
        var rt = BuildLang(
            "fun f():\n" +
            "    try:\n" +
            "        return 1\n" +
            "    catch:\n" +
            "        return 2\n" +
            "out = f()");
        Assert.AreEqual(BaseFunnyType.Int32, rt["out"].Type.BaseType);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug82 — Struct or named-type constructor literal directly
    //   followed by `.field` is rejected with FU725 "Element '{…}' has
    //   no fields". `s{v=5}.v`, `{v=5}.v`, `({v=5}).v` all fail; the
    //   `?.` variant `s{v=5}?.v` works (Optionals.md L196). Assigning
    //   to an intermediate variable also works.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug82: struct/named ctor literal followed by `.field` rejected")]
    public void StmtBug82_CtorLiteral_DotField_Rejected() {
        var rt = BuildLang(
            "type s = {v: int}\n" +
            "out = s{v=5}.v");
        rt.Run();
        Assert.AreEqual(5, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug83 — Re-annotating a variable with an incompatible
    //   composite type crashes TIC with assertion "Node is already
    //   solved" at TicNode.cs:163. `x:int = 5; x:real = 'hi'` (primitive
    //   to primitive) gives a clean FU879; `x:int = 5; x:text = 'hi'`
    //   (primitive to composite) crashes with the raw assertion.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug83: re-annotation with composite type crashes TIC assertion")]
    public void StmtBug83_ReannotateWithCompositeType_TicAssertion() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "x:int = 5\n" +
            "x:text = 'hi'\n" +
            "out = x"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug84 — Identity-through-none pattern on an untyped param
    //   causes infinite recursion in
    //   TicTypesConverter.BuildNamedTypeFromTicState → StackOverflow.
    //   Annotating the param (`x: int?`) fixes it; breaking identity
    //   (`return x + 1`) fixes it. Trigger: generic param + return-x +
    //   return-none.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("StmtBug84: identity-on-generic-optional pattern → stack overflow")]
    public void StmtBug84_IdentityThroughNone_StackOverflow() {
        Assert.DoesNotThrow(() => BuildLang(
            "fun f(x):\n" +
            "    if x == none: return none\n" +
            "    return x\n" +
            "out = f(5)"));
    }
}
