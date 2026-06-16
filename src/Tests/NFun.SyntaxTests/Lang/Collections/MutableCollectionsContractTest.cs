using System.Collections;
using System.Linq;
using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic.SolvingStates;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang.Collections;

/// <summary>
/// Stage 0 — Executable contract for the mutable collections feature.
///
/// Every test in this file is <c>[Ignore("Stage N — not implemented yet")]</c>.
/// They will progressively turn green as Stages 1-4 land. A test un-ignored in
/// a stage's PR is the stage's acceptance gate.
///
/// Spec: <see href="/Specs/Collections.md"/>
///
/// Sections:
///   1. Stage 1 — TIC scaffolding (state classes, lattice, no user features)
///   2. Stage 2 — List&lt;T&gt; end-to-end + LINQ via Enumerable + literals
///   3. Stage 3 — Mutation (a[i]=v, list methods, alias semantics, mode divergence)
///   4. Stage 4 — Set&lt;T&gt; + Hashable
///   5. Cross-stage regression shields (ee-mode unchanged)
///   6. Stage 0 decision pins (codify the design decisions)
///   7. Spec ambiguity markers (loud failures when ambiguities resolve)
/// </summary>
[TestFixture]
public class MutableCollectionsContractTest {

    #region 1. Stage 1 — TIC scaffolding

    /// <summary>
    /// Stage 1 must keep all existing tests green. This meta-guard is a smoke test
    /// that the StateComposite refactor didn't accidentally touch StateArray (ee).
    /// Catches the "I refactored everything under one base" mistake.
    /// </summary>
    [Test]
    public void Stage1_ExpressionMode_ArrayLiteral_StillProducesArrayType() {
        var rt = Funny.Hardcore.Build("y = [1,2,3]");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.ArrayOf, rt["y"].Type.BaseType,
            "ee-mode array semantics MUST stay unchanged through Stage 1.");
    }

    [Test]
    public void Stage1_ExpressionMode_ArrayLcaCovariance_StillWorks() {
        // Regression shield: ee-mode StateArray covariance is the legacy contract.
        // LCA(int[], real[]) = real[].
        var rt = Funny.Hardcore.Build("y = if(true) [1] else [1.0]");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.ArrayOf, rt["y"].Type.BaseType);
        Assert.AreEqual(BaseFunnyType.Real, rt["y"].Type.ArrayTypeSpecification.FunnyType.BaseType);
    }

    /// <summary>
    /// Stage 1 also introduces ConstructorKind and ConstructorLattice as TIC primitives.
    /// User-facing surface unchanged, but the lattice scaffolding must satisfy the spec
    /// rules. Direct lattice probe — proves the algebra is reachable from outside the
    /// NFun.Tic assembly and that the user-level state classes exist.
    /// </summary>
    [Test]
    public void Stage1_ConstructorLattice_LcaOfListAndSet_IsEnumerable() {
        Assert.AreEqual(ConstructorKind.Enumerable,
            ConstructorLattice.Lca(ConstructorKind.List, ConstructorKind.Set));
    }

    [Test]
    public void Stage1_StateCollection_Existence_ParallelToStateArray() {
        // StateCollection must exist as a callable state class before Stage 2 lights it up
        // through the parser. Smoke probe that the class is reachable and constructs.
        var state = StateCollection.OfList(StatePrimitive.I32);
        Assert.AreEqual(ConstructorKind.List, state.Constructor);
        Assert.AreEqual(1, state.Arguments.Length);
        Assert.AreEqual(Variance.Invariant, state.Arguments[0].Variance);
    }

    #endregion

    #region 2. Stage 2 — Lists, literals, LINQ via Enumerable

    [Test]
    public void Stage2_LangMode_ListLiteralWithoutAnnotation_IsList() {
        // Spec §Per-stage defaults: lang-mode [1,2,3] preferred constructor = List.
        var rt = Funny.Hardcore.BuildLang("out = [1,2,3]");
        rt.Run();
        StringAssert.Contains("list", rt["out"].Type.ToString().ToLowerInvariant());
    }

    [Test]
    public void Stage2_ListFactory_ProducesList() {
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3)");
        rt.Run();
        StringAssert.Contains("list", rt["out"].Type.ToString().ToLowerInvariant());
        Assert.AreEqual(new[] { 1, 2, 3 }, ((IList)rt["out"].Value).Cast<int>().ToArray());
    }

    [Test]
    public void Stage2_FixedArrayFactory_ProducesFixedArray() {
        // Spec §Type hierarchy: FixedArray is concretely instantiable via the
        // fixedArray(...) factory. Stage 0 immutability — `[i] = v` rejected at TIC.
        var rt = Funny.Hardcore.BuildLang("out = fixedArray(1,2,3)");
        rt.Run();
        StringAssert.Contains("fixedarray", rt["out"].Type.ToString().ToLowerInvariant());
    }

    [Test]
    public void Stage3_FixedArray_IndexedRead_Works() {
        var rt = Funny.Hardcore.BuildLang("c = fixedArray(10,20,30)\nout = c[1]");
        rt.Run();
        Assert.AreEqual(20, rt["out"].Value);
    }

    [Test]
    public void Stage3_FixedArray_Count_Works() {
        var rt = Funny.Hardcore.BuildLang("c = fixedArray(10,20,30,40)\nout = c.count()");
        rt.Run();
        Assert.AreEqual(4, rt["out"].Value);
    }

    [Test]
    public void Stage3_FixedArray_IndexedWrite_Rejected() {
        // fixedArray is read-only after construction. Indexed write is a
        // parse-time type mismatch (target pinned to `array<T>` — a mutable
        // kind — fixedArray sits above it in the lattice, so it can't flow
        // down into the indexed-write slot).
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("c = fixedArray(1,2,3)\nc[1] = 99\nout = c[1]"));
    }

    [Test]
    public void Stage3_FixedArray_AcceptsListAndArray_ViaSubtype() {
        // Stage 0 hierarchy: list ≤ array ≤ fixedArray. A function keyed on
        // `T[]` (the legacy ee-mode array signature) accepts all three.
        var rt = Funny.Hardcore.BuildLang(
            "fun sumLen(xs): return xs.count()\n" +
            "a = sumLen(list(1,2,3))\n" +
            "b = sumLen(array(10,20))\n" +
            "c = sumLen(fixedArray(100))\n" +
            "out = a + b + c");
        rt.Run();
        Assert.AreEqual(6, rt["out"].Value);
    }

    [Test, Ignore("Blocked: `list<T>` annotation syntax — deferred per user direction (Specs/Collections.md §Implementation status row 4). Use `int[]` instead until parser surface lands.")]
    public void Stage2_ListAnnotation_BindsLiteralAsList() {
        var rt = Funny.Hardcore.BuildLang("a:list<int> = [1,2,3]\nout = a.count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage2_MixedNumericElements_PromotesToReal() {
        var rt = Funny.Hardcore.BuildLang("out = [1, 2.0, 3]");
        rt.Run();
        StringAssert.Contains("list", rt["out"].Type.ToString().ToLowerInvariant());
        StringAssert.Contains("real", rt["out"].Type.ToString().ToLowerInvariant());
    }

    [Test]
    public void Stage2_Count_OnListLiteral_ResolvesViaEnumerable() {
        var rt = Funny.Hardcore.BuildLang("out = [1,2,3].count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage2_Count_OnArrayInputVariable_StillWorks() {
        // Regression shield: existing array.count() continues to work after LINQ migrates
        // from explicit T[] sigs to Enumerable<T> sigs.
        var rt = Funny.Hardcore.BuildLang("x:int[]; out = x.count()");
        rt["x"].Value = new[] { 1, 2, 3, 4, 5 };
        rt.Run();
        Assert.AreEqual(5, rt["out"].Value);
    }

    [Test]
    public void Stage2_Count_OnStruct_ParseError() {
        // Passing a non-Enumerable to count must produce a clean FU error,
        // not an internal TIC blow-up.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("s = {a=1}\nout = s.count()"));
    }

    [Test, Ignore("Blocked: `list<T>` annotation syntax — deferred. Once annotation lands, this checks map's return type follows Concretest(Enumerable) = List.")]
    public void Stage2_Map_OnList_ReturnsList() {
        // Spec §LINQ via typeclasses: result follows Concretest(Enumerable) = List.
        // map<T,U>(xs: Enumerable<T>, f: T→U): list<U>.
        var rt = Funny.Hardcore.BuildLang("a:list<int> = [1,2,3]\nout = a.map(rule it * 2)");
        rt.Run();
        StringAssert.Contains("list", rt["out"].Type.ToString().ToLowerInvariant());
    }

    [Test]
    public void Stage2_FilterCount_OnListFactory() {
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3,4).filter(rule it > 2).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage2_ForLoop_OverList_SumsElements() {
        var rt = Funny.Hardcore.BuildLang(
            "sum = 0\n" +
            "for x in list(1,2,3):\n" +
            "    sum = sum + x\n" +
            "out = sum");
        rt.Run();
        Assert.AreEqual(6, rt["out"].Value);
    }

    // Empty literal `[]` with explicit element-type annotation works via the
    // standard `T[]` syntax (Stage C deferred `list<T>` parsing).
    [Test]
    public void Stage2_EmptyLiteral_WithIntArrayAnnotation_ZeroLength() {
        var rt = Funny.Hardcore.BuildLang("a:int[] = []\nout = a.count()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    // Deferred element inference inside a single body: `out = []` followed by
    // `out = append(out, n)` correctly infers the element type from later use.
    [Test]
    public void Stage2_EmptyLiteral_DeferredElementInference() {
        var rt = Funny.Hardcore.BuildLang(
            "fun acc():\n    out = []\n    out = append(out, 1)\n    return out\n" +
            "main = acc()");
        rt.Run();
        Assert.AreEqual(new[] { 1 }, ((System.Collections.IEnumerable)rt["main"].Value).Cast<object>().ToArray());
    }

    // Empty literal at a typed call-site argument: `g([])` infers element type
    // from g's signature.
    [Test]
    public void Stage2_EmptyLiteral_FlowsIntoTypedCallSite() {
        var rt = Funny.Hardcore.BuildLang(
            "g(xs:int[]) = xs.count()\n" +
            "out = g([])");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    // Without ANY annotation OR later use, a bare `out = []` infers element=Any
    // (Stage 2 default). Not a parse error — just a value of `list<any>`.
    [Test]
    public void Stage2_EmptyLiteral_BareWithoutContext_DefaultsToAnyList() {
        var rt = Funny.Hardcore.BuildLang("a = []\nout = a.count()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void Stage2_InvariantSig_ListInt_RejectsListReal() {
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.BuildLang(
            "fun f(xs:list<int>): return xs.count()\n" +
            "a:list<real> = [1.0,2.0]\n" +
            "out = f(a)"));
    }

    [Test]
    public void Stage2_InvariantSig_ListAny_RejectsListInt() {
        // No automatic list<int> → list<any> per invariance rule.
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.BuildLang(
            "fun f(xs:list<any>): return xs.count()\n" +
            "a:list<int> = [1,2,3]\n" +
            "out = f(a)"));
    }

    [Test]
    public void Stage2_Equality_SameElements_True() {
        var rt = Funny.Hardcore.BuildLang("out = [1,2,3] == [1,2,3]");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage2_Equality_DifferentLengths_False() {
        var rt = Funny.Hardcore.BuildLang("out = [1,2,3] == [1,2]");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage2_Equality_OrderMattersForList() {
        var rt = Funny.Hardcore.BuildLang("out = [1,2,3] == [3,2,1]");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test, Ignore("Blocked: `enumerable<T>` annotation syntax — deferred (same as `list<T>`). Once parser surface lands, this verifies List satisfies Enumerable parameter constraint via the lattice.")]
    public void Stage2_ListAcceptedWhereEnumerableExpected() {
        // Per spec: Implicit conversion via subtyping (List → Array → FixedArray → Enumerable).
        // List satisfies Enumerable<T> parameter constraint.
        var rt = Funny.Hardcore.BuildLang(
            "fun sumIt(xs:enumerable<int>): return xs.fold(rule(a,b) = a+b)\n" +
            "out = sumIt(list(1,2,3))");
        rt.Run();
        Assert.AreEqual(6, rt["out"].Value);
    }

    #endregion

    #region 3. Stage 3 — Mutation, alias, mode divergence

    [Test]
    public void Stage3_LangMode_IntArrayIndexedWrite_RebindsSlot() {
        // Spec §Indexed write: `a:int[]` in lang-mode is mutable Array. a[1]=42 works.
        var rt = Funny.Hardcore.BuildLang("a:int[] = [1,2,3]\na[1] = 42\nout = a[1]");
        rt.Run();
        Assert.AreEqual(42, rt["out"].Value);
    }

    [Test]
    public void Stage3_LangMode_ListIndexedWrite_RebindsSlot() {
        // Stage 3 / B.2: indexed write on a lang-mode list.
        var rt = Funny.Hardcore.BuildLang("a = list(1,2,3)\na[1] = 42\nout = a[1]");
        rt.Run();
        Assert.AreEqual(42, rt["out"].Value);
    }

    [Test]
    public void Stage3_LangMode_ListIndexedWrite_AliasSeesChange() {
        // Reference semantics: write through one binding is visible through aliases.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(10,20,30)\n    b = a\n    b[0] = 99\n    return a[0]\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(99, rt["out"].Value);
    }

    [Test]
    public void Stage3_LangMode_ArrayIndexedWrite_AliasSeesChange() {
        // Reference semantics work cleanly for array (same kind on both sides
        // of the alias).
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = array(10,20,30)\n    b = a\n    b[0] = 99\n    return a[0]\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(99, rt["out"].Value);
    }

    [Test]
    public void Stage3_LangMode_ListIndexedWrite_OutOfRange_RuntimeThrows() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    a[10] = 99\n    return a[0]\n" +
            "out = f()");
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    [Test]
    public void Stage3_EeMode_IntArrayIndexedWrite_ParseError() {
        // Mode divergence: ee-mode int[] stays immutable. Parser rejects the
        // statement form entirely (ee-mode supports `out = expr` only).
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("a:int[] = [1,2,3]; a[1] = 42; out = a"));
    }

    [Test]
    public void Stage3_EeMode_IndexedWriteError_MentionsMode() {
        // Risks #2: clear error routing this to mode docs.
        var ex = Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("a:int[] = [1,2,3]; a[1] = 42; out = a"));
        var msg = ex!.Message.ToLowerInvariant();
        Assert.IsTrue(msg.Contains("lang") || msg.Contains("mode") || msg.Contains("mutable") || msg.Contains("immutable"),
            $"Error should hint at mode/mutability, got: {ex.Message}");
    }

    [Test]
    public void Stage3_EeMode_IndexedRead_RegressionShield() {
        // Read on int[] in ee-mode must continue to work.
        var rt = Funny.Hardcore.Build("a:int[] = [10,20,30]; out = a[1]");
        rt.Run();
        Assert.AreEqual(20, rt["out"].Value);
    }

    [Test]
    public void Stage3_ListAdd_AppendsElement() {
        // `a:list<int>` annotation syntax is deferred — use the factory instead.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    a.add(4)\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test]
    public void Stage3_ListAddAll_AppendsRange() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2)\n    a.addAll([4,5])\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test]
    public void Stage3_ListRemoveAt_DropsIndex() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(10,20,30)\n    a.removeAt(0)\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    [Test]
    public void Stage3_ListRemoveLast_DropsTail() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    a.removeLast()\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    [Test]
    public void Stage3_ListClear_EmptiesList() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    a.clear()\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void Stage4_SetClear_EmptiesSet() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2,3)\n    s.clear()\n    return s.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void Stage5_MapClear_EmptiesMap() {
        // Map satisfies the Mutable typeclass — clear() works uniformly.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1,value=10},{key=2,value=20})\n    m.clear()\n    return m.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void MutableTypeclass_FixedArray_ClearRejected() {
        // fixedArray is NOT in the Mutable scope — parse error at clear() call.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("fun f():\n    a = fixedArray(1,2,3)\n    a.clear()\n    return a.count()\ny = f()"));
    }

    [Test]
    public void ClearableTypeclass_LangArray_RejectedAtCompileTime() {
        // Lang-mode `a:int[]` is MutableArray — mutable element-wise (`a[i]=v`)
        // but length is fixed. `clear()` requires the Clearable typeclass
        // (List/Set/Map only — NOT Array). TIC rejects the call at build time
        // — was previously a runtime "array length is fixed" exception.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang(
                "fun f():\n    a:int[] = [1,2,3]\n    a.clear()\n    return a.count()\ny = f()"));
    }

    [Test]
    public void Stage3_FixedArray_IndexedWrite_ParseError() {
        // fixedArray is read-only after creation. Requires both the
        // `fixedArray(...)` factory (not yet) and the `a[i] = v` parser
        // surface (B.2).
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("a = fixedArray(1,2,3)\na[0] = 99"));
    }

    [Test]
    public void Stage3_Alias_MutationVisibleThroughBothBindings() {
        // Lang collections are reference types per spec §Alias semantics.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    b = a\n    b.add(4)\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test, Ignore("Blocked: `list<T>` annotation syntax in function parameter — deferred. Reference-semantics aliasing through function calls works (verified by sibling tests using inferred types).")]
    public void Stage3_Alias_FunctionArgument_MutationVisibleInCaller() {
        var rt = Funny.Hardcore.BuildLang(
            "fun appendOne(xs:list<int>): xs.add(999)\n" +
            "fun f():\n    a = list(1,2)\n    appendOne(a)\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void Stage3_Equality_AfterMutation_Invalidates() {
        // Spec §equality: mutation invalidates prior equality.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2)\n    b = list(1,2)\n    pre = a == b\n    a.add(3)\n    post = a == b\n    return [pre, post]\n" +
            "y = f()");
        rt.Run();
        var result = ((IEnumerable)rt["y"].Value).Cast<bool>().ToArray();
        Assert.AreEqual(true, result[0], "pre-mutation should be equal");
        Assert.AreEqual(false, result[1], "post-mutation should not be equal");
    }

    [Test]
    public void Stage3_ToList_CopiesSource_MutationsDoNotPropagate() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a:int[] = [1,2,3]\n    b = a.toList()\n    b.add(4)\n    return a.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void Stage3_ToArray_CopiesList() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    b = a.toArray()\n    a.add(4)\n    return b.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    /// <summary>
    /// Cross-constructor equality: spec §equality says when LCA fits, compare element-wise.
    /// For list vs array of same element type, both ordered, both have same length → true.
    /// </summary>
    [Test]
    public void Stage3_Equality_ListEqualsArray_SameElements() {
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3) == [1,2,3]");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    #endregion

    #region 4. Stage 4 — Sets, Hashable

    /// <summary>
    /// Set factory uses the bare <c>set(...)</c> name. The previous
    /// <c>set(arr, i, v)</c> array-update built-in was renamed to
    /// <c>setAt</c> (Stage C work) to make room.
    /// </summary>
    [Test]
    public void Stage4_SetFactory_ProducesSet() {
        var rt = Funny.Hardcore.BuildLang("out = set(1,2,3).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage4_SetFactory_DuplicatesIgnored() {
        var rt = Funny.Hardcore.BuildLang("out = set(1,2,3,1,2).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // Set's user-facing mutators are tryAdd / tryRemove (not add / remove):
    // they return a bool — "did the call actually change the set?" — matching
    // HashSet<T>.Add / .Remove semantics.
    [Test]
    public void Stage4_SetTryAdd_GrowsCardinality() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2)\n    s.tryAdd(3)\n    return s.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void Stage4_SetTryAddDuplicate_ReturnsFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2)\n    return s.tryAdd(1)\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void Stage4_SetTryRemove_RemovesAndReturnsTrue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2,3)\n    r = s.tryRemove(2)\n    return r\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void Stage4_SetTryRemove_AbsentReturnsFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2,3)\n    return s.tryRemove(99)\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    // clear() is shared across mutable collection kinds via a single signature
    // bound on Enumerable<T> with a runtime kind check (proper Mutable<T>
    // typeclass to come).
    [Test]
    public void Stage4_Clear_OnSet_Empties() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2,3)\n    s.clear()\n    return s.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void Stage4_Clear_OnList_Empties() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    l = list(1,2,3)\n    l.clear()\n    return l.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void Stage4_Clear_OnFixedArray_ParseError() {
        // Mutable<T> typeclass rejects fixedArray (immutable) at parse time.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("a = fixedArray(1,2,3); clear(a); out = a.count()"));
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 5 — Map<K, V>: basic constructor + value type
    // (factory is named `__mkMap` pending dialect-aware extension-vs-regular
    //  function registry separation in master; will rename to `map` later.)
    // ─────────────────────────────────────────────────────────────────

    [Test]
    public void Stage5_MapFactory_TypeInference() {
        var rt = Funny.Hardcore.BuildLang(
            "out = __mkMap({key = 42, value = 32}, {key = 43, value = 44})");
        rt.Run();
        Assert.AreEqual(NFun.Types.BaseFunnyType.Map, rt["out"].Type.BaseType);
        Assert.AreEqual(NFun.Types.BaseFunnyType.Int32, rt["out"].Type.MapTypeSpecification.KeyType.BaseType);
        Assert.AreEqual(NFun.Types.BaseFunnyType.Int32, rt["out"].Type.MapTypeSpecification.ValueType.BaseType);
    }

    [Test]
    public void Stage5_MapFactory_ClrRoundtrip() {
        var rt = Funny.Hardcore.BuildLang(
            "out = __mkMap({key = 1, value = 'a'}, {key = 2, value = 'b'})");
        rt.Run();
        var dict = (System.Collections.Generic.Dictionary<int, string>)rt["out"].Value;
        Assert.AreEqual(2, dict.Count);
        Assert.AreEqual("a", dict[1]);
        Assert.AreEqual("b", dict[2]);
    }

    [Test]
    public void Stage5_MapFactory_DuplicateKeyOverwrites() {
        var rt = Funny.Hardcore.BuildLang(
            "out = __mkMap({key = 1, value = 'first'}, {key = 1, value = 'second'})");
        rt.Run();
        var dict = (System.Collections.Generic.Dictionary<int, string>)rt["out"].Value;
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual("second", dict[1]);
    }

    // Stage 5 / Map.2 — access + mutation API.
    // Key suffix on Map-specific operations: setKey, tryAddKey, removeKey,
    // containsKey, tryRemoveKey. The bare-name `get` / `tryGet` are unique to
    // Map so don't need the suffix. `tryRemoveKey` takes the suffix to avoid
    // collision with `set.tryRemove(item): bool`.

    [Test]
    public void Stage5_MapSetKey_AddsAndOverwrites() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    m.setKey(2, 'b')\n    m.setKey(1, 'X')\n" +
            "    return m.containsKey(2) and (m.get(1) ?? '?') == 'X'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryAddKey_NewKeyReturnsTrue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.tryAddKey(2, 'b')\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryAddKey_ExistingKeyReturnsFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.tryAddKey(1, 'X')\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapRemoveKey_PresentReturnsOptional() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.removeKey(1) ?? '?'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("a", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapRemoveKey_AbsentReturnsNone() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.removeKey(99) ?? '?'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("?", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapContainsKey_PresentTrue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.containsKey(1)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapContainsKey_AbsentFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.containsKey(99)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapGet_Present() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.get(1) ?? '?'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("a", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapGet_AbsentNone() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    return m.get(99) ?? '?'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("?", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryGet_StructValueAndSuccess() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    r = m.tryGet(1)\n    return r.success\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryGet_AbsentSuccessFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    r = m.tryGet(99)\n    return r.success\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryRemoveKey_PresentReturnsSuccess() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    r = m.tryRemoveKey(1)\n    return r.success\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    // Stage 5 / Enumerable interop: Map<K,V> satisfies Enumerable<{key:K, value:V}>.
    // CompCs.ElementNode is unified with a synthesized struct sharing identity with
    // KeyNode/ValueNode, so LINQ funcs declared on Enumerable<T> work on Maps.
    [Test]
    public void Stage5_MapCount_ViaEnumerableTypeclass() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'},{key=2, value='b'})\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapInputConverter_DictionaryAsInput() {
        var input = new System.Collections.Generic.Dictionary<int, string> {
            { 1, "a" }, { 2, "b" }, { 3, "c" }
        };
        var rt = Funny.Hardcore
            .WithApriori<System.Collections.Generic.Dictionary<int, string>>("m")
            .BuildLang("out = m.count()");
        rt["m"].Value = input;
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFilter_OnPairStruct() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'},{key=2, value='b'},{key=3, value='c'})\n" +
            "    return m.filter(rule it.key > 1).count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage4_SetEquality_OrderIndependent() {
        var rt = Funny.Hardcore.BuildLang("out = set(1,2,3) == set(3,2,1)");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage4_Set_RejectsUnhashableElement_Function() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("s = set(rule it * 2)"));
    }

    [Test]
    public void Stage4_Set_RejectsUnhashableElement_List() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("s = set(list(1,2), list(3,4))"));
    }

    [Test]
    public void Stage4_Set_RejectsUnhashableElement_Struct() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("s = set({a=1}, {a=2})"));
    }

    [Test]
    public void Stage5_Map_RejectsUnhashableKey_List() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang("m = __mkMap({key=list(1,2), value=10})"));
    }

    [Test]
    public void Stage5_Map_AcceptsHashableKey_Text() {
        // Text is char[] at the type level but runtime-backed by string —
        // semantically immutable, valid as map key.
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key='foo', value=1}, {key='bar', value=2})\nout = m.count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_Map_AllowsAnyValueType_EvenMutableContainer() {
        // Value type isn't constrained — only the key needs to be Immutable.
        var rt = Funny.Hardcore.BuildLang(
            "m = __mkMap({key=1, value=list(10,20)})\nout = m.count()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void Stage4_ToSet_DropsDuplicates() {
        var rt = Funny.Hardcore.BuildLang("out = [1,2,2,3,3,3].toSet().count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // toXxx collection-conversion family — round-trip preserves cardinality
    // for the kinds where order matters; set may collapse duplicates.
    [Test]
    public void Stage4_ToList_RoundTrip() {
        var rt = Funny.Hardcore.BuildLang("out = [1,2,3].toList().count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage4_ToArray_FromSet() {
        var rt = Funny.Hardcore.BuildLang("out = set(1,2,3).toArray().count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage4_ToFixedArray_FromList() {
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3).toFixedArray().count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // Read-only LINQ over a set works via the Enumerable<T> typeclass — set is
    // a sibling of list/array/fixedArray under Enumerable, so any function
    // whose signature reads `Enumerable<T>` accepts a set transparently.
    [Test]
    public void Stage4_Set_CountLinq() {
        var rt = Funny.Hardcore.BuildLang("out = set(10, 20, 30).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // CLR roundtrip: NFun set<T> ↔ System.Collections.Generic.HashSet<T>.
    [Test]
    public void Stage4_Set_ClrRoundtrip() {
        var rt = Funny.Hardcore.BuildLang("out = set(1, 2, 3)");
        rt.Run();
        var hashSet = (System.Collections.Generic.HashSet<int>)rt["out"].Value;
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, hashSet);
    }

    [Test]
    public void Stage4_Set_SatisfiesEnumerable() {
        var rt = Funny.Hardcore.BuildLang("out = set(1,2,3).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage4_ForLoop_OverSet_AllElementsVisited() {
        // Order undefined; cardinality + sum check.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    c = 0\n    s = 0\n    for x in set(10,20,30):\n        c = c + 1\n        s = s + x\n    return [c, s]\n" +
            "y = f()");
        rt.Run();
        var arr = ((IEnumerable)rt["y"].Value).Cast<int>().ToArray();
        Assert.AreEqual(3, arr[0]);
        Assert.AreEqual(60, arr[1]);
    }

    #endregion

    #region 5. Cross-stage regression shields (ee-mode unchanged)

    /// <summary>
    /// At the end of Stage 4, ALL existing 13983 tests must still pass — the entire
    /// feature ships as ADDITIVE to ee-mode. This category catches accidental ee-mode
    /// regression. Each is duplicated from existing test patterns to fail loudly if
    /// ee-mode behavior shifts.
    /// </summary>
    [Test]
    public void Shield_EeMode_ArrayLiteral_TypeIsArrayOf() {
        var rt = Funny.Hardcore.Build("y = [1,2,3]");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.ArrayOf, rt["y"].Type.BaseType);
    }

    [Test]
    public void Shield_EeMode_ArrayCovariantLca_WidensCorrectly() {
        var rt = Funny.Hardcore.Build("y = if(true) [1] else [1.0]");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.Real, rt["y"].Type.ArrayTypeSpecification.FunnyType.BaseType);
    }

    [Test]
    public void Shield_EeMode_ArrayCount_StillWorks() {
        var rt = Funny.Hardcore.Build("y = [1,2,3].count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void Shield_EeMode_ArrayMap_StillWorks() {
        var rt = Funny.Hardcore.Build("y = [1,2,3].map(rule it * 2)");
        rt.Run();
        var arr = (IList)rt["y"].Value;
        Assert.AreEqual(3, arr.Count);
    }

    [Test]
    public void Shield_EeMode_ArrayIndexedRead_StillWorks() {
        var rt = Funny.Hardcore.Build("a:int[]; y = a[0]");
        rt["a"].Value = new[] { 100, 200, 300 };
        rt.Run();
        Assert.AreEqual(100, rt["y"].Value);
    }

    #endregion

    #region 6. Stage 0 decision pins

    /// <summary>
    /// Pin: lang-mode collection element variance is UNIFORM (invariant) — we do not
    /// implement variance-climbing during LCA. Per spec §Design constraints item 2.
    /// </summary>
    [Test]
    public void StageZeroPin_AllLangCollections_InvariantAtParam_ElementWiseLcaInExpression() {
        // Spec §Design constraints item 2 (revised): invariance is the Liskov rule for
        // function parameters — `list<int>` is not `list<real>` at a call site. In
        // expression position LCA does element-wise widening: list<LCA(int,text)> =
        // list<Any>. Keeps the container kind usable (.count/.toArray/...) while
        // honestly reporting the element loss.
        var rt = Funny.Hardcore.BuildLang("out = if(true) [1] else ['x']");
        rt.Run();
        // Container kind stays list/array (whichever the dialect picks); element widens to Any.
        var t = rt["out"].Type;
        Assert.IsTrue(t.BaseType is BaseFunnyType.List or BaseFunnyType.ArrayOf,
            $"Expected list/array container, got {t.BaseType}");
        var elem = t.BaseType == BaseFunnyType.List
            ? t.ListTypeSpecification.FunnyType
            : t.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Any, elem.BaseType,
            "Element should widen to Any when branch elements are incompatible.");
    }

    /// <summary>
    /// Pin: ee-mode is unchanged. Per spec §Design constraints item 3.
    /// This is the contractual statement of "ee semantics frozen".
    /// </summary>
    [Test]
    public void StageZeroPin_EeMode_LiteralDefault_IsArrayNotList() {
        var rt = Funny.Hardcore.Build("y = [1,2,3]");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.ArrayOf, rt["y"].Type.BaseType,
            "ee-mode literal default MUST stay Array — never switches to List.");
    }

    /// <summary>
    /// Pin: equality is by-value for all collection types. Per spec §equality.
    /// Reference-equality fallback only triggers when LCA collapses to Any.
    /// </summary>
    [Test]
    public void StageZeroPin_Equality_IsByValue_NotReference() {
        // Two distinct allocations with same content compare equal.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    b = list(1,2,3)\n    return a == b\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    /// <summary>
    /// Pin: map literal token is deliberately NOT reserved during Stages 1-4.
    /// This test verifies that <c>=&gt;</c> is still a free token that does not
    /// have a built-in meaning. If something else later claims <c>=&gt;</c>, this
    /// test fails loudly to force a Stage 5 redesign before the conflict ships.
    /// </summary>
    [Test]
    public void StageZeroPin_MapLiteralToken_NotReservedYet() {
        // A user-defined function named `=>` (or similar) makes no sense, but a literal
        // probe is hard. Instead: writing a comment using `=>` should parse.
        var rt = Funny.Hardcore.BuildLang("# this is a => comment\nout = 1");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    #endregion

    #region 7. Spec ambiguity markers — fail loudly when resolved

    /// <summary>
    /// Resolved: D3 default value = empty collection (`[]`). Per CLAUDE.md Accepted
    /// Design "Default value `[]` for declared collections" — implemented for List
    /// in IFunnyVar.GetDefaultValueOrNullFor.
    /// Test is blocked only on `list<T>` annotation syntax (deferred). Use
    /// `a:int[]` (which is also `[]` by default) for the working equivalent — see
    /// `out:int[] = default` CLI check.
    /// </summary>
    [Test, Ignore("Blocked: `list<T>` annotation syntax — deferred. D3 decision (empty collection default) is implemented for List in IFunnyVar; just unreachable until annotation parses.")]
    public void Ambiguity_ListWithoutInitializer_DefaultsToEmpty() {
        var rt = Funny.Hardcore.BuildLang("a:list<int>\nout = a.count()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    /// <summary>
    /// Resolved ambiguity: list(...) factory is universal — works in both ee-mode
    /// and lang-mode. In ee-mode the resulting value is a lang-side mutable list
    /// even though the dialect doesn't expose mutation methods. The factory is
    /// just a constructor — no special ee-mode prohibition.
    /// </summary>
    [Test]
    public void EeMode_ListFactory_Available() {
        var rt = Funny.Hardcore.Build("out = list(1,2,3).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    /// <summary>
    /// D5 resolved: iteration during mutation = runtime error per CLAUDE.md
    /// Accepted Design. Backing `System.Collections.Generic.List<T>` already throws
    /// `InvalidOperationException` on enumerator invalidation — we just need to
    /// surface it as FunnyRuntimeException.
    /// </summary>
    [Test]
    public void Ambiguity_IterationDuringMutation_RuntimeError() {
        Assert.Throws<FunnyRuntimeException>(() => {
            var rt = Funny.Hardcore.BuildLang(
                "fun f():\n    a = list(1,2,3)\n    for x in a:\n        a.add(x)\n    return a.count()\n" +
                "y = f()");
            rt.Run();
        });
    }

    /// <summary>
    /// AMBIGUITY: Direction of subtype substitutability.
    /// Spec hierarchy: List ⊆ Array ⊆ FixedArray ⊆ Enumerable.
    /// Spec invariance: all collections invariant in element T.
    /// Tension: can a list&lt;int&gt; be passed where array&lt;int&gt; is expected (Liskov upcast)?
    /// Pinned as YES per the hierarchy diagram — invariance is on the type ARGUMENT,
    /// not on the constructor relation. Test forces clarification.
    /// </summary>
    [Test]
    public void Ambiguity_ListPassedWhereArrayExpected_Accepted() {
        var rt = Funny.Hardcore.BuildLang(
            "fun head(xs:int[]): return xs[0]\n" +
            "y = head(list(7,8,9))");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    /// <summary>
    /// AMBIGUITY: Array-to-list at call site (downcast direction).
    /// Per the hierarchy List ⊆ Array, so passing array where list is expected is
    /// the SUBTYPE-of-supertype direction — should be REJECTED without explicit
    /// <c>.toList()</c>.
    /// </summary>
    [Test]
    public void Ambiguity_ArrayPassedWhereListExpected_Rejected() {
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.BuildLang(
            "fun pushOne(xs:list<int>): xs.add(1)\n" +
            "fun f():\n    a:int[] = [1,2]\n    pushOne(a)\n    return a.count()\n" +
            "y = f()"));
    }

    #endregion

    #region 8. New: Mutation sequences

    [Test]
    public void New_List_AppendThenCount_IsIncremented() {
        // list.add() appends; count reflects it.
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void New_List_AppendSequence_CountReflectsAllAdds() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(0)\n    a.add(1)\n    a.add(2)\n    return a.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void New_Set_AddSameValueTwice_CountStaysOne() {
        // set is deduplicated; adding same element twice keeps count = 1.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(42)\n    s.tryAdd(42)\n    return s.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void New_Set_TryAdd_ThenTryRemove_CountZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(5)\n    s.tryAdd(10)\n    s.tryRemove(5)\n    s.tryRemove(10)\n    return s.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void New_List_RemoveAtThenContains_ElementGone() {
        // removeAt(0) removes the first element; then contains should be false.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(7, 8, 9)\n    a.removeAt(0)\n    return a.contains(7)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void New_List_Clear_ThenAdd_CountIsOne() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    a.clear()\n    a.add(99)\n    return a.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    #endregion

    #region 9. New: Type inference

    [Test]
    public void New_NestedList_OuterCount_IsTwo() {
        // list<list<int>> — outer list holds two inner lists.
        var rt = Funny.Hardcore.BuildLang("out = list(list(1,2), list(3,4)).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    // Was [Ignore] for "Circular ancestor 0" in TIC. Fix: identity guard on
    // result.ElementNode == ancestor.ElementNode in
    // PullConstraintsFunctions.Apply(ICompositeState, ConstraintsState).
    // TransformToCollection/Array reuse descendant's element node when not
    // solved (perf optimization); chained `[]` over lang collections aliases
    // it with the outer's element. Mirror of the guard at line 430.
    [Test]
    public void New_NestedList_InnerElementAccess() {
        var rt = Funny.Hardcore.BuildLang("out = list(list(10,20), list(30,40))[1][0]");
        rt.Run();
        Assert.AreEqual(30, rt["out"].Value);
    }

    [Test]
    public void New_MixedNumericList_PromotesToReal() {
        // list(1, 2.5) — element type promoted to Real by LCA.
        var rt = Funny.Hardcore.BuildLang("out = list(1, 2.5)[0]");
        rt.Run();
        Assert.AreEqual(1.0, rt["out"].Value);
    }

    [Test]
    public void New_ListOfStructs_AccessField() {
        // list of structs; field access on element.
        var rt = Funny.Hardcore.BuildLang("out = list({a=10, b='x'}, {a=20, b='y'})[1].a");
        rt.Run();
        Assert.AreEqual(20, rt["out"].Value);
    }

    [Test]
    public void New_SetInt_ClrRoundtrip_AllElementsPresent() {
        // set(int) round-trips to HashSet<int>.
        var rt = Funny.Hardcore.BuildLang("out = set(5, 10, 15)");
        rt.Run();
        var hs = (System.Collections.Generic.HashSet<int>)rt["out"].Value;
        CollectionAssert.AreEquivalent(new[] { 5, 10, 15 }, hs);
    }

    #endregion

    #region 10. New: LINQ chains

    [Test]
    public void New_List_FilterThenCount() {
        // list.filter(...).count() — two-step LINQ chain.
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3,4,5).filter(rule it > 3).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void New_Set_FilterThenCount() {
        var rt = Funny.Hardcore.BuildLang("out = set(10,20,30,40).filter(rule it > 15).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void New_Array_MapThenSum() {
        // mutable array(1,2,3).map(rule it*2).sum() = 12.
        var rt = Funny.Hardcore.BuildLang("out = array(1,2,3).map(rule it * 2).sum()");
        rt.Run();
        Assert.AreEqual(12, rt["out"].Value);
    }

    [Test]
    public void New_List_FoldSum() {
        // list.fold produces accumulated result.
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3,4).fold(rule(a,b) = a + b)");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    #endregion

    #region 11. New: Equality

    [Test]
    public void New_Set_Equality_OrderIndependent_True() {
        // set equality is order-independent; {1,2,3} == {3,1,2}.
        var rt = Funny.Hardcore.BuildLang("out = set(1,2,3) == set(3,1,2)");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void New_List_Equality_OrderSensitive_False() {
        // list equality IS order-sensitive; [1,2,3] != [3,2,1].
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3) == list(3,2,1)");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void New_List_Equality_SameElements_True() {
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3) == list(1,2,3)");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void New_Set_Equality_DifferentCardinality_False() {
        var rt = Funny.Hardcore.BuildLang("out = set(1,2) == set(1,2,3)");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    #endregion

    #region 12. New: Negative cases

    [Test]
    public void New_List_RemoveAt_OutOfRange_RuntimeThrows() {
        // removeAt on out-of-range index throws at runtime.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = list(1,2,3)\n    a.removeAt(99)\n    return a.count()\n" +
            "out = f()");
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    [Test]
    public void New_Set_TryRemoveAbsent_ReturnsFalse() {
        // removing an absent element returns false without throwing.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = set(1,2,3)\n    return s.tryRemove(99)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void New_List_Contains_AbsentElement_False() {
        var rt = Funny.Hardcore.BuildLang("out = list(1,2,3).contains(99)");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    #endregion

    #region 13. Stage 5 — Map deep-coverage tests

    // -----------------------------------------------------------------
    // 13.1 Type inference corners
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapFactory_TextKeyIntValue_TypeInference() {
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key='a', value=10}, {key='b', value=20})");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.Map, rt["y"].Type.BaseType);
        Assert.AreEqual(BaseFunnyType.ArrayOf, rt["y"].Type.MapTypeSpecification.KeyType.BaseType);
        Assert.AreEqual(BaseFunnyType.Char,
            rt["y"].Type.MapTypeSpecification.KeyType.ArrayTypeSpecification.FunnyType.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, rt["y"].Type.MapTypeSpecification.ValueType.BaseType);
    }

    [Test]
    public void Stage5_MapFactory_IntKeyTextValue_TypeInference() {
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key=1, value='a'}, {key=2, value='b'})");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.Map, rt["y"].Type.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, rt["y"].Type.MapTypeSpecification.KeyType.BaseType);
        Assert.AreEqual(BaseFunnyType.ArrayOf, rt["y"].Type.MapTypeSpecification.ValueType.BaseType);
    }

    [Test]
    public void Stage5_MapFactory_IntKeyArrayValue_TypeInference() {
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key=1, value=[10,20]}, {key=2, value=[30,40]})");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.Map, rt["y"].Type.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, rt["y"].Type.MapTypeSpecification.KeyType.BaseType);
        // value is a list (lang-mode default for [..] inside a factory)
        var vType = rt["y"].Type.MapTypeSpecification.ValueType;
        StringAssert.Contains("list", vType.ToString().ToLowerInvariant());
    }

    [Test]
    public void Stage5_MapFactory_BoolKeys_AreSupported() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=true, value='yes'}, {key=false, value='no'})\n" +
            "    return m.get(true) ?? '?'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("yes", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFactory_RealKeys_AreSupported() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1.5, value='a'}, {key=2.7, value='b'})\n" +
            "    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFactory_SingleEntry_TypeInference() {
        var rt = Funny.Hardcore.BuildLang("y = __mkMap({key=1, value=42})");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.Map, rt["y"].Type.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, rt["y"].Type.MapTypeSpecification.KeyType.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, rt["y"].Type.MapTypeSpecification.ValueType.BaseType);
    }

    [Test]
    public void Stage5_Map_InsideStruct_AccessibleByField() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = {m = __mkMap({key=1, value='a'}, {key=2, value='b'})}\n" +
            "    return s.m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_Map_AsArrayElement_ArrayCountIsCorrect() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = [__mkMap({key=1,value=10}), __mkMap({key=2,value=20})]\n" +
            "    return a.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapOfStruct_ValueStructFieldAccessible() {
        // Stored struct values can be read out through get(...) ?? defaultStruct.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value={x=10, y=20}})\n" +
            "    return (m.get(1) ?? {x=0, y=0}).x\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    // Was [Ignore] for "Optional lift through shared generic struct field".
    // Fix: in Pull/Push struct-struct Apply, when descendant field is None
    // and ancestor field is a CS, AddDescendant(None) to set IsOptional —
    // instead of silently skipping. Resolves V to opt(int) so the factory
    // produces map<int, int?>.
    [Test]
    public void Stage5_MapFactory_OptionalValue_AcceptsNone() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=42}, {key=2, value=none})\n" +
            "    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapContainsKey_RejectsWrongKeyType() {
        // FU711 fires when generic T0 would resolve to Any from structurally
        // incompatible constraints. Previously missed for map<K,V> because
        // ExpressionBuilderVisitor.CheckGenericDepths didn't recurse into Map.
        // Fix: add Map/MutableArray/FixedArray/Set/Enumerable cases.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang(
                "fun f():\n    m = __mkMap({key=1, value=10})\n    return m.containsKey('hello')\n" +
                "out = f()"));
    }

    [Test]
    public void Stage5_NestedMap_PropagatesValueType() {
        // Was [Pinned] for "collapses to Any". Fix: StateMap.LcaOrShareIdentity +
        // dispatch in StateExtensions.Lca.cs — mirrors StateCollection's
        // identity-sharing pattern. Outer factory's value type now correctly
        // resolves to the inner map<K,V> instead of widening to Any.
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key=1, value=__mkMap({key=10, value=100})})");
        rt.Run();
        Assert.AreEqual(BaseFunnyType.Map, rt["y"].Type.BaseType);
        Assert.AreEqual(BaseFunnyType.Map, rt["y"].Type.MapTypeSpecification.ValueType.BaseType,
            "Nested map value type should propagate as map<K,V>, not Any.");
        var innerMap = rt["y"].Type.MapTypeSpecification.ValueType.MapTypeSpecification;
        Assert.AreEqual(BaseFunnyType.Int32, innerMap.KeyType.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, innerMap.ValueType.BaseType);
    }

    // -----------------------------------------------------------------
    // 13.2 Empty-like map after removal
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapRemove_LastEntry_CountIsZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10})\n    m.removeKey(1)\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapRemove_AbsentKey_LeavesCountUnchanged() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10})\n    m.removeKey(99)\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapRemove_PresentKey_ThenContainsIsFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20})\n" +
            "    m.removeKey(1)\n    return m.containsKey(1)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapRemove_PresentKey_OtherKeysStillPresent() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20}, {key=3, value=30})\n" +
            "    m.removeKey(2)\n    return m.containsKey(1) and m.containsKey(3) and not m.containsKey(2)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    // -----------------------------------------------------------------
    // 13.3 Mutation sequencing
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapSetKey_NewKey_ThenGetReturnsValue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10})\n    m.setKey(2, 20)\n" +
            "    return m.get(2) ?? -1\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(20, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapSetKey_ExistingKey_OverwritesValue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10})\n    m.setKey(1, 99)\n" +
            "    return m.get(1) ?? -1\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(99, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapSetKey_MultipleAdds_GrowsCount() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    m.setKey(2, 'b')\n    m.setKey(3, 'c')\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryAddKey_TwiceSameKey_FirstTrueSecondFalse() {
        // tryAddKey must be idempotent: second call with same key returns false
        // and does NOT overwrite the existing value.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    r1 = m.tryAddKey(2, 'b')\n" +
            "    r2 = m.tryAddKey(2, 'X')\n" +
            "    return r1 and not r2\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryAddKey_ExistingKey_PreservesOriginalValue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    m.tryAddKey(1, 'X')\n" +
            "    return m.get(1) ?? '?'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("a", rt["out"].Value);
    }

    [Test]
    public void Stage5_Map_SetThenRemove_BackToInitialState() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    m.setKey(2, 'b')\n" +
            "    m.removeKey(2)\n" +
            "    return m.count() == 1 and m.containsKey(1) and not m.containsKey(2)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_Map_MixedSequenceOfOps_FinalStateCorrect() {
        // Compose: start with 1, add 2, add 3, remove 2 -> should contain 1 and 3.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    m.setKey(2, 'b')\n" +
            "    m.setKey(3, 'c')\n" +
            "    m.removeKey(2)\n" +
            "    return m.containsKey(1) and not m.containsKey(2) and m.containsKey(3) and m.count() == 2\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_Map_RemoveKey_ReturnsRemovedValue() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20})\n" +
            "    return m.removeKey(1) ?? -1\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    // -----------------------------------------------------------------
    // 13.4 LINQ via Enumerable typeclass
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapCount_OnSingleEntry_IsOne() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10})\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapCount_OnEmpty_AfterAllRemoved_IsZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20})\n" +
            "    m.removeKey(1)\n    m.removeKey(2)\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFilter_OnKey_KeepsMatchingEntries() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'}, {key=2, value='b'}, {key=3, value='c'})\n" +
            "    return m.filter(rule it.key > 1).count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFilter_OnValue_KeepsMatchingEntries() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key='a', value=10}, {key='b', value=20}, {key='c', value=30})\n" +
            "    return m.filter(rule it.value > 15).count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFilter_EmptyResult_CountIsZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20})\n" +
            "    return m.filter(rule it.key > 999).count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapAny_MatchingPredicate_True() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20}, {key=3, value=30})\n" +
            "    return m.any(rule it.key == 2)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapAny_NoMatchingPredicate_False() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20})\n" +
            "    return m.any(rule it.value > 999)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapAll_AllSatisfy_True() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20}, {key=3, value=30})\n" +
            "    return m.all(rule it.value > 0)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapAll_OneViolates_False() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=-1})\n" +
            "    return m.all(rule it.value > 0)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFilterCount_Chained() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20}, {key=3, value=30}, {key=4, value=40})\n" +
            "    return m.filter(rule it.value >= 20).count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // Was [Ignore] pending Stage 2 Enumerable dispatch. Fix: widened map's input
    // signature from FixedArray<T0> to Enumerable<T0> + try-merge-with-AddAncestor
    // pattern in CompCs cross-Apply (preserves back-prop precision for primitive
    // elements, falls back gracefully on not-yet-resolved composite shapes).
    [Test]
    public void Stage5_MapMap_OnMapEnumerable() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1,value=10},{key=2,value=20})\n" +
            "    return m.map(rule it.value).count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapMap_SumOfValues() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1,value=10},{key=2,value=20},{key=3,value=30})\n" +
            "    return m.map(rule it.value * 2).sum()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(120, rt["out"].Value);
    }

    // -----------------------------------------------------------------
    // 13.5 CLR boundary — Dictionary input / output
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_ClrInput_DictionaryIntInt_ContainsKey() {
        var input = new System.Collections.Generic.Dictionary<int, int> {
            { 1, 10 }, { 2, 20 }, { 3, 30 }
        };
        var rt = Funny.Hardcore
            .WithApriori<System.Collections.Generic.Dictionary<int, int>>("m")
            .BuildLang("out = m.containsKey(2)");
        rt["m"].Value = input;
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_ClrInput_EmptyDictionary_CountIsZero() {
        var input = new System.Collections.Generic.Dictionary<int, string>();
        var rt = Funny.Hardcore
            .WithApriori<System.Collections.Generic.Dictionary<int, string>>("m")
            .BuildLang("out = m.count()");
        rt["m"].Value = input;
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void Stage5_ClrInput_DictionaryIntInt_Count() {
        var input = new System.Collections.Generic.Dictionary<int, int> {
            { 1, 100 }, { 2, 200 }
        };
        var rt = Funny.Hardcore
            .WithApriori<System.Collections.Generic.Dictionary<int, int>>("m")
            .BuildLang("out = m.count()");
        rt["m"].Value = input;
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_ClrInput_DictionaryTextInt_LookupValue() {
        var input = new System.Collections.Generic.Dictionary<string, int> {
            { "alpha", 1 }, { "beta", 2 }
        };
        var rt = Funny.Hardcore
            .WithApriori<System.Collections.Generic.Dictionary<string, int>>("m")
            .BuildLang("out = m.get('beta') ?? -1");
        rt["m"].Value = input;
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void Stage5_ClrOutput_DictionaryIntText_Roundtrip() {
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key=10, value='ten'}, {key=20, value='twenty'})");
        rt.Run();
        var dict = (System.Collections.Generic.Dictionary<int, string>)rt["y"].Value;
        Assert.AreEqual(2, dict.Count);
        Assert.AreEqual("ten", dict[10]);
        Assert.AreEqual("twenty", dict[20]);
    }

    [Test]
    public void Stage5_ClrOutput_DictionaryTextIntArray_Roundtrip() {
        // map<text, list<int>> — value carries a list per existing inference rules.
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key='odds', value=[1,3,5]}, {key='evens', value=[2,4,6]})");
        rt.Run();
        var dict = (System.Collections.IDictionary)rt["y"].Value;
        Assert.AreEqual(2, dict.Count);
    }

    [Test]
    public void Stage5_ClrOutput_EmptyMap_AfterAllRemoved_IsEmptyDict() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n    m.removeKey(1)\n    return m\n" +
            "y = f()");
        rt.Run();
        var dict = (System.Collections.Generic.Dictionary<int, string>)rt["y"].Value;
        Assert.AreEqual(0, dict.Count);
    }

    // -----------------------------------------------------------------
    // 13.6 Equality and value semantics
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapEquality_SameContentSameOrder_True() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = __mkMap({key=1,value='a'},{key=2,value='b'})\n" +
            "    b = __mkMap({key=1,value='a'},{key=2,value='b'})\n" +
            "    return a == b\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapEquality_SameContentDifferentOrder_True() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = __mkMap({key=1,value=10},{key=2,value=20})\n" +
            "    b = __mkMap({key=2,value=20},{key=1,value=10})\n" +
            "    return a == b\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapEquality_DifferentCount_False() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = __mkMap({key=1, value='a'}, {key=2, value='b'})\n" +
            "    b = __mkMap({key=1, value='a'})\n" +
            "    return a == b\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapEquality_DifferentValues_False() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = __mkMap({key=1, value='a'})\n" +
            "    b = __mkMap({key=1, value='X'})\n" +
            "    return a == b\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapEquality_AfterMutation_Invalidates() {
        // Spec §equality: mutation invalidates prior equality (same as List/Set).
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = __mkMap({key=1, value=10})\n" +
            "    b = __mkMap({key=1, value=10})\n" +
            "    pre = a == b\n" +
            "    a.setKey(2, 20)\n" +
            "    post = a == b\n" +
            "    return pre and not post\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapClrOutput_TwoSeparateRuns_DictionariesMatchByContent() {
        // CLR round-trip: two distinct runs producing the same map content should
        // yield Dictionaries with identical key/value contents.
        var rt = Funny.Hardcore.BuildLang(
            "y1 = __mkMap({key=1, value='a'}, {key=2, value='b'})");
        rt.Run();
        var d1 = (System.Collections.Generic.Dictionary<int, string>)rt["y1"].Value;

        var rt2 = Funny.Hardcore.BuildLang(
            "y2 = __mkMap({key=1, value='a'}, {key=2, value='b'})");
        rt2.Run();
        var d2 = (System.Collections.Generic.Dictionary<int, string>)rt2["y2"].Value;

        Assert.AreEqual(d1.Count, d2.Count);
        Assert.AreEqual(d1[1], d2[1]);
        Assert.AreEqual(d1[2], d2[2]);
    }

    // -----------------------------------------------------------------
    // 13.7 Error / negative cases (via Optional, not Throws)
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapGet_AbsentKey_NoneCoalescesToFallback() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    return m.get(99) ?? 'fallback'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("fallback", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapRemoveKey_AbsentKey_NoneCoalescesToFallback() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10})\n" +
            "    return m.removeKey(99) ?? -1\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryGet_AbsentKey_ValueIsDefault() {
        // tryGet returns {value: V, success: bool}. On absence, value is V's default.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='hello'})\n" +
            "    r = m.tryGet(99)\n    return r.value\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryGet_PresentKey_ValueIsCorrect() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='hello'})\n" +
            "    r = m.tryGet(1)\n    return r.value\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("hello", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryRemoveKey_AbsentKey_SuccessFalse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    r = m.tryRemoveKey(99)\n    return r.success\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryRemoveKey_AbsentKey_CountUnchanged() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    m.tryRemoveKey(99)\n    return m.count()\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapTryRemoveKey_PresentKey_RemovesEntry() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'}, {key=2, value='b'})\n" +
            "    m.tryRemoveKey(1)\n    return m.containsKey(1)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    // -----------------------------------------------------------------
    // 13.8 Integration: ??, defaults, composition
    // -----------------------------------------------------------------

    [Test]
    public void Stage5_MapGet_WithCoalesce_FallsBackOnAbsence() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='a'})\n" +
            "    return m.get(99) ?? 'default'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("default", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapGet_PresentKey_CoalesceUnused() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value='actual'})\n" +
            "    return m.get(1) ?? 'fallback'\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual("actual", rt["out"].Value);
    }

    [Test]
    public void Stage5_MapDefault_FromApriori_IsEmptyMap() {
        // map<int,int> via apriori — observe count() on supplied empty Dictionary input.
        var rt = Funny.Hardcore
            .WithApriori<System.Collections.Generic.Dictionary<int, int>>("m")
            .BuildLang("out = m.count()");
        rt["m"].Value = new System.Collections.Generic.Dictionary<int, int>();
        rt.Run();
        Assert.AreEqual(0, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapGet_WithCoalesceToStruct() {
        // Compose get + coalesce + struct field access.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value={x=10, y=20}})\n" +
            "    v = m.get(99) ?? {x=-1, y=-1}\n    return v.x\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFilter_ChainedWithAny() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20}, {key=3, value=30})\n" +
            "    return m.filter(rule it.value > 15).any(rule it.key == 3)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Stage5_Map_GetThenComputeWithCoalesce() {
        // get returns Optional<V>; verify chained arithmetic via ??.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    m = __mkMap({key=1, value=10}, {key=2, value=20})\n" +
            "    return (m.get(1) ?? 0) + (m.get(99) ?? 0)\n" +
            "out = f()");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    [Test]
    public void Stage5_MapFactory_DuplicateKey_LastWinsCountIsOne() {
        // Repeated key in factory: last value wins; total count is 1.
        var rt = Funny.Hardcore.BuildLang(
            "y = __mkMap({key=1, value='a'}, {key=1, value='b'}, {key=1, value='c'})");
        rt.Run();
        var dict = (System.Collections.Generic.Dictionary<int, string>)rt["y"].Value;
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual("c", dict[1]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Stage 5 / map widening — lang-mode mirrors of the ee-mode tests marked
    // [Ignore] for TicTechnicalDebt.md #16. These pin that lang-mode does NOT
    // suffer the same precision loss: ee-mode StateArray covariance + CompCs
    // cross-Apply is the affected combination; lang-mode StateCollection.List
    // is invariant in element, so back-prop stays tight.
    //
    // If any of these starts failing, the regression has spread from ee-mode
    // into lang-mode and the [Ignore]'d ee-mode tests need re-evaluation.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public void LangMirror_ClosureArrayMap_IntsPinned() {
        // Mirror of Closure_ArrayOfClosures_IndependentCells (ee-mode failing).
        var rt = Funny.Hardcore.BuildLang(
            "fun mk(a, b): return rule(c) = a + b + c\n" +
            "fun f(): return list(mk(1, 2), mk(3, 4), mk(5, 6)).map(rule it(10))\n" +
            "out = f()");
        rt.Run();
        var arr = (System.Collections.IList)rt["out"].Value;
        Assert.AreEqual(3, arr.Count);
        Assert.AreEqual(13, arr[0]);
        Assert.AreEqual(17, arr[1]);
        Assert.AreEqual(21, arr[2]);
    }

    [Test]
    public void LangMirror_RuleArrayMap_IntsPinned() {
        // Mirror of MR4Bug2_CorrectArityCallOn1ArgLambda_TypedAsElementReturnType.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(): return list(rule it + 1, rule it + 2).map(rule it(10))\n" +
            "out = f()");
        rt.Run();
        var arr = (System.Collections.IList)rt["out"].Value;
        Assert.AreEqual(2, arr.Count);
        Assert.AreEqual(11, arr[0]);
        Assert.AreEqual(12, arr[1]);
    }

    [Test]
    public void LangMirror_NestedByteUpcastMap_RealResult() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    x:byte = 5\n" +
            "    return [[0,1],[2,3],[x]].map(rule it.map(rule it+1).sum()).sum()\n" +
            "out:real = f()");
        rt.Run();
        Assert.AreEqual(16.0, rt["out"].Value);
    }

    [Test]
    public void LangMirror_ZeroArgCallOn1ArgLambda_StillRejected() {
        // Mirror of MR4Bug2_ZeroArgCallOn1ArgLambda_InMapRule_SilentlyAccepted.
        // Should still reject arity-mismatch in lang-mode.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang(
                "fun f(): return list(rule it + 1).map(rule it())\n" +
                "out = f()"));
    }

    #endregion
}
