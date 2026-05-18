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
    public void StmtBug73_MissingSeparator_NowRejected() {
        // Per Basics.md §Nfun script L52: "Each of these elements begins with
        // a new line. In this case, symbol `;` is the full equivalent of a
        // line break." The lang-mode parser's loop now requires NewLine, `;`,
        // EOF, or block terminator (Dedent) after each statement.
        Assert.Throws<FunnyParseException>(() => BuildLang("y = 5 z = 6"));
    }

    // 73b — Same enforcement inside indented function blocks.
    [Test]
    public void StmtBug73_MissingSeparatorInBlock_Rejected() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f():\n" +
            "    a = 1 b = 2\n" +
            "    return a + b\n" +
            "out = f()"));
    }

    // 73c — Controls: `;` and newline both accepted.
    [Test]
    public void StmtBug73_Control_SemicolonAccepted() {
        var rt = BuildLang("y = 5; z = 6");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
        Assert.AreEqual(6, rt["z"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug74 — Type-annotation `y:int` AFTER an existing `y = 5`
    //   crashes with raw .NET InvalidOperationException "Sequence
    //   contains no matching element" from LINQ. Should be a clean
    //   FunnyParseException (analogous to FU879 "already declared").
    //   Reproduces for any primitive or composite type-annotation.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug74_TypeAnnotationAfterInit_CleanError() {
        // R6 reproduced as raw .NET InvalidOperationException ("Sequence
        // contains no matching element") from LINQ. After subsequent fixes,
        // this surfaces as a clean FunnyParseException FU879 "Variable y is
        // already declared" — the documented analogous error.
        Assert.Throws<FunnyParseException>(() => BuildLang("y = 5\ny:int"));
    }

    // 74b — Composite type variant: same family. Surfaces as FU740
    // (type mismatch on the original equation, since the late annotation
    // narrows the variable's apparent type) — also a clean parse error,
    // not a raw .NET exception.
    [Test]
    public void StmtBug74_CompositeTypeAnnotation_CleanError() {
        Assert.Throws<FunnyParseException>(() => BuildLang("y = 5\ny:int[]"));
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
    public void StmtBug75_MixedReturn_LiteralVsParam_CleanParseError() {
        // R6 reproduced this as a raw .NET InvalidCastException at runtime
        // (the test's original failure mode). After subsequent MR-series
        // fixes (notably the TicTypesConverter completion in MR10/MR11),
        // TIC now detects the incompatible return paths during Destruction
        // and surfaces a clean FunnyParseException at build time. The
        // raw-crash bug is gone; a typed parse exception is the documented
        // acceptable outcome per the original ticket.
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f(x):\n" +
            "    if x == 0: return 'a'\n" +
            "    return x\n" +
            "out = f(5)"));
    }

    // 75b — Follow-up of the SAME family (mixed literal/param return paths)
    //   but with bool↔int C-style conversion in the mix. The text variant
    //   above errors cleanly because NFun has no Char[] ↔ Int LCA. Here
    //   TIC infers return type = Bool from the `return true` branch and
    //   propagates that as an upper bound on x; the call `f(5)` then ought
    //   to either:
    //     (a) widen the return slot to Any (or apply numeric→bool C-style
    //         conversion at the runtime return point so the slot holds
    //         a Bool), OR
    //     (b) reject at build time like the text variant.
    //
    //   Instead the function compiles with signature `f(int):Bool`, but at
    //   runtime `return x` writes the raw Int32 value `5` into a Bool slot.
    //   Consumers reading `rt["out"].Value` typed against the declared
    //   `Bool` get `InvalidCastException` — same root-cause family as the
    //   original StmtBug75 (TIC ignores the param branch's contribution to
    //   the return slot's LCA).
    [Test]
    public void StmtBug75b_MixedReturn_BoolVsParam_TypeValueMatch() {
        // After fix: return-statement applies a cast to the function's
        // declared return type when expr type differs. For this bool/int
        // pattern, x=5 → true (C-style numeric→bool: non-zero = true) is
        // stored in the Bool slot. Consumer reads true: bool, no crash.
        var rt = BuildLang(
            "fun f(x):\n" +
            "    if x == 0: return true\n" +
            "    return x\n" +
            "out = f(5)");
        rt.Run();
        var declaredType = rt["out"].Type;
        var actualValue = rt["out"].Value;
        Assert.AreEqual(NFun.Types.BaseFunnyType.Bool, declaredType.BaseType);
        Assert.IsInstanceOf<bool>(actualValue);
        Assert.AreEqual(true, actualValue); // 5 != 0 → true
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug76 — `fun f(...xs, b=10):` form silently drops one
    //   positional argument when a keyword-only param follows varargs.
    //   `fun f(...xs, b=10): f(1,2,3)` makes xs = [2,3] (loses 1).
    //   Classic-form `f(...xs, b=10) = ...` handles this correctly.
    //   CRITICAL: silent data loss in user-facing function dispatch.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug76_FunFormVarargsKwarg_NoArgDrop() {
        var rt = BuildLang(
            "fun f(...xs, b=10):\n" +
            "    return b + xs.sum()\n" +
            "out = f(1, 2, 3)");
        rt.Run();
        Assert.AreEqual(16, rt["out"].Value); // 10 + 1 + 2 + 3
    }

    [Test]
    public void StmtBug76_FunFormVarargsKwarg_FewerArgs() {
        var rt = BuildLang(
            "fun f(...xs, b=10):\n" +
            "    return b + xs.sum()\n" +
            "out = f(1, 2)");
        rt.Run();
        Assert.AreEqual(13, rt["out"].Value); // 10 + 1 + 2
    }

    [Test]
    public void StmtBug76_FunFormVarargsKwarg_ExplicitKwarg() {
        var rt = BuildLang(
            "fun f(...xs, b=10):\n" +
            "    return b + xs.sum()\n" +
            "out = f(1, 2, 3, b=100)");
        rt.Run();
        Assert.AreEqual(106, rt["out"].Value); // 100 + 1 + 2 + 3
    }

    [Test]
    public void StmtBug76_FunForm_RequiredAfterSpread_Rejected() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f(...xs, b):\n" +
            "    return b + xs.sum()\n" +
            "out = f(1, 2, 3)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug77 — `fun:` implicit-`it` lambda rejects a block body.
    //   `fun(args):` block body works (Round 5 #62 fix); `fun:` block
    //   body still emits FU520 "Anonymous fun body is missing".
    //   Per Statements.md §Lambdas L141: "The body may itself be a
    //   block." applies to both forms.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug77_FunImplicitIt_BlockBody_Accepted() {
        var rt = BuildLang(
            "y = [1,2,3].map(fun:\n" +
            "    return it * 2)");
        rt.Run();
        var y = (System.Collections.IEnumerable)rt["y"].Value;
        var enumerator = y.GetEnumerator();
        enumerator.MoveNext(); Assert.AreEqual(2, enumerator.Current);
        enumerator.MoveNext(); Assert.AreEqual(4, enumerator.Current);
        enumerator.MoveNext(); Assert.AreEqual(6, enumerator.Current);
    }

    [Test]
    public void StmtBug77_FunImplicitIt_MultiStatementBlock() {
        var rt = BuildLang(
            "y = [1,2,3].map(fun:\n" +
            "    a = it * 2\n" +
            "    return a + 1)");
        rt.Run();
        var y = (System.Collections.IEnumerable)rt["y"].Value;
        var enumerator = y.GetEnumerator();
        enumerator.MoveNext(); Assert.AreEqual(3, enumerator.Current);
        enumerator.MoveNext(); Assert.AreEqual(5, enumerator.Current);
        enumerator.MoveNext(); Assert.AreEqual(7, enumerator.Current);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug78 — Leading binary operator on next line inside `()` /
    //   `[]` brackets fails to continue the expression. Trailing-op
    //   form works (`1 +\n  2`); leading-op form rejects with FU606
    //   "expression is missed before '+'". Per IndentRules.md §8
    //   L130-147: "Indentation inside brackets is ignored (free-form)."
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug78_NlBeforeLeadingOp_InsideBrackets_Rejected() {
        var rt = BuildLang(
            "y = (\n" +
            "  1\n" +
            "  + 2\n" +
            ")");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug79 — `fun` form accepts required parameter AFTER a
    //   default-valued parameter, silently. Classic `f(a, b=10, c) =`
    //   form correctly rejects with FU420. Per Basics.md §Default
    //   values L539: "Required args must come before defaults."
    //   Inconsistent across the two definition forms.
    // ───────────────────────────────────────────────────────────────
    [Test]
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
    public void StmtBug80_CatchE_ReturnEMessage_NoStackOverflow() {
        // Fix: extend StagesExtension.Invoke's visited-pair guard fast-path
        // to also engage when either node carries StateOptional. The
        // WrapDescendantInOptional / WrapAncestorInOptional unwrap-then-Invoke
        // pattern can re-enter the same (nodeA, nodeB) pair when state
        // mutations re-establish the original Optional × non-Optional shape;
        // the guard's coinductive return is the termination signal. Without
        // this, `try: ... catch e: return e.message` looped infinitely on
        // (V0:opt(V2), arr(Ch)).
        var rt = BuildLang(
            "fun f():\n" +
            "    try:\n" +
            "        oops('x')\n" +
            "    catch e:\n" +
            "        return e.message\n" +
            "out = f()");
        rt.Run();
        // Run completed; out is typed Any? (Optional Any) with the error
        // message as runtime value.
        Assert.IsNotNull(rt["out"].Value);
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
    public void StmtBug84_IdentityThroughNone_NoStackOverflow() {
        // Same root cause as StmtBug80 — WrapDescendantInOptional /
        // WrapAncestorInOptional were cycling on Optional × non-Optional
        // node pairs because the visited-pair guard fast-path skipped the
        // Optional case when _isRecursion=false. StmtBug80's fix (extend
        // the fast-path to engage when either node carries StateOptional)
        // also resolves this identity-through-none pattern: TIC now
        // terminates with `out:Any? = 5`.
        var rt = BuildLang(
            "fun f(x):\n" +
            "    if x == none: return none\n" +
            "    return x\n" +
            "out = f(5)");
        rt.Run();
        Assert.AreEqual(5, rt["out"].Value);
    }
}
