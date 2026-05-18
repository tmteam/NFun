using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on nfun-lang-v4 (round 11,
/// post MR10Bug1 fix). 3 agents × 50 iterations. 2 confirmed actionable bugs;
/// 1 critical (NRE on generic struct return) + 1 spec-violation (parser rejects
/// `else default` despite Basics.md showing it as a valid example).
/// Other 5 reports were false positives or already-known TIC limitations.
/// </summary>
public class BugHuntMasterRound11 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR11Bug1 — NullReferenceException on generic user-fn returning a
    //   struct literal that REUSES the input struct's field names:
    //
    //     f(p) = {a = p.a}
    //     out = f({a = 1})            # NullReferenceException at runtime
    //
    //   Reproduces with:
    //     f(p) = {a = p.a}                                — single shared field
    //     f(p) = {a = p.a, b = p.b}                       — multiple shared
    //     f(p) = {a = p.b, b = p.a}                       — swap pattern
    //     f(p) = {a = p.a + 0}                            — with arithmetic
    //
    //   Does NOT reproduce when:
    //     f(p) = {x = p.a}                                — different name
    //     f(p) = {a = p.a, c = 99}                        — extra new field
    //     f(p:{a:int}) = {a = p.a}                        — typed param
    //     x = f({a=1, b=2}); out = x.a + x.b              — via intermediate
    //
    //   Smoking gun: shared field name + generic param. Likely a
    //   StateStruct field-carrier identity issue when input and output
    //   structs share field names through the same generic.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR11Bug1_GenericFnReturnSharedField_NoCrash() {
        "f(p) = {a = p.a}\rout = f({a = 1})"
            .Calc().AssertResultHas("out", new { a = 1 });
    }

    [Test]
    public void MR11Bug1_GenericFnReturnTwoSharedFields_NoCrash() {
        "f(p) = {a = p.a, b = p.b}\rout = f({a = 1, b = 2})"
            .Calc().AssertResultHas("out", new { a = 1, b = 2 });
    }

    [Test]
    public void MR11Bug1_GenericFnReturnSwapFields_NoCrash() {
        "f(p) = {a = p.b, b = p.a}\rout = f({a = 1, b = 2})"
            .Calc().AssertResultHas("out", new { a = 2, b = 1 });
    }

    // 1d. Control: different output field name works.
    [Test]
    public void MR11Bug1_Control_DifferentFieldName_Works() {
        "f(p) = {x = p.a}\rout = f({a = 1})"
            .Calc().AssertResultHas("out", new { x = 1 });
    }

    // 1e. Control: typed param works.
    [Test]
    public void MR11Bug1_Control_TypedParam_Works() {
        "f(p:{a:int}) = {a = p.a}\rout = f({a = 1})"
            .Calc().AssertResultHas("out", new { a = 1 });
    }

    // ───────────────────────────────────────────────────────────────
    // MR11Bug2 — `default` keyword in if-expression else-branch fails
    //   to parse, despite Specs/Basics.md explicitly listing this exact
    //   pattern as a valid example (3 places: lines 438, 440, 518, 621):
    //
    //     a = if(1>2) true else default        # FU0 "requires an 'else' clause"
    //
    //   Reproduces with all variants:
    //     a = if(1>2) true else default
    //     a = if(1>2) 1 else default
    //     out = if(false) 1 else default
    //     a:int32 = if(1>2) 1 else default
    //     a = if(1>2) true else (default)      — even with parens
    //
    //   THEN-branch is fine:
    //     out = if(true) default else 1        # works → 0
    //
    //   Via intermediate variable works:
    //     y = default; out = if(false) 1 else y
    //
    //   Smoking gun: the `default` keyword in the immediate else-position
    //   confuses the if-expression parser into thinking there's no else.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR11Bug2_IfExprElseDefault_Works() {
        // Spec Basics.md:438 — `a = if(1>2) true else default #false`
        "a = if(1>2) true else default".Calc().AssertResultHas("a", false);
    }

    [Test]
    public void MR11Bug2_IfExprIntElseDefault_Works() {
        "out = if(false) 1 else default".Calc().AssertResultHas("out", 0);
    }

    // 2c. Control: default in then-branch works.
    [Test]
    public void MR11Bug2_Control_ThenDefault_Works() {
        "out = if(true) default else 1".Calc().AssertResultHas("out", 0);
    }

    // 2d. Control: via intermediate variable works.
    [Test]
    public void MR11Bug2_Control_ViaIntermediate_Works() {
        "y = default\rout = if(false) 1 else y".Calc().AssertResultHas("out", 0);
    }
}
