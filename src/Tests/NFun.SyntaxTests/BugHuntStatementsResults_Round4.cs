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
/// Bug-hunt round 4 for statement (lang) mode. 300 iterations across 3 agents
/// (SIMPLE_AND_TRICKY, HELL_AND_NESTED, EDGE_AND_CREATIVE), after rounds 2/3 fixes
/// were already in place. 20 unique confirmed bugs after de-duplication.
/// All marked [Ignore] until fixed; expected behaviour per Statements.md /
/// Basics.md / Optionals.md.
/// </summary>
public class BugHuntStatementsResults_Round4 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    private static FunnyRuntime BuildLang(string script) => Funny.Hardcore.BuildLang(script);

    // ───────────────────────────────────────────────────────────────
    // StmtBug41 — Fn body falls off without `return` only on some paths:
    //   `fun f(x): if x>0: return x` for x≤0 returns none, but type is Int32.
    //   Distinct from StmtBug27 (which had explicit `bare return; return N`).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug41_ImplicitFallOff_OneBranchReturns_TypeMismatch() {
        var rt = BuildLang(
            "fun f(x):\n" +
            "    if x > 0:\n" +
            "        return x\n" +
            "y = f(-5)");
        Assert.AreEqual(BaseFunnyType.Optional, rt["y"].Type.BaseType,
            "Type/value invariant: implicit fall-off path produces none → must be Optional");
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug42 — Same fall-off as #41, but for non-primitive types
    //   (text/array). Wrong static type causes runtime InvalidCastException.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug42_FallOff_NonPrimitive_CrashesAtRuntime() {
        Assert.DoesNotThrow(() => BuildLang(
            "fun f():\n" +
            "    z = 'hi'\n" +
            "out = f()").Run());
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug43 — Top-level `if` (or `when`, or bare expression) block
    //   silently clobbers explicit `out = …`. Round-2 #24 fixed phantom
    //   outputs for for/while only — if/when still leak.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug43_TopLevelIf_ClobbersExplicitOut() {
        var rt = BuildLang(
            "out = 7\n" +
            "if true:\n" +
            "    z = 100");
        rt.Run();
        Assert.AreEqual(7, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug44 — Generic user fn called via pipe-forward crashes
    //   with internal NFunImpossibleException MJ78 ("Function f`1 was not
    //   found"). Direct call works; typed-arg variant works; only generic
    //   user fn via pipe path is broken.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug44_GenericUserFn_ViaPipe_Crashes() {
        var rt = BuildLang(
            "fun double(x):\n" +
            "    return x * 2\n" +
            "out = 5.double()");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug45 — Multi-line `fun name(...args):` form rejects the
    //   varargs `...` prefix that the short-form `name(...args) = expr`
    //   accepts. Same asymmetry as StmtBug39 (defaults).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug45_MultiLineFun_Varargs_Rejected() {
        var rt = BuildLang(
            "fun f(...xs):\n" +
            "    return xs.sum()\n" +
            "out = f(1, 2, 3, 4)");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug46 — Annotated `-> int?` with missing-return path returns
    //   the type's default `0` instead of `none`. StmtBug12 only asserted
    //   DoesNotThrow; value was never checked.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug46_AnnotatedOptional_FallOff_ReturnsTypeDefault() {
        var rt = BuildLang(
            "fun f(x: int) -> int?:\n" +
            "    if x > 0: return x\n" +
            "out = f(-5)");
        rt.Run();
        // `Int32?` maps to CLR nullable — FunnyNone surfaces as `null`.
        Assert.IsNull(rt["out"].Value,
            "Fall-off path must produce `none`, not the type's default value");
    }

    // ───────────────────────────────────────────────────────────────
    // (StmtBug47 reclassified as false positive — `Any` legitimately
    //  contains `none`, type-widening is a style nit, not a spec violation.)
    // ───────────────────────────────────────────────────────────────

    // ───────────────────────────────────────────────────────────────
    // StmtBug48 — Recursive named-type ctor inside fn body that
    //   references the same struct's optional field via `!`
    //   (`tree{v=2, l=t!.l}`) triggers TIC assertion failure
    //   `Node is already solved`.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug48_RecursiveNamedCtor_OptionalField_TicAssertion() {
        Assert.DoesNotThrow(() => BuildLang(
            "type tree = {v: int, l: tree? = none}\n" +
            "fun ins(t:tree?):\n" +
            "    if t == none: return tree{v=99}\n" +
            "    return tree{v=2, l=t!.l}\n" +
            "out = ins(none)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug49 — Recursive fn over recursive named struct that ALSO
    //   takes a higher-order `rule(int)->int` arg crashes with
    //   `Circular ancestor 0`. Either ingredient alone is fine.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug49_RecFn_RecStruct_HOFArg_CircularAncestor() {
        Assert.DoesNotThrow(() => BuildLang(
            "type t = {v: int, kids: t[] = []}\n" +
            "fun walk(s: t, f: rule(int)->int):\n" +
            "    a = f(s.v)\n" +
            "    for k in s.kids:\n" +
            "        a += walk(k, f)\n" +
            "    return a\n" +
            "out = walk(t{v=1, kids=[t{v=2}, t{v=3}]}, rule it * 10)"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug50 — `v?.field ?? default` doesn't unwrap composite-type
    //   field. With `field: int?` works (int?+int → int); with
    //   `field: int[]?` (composite) result type stays int[]? instead of
    //   collapsing to int[].
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug50_NullCoalesce_CompositeOptional_NotUnwrapped() {
        var rt = BuildLang(
            "type s = {opt: int[]?}\n" +
            "v = s{opt = [1,2]}\n" +
            "r = v?.opt ?? []\n" +
            "out = r");
        // Stage B.3.3: lang-mode `int[]` field annotation now maps to
        // `array<int>` (BaseFunnyType.MutableArray). The bare `[]` in the
        // right operand of `??` has no annotation context and defaults to
        // `list<T>` — by the lattice cross-kind merge identity rule
        // (`Specs/Tic/ConstructorLattice.md` §Cross-kind merge identity),
        // the narrower side (List) wins.
        // What still matters in this regression: result is NOT an Optional
        // (the `??` unwrap fires) and is some Array-branch kind.
        var resultKind = rt["r"].Type.BaseType;
        Assert.IsTrue(
            resultKind == BaseFunnyType.List || resultKind == BaseFunnyType.MutableArray,
            "?? must unwrap the Optional layer — result must be a concrete Array-branch kind, "
            + $"got {resultKind}");
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug51 — Two or more field mutations on an anonymous-typed
    //   struct variable crash at execution with NullReferenceException.
    //   Single mutation works; named-struct-typed variable is fine.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug51_MultipleFieldMutations_AnonymousStruct_Crash() {
        var rt = BuildLang(
            "p = {x = 0}\n" +
            "p.x = 1\n" +
            "p.x = 2\n" +
            "out = p.x");
        Assert.DoesNotThrow(() => rt.Run());
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug52 — Named-type constructor as default value of a fn
    //   parameter crashes the elaborator with NFunImpossibleException
    //   "NamedTypeConstructorSyntaxNode should be removed during
    //   elaboration".
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug52_NamedCtor_AsDefaultParam_ElaboratorCrash() {
        Assert.DoesNotThrow(() => BuildLang(
            "type p = {x: int, y: int}\n" +
            "fun mid(a: p = p{x=0, y=0}): a.x + a.y\n" +
            "out = mid()"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug53 — Multi-line lambda body's return type silently widens
    //   to `Any` instead of inferring from body. Single-line lambda
    //   correctly infers.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug53_MultiLineLambda_ReturnType_WidensToAny() {
        var rt = BuildLang(
            "f = fun(x):\n" +
            "    return x * 2\n" +
            "out = f(5)");
        Assert.AreEqual(BaseFunnyType.Int32, rt["out"].Type.BaseType,
            "Multi-line lambda body returns Int32, output should not widen to Any");
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug54 — `return try: oops(...) catch: return -1` leaks the
    //   internal ReturnSignal sentinel as the function's output value.
    //   The inner catch's `return -1` produces the sentinel which the
    //   outer `return` then passes through verbatim.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug54_ReturnTryCatchWithInnerReturn_LeaksReturnSignal() {
        var rt = BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        oops('e')\n" +
            "    catch:\n" +
            "        return -1\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug55 — `return x ?? return -1` (return-as-expression in
    //   `??` RHS, wrapped in outer return) leaks ReturnSignal sentinel.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug55_ReturnNullCoalesceReturn_LeaksReturnSignal() {
        var rt = BuildLang(
            "fun f(x):\n" +
            "    return x ?? return -1\n" +
            "out = f(none)");
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug56 — `return return X` leaks ReturnSignal sentinel.
    //   Should either be parse-rejected or correctly produce X.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug56_DoubleReturn_LeaksReturnSignal() {
        var rt = BuildLang(
            "fun f(): return return 5\n" +
            "out = f()");
        rt.Run();
        // Either reject at parse, or produce 5 — but not leak the sentinel object.
        Assert.IsTrue(rt["out"].Value is int v && v == 5,
            "Either parse-reject or produce the inner return's value");
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug57 — `return X` used inside a binary op (`5 + return 3`)
    //   feeds ReturnSignal into the operator → cast exception at runtime.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug57_ReturnInBinaryOp_CastCrash() {
        var rt = BuildLang(
            "fun f():\n" +
            "    x = 5 + return 3\n" +
            "out = f()");
        // Either parse-reject or short-circuit on ReturnSignal — but not throw cast.
        Assert.DoesNotThrow(() => rt.Run());
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug58 — Optional parameter with non-none default value
    //   (e.g. `x: int? = 5`) crashes at build with
    //   "Complex constant type is not supported". `x: int? = none` works.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug58_OptionalParam_NonNoneDefault_Crash() {
        var rt = BuildLang(
            "fun f(x: int? = 5): x ?? -1\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(5, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug59 — Field assignment to incompatible type silently widens
    //   to Any. Round-2 #25 forbids this for variable reassignment;
    //   round-3 #32 covered fn-body vars; this is the field/mutation path.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug59_FieldAssign_IncompatibleType_SilentAnyWiden() {
        Assert.Throws<FunnyParseException>(() => BuildLang(
            "p = {x = 1}\n" +
            "p.x = 'hello'\n" +
            "out = p.x"));
    }

    // ───────────────────────────────────────────────────────────────
    // StmtBug60 — Single-line `try: V catch: W` form is rejected by
    //   parser despite IndentRules.md/Statements.md saying single-line
    //   blocks work "everywhere a block is expected" including
    //   try/catch/anyway.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void StmtBug60_SingleLineTryCatch_NotParsed() {
        var rt = BuildLang(
            "x = try: 5 catch: -1\n" +
            "out = x");
        rt.Run();
        Assert.AreEqual(5, rt["out"].Value);
    }
}
