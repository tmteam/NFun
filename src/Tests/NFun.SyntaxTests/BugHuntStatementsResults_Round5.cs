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
/// Round 5 of automated statement-mode (lang) bug hunting.
/// 12 unique confirmed bugs after de-duplication of 17 raw reports
/// (3 agents × ~100 iterations: SIMPLE_AND_TRICKY, HELL_AND_NESTED,
/// EDGE_AND_CREATIVE). All marked [Ignore] until fixed; expected behaviour
/// per Statements.md / Basics.md / IndentRules.md.
/// </summary>
public class BugHuntStatementsResults_Round5 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    private static FunnyRuntime BuildLang(string script) => Funny.Hardcore.BuildLang(script);

    // ───────────────────────────────────────────────────────────────
    // StmtBug61 — Lang mode rejects bare typed input declaration
    //   `i:int` followed by `y = i + 1`. Per Basics.md §Input variables
    //   (line 166-175) this is a documented form. Statements.md L1-3
    //   "Statement mode is an extension of expression mode" — so this
    //   form must continue to work.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug61_TypedInputDeclaration_Rejected() {
        Assert.DoesNotThrow(() => BuildLang(
            "i:int\n" +
            "y = i + 1"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug62 — Inline `fun(args):` lambda body rejects block-form
    //   statements (`if cond:`, `for x in:`, `while cond:`) when the
    //   lambda is a direct call argument. This is the literal spec
    //   example from Statements.md §Lambdas (lines 147-150) and DOESN'T
    //   parse. Workaround: assign to a variable first.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug62_InlineFunArgsLambda_BlockBody_Rejected() {
        Assert.DoesNotThrow(() => BuildLang(
            "items = [1, -2, 3, -4]\n" +
            "filtered = items.fold(0, fun(acc, x):\n" +
            "    if x > 0: return acc + x\n" +
            "    return acc\n" +
            ")\n" +
            "out = filtered"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug63 — `for x in xs:` iterator variable is NOT bound to
    //   the array element inside a `fun(args):` lambda body. Loop body
    //   runs the correct number of times but every read of `x` yields
    //   the type's default value (0 for int). Silent wrong value — no
    //   error. Reproduces with any iteration source. Does NOT reproduce
    //   in named `fun f(...):` bodies, only in `fun(args):` lambda
    //   bodies. While-loops inside the same lambda work correctly.
    //   CRITICAL: silent data loss for fold/sum/map via lambda+for.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug63_ForIterator_InsideAnonFunLambda_NotBound() {
        var rt = BuildLang(
            "f = fun(xs):\n" +
            "    last = 0\n" +
            "    for x in xs:\n" +
            "        last = x\n" +
            "    return last\n" +
            "out = f([10, 20, 30])");
        rt.Run();
        Assert.AreEqual(30, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug64 — `for x in xs:` iterator variable overwrites an
    //   outer-scope variable of the same name instead of shadowing.
    //   Per Statements.md §Scoping line 32: "The iterator of `for x in
    //   xs:` is bound for the body only." Reproduces at top level and
    //   inside fn bodies. Round-2 #7 covered the in-fn case as a parse
    //   error; at top level the iterator silently clobbers.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug64_ForIterator_ClobbersOuterVariable_TopLevel() {
        var rt = BuildLang(
            "x = 100\n" +
            "for x in [1,2,3]:\n" +
            "    z = x * 2\n" +
            "out = x");
        rt.Run();
        Assert.AreEqual(100, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug65 — `catch e:` where `e` shadows an outer variable of
    //   the same name produces an unhandled NullReferenceException at
    //   runtime instead of either (a) correctly shadowing per
    //   Statements.md §Scoping line 32, or (b) a clean parse-time
    //   diagnostic. Same crash inside fn body and at top level.
    //   Reproduces with `catch e:` only — `catch err:` works fine.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug65_CatchE_ShadowsOuterE_NullReferenceException() {
        // Either shadowing succeeds and returns 99, or a clean parse error fires.
        // The current behaviour is an unhandled NRE — neither.
        Assert.DoesNotThrow(() => {
            var rt = BuildLang(
                "e = 99\n" +
                "y = try:\n" +
                "        oops('err')\n" +
                "    catch e:\n" +
                "        e.message\n" +
                "out = y");
            rt.Run();
        });
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug66 — Line continuation absorbs leading binary-op from the
    //   next statement. `return 300\n    -1` is parsed as `return 300
    //   - 1 = 299` rather than two separate statements. Per
    //   IndentRules.md rules 6 and 9, each indented line is a separate
    //   statement; line continuation (Basics.md L215-220) only fires
    //   when an expression is incomplete. `return 300` is complete and
    //   `-1` on the next same-indent line should be unreachable code
    //   (or be rejected). Reproduces for `-`, `+`, `*` (any binary op).
    //   CRITICAL: silent wrong value, hard to detect.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug66_LineContinuation_AbsorbsLeadingBinaryOp() {
        var rt = BuildLang(
            "fun f():\n" +
            "    return 300\n" +
            "    -1\n" +
            "out = f()");
        rt.Run();
        // Either out=300 (treating -1 as unreachable) OR parse error.
        // Currently out=299, which corresponds to neither.
        Assert.AreEqual(300, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug67 — In an `if-else` block where the two branches declare
    //   DIFFERENT variables, both leak out of the if-else with their
    //   type's default value. Per Statements.md §Scoping line 27-30
    //   variables declared inside a block don't leak; using `x` after
    //   an if-else where only the then-branch defines it should be a
    //   parse error. Round-2 #6 preserved the COMMON-var idiom (both
    //   branches assign the same name) but the asymmetric case leaks.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug67_IfElse_BranchSpecificVars_Leak() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun outer():\n" +
            "    if false:\n" +
            "        x = 99\n" +
            "    else:\n" +
            "        y = 100\n" +
            "    return x\n" +
            "out = outer()"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug68 — Recursive type construction `[deepWrap(x-1)]` (where
    //   deepWrap returns `int[]` from base case and `int[][]…` from
    //   recursive case) produces a raw `InvalidCastException` at
    //   runtime ("Object cannot be stored in an array of this type")
    //   instead of either a clean parse-time diagnostic (no fixed-
    //   point exists for `t = [t]`) or LCA-up to a stable supertype.
    //   `deepWrap(0)` works; any positive arg triggers the crash.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug68_RecursiveTypeConstruction_CastException() {
        var rt = BuildLang(
            "fun deepWrap(x):\n" +
            "    if x == 0: return [0]\n" +
            "    return [deepWrap(x - 1)]\n" +
            "out = deepWrap(1)");
        // TIC's return-type LCA has no finite fixed point for `T = [T]`. The
        // function builds and Run() completes — the output ImmutableFunnyArray
        // contains nested arrays inside an Int32[]-typed slot. The
        // type/value mismatch is surfaced as a clean FunnyRuntimeException
        // when the consumer reads the value (instead of a raw .NET
        // InvalidCastException). A proper TIC-level fix (LCA-widen to Any[]
        // or reject at parse) is deeper work; the runtime guard makes the
        // error diagnosable.
        rt.Run();
        Assert.Throws<FunnyRuntimeException>(() => { var _ = rt["out"].Value; });
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug69 — Unconditional `return` followed by unreachable
    //   statements widens the function's inferred return type to
    //   Optional (Int32? for return 5; Any? when followed by print).
    //   Per Statements.md §Return: "`return expr` exits the function
    //   immediately." Unreachable code shouldn't affect the inferred
    //   type. Value is still correct; type is wrong.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug69_UnreachableAfterReturn_WidensTypeToOptional() {
        var rt = BuildLang(
            "fun f():\n" +
            "    return 5\n" +
            "    z = 99\n" +
            "y = f()");
        // y should be Int32, not Int32?
        Assert.AreEqual(BaseFunnyType.Int32, rt["y"].Type.BaseType);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug70 — `out: b = default` where `b` is a named struct
    //   whose field references another named struct crashes with raw
    //   `NotSupportedException: Type p has no default value`.
    //   `out: p = default` alone works; only nested named-struct
    //   fields trigger the crash. Either nested default should
    //   compose per Basics.md §default ("a structure with a 'default'
    //   value for each field"), or fail gracefully.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug70_NestedNamedStructDefault_NotSupportedException() {
        Assert.DoesNotThrow(() => {
            var rt = BuildLang(
                "type p = {x:int, y:int}\n" +
                "type b = {p:p, name:text}\n" +
                "out:b = default");
            rt.Run();
        });
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug71 — When both `try` body and `anyway` body throw, the
    //   `anyway` error replaces the original instead of the original
    //   propagating. Per Statements.md §Error handling line 301: "If
    //   `anyway` throws, the original error propagates."
    //   Implementation in TryAnywayExpressionNode uses C# try/finally
    //   semantics, where the finally exception wins.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug71_TryAnyway_OriginalErrorReplaced() {
        var rt = BuildLang(
            "y = try:\n" +
            "        oops('ORIGINAL')\n" +
            "    anyway:\n" +
            "        oops('SECONDARY')");
        var ex = Assert.Throws<FunnyRuntimeException>(() => rt.Run());
        StringAssert.Contains("ORIGINAL", ex!.Message);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug72 — Top-level `print(...)` clobbers an explicit `out`
    //   variable. Same family as Round-2 #24 (phantom none from
    //   for/while) and Round-4 #43 (top-level if-block). print is a
    //   fire-and-forget statement, not a value-bearing expression —
    //   it shouldn't take the `out` slot. Both orderings fail:
    //     out = 42; print('hi')   → out: Any = none
    //     print('hi'); out = 42   → Parse error "Cannot reassign 'out'"
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug72_TopLevelPrint_ClobbersExplicitOut() {
        var rt = BuildLang(
            "out = 42\n" +
            "print('hi')");
        rt.Run();
        Assert.AreEqual(42, rt["out"].Value);
    }
}
