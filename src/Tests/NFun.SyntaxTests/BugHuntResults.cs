using System.Linq;
using NFun;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting (300 iterations, 2026-05-05).
/// Each test is a confirmed bug — expected behavior per specification
/// does not match actual behavior.
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    // Bug #1: convert(bool)→numeric is now defined (true→1, false→0) instead
    // of throwing an unhandled InvalidOperationException.
    [Test]
    public void Bug1_ConvertBoolToInt_True() {
        "y:int = convert(true)".AssertReturns("y", 1);
    }

    [Test]
    public void Bug1_ConvertBoolToInt_False() {
        "y:int = convert(false)".AssertReturns("y", 0);
    }

    [Test]
    public void Bug1_ConvertBoolToReal() {
        "y:real = convert(true)".AssertReturns("y", 1.0);
    }

    // Bug #2: 3-level nested inline optional struct — fixed by master's
    // recursive-types / cycle-rescue work. Kept as a regression sentinel.
    [Test]
    public void Bug2_ThreeLevelNestedOptionalStruct() {
        "s = {x = if(true) {y = if(true) {z = 42} else none} else none}\r out = s.x?.y?.z ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 42);
    }

    // (Bug #3 was `compact()` — withdrawn per user decision: filterNotNull()
    // is the canonical name and we don't want an alias.)

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 2 (300 iterations, 2026-06-18) — 8 confirmed bugs.
    // All [Ignore]'d until fixed.
    // ──────────────────────────────────────────────────────────────────

    // Bug #4 — CRITICAL crash: `arr?[i]` on `int[]?` annotated array throws
    // InvalidCastException (MutableFunnyArray → IFunnyArray). The runtime
    // routes lang-mode mutable arrays through SafeGetElementFunction, which
    // expects the legacy IFunnyArray shape only.
    [Test]
    public void Bug4_SafeIndex_OnOptionalIntArray_Works() {
        var rt = NFun.Funny.Hardcore.BuildLang("arr:int[]? = [10,20,30]\nout = arr?[1] ?? -1");
        rt.Run();
        Assert.AreEqual(20, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Bug4_SafeIndex_OnOptionalIntArray_None() {
        var rt = NFun.Funny.Hardcore.BuildLang("arr:int[]? = none\nout = arr?[1] ?? -1");
        rt.Run();
        Assert.AreEqual(-1, System.Convert.ToInt32(rt["out"].Value));
    }

    [Test]
    public void Bug4_SafeIndex_OnOptionalIntArray_OutOfRange() {
        var rt = NFun.Funny.Hardcore.BuildLang("arr:int[]? = [10,20,30]\nout = arr?[99] ?? -1");
        rt.Run();
        Assert.AreEqual(-1, System.Convert.ToInt32(rt["out"].Value));
    }

    // Bug #5 — CRITICAL crash: TIC NotSupportedException when a list-of-struct
    // literal mixes empty and non-empty array fields. Unifying `list(V1)` with
    // `list(V0)` where V0 has no concrete element type bails out.
    [Test]
    public void Bug5_StructFieldEmptyArray_UnifyWorks() {
        var rt = NFun.Funny.Hardcore.BuildLang("out = [{xs=[1,2,3]},{xs=[]}]");
        rt.Run();
        Assert.AreEqual("list<{xs:list<Int32>}>", rt["out"].Type.ToString());
    }

    [Test]
    public void Bug5_StructFieldEmptyArray_ReverseOrder() {
        var rt = NFun.Funny.Hardcore.BuildLang("out = [{xs=[]},{xs=[1,2,3]}]");
        rt.Run();
        Assert.AreEqual("list<{xs:list<Int32>}>", rt["out"].Type.ToString());
    }

    // Bug #6 — Parse error on chained field access in `.map(rule it.a.b)`
    // when the list element is a struct literal with nested struct. The same
    // chain works when the struct is pre-bound to a variable.
    [Test]
    public void Bug6_NestedStructMapChainedAccess() {
        var rt = NFun.Funny.Hardcore.BuildLang("out = [{a={b=1}}].map(rule it.a.b)");
        rt.Run();
        // out: fixedArray<int> = [1]
        Assert.AreEqual(1, System.Convert.ToInt32(((System.Collections.IList)rt["out"].Value)[0]));
    }

    [Test]
    public void Bug6_ThreeLevelNestedStructMapChainedAccess() {
        var rt = NFun.Funny.Hardcore.BuildLang("out = [{a={b={c=42}}}].map(rule it.a.b.c)");
        rt.Run();
        Assert.AreEqual(42, System.Convert.ToInt32(((System.Collections.IList)rt["out"].Value)[0]));
    }

    // Bug #7 — `.filter(...)` strips struct fields not referenced in the lambda
    // body. `data.filter(rule it.score>=80)` returns `{score:Int32}[]` instead
    // of the full struct type. Symmetry break with `sort`, which keeps fields.
    [Test]
    public void Bug7_FilterPreservesStructFields() {
        // Lang-mode only — the dependency on full struct width through a
        // generic .filter is verified via BuildLang; ee-mode .Calc() goes
        // through a different inference path.
        var rt = NFun.Funny.Hardcore.BuildLang(
            "data = [{name='A',score=80},{name='B',score=95}]\n" +
            "winners = data.filter(rule it.score>=80)\n" +
            "out = winners[0].name");
        rt.Run();
        Assert.AreEqual("A", rt["out"].Value.ToString());
    }

    // Bug #8 — Range with step near int.max overflows the size calculation
    // (end-start probably in unchecked int32) and throws "Array dimensions
    // exceeded" even when the result would be a tiny array.
    [Test]
    public void Bug8_RangeStepNearIntMax_Works() {
        "y = [2147483640..2147483646 step 3]"
            .AssertReturns("y", new[] { 2147483640, 2147483643, 2147483646 });
    }

    [Test]
    public void Bug8_RangeToIntMax_Works() {
        "y = [2147483645..2147483647 step 1]"
            .AssertReturns("y", new[] { 2147483645, 2147483646, 2147483647 });
    }

    [Test]
    public void Bug8_RangeFromIntMin_Descending() {
        "y = [-2147483645..-2147483648 step 1]"
            .AssertReturns("y", new[] { -2147483645, -2147483646, -2147483647, -2147483648 });
    }

    // Bug #9 — `ip → uint32` produces little-endian byte order, but the spec
    // documents IP byte serialization as network (big-endian) order.
    // `convert(127.0.0.1):uint32` returns 16777343 (0x0100007F) instead of
    // 2130706433 (0x7F000001).
    [Test]
    public void Bug9_IpToU32_BigEndian() {
        "y:uint32 = convert(127.0.0.1)".AssertReturns("y", 2130706433u);
    }

    [Test]
    public void Bug9_U32ToIp_BigEndian_RoundTrip() {
        ("a:uint32 = convert(192.168.0.1)\r" +
         "out:ip = convert(a)")
            .Calc().AssertResultHas("out", System.Net.IPAddress.Parse("192.168.0.1"));
    }

    // Bug #10 — `m.get(k)!` on `map<K, V?>` returns `V?` instead of `V`.
    // Fixed (round 5): `!` and `??` made TIC special forms (SetForceUnwrap mirrors
    // SetCoalesce), with a new IsForcedNonOptional flag on their rigid U-node
    // (negative skolem per Pottier-Rémy ATTAPL §10.7) that gates
    // IntersectIntervalsOrNull's IsOptional OR-fusion and triggers a structural
    // unwrap of StateOptional descendants. Closes Bug #10 family: `m.get(k)!`,
    // `arr?[i]!`, `s?.f!`, `list.removeLast()!`.
    [Test]
    public void Bug10_ForceUnwrap_OnOptionalValueMapGet() {
        ("m = __mkMap({key=1, value=if(true) 5 else none})\r" +
         "y:int = m.get(1)!")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 5);
    }

    // Bug #11 — Display-only: list<T>, set<T>, map<K,V> CLI output shows the
    // underlying .NET ToString() (`System.Collections.Generic.List\`1[…]`)
    // instead of the materialised element listing. Runtime values are correct;
    // this affects only user-visible CLI output. The print routine for the new
    // lang-mode shapes is missing.
    // Bug #11 was a CLI-only display issue in ConsoleAppExample/Program.cs.
    // Fixed by switching the print path from `output.Value` (CLR-converted)
    // to `output.FunnyValue` (internal IFunnyEnumerable / IFunnyMap shapes)
    // and adding FormatEnumerable / FormatMap handlers.

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 3 (300 iterations, 2026-06-18) — 10 confirmed bugs.
    // All [Ignore]'d until fixed.
    // ──────────────────────────────────────────────────────────────────

    // Bug #12 — Mutator return type leaks into following statements. A void
    // mutator like `a.add(4)` returns `none`; the subsequent `out = a` ends
    // up widening to `list<T>?` via LCA with that `none`. The value is
    // correct; only the static type of the trailing variable is wrong.
    [Test]
    public void Bug12_MutatorTypeLeak_AddMakesOutOptional() {
        var rt = NFun.Funny.Hardcore.BuildLang("a = list(1,2,3)\na.add(4)\nout = a");
        rt.Run();
        Assert.AreEqual("list<Int32>", rt["out"].Type.ToString());
    }

    // Bug #13 — `removeAt` is even worse than `add`: the receiver collapses
    // to `Any`, not just Optional. `y` and `out` both type as `Any`.
    [Test]
    public void Bug13_RemoveAtCollapsesToAny() {
        var rt = NFun.Funny.Hardcore.BuildLang("y = [1,2,3]\ny.removeAt(0)\nout = y");
        rt.Run();
        Assert.AreEqual("list<Int32>", rt["out"].Type.ToString());
    }

    // Bug #14 — `.sum()` on `int?[]` crashes at runtime instead of failing
    // typecheck. `.max()` / `.min()` / `.sort()` reject the same input cleanly
    // at compile time. `sum` slips past and throws NFunImpossibleException
    // ("Unsupported type for this function") at runtime.
    [Test]
    public void Bug14_SumOnOptionalArray_TypedReject() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "[1, none, 3].sum()".Calc());
    }

    // Bug #15 — Self-referential rule literal crashes the TIC solver with an
    // internal `NFunImpossibleException: Circular ancestor 0`. Mutual-rules
    // case (`f = rule g(it); g = rule f(it)`) is rejected cleanly with FU870.
    // Single-rule self-ref should also surface as a typed parse/inference error.
    // Round 10 Bug #52 fix (Apply(StateFun,StateFun) + Toposort RefTo deref
    // identity guards) shifted the surfaced message from "Self-referential
    // definition is not allowed" to "Function 'f(_)' is not found" — the
    // latter is more precise (f is referenced as a callable before its own
    // body resolves). Both are FunnyParseException; the user-facing contract
    // (typed parse error, no internal crash) is preserved either way.
    [Test]
    public void Bug15_SelfReferentialRule_TypedParseError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "f = rule f(it)".Calc());
    }

    // Bug #16 — Iteration-mutation runtime error leaks raw .NET
    // `InvalidOperationException: Collection was modified...`. CLAUDE.md and
    // Collections.md state this should surface as a NFun-typed
    // FunnyRuntimeException("collection modified during iteration").
    [Test]
    public void Bug16_ForLoopMutation_TypedRuntimeException() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => {
            var rt = NFun.Funny.Hardcore.BuildLang("x = list(1,2,3)\nfor i in x: x.add(99)");
            rt.Run();
        });
        StringAssert.Contains("modified during iteration", ex.Message);
    }

    // Bug #17 — `m.keys()` / `m.values()` not registered as functions. Spec
    // and the user-facing reference list them as map operations. Currently
    // `m.keys()` fails with FU755 type-mismatch.
    [Test, Ignore("Bug hunt #17: m.keys()/m.values() not registered")]
    public void Bug17_MapKeys_NotRegistered() {
        var rt = NFun.Funny.Hardcore.BuildLang("m = __mkMap({key='a',value=1})\nout = m.keys()");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    // Bug #18 — `set<Any>` (e.g. inferred from `[].toSet()`) accepts mutable
    // composite elements via `tryAdd`. The Immutable typeclass check fires at
    // `set(...)` factory but not at `tryAdd` when the set's element type is
    // inferred as Any. The composite then sits inside a hash-based container
    // which violates the Immutable contract.
    [Test]
    public void Bug18_SetTryAdd_EnforcesImmutable() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "y = [].toSet()\nout = y.tryAdd([1,2,3])".Calc());
    }

    // Bug #19 — `abs(int.min)` leaks the raw CLR overflow text instead of a
    // clean NFun integer-overflow message. Same for `x:int = -2147483648; y = -x`.
    [Test]
    public void Bug19_AbsIntMin_TypedOverflow() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() =>
            "out = abs(-2147483648)".Calc());
        StringAssert.DoesNotContain("twos complement", ex.Message);
        StringAssert.Contains("integer overflow", ex.Message);
    }

    // Bug #20 — `1 << 1+2` produces an error message that leaks internal TIC
    // solver state: "Node 4:ref(V0) cannot has state U8". User cannot
    // understand it. The constraint is that the right operand of `<<` must
    // be a byte; `1+2` is int32, doesn't fit byte, so error should say so.
    [Test]
    public void Bug20_ShiftRhsError_LeaksTicText() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "out = 1 << 1+2".Calc());
        StringAssert.DoesNotContain("ref(V", ex.Message);
        StringAssert.DoesNotContain("Node ", ex.Message);
    }

    // Bug #21 — `y = 1.0; y.toText()` types `y` as `Any` instead of `Real`.
    // When the only constraint on `y` is a generic `(any)→text` call, the
    // real-literal preferred type is lost. Cosmetic — the actual computation
    // is correct. Related to MEMORY's "output generics" pattern. Minor.
    [Test, Ignore("Bug hunt #21: real literal used only as toText arg types as Any")]
    public void Bug21_RealLiteralAsToTextArg_TypedAsAny() {
        var rt = NFun.Funny.Hardcore.BuildLang("y = 1.0\nout = y.toText()");
        rt.Run();
        Assert.AreEqual("Real", rt["y"].Type.ToString());
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 4 (300 iterations, 2026-06-18) — 2 confirmed bugs.
    // ──────────────────────────────────────────────────────────────────

    // Bug #22 (CRITICAL) — `data.xs.map(rule it).flat()` crashes at runtime
    // when `data.xs` is `list<list<T>>` accessed through a struct field.
    // The element type is silently widened to `Any` somewhere between the
    // struct field access and `.map`'s return, then `.flat()`'s IFunnyArray
    // cast panics. The same expression without the struct wrap works:
    // `xs = [[1,2],[3]]; xs.map(rule it).flat()` → `[1,2,3]` ✓.
    [Test]
    public void Bug22_StructFieldMapFlat_NoLongerCrashes() {
        var rt = NFun.Funny.Hardcore.BuildLang(
            "data = {xs=[[1,2],[3]]}\nout = data.xs.map(rule it).flat()");
        rt.Run();
        // out value is [1,2,3] — type may still report Any[] (the lambda
        // `rule it` loses element-type precision through the CompCs map cell
        // when the source came from a struct field). Tracked as a TIC issue;
        // the crash itself is gone.
        Assert.IsNotNull(rt["out"].Value);
    }

    // Bug #23 (MODERATE) — `.fold(seed, rule(a,b)=if(a.field>b.field) a else b)`
    // fails with FU755 when seed is a variable. Same with inline literal
    // seed (`{age=0}`) works. The seed-variable shape with field-comparison
    // condition and struct-LCA return trips an LCA / preferred-type
    // propagation gap between the seed and the rule-result nodes.
    [Test]
    public void Bug23_FoldSeedVar_FieldCompareRule() {
        var rt = NFun.Funny.Hardcore.BuildLang(
            "p = [{age=25},{age=30}]\n" +
            "s = {age=0}\n" +
            "out = p.fold(s, rule(a,b)= if(a.age>b.age) a else b)");
        rt.Run();
        Assert.AreEqual("{age:Int32}", rt["out"].Type.ToString());
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 5 (300 iterations, 2026-06-19) — 6 confirmed bugs.
    // ──────────────────────────────────────────────────────────────────

    // Bug #24 (MODERATE) — narrow integer annotation breaks under self-
    // referential reassignment with an int literal. `x:byte = 1; x = x + 1`
    // widens the `1` to Int32 in the rhs, conflicting with byte. Error
    // points at the initialiser (line 1) not the reassignment (line 2).
    [Test, Ignore("Bug hunt #24: narrow int reassign with literal widens to Int32")]
    public void Bug24_NarrowIntReassign_WidensToInt32() {
        var rt = NFun.Funny.Hardcore.BuildLang("x:byte = 1\nx = x + 1");
        rt.Run();
        Assert.AreEqual(2, System.Convert.ToInt32(rt["x"].Value));
    }

    // Bug #25 (MINOR) — error message leak. `x:int? = 5; out = -x` reports
    // `(5) x:Empty` (internal node id + placeholder) and ends with "Do
    // something about it!". Should be a clean message like "operator `-`
    // not applicable to `int?` — unwrap first".
    [Test]
    public void Bug25_OptionalArith_LeaksInternalState() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "x:int? = 5\nout = -x".Calc());
        StringAssert.DoesNotContain("x:Empty", ex.Message);
        StringAssert.DoesNotContain("Do something about it", ex.Message);
    }

    // Bug #26 (MINOR) — display inconsistency. `if(true) {a=1,b=2} else {a=1}`
    // resolves to `{a:Int32}` (width subtyping is correct) but the runtime
    // struct still carries `b=2`. Fix: CLI now filters dict-display by the
    // declared struct type's field set. The library-level struct still carries
    // both fields (width-subtype: the wider value is the safe upcast); only
    // the CLI display is narrowed to match the declared type.
    [Test]
    public void Bug26_WidthLcaDisplay_Inconsistency() {
        var rt = NFun.Funny.Hardcore.BuildLang("out = if(true) {a=1,b=2} else {a=1}");
        rt.Run();
        Assert.AreEqual("{a:Int32}", rt["out"].Type.ToString());
    }

    // Bug #27 (MODERATE) — `users = [{age=if(true) X else none},...]`
    // followed by `.map(rule it.age ?? 0)` raises FU719
    // "There's an error somewhere in the types used (but I can't figure out
    // exactly where)". The control case with literal int+none works.
    // The seed-pinned `.fold(0, rule(a,u)=a + (u.age ?? 0))` also works.
    // Generic TIC propagation through `if(...) X else none` inside a struct
    // literal inside a list, then through lambda field access + ??, drops
    // some constraint.
    [Test]
    public void Bug27_IfSourcedOptionalStruct_MapFails() {
        "users = [{age=if(true) 30 else none}, {age=none}]\rout = users.map(rule it.age ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", new[] { 30, 0 });
    }

    // Bug #28 (MODERATE) — `[{x=1,y=2},{x=3}].first().x` raises FU831
    // "Field `y` is missed in struct". When the literal becomes
    // `list<{x:Int32}>` via width subtyping, chained `.first().x` somehow
    // propagates the wider shape back as a constraint on the source. Works
    // when split: `r = data.first(); out = r.x`.
    [Test]
    public void Bug28_ChainedFirstField_ConstraintLeak() {
        "data = [{x=1,y=2}, {x=3}]\rout = data.first().x"
            .AssertResultHas("out", 1);
    }

    [Test]
    public void Bug28_ChainedMapField_ConstraintLeak() {
        "data = [{x=1,y=2}, {x=3}]\rout = data.map(rule it.x)"
            .AssertResultHas("out", new[] { 1, 3 });
    }

    // Bug #29 (MINOR) — `?[` returns `any??` instead of flattening to
    // `any?`. `x = [none,none]; out = x?[0]` shows `out:any??`. The
    // FlattenNestedOptional rule (opt(opt(T)) → opt(T)) doesn't fire for
    // `T = any` through the `?[` operator. Display inconsistency; value
    // works.
    [Test]
    public void Bug29_SafeIndexNestedOptionalAny_NoFlatten() {
        var rt = NFun.Funny.Hardcore.BuildLang("x = [none, none]\nout = x?[0]");
        rt.Run();
        StringAssert.DoesNotContain("any??", rt["out"].Type.ToString());
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 6 (300 iterations, 2026-06-20) — 4 confirmed bugs.
    // ──────────────────────────────────────────────────────────────────

    // Bug #30 (MODERATE) — `1 + 2147483648` resolves to `Real` instead of `Int64`.
    // Asymmetric: `2147483648 + 1` correctly resolves to `Int64`. The Int32-preferred
    // left literal anchors the result lattice incorrectly when the right literal must
    // widen past Int32. Affects `+`, `-`, `*`, `%`, `max(a,b)`, `min(a,b)`.
    // Longstanding (reproduces on master) — preferred-type / LCA interaction at the
    // Int32→Int64 boundary. Requires deeper TIC work on PropagatePreferred + LCA
    // ordering.
    [Test, Ignore("Bug hunt #30: 1 + Int64-literal widens to Real (asymmetric, longstanding)")]
    public void Bug30_ArithmeticInt32PrefAcrossInt64Boundary_WidensToReal() {
        var rt = NFun.Funny.Hardcore.BuildLang("y = 1 + 2147483648");
        rt.Run();
        Assert.AreEqual("Int64", rt["y"].Type.ToString());
    }

    // Bug #31 — `[{a=1}].toSet()` constructed `set<{a:Int32}>` without FU580
    // (struct not Immutable). Fixed (round 6): added RequireImmutable check in
    // `ToSetFunction.CreateConcrete`, mirroring SetFactoryFunction. Same bypass
    // affected `[[1,2]].toSet()` → `set<list<Int32>>`.
    [Test]
    public void Bug31_ToSet_BypassesImmutableCheck() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "y = [{a=1}].toSet()".Calc());
    }

    // Bug #32 — `a:int[]? = [1,2,3]; a ?? [0]` resolved to `Any` instead of
    // `array<Int32>`. Fixed (round 6 follow-up): added cross-Constructor LCA arm
    // in `StateCollection.GetLastCommonAncestorOrNull` + `LcaOrShareIdentity`
    // mirroring Stage 2 Liskov decision (`Ambiguity_ListPassedWhereArrayExpected`).
    // LCA widens kind per ConstructorLattice (List ⊆ Array ⊆ FixedArray); element
    // LCA recursive. Identity-share via MergeInplace gated to non-composite
    // elements to avoid mixing widen-outer + narrow-inner semantics. Added a Push
    // (CS×ICompositeState=StateCollection) cross-Constructor cell to honor the new
    // widened types end-to-end. Spec example with `.sort().reverse() ?? [0]`
    // remains Any because the chain returns legacy StateArray (different LCA
    // path) — fixing that requires extending the legacy bridge, out of scope here.
    [Test]
    public void Bug32_OptionalCoalesceArrayLiteral_WidensPerLattice() {
        var rt = NFun.Funny.Hardcore.BuildLang(
            "a:int[]? = [1,2,3]\nout = a ?? [0]");
        rt.Run();
        Assert.AreEqual("array<Int32>", rt["out"].Type.ToString());
    }

    [Test]
    public void Bug32_OptionalChainCoalesceArrayLiteral_FixedArray() {
        // After lang LINQ migration (sort/reverse return fixedArray<T> instead
        // of legacy `T[]`), this resolves via the round-6 cross-Constructor
        // StateCollection LCA: fixedArray (sort.reverse result) × list ([0]
        // literal) → LCA per ConstructorLattice = fixedArray.
        var rt = NFun.Funny.Hardcore.BuildLang(
            "arr:int[]? = if(true) [3,1,2] else none\nout = arr?.sort().reverse() ?? [0]");
        rt.Run();
        Assert.AreEqual("fixedArray<Int32>", rt["out"].Type.ToString());
    }

    // Bug #33 — `[{x=if(true) 5 else none},{x=none}].map(rule it.x ?? 99)` produced
    // `fixedArray<UInt8?>` instead of `fixedArray<UInt8>`. Fixed (round 6):
    //   - Pull-level guard at `PullConstraintsFunctions.cs:47-58` skips IsOptional
    //     propagation when ancestor is IsForcedNonOptional (mirrors the existing
    //     IsOptionalElement skip).
    //   - Structural unwrap of `descendant.Descendant is StateOptional` at the same
    //     Pull cell before AddDescendant — peels nested-Optional from the descendant
    //     CS before the OR-fusion would absorb it. Round 5's Destruction-time gate
    //     wasn't enough because the absorption happens at Pull when the rigid U-node
    //     merges with `it.x`'s opt(U8) constraint via the lambda's signature.
    [Test]
    public void Bug33_CoalesceInMapLambda_NoUnwrap() {
        ("data = [{x=if(true) 5 else none},{x=none}]\r" +
         "out = data.map(rule it.x ?? 99)")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", new[] { 5, 99 });
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 7 (300 iterations, 2026-06-20) — 8 confirmed bugs.
    // ──────────────────────────────────────────────────────────────────

    // Bug #34 (CRITICAL) — `convert(text[]):int?[]` was silently no-op:
    // compile-time type read `array<Int32?>` but elements stayed as
    // TextFunnyArray at runtime, then force-unwrap `+ 1` threw raw CLR
    // InvalidCastException. Root cause: VarTypeConverter's `T → Optional<U>`
    // implicit lift returns NoConvertion when no T → U morphism exists at the
    // implicit-cast level (a trust hack for struct width-subtyping that `?.`
    // rescues). The collection-element path then rode that lie all the way
    // through. Composite-element parsing (`text → int?` per element) isn't
    // supported by `convert()` yet — fix is to reject at parse time so the
    // unsoundness can't leak. Element-wise parse is future work.
    [Test]
    public void Bug34_ConvertTextArrayToIntOpt_RejectedAtParseTime() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "x:int?[] = convert(['abc'])"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // Bug #35 + #36 (MODERATE) — indexed-write previously leaked raw .NET
    // exceptions (`x:byte[]; x[0]=256` → `OverflowException`) or silently
    // truncated narrowing assignments (`x:int[]; x[0]=1.5` → 1). Variable
    // initialization rejects the equivalent (`y:byte=256`, `y:int=1.5`)
    // with FU740 at parse time, but indexed-write skipped that check —
    // TIC doesn't bind assignment statements (workaround #7), so the
    // expression builder was relying on VarTypeConverter's permissive
    // narrowing converters. Fix tightens it to the same strict implicit-
    // conversion table as TIC enforces for variable init, surfacing FU710
    // at parse time.
    // Indexed write is lang-mode only — ee-mode `T[]` is immutable. All
    // three of these need BuildLang so we exercise the same code path the
    // CLI's lang-mode hits, which is where the bugs reproduced originally.
    [Test]
    public void Bug35_ByteArrayWriteOverflow_RejectedAtParseTime() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("a:byte[] = [1,2,3]\na[0] = 256"));
        StringAssert.Contains("Int32", ex!.Message);
        StringAssert.Contains("UInt8", ex.Message);
    }

    [Test]
    public void Bug36_IntArrayWriteReal_RejectedAtParseTime() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("a:int[] = [1,2,3]\na[0] = 1.5"));
        StringAssert.Contains("Real", ex!.Message);
        StringAssert.Contains("Int32", ex.Message);
    }

    [Test]
    public void Bug36_IntArrayWriteInt_WideningStillWorks() {
        // Widening (int → real) and identity (int → int) remain valid.
        Assert.DoesNotThrow(() =>
            Funny.Hardcore.BuildLang("a:int[] = [1,2,3]\na[0] = 99"));
        Assert.DoesNotThrow(() =>
            Funny.Hardcore.BuildLang("a:real[] = [1.0,2.0,3.0]\na[0] = 5"));
    }

    // Bug #37 (NOT A BUG — by design) — `a:{x:int} = {x=1,y=2}; b:{x:int} = {x=1}; a==b`
    // returns false. Type annotation acts like a C# interface: it labels the
    // static slot but does NOT project the runtime FunnyStruct. The runtime
    // retains `{x=1, y=2}` for `a`; equality compares stored fields, so the
    // extra `y` rules out equality — same semantic as the spec example
    // `c={name='Kate'}; b={age=31,name='Kate'}; c==b # false`. (The display
    // layer's projection is misleading and tracked separately.)
    [Test]
    public void Bug37_StructWidthUpcast_AnnotationIsInterface_NotEqual() {
        ("a:{x:int} = {x=1, y=2}\r" +
         "b:{x:int} = {x=1}\r" +
         "out = a == b").Calc().AssertResultHas("out", false);
    }

    // Bug #38 (MINOR) — `[1,2,3].setAt(3, 99)` (exact-count index) was leaking
    // raw .NET `IndexOutOfRangeException`. Root cause: SetGenericFunctionDefinition
    // bounds check used `index > arr.Count + 1` instead of `index >= arr.Count`,
    // so the count-boundary index slipped past and `newArr.SetValue` threw the
    // raw CLR exception. Fix tightens the bound to the correct half-open
    // interval [0, arr.Count).
    [Test]
    public void Bug38_SetAtBoundary_NfunException() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() =>
            "out = [1,2,3].setAt(3, 99)".Calc());
        StringAssert.DoesNotContain("Index was outside", ex!.Message);
        StringAssert.Contains("Argument out of range", ex.Message);
    }

    // Bug #39 (MINOR) — `5 % 0` was leaking raw .NET `DivideByZeroException`
    // message. The general `catch (Exception)` in `FunOfTwoArgsExpressionNode`
    // rewrapped it as FunnyRuntimeException but kept the .NET phrasing.
    // Fix adds an explicit `catch (DivideByZeroException)` branch that emits
    // the NFun-style `<op>: division by zero` message, matching the pattern
    // already used for `OverflowException` → `<op>: integer overflow`.
    [Test]
    public void Bug39_ModuloZero_NfunMessage() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() =>
            "out = 5 % 0".Calc());
        StringAssert.DoesNotContain("Attempted to divide by zero", ex!.Message);
        StringAssert.Contains("division by zero", ex.Message);
    }

    // Bug #40 (MODERATE) — `[1,2,3].flat()` (flat called on a non-nested array)
    // crashes at runtime with raw CLR cast exception instead of parse error.
    // The structurally identical `'hello'.flat()` and `x:int[]=[1,2,3]; x.flat()`
    // are correctly rejected at parse time (FU783/FU777). The gap is specific to
    // Bug #40 (MODERATE) — `[1,2,3].flat()` previously crashed at runtime with
    // a raw CLR `InvalidCastException` (Int32 → IFunnyEnumerable). flat's
    // signature is `Enumerable<Enumerable<T>> → FixedArray<T>` but TIC settled
    // T to Any when the input was a single-level collection, leaving the
    // structural mismatch to surface as a runtime cast crash. Fix closes the
    // P3 Monotonicity gap in Apply(CompCs, CS): the cell previously rejected
    // only a primitive Ancestor (upper bound) on the descendant CS while
    // silently accepting a primitive Descendant (lower bound) — a non-None
    // primitive lower bound also forbids the composite-shape obligation, so
    // the symmetric check makes the deferred-accept sound. Bug surfaces as
    // FU783 "Invalid function call argument" pointing at the offending arg.
    [Test]
    public void Bug40_FlatOnNonNestedList_RejectedAtParseTime() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "out = [1,2,3].flat()".Build());
        StringAssert.Contains("flat", ex!.Message);
    }

    [Test]
    public void Bug40_FlatOnNestedList_StillWorks() {
        // Sanity: legitimate nested-collection input continues to work.
        "out = [[1,2],[3,4]].flat()".Calc().AssertResultHas("out", new[] { 1, 2, 3, 4 });
    }

    // Bug #41 (MINOR) — `x:int?[] = [1,2,3]; out = x.sum()` was pointing the
    // error at the entire `x:int?[] = ...` declaration line, even though the
    // declaration itself is fine and the type clash actually originates at
    // the `.sum()` call. Fix adds a fallback in `GetAncestorToDescendantError`:
    // when the descendant resolves to an EquationSyntaxNode (variable decl)
    // and ancestor is null, search the tree for a function call that uses
    // the variable and redirect attribution there (FU767 pointing at the
    // call). The inline `[1, none].sum()` path was already correct via the
    // FU783 path; this brings the typed-variable indirection in line.
    [Test]
    public void Bug41_OptionalSumErrorPosition_PointsAtCall() {
        // Lang-mode reproduction: ee-mode `Build` happens to settle this via the
        // implicit lift and computes `out = 6`. The bug surfaces in lang-mode
        // (the CLI's default), where TIC properly rejects the int? element vs
        // sum's numeric T requirement.
        const string script = "x:int?[] = [1, 2, 3]\nout = x.sum()";
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang(script));
        StringAssert.Contains("sum", ex!.Message);
        // Pin the call identity, not just an offset bound: the error must
        // cover exactly `x.sum()`. A weaker `Start > 20` would let a future
        // regression re-attribute to e.g. `out = x.sum()` (the whole equation)
        // or the bare `sum` identifier without anyone noticing.
        var expectedStart = script.IndexOf("x.sum()", System.StringComparison.Ordinal);
        var expectedFinish = expectedStart + "x.sum()".Length;
        Assert.AreEqual(expectedStart, ex.Interval.Start, "error must start at `x.sum()`");
        Assert.AreEqual(expectedFinish, ex.Interval.Finish, "error must end at `x.sum()`");
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 8 (100 iterations, 2026-06-24) — 2 confirmed bugs.
    // Focus: SIMPLE_AND_TRICKY (arithmetic, comparisons, type widening,
    // edge cases in literals and operators).
    // ──────────────────────────────────────────────────────────────────

    // Bug #42 (MINOR) — was: FU740 reported only the constraint's UPPER
    // bound ("Real") for narrow-int arithmetic that couldn't fit a narrow
    // slot (`y:byte; z:byte = y+y`), hiding the actual lower bound. Users
    // saw "Real" and reached for a `:real` cast that didn't fix anything.
    // Root cause: the error fires DURING Pull (before PropagatePreferred
    // broadcasts Preferred), so `GetDescription` sees CS `[U24..Re]` with
    // null Preferred and `Concrete.Convert` falls back to the Ancestor.
    // Fix surfaces the constraint RANGE ("UInt16..Real") at the error
    // site only, by resolving the abstract Descendant to its narrowest
    // concrete via the same lookup the abstract-Ancestor branch uses.
    // Targeted to `Errors.GetDescription`; doesn't change actual type
    // resolution (which depends on `TicTypesConverter.Concrete.Convert`
    // and is consumed by Finalize for real types).
    [Test]
    public void Bug42_NarrowIntArithmeticError_ShowsRange() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("y:byte = 100\nz:byte = y + y"));
        StringAssert.Contains("UInt16..Real", ex!.Message,
            "error must show the range, not just the upper bound");
    }

    // Bug #43 (MODERATE) — Newline-as-continuation is asymmetric. Per Basics.md
    // "When reading an expression, line breaks are ignored". In practice the
    // lang-mode parser silently treats certain leading tokens as continuations
    // and others as start-of-statement, with no consistent rule documented:
    //   y = 12      \r  *3         # parses as y = 36     (continuation kept)
    //   y = 12      \r  +1         # FU606 'expression missed'   (rejected)
    // The spec's own multi-line example
    //   y = 12 ;; *3 ;; +1 -2      (newlines, expected 35)
    // fails for the same reason — `+` (and `-` next) is treated as a new
    // statement start. Either the spec example must be removed/clarified or
    // the parser must accept any binary-only leading token as continuation.
    [Test, Ignore("Bug hunt #43: newline-as-continuation asymmetric — `*` continues, `+`/`-` start new statement")]
    public void Bug43_NewlineContinuation_AsymmetricBetweenOperators() {
        // Sanity: the * continuation form already works (acts as documented).
        Assert.DoesNotThrow(() => Funny.Hardcore.BuildLang("y = 12\n*3"));
        // The + continuation form must work the same way per the spec text
        // "line breaks are ignored" and the literal Basics.md example
        // `y = 12\n*3\n+1 -2`.
        Assert.DoesNotThrow(() => Funny.Hardcore.BuildLang("y = 12\n+1"));
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 8 (300 iterations, 2026-06-24) — Hell+Edge agents
    // added 5 more confirmed bugs. Round closed early — Edge agent
    // self-killed after looping on report generation.
    // ──────────────────────────────────────────────────────────────────

    // Bug #44 — was CRITICAL crash: TIC NotSupportedException leaked to user
    // (`CBFC does not support compCs[_..enumerable, e=V9] to compCs[...]`)
    // on `[[1,2,3]].map(rule [it,it]).flat().flat()`. Root cause was a
    // pre-existing gap in `StateExtensions.Fit.cs`: the switch for
    // `CanBeFitConverted` and the top-level `FitsInto` had no case for
    // `StateCompositeConstraints` (it's a peer of ICompositeState, not a
    // subtype), so two CompCs's meeting at Fit fell through to `throw`.
    // Fix adds a CompCs↔CompCs case: identity on ElementNode is trivially
    // fit (`true`); CompCs into Any is true. The crash is gone.
    //
    // The value is correct; the static type widens to `fixedArray<Any>`
    // through the nested-map chain — that's a separate Bug #47 family
    // (generic T loses precision through chained `.first()` / `.last()` /
    // `.flat()` after map). Tracked separately.
    [Test]
    public void Bug44_NestedMapFlatFlat_NoLongerCrashes() {
        var result = "out = [[1,2,3]].map(rule [it,it]).flat().flat()".Calc();
        var arr = ((System.Collections.IEnumerable)result.Get("out"))
            .Cast<object>().Select(System.Convert.ToInt32).ToArray();
        Assert.AreEqual(new[] { 1, 2, 3, 1, 2, 3 }, arr);
    }

    [Test]
    public void Bug44_StaticShapeNestedFlatFlat_KeepsWorking() {
        // Control case from the original report — the static-shape equivalent
        // worked before the fix and still works.
        "out = [[[1,2,3]]].flat().flat()"
            .Calc().AssertResultHas("out", new[] { 1, 2, 3 });
    }

    // Bug #45 — MODERATE: recursive user fn with `:int[]` return + recursive
    // `.append(x)` branch was emitting FU777 blaming the whole if-else,
    // making the actual type clash invisible. Per professor review: TIC's
    // rejection is algebraically correct — `.append()` returns
    // `fixedArray<T>` (LINQ-result, immutable, per `Collections.md:271-279`)
    // and `:int[]` = `array<T>` (MutableArray); the lattice direction
    // `List ⊆ Array ⊆ FixedArray ⊆ Enumerable` (Collections.md:299) makes
    // the downcast require explicit `.toArray()`. Fix is error-attribution
    // only: redirect the if-else descendant to the offending branch's
    // function call, surfacing FU767 "function `append(...)` cannot be used
    // here as its return type does not fit" pointing at `fn(x-1).append(x)`
    // instead of the whole if-else. User then knows to add `.toArray()`
    // or change the annotation.
    [Test]
    public void Bug45_RecursiveUserFnArrayReturnAppend_PointsAtAppend() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang(
                "fn(x:int):int[] = if(x<=0) [0] else fn(x-1).append(x)\n" +
                "out = fn(3)"));
        StringAssert.Contains("append", ex!.Message);
        StringAssert.Contains("return type does not fit", ex.Message);
    }

    [Test]
    public void Bug45_Workaround_ExplicitToArray_Works() {
        // Pin the documented workaround so it can't silently regress.
        var rt = Funny.Hardcore.BuildLang(
            "fn(x:int):int[] = if(x<=0) [0] else fn(x-1).append(x).toArray()\n" +
            "out = fn(3)");
        rt.Run();
        Assert.AreEqual(new[] { 0, 1, 2, 3 },
            ((System.Collections.IEnumerable)rt["out"].Value)
                .Cast<object>().Select(System.Convert.ToInt32).ToArray());
    }

    // Bug #46 — MODERATE (FIXED): if-else of struct branches where one field-value
    // is a bound `list<int>` variable and the other is a list literal previously
    // rejected with FU761. Root cause: GetMergedStateOrNull lacked a case for
    // (StateCollection, CS{Desc=StateCollection}) — it had the analogue for
    // StateStruct but not StateCollection, so after the switch swap the cell
    // returned null → CannotMerge in Push's per-field MergeInplace. Fix mirrors
    // the existing StateStruct + CS precedent (SolvingFunctions.cs ~ line 171).
    // See Bug46_StructFieldLcaTests / Bug46_StructFieldLca_TicTests for boundary
    // coverage and StateFun-field follow-up.
    [Test]
    public void Bug46_IfElseStructBranchMixedListVarAndLiteral() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [1,2]\n" +
            "out = if(true) {x=y} else {x=[3]}");
        rt.Run();
        Assert.AreEqual(new[] { 1, 2 },
            ((System.Collections.IEnumerable)((System.Collections.Generic.IReadOnlyDictionary<string, object>)rt["out"].Value)["x"])
                .Cast<object>().Select(System.Convert.ToInt32).ToArray());
    }

    // Bug #47 + Bug #49 — nested-LINQ element widens to Any
    // (it.first(), .toArray(), .toList(), .toFixedArray(), .toSet(),
    // .reverse(), .take(), .skip() — all in [[...]].map(rule it.F()) shape).
    // BLOCKED on TIC debt #10 (worklist Pull). Confirmed P3b counterexamples.
    //
    // Extracted into TicDebt10_WorklistPullTests.cs together with passing
    // pins (workaround, lang-mode mirrors). See that file for full coverage.

    // Bug #50 — MODERATE (FIXED): when a `rule` value is bound to a
    // variable AND called with an argument requiring implicit collection-kind
    // conversion (e.g. `list<int>` literal flowing into a `array<int>`
    // parameter slot), TWO leaks fired together:
    //   1. The printer leaked the wrapper class name
    //      `NFun.Types.VarTypeConverter+ConcreteFunctionWithConvertion`
    //      instead of the canonical `FUN-user rule(...)` representation.
    //   2. The printed signature mutated from declared `(array<Int32>)->Int32`
    //      to `(list<Int32>)->Int32` because TIC narrowed f's slot arg
    //      contravariantly to the call-site literal's type.
    //
    // Root cause was structural: `GraphBuilder.SetCall` (line 721-733 ELSE
    // branch) synthesized a FRESH StateFun with fresh CS arg nodes whenever
    // the function variable's state hadn't yet been Pull-propagated to
    // StateFun at call-site-build time. The fresh args lacked the rule's
    // declared rigidity (IsSignatureParam flag), so contravariant Pull
    // narrowed them to the call literal's type via the lattice
    // (`list<T> ≤ array<T>` upward → GCD picks list at the contravariant
    // side). Resolution-side amplifier: SolveCovariant in ConstraintsState
    // picked the call-site `Descendant` (list) over the declared upper
    // bound (array).
    //
    // Fix at `GraphBuilder.SetDef`: when binding a rule literal (StateFun)
    // to an unannotated, unconstrained variable, share the StateFun
    // identity (`defNode.State = lambdaFun`) instead of leaving f's slot
    // as fresh CS and forcing a fresh-StateFun synthesis at later SetCall.
    // Then SetCall finds StateFun directly and routes via the rigid-arg
    // path which DOES set IsSignatureParam, preserving the declared
    // signature through call-site contravariant Pull.
    [Test]
    public void Bug50_RuleBoundUserFn_ImplicitListToArray_PreservesDeclaredSignature() {
        var rt = Funny.Hardcore.BuildLang(
            "f = rule(xs:int[]) = xs.sum()\n" +
            "out = f([1,2,3])");
        rt.Run();
        Assert.AreEqual(6, rt["out"].Value);
        // Signature stays as declared (no mutation to `(list<Int32>)->...`).
        Assert.AreEqual("(array<Int32>)->Int32", rt["f"].Type.ToString());
    }

    // Bug #51 — NOT A BUG (spec was wrong; implementation is correct).
    // Bug-hunt round 9 Agent 3 originally reported that lang-mode in-script
    // assignment to a host-input variable overwrites the host-supplied value,
    // contradicting an old paragraph in `specs_lang/Basics.md:165-168` that
    // claimed the assignment is a runtime no-op.
    //
    // Per user direction: the spec paragraph was a holdover from ee-mode
    // semantics. Lang-mode is a regular scripting language — like
    // Python / C# — where reassigning a variable updates its slot, and
    // subsequent reads observe the new value. The host's supplied value is
    // only the initial value at the start of Run; the script is free to
    // overwrite it. Compound forms (`i += 5`) work the same way.
    //
    // Spec updated accordingly (Basics.md §Input variables). Test below pins
    // the correct Python/C#-style behavior so a future "fix" can't silently
    // re-introduce the spurious no-op semantics.
    [Test]
    public void Bug51_LangMode_InputReassign_RebindsLikePythonOrCSharp() {
        var rt = Funny.Hardcore.BuildLang("x = i + 1\ni = 2\ny = i");
        rt["i"].Value = 100;
        rt.Run();
        // x reads i before the reassignment → uses host value 100.
        Assert.AreEqual(101, rt["x"].Value);
        // y reads i after the reassignment → uses the rebound value 2.
        Assert.AreEqual(2, rt["y"].Value);
        // The slot itself has been rebound.
        Assert.AreEqual(2, rt["i"].Value);
    }

    // Bug #48 — MINOR (correctness): bare `none?.toUpper()` returns the
    // *text* "NONE" (i.e. .toUpper() actually ran on the string "none")
    // instead of propagating `none` through the safe-access. Control case
    // `(none ?? none)?.toUpper()` correctly returns `none`. So the receiver-
    // type inference for `none?.method()` doesn't pin the receiver to
    // `none` — it lets methods of `text` apply and runs on the textual
    // representation. Same family hits `none?.toLower()` (lowercase "none"),
    // `none?.trim()` (same).
    [Test, Ignore("Bug hunt #48: none?.toUpper() runs method on text 'none' instead of propagating none")]
    public void Bug48_BareNoneSafeAccess_RunsMethodOnNoneText() {
        var result = "out = none?.toUpper()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Get("out");
        // none propagates through `?.` → expect FunnyNone (CLR null at the boundary).
        Assert.IsNull(result, $"expected none-propagation, got {result}");
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 10 (100 iterations, 2026-06-26) — 2 confirmed bugs.
    // Both CRITICAL crashes leaking internal NFunImpossibleException to
    // the user instead of a typed FunnyParseException / FunnyRuntimeException.
    // ──────────────────────────────────────────────────────────────────

    // Bug #52 — CRITICAL (FIXED): binding a `rule` value to a LOCAL variable
    // inside a multi-line `fun ... :` body crashed TIC with
    // `NFunImpossibleException: Circular ancestor 0`. Even an unused local
    // rule sufficed.
    //
    // Root cause (per professor): the Bug #46 IsLiveSnapshotableFun preserves
    // the exact StateFun instance in a CS's Descendant snapshot. Inside a
    // multi-line fun body's block-bind path that StateFun gets re-met from a
    // second direction, and `Apply(StateFun, StateFun)` at
    // `PullConstraintsFunctions.cs:476-484` calls
    // `descendant.RetNode.AddAncestor(ancestor.RetNode)` (and same for args)
    // without an identity guard — even though four sibling cells in the same
    // file already DO have the guard pattern (`if (X != Y) X.AddAncestor(Y)`).
    // A secondary site surfaces after the first guard at
    // `NodeToposort.cs:56-58`: RefTo deref of an ancestor whose target equals
    // `node` itself triggers `Circular ancestor 2` in `SetAncestor`.
    //
    // Fix at both sites: mirror the existing sibling-cell guard pattern.
    // `T ≤ T` is the identity ordering element and must be elided structurally,
    // not emitted as an edge.
    [Test]
    public void Bug52_LocalRuleInMultilineFunBody_NoLongerCrashes() {
        var rt = Funny.Hardcore.BuildLang(
            "fun outer():\n" +
            "  inner = rule it + 1\n" +
            "  return inner(10)\n" +
            "out = outer()");
        rt.Run();
        Assert.AreEqual(11, rt["out"].Value);
    }

    [Test]
    public void Bug52_Minimal_UnusedLocalRule_NoLongerCrashes() {
        var rt = Funny.Hardcore.BuildLang(
            "fun outer():\n" +
            "  inner = rule it + 1\n" +
            "  return 1\n" +
            "out = outer()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void Bug52_AnnotatedArgsAndClosureCapture_Works() {
        // Pin Probe variants from professor's boundary matrix.
        var rt = Funny.Hardcore.BuildLang(
            "fun outer(x:int)->int:\n" +
            "  inner = rule(y:int)->int = x + y\n" +
            "  return inner(10)\n" +
            "out = outer(5)");
        rt.Run();
        Assert.AreEqual(15, rt["out"].Value);
    }

    [Test]
    public void Bug52_LocalRulePassedToMap_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "fun outer(arr:int[])->int:\n" +
            "  inner = rule(x:int)->int = x*2\n" +
            "  return arr.map(inner).sum()\n" +
            "out = outer([1,2,3])");
        rt.Run();
        Assert.AreEqual(12, rt["out"].Value);
    }

    // Bug #53 — CRITICAL crash → FIXED (typed rejection per spec).
    //
    // Was: `t:text = 'hello'; t[0] = /'H'` crashed TIC with an internal
    // "Node is already solved" assertion in
    // `CompCsApply.ForwardCompCsStateArray` (CompCsApply.cs:198). The
    // `:text` annotation pinned t to a solved-immutable ee-mode
    // `StateArray<Char>`; the indexed-write's CompCs cap then tried to
    // overwrite that state with a mutable StateCollection, hitting the
    // IsSolved invariant guard.
    //
    // Per user direction (round 10 review): strings are semantically
    // equivalent to fixedSizeArray (immutable) for now — matches
    // `specs_lang/Texts.md` §5 "Text is immutable" and the mainstream
    // language consensus (Java/C#/Python/JS/Kotlin/Swift). Fix adds an
    // early rejection at `TicSetupVisitor.Visit(IndexedAssignmentSyntaxNode)`:
    // if the target VAR's TIC node is already a solved StateArray
    // (ee-mode immutable T[], which is what `:text` produces), throw the
    // typed `FU541` "Text is immutable — produce a new value (…) instead."
    // matching what `t.add(/'!')` already produces on `:text`. Lang-mode
    // mutable kinds (`list<T>`, `array<T>` via `:char[]`) continue to
    // support indexed-write.
    [Test]
    public void Bug53_IndexedWriteOnTextAnnotated_TypedRejection() {
        var ex = Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("t:text = 'hello'\nt[0] = /'H'"));
        StringAssert.Contains("immutable", ex.Message);
    }

    [Test]
    public void Bug53_CharArrayAnnotation_StillSupportsIndexedWrite() {
        // Sanity pin — `:char[]` (lang-mode mutable `array<Char>`) still
        // supports indexed-write so the fix doesn't over-restrict.
        var rt = Funny.Hardcore.BuildLang(
            "t:char[] = 'hello'\n" +
            "t[0] = /'H'");
        rt.Run();
        Assert.AreEqual(new[] { 'H', 'e', 'l', 'l', 'o' }, rt["t"].Value);
    }

    [Test]
    public void Bug53_UnannotatedTextLiteral_StillSupportsIndexedWrite() {
        // Unannotated `t = 'hello'` infers t as mutable lang-mode array,
        // so indexed-write succeeds. Pin the inference + mutation path.
        var rt = Funny.Hardcore.BuildLang(
            "t = 'hello'\n" +
            "t[0] = /'H'");
        rt.Run();
        Assert.AreEqual(new[] { 'H', 'e', 'l', 'l', 'o' }, rt["t"].Value);
    }

    // ──────────────────────────────────────────────────────────────────
    // Bug hunt round 11 (100 iterations, 2026-06-26) — 1 confirmed bug.
    // Independently found by both Agent 2 (HELL_AND_NESTED) and Agent 3
    // (EDGE_AND_CREATIVE); Agent 1 (SIMPLE_AND_TRICKY) ran 34+ iterations
    // with zero finds.
    // ──────────────────────────────────────────────────────────────────

    // Bug #54 — MODERATE (FIXED): a 2D+ array literal directly assigned to
    // a slot typed `T[][]?` (outer optional + inner nested array) previously
    // crashed TIC with `IncompatibleNodes` at DestructionFunctions.cs:395 →
    // `MergeInplace(descendant.ElementNode, ancestor.ElementNode)`. The two
    // inner element nodes held cross-kind solved collections (`list(I32)`
    // vs `mutArr(I32)`); MergeInplace requires invariance (and is the
    // contract guarded by 15 unit tests in `NFun.Tic.Tests`), so the
    // cross-kind pair cannot reach it.
    //
    // Root cause (per professor): `DestructionFunctions.Apply(StateCollection,
    // StateCollection)` checks cross-kind compatibility at line 387-393
    // (`list ≤ array` via the lattice), then unconditionally MergeInplaces
    // element nodes. For nested-collection element shapes, that MergeInplace
    // is the wrong primitive — it requires invariance.
    //
    // Fix at DestructionFunctions: when cross-kind is detected, dispatch the
    // element pair through `SolvingFunctions.Destruction` (recursive),
    // mirroring StateArray's Apply at line 376. The recursive Destruction
    // routes through the same Apply cell dispatch, so same-kind element
    // pairs still MergeInplace, cross-kind element pairs recurse, and the
    // invariance contract of GetMergedStateOrNull stays intact.
    //
    // The related fallout (`x:int[][]? = none; out = x ?? [[1,2]]` widens
    // to `Any`) is a SEPARATE bug in the LCA path — the Stage 1 invariance
    // pin documented in CLAUDE.md (`StateCollection.LcaOrShareIdentity`'s
    // `Element is not ICompositeState` guard). Not closed by this fix.
    [Test]
    public void Bug54_TwoDimArrayLiteralToOptionalSlot_Works() {
        var rt = Funny.Hardcore.BuildLang("a:int[][]? = [[1,2,3]]\nout = a");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    [Test]
    public void Bug54_TextNested_OuterOptional_Works() {
        var rt = Funny.Hardcore.BuildLang("a:text[][]? = [['a','b']]\nout = a");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    [Test]
    public void Bug54_InnerAndOuterOptional_Works() {
        // `int[]?[]?` — both inner and outer optional. Confirms the recursive
        // Destruction handles Optional-wrapped element shapes too.
        var rt = Funny.Hardcore.BuildLang("a:int[]?[]? = [[1,2]]\nout = a");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    [Test]
    public void Bug54_Workaround_ViaIntermediateVariable_StillWorks() {
        // Pin the previously-documented workaround so it can't silently regress.
        var rt = Funny.Hardcore.BuildLang(
            "tmp = [[1,2,3]]\n" +
            "a:int[][]? = tmp");
        rt.Run();
        Assert.IsNotNull(rt["a"].Value);
    }

    // Bug #55 (2D-depth cross-kind nested-composite LCA) — CLOSED.
    // Pinned in `Stage1InvariancePinTests.cs` + `Stage1InvariancePinAlgebraTests.cs`.
    // 3D+ residual rolled into debt #10 (worklist Pull) per professorial review.
}
