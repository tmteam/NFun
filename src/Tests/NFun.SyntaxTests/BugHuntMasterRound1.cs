using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 1).
/// 3 agents × ~100 iterations (SIMPLE_AND_TRICKY, HELL_AND_NESTED,
/// EDGE_AND_CREATIVE). 4 unique confirmed bugs after filtering nits,
/// FP-related issues, and known limitations. Each marked [Ignore] until fixed.
/// </summary>
public class BugHuntMasterRound1 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MBug1 — LCA of `int[]` with `int?[]` (different optional-element-
    //   ness) crashes with raw .NET ArgumentOutOfRangeException
    //   "Actual value was Int32?". Symmetric form `[1, none] == [1]`
    //   works fine; only the asymmetric direction crashes. Explicit
    //   annotations also avoid the bug. Missing lift on one direction
    //   of LCA application.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MBug1_ArrayLcaWithOptional_RawCrash() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("y = [1] == [1, none]"));
    }

    [Test]
    public void MBug1_IfElseArrayLca_RawCrash() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("y = if(true) [1] else [1, none]"));
    }

    // ───────────────────────────────────────────────────────────────
    // MBug2 — `y:int? = 1 ?? 2` crashes with NFunImpossibleException
    //   "fitless". Per Optionals.md: `??` returns `LCA(unwrap(left),
    //   right) = T`; spec also says "Non-optional T is convertible to
    //   T? (implicit lift)". The combination should produce `y:int? = 1`.
    //   Works without `:int?` annotation, works in nested context like
    //   `y:int? = if(true) (1??2) else (none??3)`. Crash specific to
    //   top-level `T?`-annotated variable assigned a `??` result.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MBug2_TypedOptionalCoalesceAssign_Crash() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("y:int? = 1 ?? 2");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void MBug2_TypedRealOptionalCoalesceAssign_Crash() {
        Assert.DoesNotThrow(() => Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("y:real? = 1 ?? 2"));
    }

    // ───────────────────────────────────────────────────────────────
    // MBug3 — `0b_` (binary prefix with only an underscore body)
    //   crashes the tokenizer with raw ArgumentOutOfRangeException
    //   "Index was out of range". `0b` alone produces clean FU134;
    //   `0x_` produces FU136 "overflow" (misleading but caught).
    //   `0b_` is the only path that escapes to a raw .NET exception.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MBug3_BinaryLiteralWithUnderscoreOnly_RawCrash() {
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.Build("y = 0b_"));
    }

    // ───────────────────────────────────────────────────────────────
    // MBug4 — Filter-then-first on struct-typed array, with a
    //   `T?` return-type annotation and an inferred-parameter lambda,
    //   fails with FU710 "Unable to cast (T)->Bool to (T?)->Bool".
    //   The implicit `T → T?` lift at the return position is incorrectly
    //   back-propagating into the filter predicate's parameter type.
    //   Workaround: annotate the lambda parameter, OR drop the `?` from
    //   return type, OR use map/first without filter.
    //   Specific to struct element types — primitive and array elements
    //   work fine. Idiomatic findFirstMatching pattern broken.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MBug4_FilterFirstStructOptionalReturn_BackPropagatesOptional() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build(
                "type p = {v:int}\r" +
                "f(arr:p[]):p? = arr.filter(rule it.v > 0).first()\r" +
                "out = f([p{v=-1}, p{v=2}])?.v ?? -99");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

}
