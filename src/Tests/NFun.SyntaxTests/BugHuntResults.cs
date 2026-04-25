using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting.
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    // FIXED: #2 (?? optionals), #3 (empty interpolation), #4 ([] else [300]),
    //        #5 (format specifier crash), #6 (max NaN), #7 (duplicate struct field),
    //        #9 (U12→UInt8 mapping caused overflow)
    // NOT A BUG: #8 (empty format specifier '{42:}' — by design)

    #region Unfixed bugs

    [Test] // FIXED: Destruction now detects opt(T) ≤ T incompatibility
    public void Bug1_OptionalInNonOptionalArray_CompileError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "y:int? = none; out:int[] = [y, 1, 2]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    // === Named Types Bug Hunt (session 2) ===

    [Test] // FIXED: AllLeafTypes cycle guard added
    public void BugHunt2_TreeArrayRecursive_StackOverflow() {
        "type tree = {v:int, children:tree[] = []}; forest = [tree{v=1, children=[tree{v=10}]}, tree{v=2}]; out = forest[0].children[0].v"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 10);
    }

    [Test] // FIXED: AllLeafTypes cycle guard also fixed this
    public void BugHunt2_ArrayRecursiveOptionalAccess_TypeError() {
        "type node = {v:int, next:node? = none}; arr = [node{v=1, next=node{v=10}}, node{v=2, next=node{v=20}}]; out = arr[0].next?.v ?? -1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 10);
    }

    [Test] // FIXED: Named struct type override in concrete function prototype
    public void BugHunt2_RecursiveFunctionDepth4_TypeError() {
        "type node = {v:int, next:node? = none}; maxVal(n:node):int = if(n.next == none) n.v else max(n.v, maxVal(n.next!)); n = node{v=3, next=node{v=7, next=node{v=2, next=node{v=9}}}}; out = maxVal(n)"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 9);
    }

    [Test] // Recursive function at depth 15 — must work without any depth limit
    public void BugHunt2_RecursiveFunctionDepth15() {
        "type node = {v:int, next:node? = none}; lastVal(n:node):int = if(n.next == none) n.v else lastVal(n.next!); n = node{v=1, next=node{v=2, next=node{v=3, next=node{v=4, next=node{v=5, next=node{v=6, next=node{v=7, next=node{v=8, next=node{v=9, next=node{v=10, next=node{v=11, next=node{v=12, next=node{v=13, next=node{v=14, next=node{v=15}}}}}}}}}}}}}}}; out = lastVal(n)"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 15);
    }

    [Test] // FIXED: Destruction preserves IsOptional when transforming struct constraint
    public void BugHunt2_DoubleAnonymousOptionalSafeAccess() {
        "inner = if(true) {b = 42} else none; x = if(true) {a = inner} else none; out = x?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);
    }

    // Bug hunt FP: Circular alias IS detected — agent tested without named types dialect
    [Test]
    public void BugHunt2_CircularAlias_IsDetected() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "type a = b; type b = a; out = 1"
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled));
    }

    // Bug hunt FP: Field type inference works — InferTypeFromConstantOrAny handles it
    [Test]
    public void BugHunt2_StructFieldTypeInference_Works() {
        "type config = {retries = 3}; c = config{}; out = c.retries + 1"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 4);
    }

    [Test] // FIXED: IEEE 754 NaN equality
    public void BugHunt2_NaN_EqualsSelf() {
        "out = (0.0/0.0) == (0.0/0.0)".AssertReturns("out", false);
    }

    // === Bug Hunt Session 3 (200 iterations, named/recursive/optional focus) ===
    // NOT A BUG: ?? and ! on non-optional — no-op by design (convenience)

    [Test] // FIXED: Preferred type propagation in Pull + Concretest for Optional arrays
    public void BugHunt3_ArrayWithNone_PreferredTypeWorks() {
        // [1, none, 3] should produce Int32?[] (not UInt8?[]) — preferred type applied
        var r = "[1, none, 3]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (int?[])r.Get("out");
        Assert.AreEqual(new int?[] { 1, null, 3 }, arr);
    }

    [Test] // FIXED: Tokenizer checks for IP pattern after hex octet
    public void BugHunt3_HexIpLiteral_Works() {
        "out = 0xFF.0.0xA.0xFA".Calc().AssertResultHas("out", System.Net.IPAddress.Parse("255.0.10.250"));
    }

    [Test] // FIXED: Cycle detection in ExpandConstructor default expansion
    public void BugHunt3_RecursiveDefaultConstructor_CompileError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "type node = {v:int, next:node? = node{v=0}}; n = node{v=1}; out = n.v"
                .CalcWithDialect(
                    optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                    namedTypesSupport: NamedTypesSupport.ExperimentalEnabled));
    }

    [Test] // FIXED: FunnyStruct.GetHashCode now consistent with Equals
    public void BugHunt3_StructIntersect_Works() {
        var r = "a=[{x=1},{x=2}]; b=[{x=2},{x=3}]; out=a.intersect(b)".Calc();
        var arr = (object[])r.Get("out");
        Assert.AreEqual(1, arr.Length, "intersect should find {x=2} in both arrays");
    }

    [Test] // FIXED: ExpandConstructor rejects duplicate fields
    public void BugHunt3_DuplicateFieldInNamedConstructor_CompileError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "type pt = {x:int, y:int}; out = pt{x=1, y=2, x=3}.x"
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled));
    }

    [Test] // FIXED: Field names normalized to lowercase in registry
    public void BugHunt3_UppercaseFieldName_Works() {
        "type pt = {A:int}; out = pt{A=42}.A"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);
    }

    [Test] // FIXED: ExpandConstructor follows alias chain
    public void BugHunt3_AliasToStructConstructor_Works() {
        "type pair = {a:int, b:int}; type alias = pair; out = alias{a=1, b=2}.a"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 1);
    }

    [Test] // FIXED: IP literals recognized as constants in ValidateConstantExpression
    public void BugHunt3_IpDefaultInStruct_Works() {
        "type server = {addr:ip = 0.0.0.0, port:int = 80}; out = server{}.port"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 80);
    }

    [Test] // FIXED: ApplyTiResults unwraps Optional on ??/! expression results
    public void BugHunt3_CoalesceOnNamedStructField_Works() {
        "type w = {items:int?[]}; y = w{items=[42, none]}; out = y.items[0] ?? -1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);
    }

    // === Bug Hunt Session 4 (300 iterations, deep TIC stress test) ===

    [Test] // FIXED: FunnyStruct.Equals uses TryGetValue instead of indexer
    public void BugHunt4_StructEqualityDifferentFields_Crash() {
        Assert.DoesNotThrow(() =>
            "a = {x=1, y=2}; b = {x=1, z=3}; out = a == b".Calc());
    }

    [Test] // FIXED: Cycle guards in StateOptional.IsSolved + TicTypesConverter
    public void BugHunt4_GenericWrapNone_StackOverflow() {
        Assert.DoesNotThrow(() =>
            "wrap(x) = if(true) x else none; out = wrap(42)"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test] // FIXED: SetCall wraps unsolved ICompositeState in StateRefTo + self-loop guard
    public void BugHunt4_RecursiveFunctionWithLambda_CircularAncestor() {
        "applyN(x, f, n:int) = if(n > 0) applyN(f(x), f, n-1) else x; out = applyN(1, rule it*2, 3)"
            .AssertReturns("out", 8);
    }

    [Test] // FIXED: Chain propagation in parser — .method() after ?.method() also safe
    public void BugHunt4_SafeCallChainOnNone_RuntimeCrash() {
        "arr:int[]? = none; out = arr?.sort().reverse() ?? []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", new int[0]);
    }

    [Test] // FIXED: Cycle-aware Optional materialization in Destruction
    public void BugHunt4_MapOptionalVarThenMap_StackOverflow() {
        Assert.DoesNotThrow(() =>
            "source = [1,2,3].map(rule if(it>1) it else none); out = source.map(rule it ?? 0)"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test][Ignore("Bug hunt 4#6: Optional field int? lost in array of named structs")]
    public void BugHunt4_OptionalFieldLostInArray() {
        // type t = {x: int?}; [t{x=1}, t{x=2}] → should be {x:Int32?}[] not {x:Int32}[]
        Assert.DoesNotThrow(() =>
            "type t = {x: int?}; items = [t{x=1}, t{x=2}]; out = items[0].x ?? -1"
                .CalcWithDialect(
                    optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                    namedTypesSupport: NamedTypesSupport.ExperimentalEnabled));
    }

    [Test][Ignore("Bug hunt 4#7: Named struct int?[] field + .map().sum() widens to Real")]
    public void BugHunt4_OptionalArrayFieldMapSum_WidensToReal() {
        "type d = {items: int?[]}; x = d{items=[1,none,3]}; out = x.items.map(rule it ?? 0).sum()"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 4);
    }

    [Test][Ignore("Bug hunt 4#8: ?? on incompatible types widens to Any instead of error")]
    public void BugHunt4_CoalesceIncompatibleTypes_ShouldError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "a:int? = 42; out = a ?? 'hello'"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test][Ignore("Bug hunt 4#9: if-else Optional struct with none Optional field — type degradation")]
    public void BugHunt4_IfElseOptionalStructNoneField() {
        "type t = {x: int?}; a = if(true) t{x=none} else none; out = a?.x ?? -1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
                namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);
    }

    // === Bug Hunt Session 5 (600 iterations, all new features stress test) ===

    [Test]
    public void BugHunt5_ArithmeticOnBool_Crash() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => "y = true + 1".Calc());
    }

    [Test]
    public void BugHunt5_RangeWithoutAnnotation_Crash() {
        "y:int[] = [1..5]".AssertReturns("y", new[]{1,2,3,4,5}); // works WITH annotation
        Assert.DoesNotThrow(() => "y = [1..5]".Calc()); // crashes WITHOUT
    }

    [Test] // ! on non-optional is a no-op by design (convenience, same as ??)
    public void BugHunt5_ForceUnwrapNonOptional_IsNoop() {
        "y = 42!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);
    }

    [Test]
    public void BugHunt5_TextSort_RuntimeCrash() {
        Assert.DoesNotThrow(() => "y = 'cba'.sort()".Calc());
    }

    [Test]
    public void BugHunt5_PrimitiveTypeAlias_AsAnnotation() {
        "type age = int; x:age = 42; y = x + 1"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 43);
    }

    [Test]
    public void BugHunt5_IncompatibleAnnotation_SilentAny() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => "y:bool = 42".Calc());
    }

    // NOT A BUG: 5#7 — y ?? none is equivalent to y (no-op). Semantically correct.

    [Test]
    public void BugHunt5_LogicalOnInt_Crash() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => "y = 1 and 2".Calc());
    }

    // === Bug Hunt Session 6 (600 iterations, post-fix validation) ===
    // Hell & Nested: 0 bugs in 200 iterations!

    [Test]
    public void BugHunt6_NaN_LessThan_ReturnsTrue() {
        // IComparable.CompareTo treats NaN as smallest, but IEEE 754 says false
        "out = (0.0/0.0) < 1.0".AssertReturns("out", false);
    }

    [Test]
    public void BugHunt6_NegateIntMin_ShouldOverflow() {
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(
            () => "y:int = -2147483648\r out = -y".Calc());
    }

    [Test]
    public void BugHunt6_IntLiteralInIfCondition_Crash() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "y = if(42) true else false".Calc());
    }

    [Test]
    public void BugHunt6_InlineStructAnnotation_UppercaseFieldCrash() {
        // {minVal:int} → lowercase normalize creates duplicate "minval" key
        Assert.DoesNotThrow(
            () => "y:{minVal:int, maxVal:int} = {minVal=1, maxVal=2}".Calc());
    }

    // 6#5: y?.a?.b double chain — same as 4#9, already tracked
    // 6#6: y?.a ?? 0 > 0 crash — precedence mistake but should be clean error
    // 6#7: convert([1,2,3]) → 'Arr[1,2,3]' — cosmetic, low priority
    // 6#8: ?.field ?? fallback optional leak in map — known TIC workaround issue

    // === Bug Hunt Session 7 (300 iterations, post-all-fixes validation) ===
    // Simple: pending
    // Hell: 4 bugs. Edge: 3 bugs.

    [Test]
    public void BugHunt7_DeepSafeAccessChainMethod() {
        "x = if(true) {a={b='hello'}} else none; out = x?.a.b.reverse() ?? ''"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", "olleh");
    }

    [Test]
    public void BugHunt7_MaxNaN_ShouldPropagateNaN() {
        "out = max(0.0/0.0, 1.0)".Calc().AssertResultHas("out", double.NaN);
    }

    [Test]
    public void BugHunt7_BoolToText_ShouldBeLowercase() {
        "out = true.toText()".Calc().AssertResultHas("out", "true");
        "out = false.toText()".Calc().AssertResultHas("out", "false");
    }

    [Test]
    public void BugHunt7_TextInText_ShouldBeTypeError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "out = 'h' in 'hello'".Calc());
    }

    [Test]
    public void BugHunt7_RecursiveFuncShadowsBuiltin_Crash() {
        // 'last' shadows built-in last(T[]):T. Recursive call must resolve to the user function.
        "last(x:int):int = if(x > 0) last(x-1) else 0; out = last(5)".Calc()
            .AssertResultHas("out", 0);
        // 'count' shadows built-in count(T[]):int.
        "count(x:int):int = if(x > 0) 1 + count(x-1) else 0; out = count(5)".Calc()
            .AssertResultHas("out", 5);
    }

    [Test]
    public void BugHunt7_TreeArrayMap_StackOverflow() {
        Assert.DoesNotThrow(() =>
            "type tree = {v:int, children:tree[] = []}; forest = [tree{v=1, children=[tree{v=10}]}, tree{v=2}]; out = forest.map(rule it.children.count())"
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.ExperimentalEnabled));
    }

    [Test] // FIXED: auto-fixed by recent TIC struct changes
    public void BugHunt7_GenericFuncLosesStructFields() {
        "getMax(items) = items.fold(rule if(it1.v > it2.v) it1 else it2); out = getMax([{v=3,name='a'},{v=1,name='b'},{v=5,name='c'}]).name"
            .AssertReturns("out", "c");
    }

    // === Bug Hunt Session 8 (600 iterations, final validation) ===
    // Simple: 1 bug in 200 iterations — core language very stable

    [Test]
    public void BugHunt8_TripleNestedOptionalStruct() {
        "inner = if(true) {d=99} else none; mid = if(true) {c=inner} else none; outer = if(true) {b=mid} else none; out = outer?.b?.c?.d ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 99);
    }

    [Test] // FIXED: ?[ now returns none on out-of-bounds
    public void BugHunt8_SafeArrayAccess_OutOfBounds() {
        "arr = [10,20,30]; out = arr?[99] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 0);
    }

    [Test] // FIXED: TIC now rejects None as non-Comparable in SimplifyOrNull
    public void BugHunt8_NoneCompareNone_RuntimeCrash() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "out = none > none"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test] // FIXED: FunnyStruct.Equals now requires same field count (spec-correct)
    public void BugHunt8_StructEquality_DifferentFieldCount() {
        "a = {x=1, y=2}; b = {x=1}; out = a == b".Calc().AssertResultHas("out", false);
    }

    // 8#2: generic func + double ?. on recursive type — related to 8#1
    // 8#3: val/var/let reserved — design choice
    // 8#4: f(x)=f(x) not caught — low priority, runtime catches it
    // 8#5+8#6: rule output/field invoke — known limitation
    // 8#8: y??none type unsound — already noted in session 5

    #endregion

    // === Bug Hunt Session 13 (300 iterations, 3 parallel agents) ===

    [Test][Ignore("Bug hunt 13#1: (42 ?? none) ?? 0 returns Int32? instead of Int32 — outer ?? should unwrap")]
    public void BugHunt13_NestedCoalesceWithNone_TypeNotUnwrapped() {
        "(42 ?? none) ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);
    }

    [Test][Ignore("Bug hunt 13#2: arr[i:4] parsed as type annotation instead of slice — parser ambiguity")]
    public void BugHunt13_SliceWithVariableStart_ParsedAsTypeAnnotation() {
        "arr = [1,2,3,4,5]\r i = 2\r y = arr[i:4]"
            .AssertReturns("y", new[] { 3, 4 });
    }
}
