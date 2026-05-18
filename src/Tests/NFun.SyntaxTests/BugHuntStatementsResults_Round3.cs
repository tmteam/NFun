using System;
using System.Linq;
using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bug-hunt round 3 for statement (lang) mode. 300 iterations across 3 agents
/// (SIMPLE_AND_TRICKY, HELL_AND_NESTED, EDGE_AND_CREATIVE). 14 unique confirmed
/// bugs after de-duplication. All marked [Ignore] until fixed; expected
/// behaviour per Statements.md / Basics.md / Optionals.md.
/// </summary>
public class BugHuntStatementsResults_Round3 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    private static FunnyRuntime BuildLang(string script) => Funny.Hardcore.BuildLang(script);

    // ───────────────────────────────────────────────────────────────
    // StmtBug27 — `fun f(...): if c: return; return N` infers
    //   non-optional return type but value is `none` on the
    //   bare-return path. Type/value invariant violation.
    // Per Statements.md "Bare `return` returns `none`": the function
    // must be Int32? (or the union must be a type error).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug27_BareReturnMixed_TypeInvariantViolation() {
        var rt = BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "        return\n" +
            "    return 99\n" +
            "v = f(5)");
        // EXPECT: v has Optional type (or function rejected as ambiguous return).
        Assert.AreEqual(BaseFunnyType.Optional, rt["v"].Type.BaseType,
            "Type/value invariant violated: bare return value `none` cannot be Int32 (non-optional)");
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug28 — `try: 42 anyway: 99` (no catch) drops try-branch
    //   value, result becomes `none`. Per Statements.md, anyway's
    //   value is discarded, the try body's value is the result.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug28_TryAnyway_NoCatch_DropsTryValue() {
        var rt = BuildLang(
            "result = try:\n" +
            "        42\n" +
            "    anyway:\n" +
            "        99\n" +
            "out = result");
        rt.Run();
        Assert.AreEqual(42, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug29 — bare `return` in a function whose result flows
    //   into `??` crashes with NullReferenceException at runtime.
    //   Per Statements.md, bare return returns `none`; `none ?? x` is `x`.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug29_BareReturn_NullCoalesce_Crash() {
        var rt = BuildLang(
            "fun f():\n" +
            "    return\n" +
            "out = f() ?? 'was none'");
        Assert.DoesNotThrow(() => rt.Run());
        Assert.AreEqual("was none", rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug30 — `if cond: x` (no else) inside `fun` body is
    //   silently accepted as expression even though same form at
    //   script-top is correctly rejected as "if as expression
    //   requires else".
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug30_IfWithoutElseAsExpression_InsideFunBody_NotRejected() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f(x):\n" +
            "    y = if x > 0: 1\n" +
            "    return y\n" +
            "out = f(5)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug31 — `when n: arms` (no else) inside `fun` body is
    //   silently accepted as expression even though same form at
    //   script-top is correctly rejected.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug31_WhenWithoutElseAsExpression_InsideFunBody_NotRejected() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f(n):\n" +
            "    r = when n:\n" +
            "        1: 'one'\n" +
            "        2: 'two'\n" +
            "    return r\n" +
            "out = f(5)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug32 — reassignment to a type that doesn't widen into one
    //   type silently widens to `Any` inside `fun` body. At top-level
    //   this is correctly rejected (BugHunt round 2 #25).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug32_ReassignWidensToAny_InsideFunBody_NotRejected() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun f():\n" +
            "    x = 1\n" +
            "    x = 'hello'\n" +
            "    return x\n" +
            "out = f()"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug33 — assigning `none` to a field whose declared (named)
    //   type is non-optional is silently accepted. Per Optionals.md,
    //   "`none` cannot be assigned to a non-optional type".
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug33_NoneAssignToNonOptionalField_NotRejected() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "type box = {v: int}\n" +
            "p = box{v = 10}\n" +
            "p.v = none\n" +
            "out = p.v ?? -1"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug34 — indentation/tab-vs-space errors throw raw
    //   InvalidOperationException from IndentTokenizer instead of
    //   the FunnyParseException contract for compile-time issues.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug34_IndentError_WrongExceptionClass() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "fun foo():\n" +
            "    x = 1\n" +
            "   return x"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug35 — deep optional-chain through a `rule()->stream?`
    //   field call crashes with NRE. `?.` chains should propagate
    //   none safely through method calls.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug35_DeepOptionalChain_RuleReturnsOptional_Crash() {
        var rt = BuildLang(
            "type stream = {head:int, tail:rule()->stream?}\n" +
            "s1 = stream{head=1, tail=rule stream{head=2, tail=rule stream{head=3, tail=rule none}}}\n" +
            "out3 = s1.tail()?.tail()?.head ?? -1");
        Assert.DoesNotThrow(() => rt.Run());
        Assert.AreEqual(3, rt["out3"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug36 — local variables declared inside a multi-line
    //   lambda body (`f = fun(x): …`) leak into the enclosing
    //   scope. Per Statements.md scoping: lambda body is a fresh
    //   lexical scope.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug36_LambdaLocal_LeaksToOuterScope() {
        var rt = BuildLang(
            "f = fun(x: int):\n" +
            "    y = x + 1\n" +
            "    y * 2\n" +
            "out = f(5)");
        Assert.IsFalse(rt.Variables.Any(v => v.Name == "y"),  // System.Linq.Any over IReadOnlyList<IFunnyVar>
            "Lambda-local `y` must not be visible at script scope");
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug37 — `e?.nums.sort()[0] ?? -1` crashes when `e` is
    //   `none`. The `?.` should propagate none through subsequent
    //   `[idx]` indexing too. Currently runtime tries to index
    //   `FunnyNone` as array → InvalidCastException.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug37_OptionalChain_Suffix_Indexing_Crash() {
        var rt = BuildLang(
            "type data = {nums:int[]}\n" +
            "e:data? = none\n" +
            "out = e?.nums.sort()[0] ?? -1");
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug38 — `arr[0](10)` (indexed call) is mis-parsed as
    //   `arr[0]` (function) followed by `(10)` (separate anonymous
    //   output). The call should index then apply the function.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug38_IndexedCall_MisParsedAsSeparateStatements() {
        var rt = BuildLang(
            "arr = [rule it + 1, rule it * 2, rule it - 3]\n" +
            "out = arr[0](10)");
        rt.Run();
        Assert.AreEqual(11, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug39 — multi-line `fun name(args):` form rejects default
    //   parameter values that the single-line `name(args) = expr`
    //   form accepts. Spec does not document this asymmetry.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug39_MultiLineFun_DefaultParam_Rejected() {
        var rt = BuildLang(
            "fun greet(name, greeting='Hello', punct='!'):\n" +
            "    return concat(concat(concat(greeting, ' '), name), punct)\n" +
            "out = greet('world')");
        rt.Run();
        Assert.AreEqual("Hello world!", rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug40 — single-line `when` arm rejects assignment
    //   statements (`1: result = 100`) while single-line `if`
    //   accepts them. Multi-line `when` arm works fine.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug40_WhenArm_AssignmentStatement_Rejected() {
        var rt = BuildLang(
            "x = 5\n" +
            "result = 0\n" +
            "when x:\n" +
            "    1: result = 100\n" +
            "    2: result = 200\n" +
            "out = result");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }
}
