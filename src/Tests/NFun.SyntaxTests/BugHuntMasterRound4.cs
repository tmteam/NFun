using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 4,
/// post Round-1/2/3 fixes). 3 agents × ~100 iterations. 7 confirmed bugs
/// after filtering A2#1 and A2#3 (known TIC output-generic limitation).
/// </summary>
public class BugHuntMasterRound4 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR4Bug1 — Integer literal type inference at the Int32 boundary
    //   depends on the literal's base, not its value:
    //     0xFFFF_FFFF       → Int64
    //     4294967295        → Real (same value, decimal)
    //     0x80000000        → Int64
    //     2147483648        → Real (same value, decimal)
    //   Types.md doesn't distinguish by base. Either all should fall to
    //   Int64 first (most-specific that fits), or all to Real (most-abstract).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug1_LiteralBaseAffectsType_AtInt32Boundary() {
        "out = 4294967295".AssertResultHas("out", 4294967295L);
    }

    [Test]
    public void MR4Bug1_NegativeLiteralBelowInt32Min_IsInt64() {
        "out = -2147483649".AssertResultHas("out", -2147483649L);
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug2 (CRITICAL) — Zero-arg call on a 1-arg lambda silently
    //   succeeds when wrapped in `map(rule it())`. The missing argument
    //   is filled with the type's default (0 for Int32). Top-level
    //   `f = rule(x:int):int = x+10; f()` is correctly rejected.
    //
    //   `fns = [rule it+1, rule it+2, rule it+3]; fns.map(rule it())`
    //   returns `Any[] = [1,2,3]` instead of FU arity-mismatch error.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug2_ZeroArgCallOn1ArgLambda_InMapRule_SilentlyAccepted() {
        Assert.Throws<FunnyParseException>(() => "fns = [rule it+1]\rout = fns.map(rule it())".Calc());
    }

    [Test]
    public void MR4Bug2_CorrectArityCallOn1ArgLambda_TypedAsElementReturnType() {
        // Bonus from MR4Bug2 fix: correct arity also no longer loses to Any[].
        "fns = [rule it+1, rule it+2]\rout = fns.map(rule it(10))"
            .AssertResultHas("out", new[] { 11, 12 });
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug3 — `out:byte = 1 + 1` parses with cryptic FU761 "Seems like
    //   expression ` + 1` cannot be used here" instead of a clear type
    //   mismatch FU740 (which is what `out:byte = if(true) 1+1 else 5`
    //   correctly produces). UX issue — the parse error misattributes
    //   the failure to the `+1` token.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug3_ByteAnnotation_OnArithmetic_CrypticErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(() => "out:byte = 1 + 1".Calc());
        // Now produces clean FU740 ("Variable 'out' cannot be initialized ...")
        // instead of cryptic FU761. Assert the FU740 hallmarks.
        StringAssert.Contains("'out'", ex.Message);
        StringAssert.Contains("cannot be initialized", ex.Message);
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug4 — Type default values are validated lazily (at first use).
    //   `type t = {x:int = 'hello'}` declared but never instantiated
    //   silently compiles. Violates Basics.md Construction-stage rule:
    //   "checking the correctness of the script and calculating the
    //   types of all expressions in the script."
    //
    //   The bad default leaks to production when t{} is eventually used.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug4_TypeDefault_BadValue_NotValidatedEagerly() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = 'hello'}"));
    }

    [Test]
    public void MR4Bug4_TypeDefault_BadValue_CaughtEvenWhenOverridden() {
        // Before the fix: the bad default was lazily checked only when triggered.
        // `v = t{x=42}` overrode the default, hiding the broken declaration.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = 'hello'}\rv = t{x=42}"));
    }

    [Test]
    public void MR4Bug4_TypeDefault_BoolForInt_CaughtAtDeclaration() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = true}"));
    }

    [Test]
    public void MR4Bug4_TypeDefault_ValidValue_AcceptedAtDeclaration() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = 42}"));
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug5 (resolved) — `out:any = none` is legal per spec: `any` is
    // the top of the type hierarchy and accepts every value, including
    // `none`. Optionals.md updated with the exception. Impl already
    // accepted it; the bug was a spec gap.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug5_AnyAnnotation_AcceptsNone() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:any = none"));
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug6 — Typographic quote pair `‘…’` (LEFT-open + RIGHT-close)
    //   now supported alongside the matching `‘…‘` form, fixing the
    //   spec-impl divergence. Matches how text editors auto-replace
    //   ASCII quotes. Spec text + example aligned.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug6_TypographicPair_SingleQuotes() {
        "y = ‘Hello’".AssertResultHas("y", "Hello");
    }

    [Test]
    public void MR4Bug6_TypographicPair_DoubleQuotes() {
        "y = “Hello”".AssertResultHas("y", "Hello");
    }

    [Test]
    public void MR4Bug6_TypographicQuote_SpecExample_Works() {
        "y = ‘Kate said: “hi”!’".AssertResultHas("y", "Kate said: “hi”!");
    }

    [Test]
    public void MR4Bug6_MatchingPair_BackCompat_StillWorks() {
        "y = ‘Hello‘".AssertResultHas("y", "Hello");
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug7 — `**` is now right-associative: `2**3**2 = 2**(3**2) = 512`,
    // matching math/Python/Ruby/JS/Fortran/Haskell/Lua convention.
    // Previously was left-associative (= 64).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug7_PowerOperator_RightAssociative_PerMathConvention() {
        "out = 2**3**2".AssertResultHas("out", 512.0);
    }
}
