using System;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests for recursive named types.
/// Non-optional recursive fields are impossible (infinite size).
/// Optional recursive fields are valid (none breaks the recursion).
/// Array-of-self fields are valid (empty array breaks the recursion).
/// </summary>
public class RecursiveTypeTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    static CalculationResult Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled);

    static FunnyRuntime Build(string expr) =>
        Funny.Hardcore.WithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled).Build(expr);

    // Impossible recursive type definitions (non-optional direct/indirect self-refs)
    // are covered in ImpossibleRecursiveTypeDefinitionsTest.cs.

    // ═══════════════════════════════════════════════════════════════
    // OPTIONAL DIRECT RECURSION — valid (none breaks recursion)
    // ═══════════════════════════════════════════════════════════════

    #region Linked list

    [Test]
    public void LinkedList_Leaf() {
        var r = Calc("type node = {v:int, next:node? = none}; out = node{v = 42}.v");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void LinkedList_TwoNodes() {
        var r = Calc("type node = {v:int, next:node? = none}; n = node{v=1, next=node{v=2}}; out = n.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void LinkedList_ThreeNodes() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2, next=node{v=3}}}; out = n.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void LinkedList_SafeAccess_HasValue() {
        var r = Calc("type node = {v:int, next:node? = none}; n = node{v=1, next=node{v=2}}; out = n.next?.v ?? 0");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void LinkedList_SafeAccess_None() {
        var r = Calc("type node = {v:int, next:node? = none}; n = node{v=1}; out = n.next?.v ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    [Test]
    public void LinkedList_DoubleChain() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2, next=node{v=3}}}; " +
            "out = n.next?.next?.v ?? 0");
        Assert.AreEqual(3, r.Get("out"));
    }

    [Test]
    public void LinkedList_DoubleChain_NoneAtEnd() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2}}; " +
            "out = n.next?.next?.v ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    #endregion

    #region Binary tree

    [Test]
    public void BinaryTree_Leaf() {
        var r = Calc("type tree = {v:int, left:tree? = none, right:tree? = none}; out = tree{v=10}.v");
        Assert.AreEqual(10, r.Get("out"));
    }

    [Test]
    public void BinaryTree_WithChildren() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=10, left=tree{v=5}, right=tree{v=15}}; out = t.v");
        Assert.AreEqual(10, r.Get("out"));
    }

    [Test]
    public void BinaryTree_LeftChild() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=10, left=tree{v=5}}; out = t.left?.v ?? 0");
        Assert.AreEqual(5, r.Get("out"));
    }

    [Test]
    public void BinaryTree_RightNone() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=10, left=tree{v=5}}; out = t.right?.v ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    [Test]
    public void BinaryTree_DeepLeft() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=1, left=tree{v=2, left=tree{v=3}}}; " +
            "out = t.left?.left?.v ?? 0");
        Assert.AreEqual(3, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // OPTIONAL INDIRECT RECURSION — valid
    // ═══════════════════════════════════════════════════════════════

    #region Indirect optional recursion

    [Test]
    public void Indirect_Opt_TwoTypes() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; type b = {y:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2}}.x");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Indirect_Opt_CrossAccess() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; type b = {y:int, a:a? = none}; " +
            "v = a{x=1, b=b{y=2}}; out = v.b?.y ?? 0");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Indirect_Opt_ThreeTypes() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; type b = {y:int, c:c? = none}; " +
            "type c = {z:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2, c=c{z=3}}}.x");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Indirect_Opt_ThreeTypes_DeepAccess() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; type b = {y:int, c:c? = none}; " +
            "type c = {z:int, a:a? = none}; " +
            "v = a{x=1, b=b{y=2, c=c{z=3}}}; out = v.b?.c?.z ?? 0");
        Assert.AreEqual(3, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // ARRAY-OF-SELF — valid (empty array breaks recursion)
    // ═══════════════════════════════════════════════════════════════

    #region Array of self

    [Test]
    public void ArrayOfSelf_Leaf() {
        var r = Calc("type t = {v:int, children:t[] = []}; out = t{v=1}.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void ArrayOfSelf_WithChildren() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "out = t{v=0, children=[t{v=1}, t{v=2}]}.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void ArrayOfSelf_ChildCount() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "p = t{v=0, children=[t{v=1}, t{v=2}, t{v=3}]}; out = p.children.count()");
        Assert.AreEqual(3, r.Get("out"));
    }

    [Test]
    public void ArrayOfSelf_Nested() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=1, children=[t{v=3}, t{v=4}]}, t{v=2}]}; out = root.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // NON-RECURSIVE references (always valid)
    // ═══════════════════════════════════════════════════════════════

    #region Non-recursive

    [Test]
    public void NonRecursive_SimpleNesting() {
        var r = Calc("type inner = {v:int}; type outer = {i:inner}; out = outer{i=inner{v=42}}.i.v");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void NonRecursive_ThreeLevels() {
        var r = Calc("type c = {v:int = 99}; type b = {c:c}; type a = {b:b}; out = a{b=b{c=c{}}}.b.c.v");
        Assert.AreEqual(99, r.Get("out"));
    }

    [Test]
    public void NonRecursive_ForwardReference() {
        var r = Calc("type a = {b:b}; type b = {v:int}; out = a{b=b{v=42}}.b.v");
        Assert.AreEqual(42, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // EDGE CASES
    // ═══════════════════════════════════════════════════════════════

    #region Edge cases

    [Test]
    public void Edge_OptSelfOnly() {
        var r = Calc("type t = {self:t? = none}; out = t{}");
        // Just builds without error
    }

    [Test]
    public void Edge_RecursiveInArray() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "arr = [node{v=1}, node{v=2, next=node{v=3}}]; out = arr.count()");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Edge_RecursiveAsParam() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "getV(n:node):int = n.v; out = getV(node{v=42})");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void Edge_AllDefaults_Recursive() {
        var r = Calc("type node = {v:int = 0, next:node? = none}; n = node{}; out = n.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void Edge_PartialCycleBreak() {
        // a→b (non-opt), b→a (optional) — cycle broken by optional on one side
        var r = Calc(
            "type a = {x:int, b:b}; type b = {y:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2}}.b.y");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Edge_MixedArrayAndOpt() {
        var r = Calc(
            "type t = {v:int, children:t[] = [], parent:t? = none}; out = t{v=1}.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // Recursive type stress tests
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TreeArrayRecursive_StackOverflow() {
        "type tree = {v:int, children:tree[] = []}; forest = [tree{v=1, children=[tree{v=10}]}, tree{v=2}]; out = forest[0].children[0].v"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 10);
    }

    [Test]
    public void ArrayRecursiveOptionalAccess() {
        "type node = {v:int, next:node? = none}; arr = [node{v=1, next=node{v=10}}, node{v=2, next=node{v=20}}]; out = arr[0].next?.v ?? -1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 10);
    }

    [Test]
    public void RecursiveFunctionDepth4() {
        "type node = {v:int, next:node? = none}; maxVal(n:node):int = if(n.next == none) n.v else max(n.v, maxVal(n.next!)); n = node{v=3, next=node{v=7, next=node{v=2, next=node{v=9}}}}; out = maxVal(n)"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 9);
    }

    [Test]
    public void RecursiveFunctionDepth15() {
        "type node = {v:int, next:node? = none}; lastVal(n:node):int = if(n.next == none) n.v else lastVal(n.next!); n = node{v=1, next=node{v=2, next=node{v=3, next=node{v=4, next=node{v=5, next=node{v=6, next=node{v=7, next=node{v=8, next=node{v=9, next=node{v=10, next=node{v=11, next=node{v=12, next=node{v=13, next=node{v=14, next=node{v=15}}}}}}}}}}}}}}}; out = lastVal(n)"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 15);
    }

    // Recursive default constructor (`type node = {..., next:node? = node{v=0}}`)
    // and non-contractive F-bound rejection are covered in
    // ImpossibleRecursiveTypeDefinitionsTest.cs.

    [Test]
    public void TreeArrayMap_NoStackOverflow() {
        Assert.DoesNotThrow(() =>
            "type tree = {v:int, children:tree[] = []}; forest = [tree{v=1, children=[tree{v=10}]}, tree{v=2}]; out = forest.map(rule it.children.count())"
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled));
    }

    // Recursive function with DIRECT field access (t.value) on named recursive type
    // — works with explicit annotation like RecursiveFunctionDepth4.
    [Test]
    public void RecursiveFunction_DirectFieldAccess_TreeSum_Annotated() {
        var result = "type tree = {value: int, left: tree? = none, right: tree? = none}\r treeSum(t: tree): int = if(t.left == none) (if(t.right==none) t.value else t.value + treeSum(t.right!)) else if(t.right==none) t.value + treeSum(t.left!) else t.value + treeSum(t.left!) + treeSum(t.right!)\r y = treeSum(tree{value=1, left=tree{value=2}, right=tree{value=3}})"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 6);
    }

    [Test]
    public void RecursiveFunction_SafeAccessOnly_TreeSize() {
        var result = "type tree = {value: int, left: tree?, right: tree?}\r treeSize(t) = if(t==none) 0 else 1 + treeSize(t?.left) + treeSize(t?.right)\r y = treeSize({value=1, left={value=2, left=none, right=none}, right=none})"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 2);
    }

    // Issue #121: unannotated recursive function on named type — Push reform
    // restores Optional break on the opt-sourced self-closing struct cycle,
    // yielding the principal type μX. opt(struct{value:int, left:X, right:X}).
    [Test]
    public void RecursiveFunction_DirectFieldAccess_TreeSum_Unannotated() {
        var result = "type tree = {value: int, left: tree? = none, right: tree? = none}\r treeSum(t) = if(t==none) 0 else t.value + treeSum(t?.left) + treeSum(t?.right)\r y = treeSum(tree{value=1, left=tree{value=2}, right=tree{value=3}})"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 6);
    }

    [Test]
    public void RecursiveFunction_LinkedListSum_Unannotated() {
        var result = "type node = {v: int, next: node? = none}\r listSum(n) = if(n==none) 0 else n.v + listSum(n?.next)\r y = listSum(node{v=1, next=node{v=2, next=node{v=3}}})"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 6);
    }

    // Row-polymorphic regression sentinel: non-recursive structural function
    // must NOT be tied to a specific named type. Push reform's wrap is gated on
    // self-closing cycles only — `length(p)` forms no cycle and is untouched.
    [Test]
    public void RowPolymorphic_LengthFunction_StaysGeneric() {
        var result = "type point2d = {x: int, y: int}\r length(p) = p.x*p.x + p.y*p.y\r y = length({x=3, y=4})"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled);
        result.AssertReturns("y", 25);
    }

    // Regression sentinel: recursion through Array constructors works the same
    // as through Optional. `tree.kids[]` is a contractive back-edge — the array
    // wrapper breaks the structural cycle. Includes value construction, direct
    // index access, and a recursive function via `map` + `sum`.
    [Test]
    public void ArrayRecursion_Works() {
        var script =
            "type forest = {kids: forest[]}\r " +
            "deepCount(t) = if(t.kids.count() == 0) 1 else 1 + t.kids.map(rule deepCount(it)).sum()\r " +
            "y = deepCount(forest{kids = [forest{kids=[]}, forest{kids=[forest{kids=[]}]}]})";
        var result = script.CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled);
        result.AssertReturns("y", 4);
    }

    // Output-type-annotated unannotated-arg recursive function. The `sumA(x):int`
    // signature has annotated return but inferred arg. After body solving +
    // propagation, x is `node?` and return is int — both concrete. RuntimeBuilder
    // routes through the concrete path even though body solving has residual CS
    // (operator's `==` T, `+` T) — see SignatureIsFullyConcrete.
    [Test]
    public void RecursiveFunction_OutputAnnotatedOnly_SumA() {
        var result = ("type node = {v: int, next: node? = none}\r " +
                      "sumA(x):int = if(x == none) 0 else x.v + sumA(x?.next)\r " +
                      "y = sumA(node{v=1, next=node{v=2}})")
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 3);
    }

    // KNOWN LIMITATION sentinel: when two declared named types share the same
    // recursive shape (here: `a` and `b` both `{v:int, n:self?}`), cycle-rescue
    // cannot disambiguate which name to stamp on the inferred μ-struct. The
    // function fails with FU710 even though only `a` is referenced.
    //
    // Resolving this requires call-site evidence (the literal `a{...}` is `a`,
    // never `b`) flowing into TIC. See Specs/Tic/PushReform.md "Known limitations".
    // Push reform M1+M2.E: when two named types share the same recursive shape,
    // cycle-rescue skips TypeName stamp on ambiguity and the F-bound lift takes
    // over. Runtime Fit expands NamedStruct via registry to validate. End-to-end.
    [Test]
    public void RowPolyMuType_AmbiguousNamedTypes_SameShape() {
        var result = ("type a = {v: int, n: a? = none}\r " +
                      "type b = {v: int, n: b? = none}\r " +
                      "sumA(x) = if(x == none) 0 else x.v + sumA(x?.n)\r " +
                      "y = sumA(a{v=1, n=a{v=2}})")
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 3);
    }

    // Annotated-form sentinel: getLast with full type annotations works.
    [Test]
    public void RecursiveFunction_GetLast_Annotated() {
        var result = ("type node = {v: int, next: node? = none}\r " +
                      "getLast(n: node?): node? = if (n == none) none else (getLast(n?.next) ?? n)\r " +
                      "y = getLast(node{v=1, next=node{v=2}})!.v")
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 2);
    }

    // KNOWN LIMITATION sentinel: row-polymorphic recursive function over
    // multiple named types of the SAME shape. `listSum` defined once should
    // accept both `a:node` and `b:node2`. Currently cycle-rescue stamps the
    // function's signature with one specific TypeName, preventing polymorphic
    // dispatch across structurally-equivalent named types. Resolving this
    // needs per-call-site monomorphization of recursive function bodies.
    // Push reform Milestone 1 — F-bounded polymorphism wired end-to-end.
    // listSum infers `(T)->N where T <: {v:N, next:T?}` and accepts both `node`
    // and `node2` (different declared types of the same recursive shape).
    [Test]
    public void RowPolyMuType_TwoNamedTypes_M1() {
        var result = ("type node = {v: int, next: node? = none}\r " +
                      "type node2 = {v: real, next: node2? = none}\r " +
                      "listSum(n) = if(n == none) 0 else n.v + listSum(n?.next)\r " +
                      "ra = listSum(node{v=1, next=node{v=2}})\r " +
                      "rb = listSum(node2{v=1.5, next=node2{v=2.5}})")
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled, optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns(("ra", 3), ("rb", 4.0));
    }

    // ===================================================================
    // F-bounded polymorphism roadmap sentinels.
    // All UNANNOTATED — function should infer F-bounded generic signature
    // like `f(T): R where T <: {field: T?, ...}`.
    // See /Users/tmt/.claude/.../recursive-types-fbounded-roadmap.md
    // ===================================================================

    // ROADMAP sentinel: getLast on a linked list — inferred bound
    // `getLast(T): T? where T <: {next: T?}`.
    // Must accept ANY struct type with that shape, including extra fields.
    [Test]
    public void Roadmap_GetLast_LinkedList_Generic_TwoTypes() {
        var script =
            "type a = {v: int, next: a? = none}\r " +
            "type b = {v: real, next: b? = none, label: text = ''}\r " +
            "getLast(n) = if (n == none) n else (getLast(n?.next) ?? n)\r " +
            "ra = getLast(a{v=1, next=a{v=2}})!.v\r " +
            "rb = getLast(b{v=1.5, next=b{v=2.5, label='x'}})!.v";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns(("ra", 2), ("rb", 2.5));
    }

    // ROADMAP sentinel: max-value node in a binary tree — inferred bound
    // `maxNode(T): T? where T <: {value: N, left: T?, right: T?}, N: Comparable`.
    // Returns the node carrying the maximum value, or none if tree is empty.
    [Test]
    public void Roadmap_MaxNode_BinaryTree_Generic() {
        var script =
            "type tree = {value: int, left: tree? = none, right: tree? = none}\r " +
            // pickMax: return the t with greater .value among (a,b,c), skipping none
            "pickMax(a, b) = if (a == none) b else if (b == none) a else if (a!.value > b!.value) a else b\r " +
            "maxNode(t) = if (t == none) none else pickMax(t, pickMax(maxNode(t?.left), maxNode(t?.right)))\r " +
            "y = maxNode(tree{value=2, left=tree{value=5}, right=tree{value=3, left=tree{value=4}}})!.value";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 5);
    }

    // ROADMAP sentinel: linked-list count — inferred bound
    // `count(T): int where T <: {next: T?}`.
    // No `.value` access — purely structural recursion via `next`.
    // Push reform M1.4 + #68 enables this end-to-end.
    [Test]
    public void Roadmap_Count_LinkedList_Generic_TwoTypes() {
        var script =
            "type a = {v: int, next: a? = none}\r " +
            "type b = {label: text, next: b? = none}\r " +
            "count(n) = if (n == none) 0 else 1 + count(n?.next)\r " +
            "ra = count(a{v=1, next=a{v=2, next=a{v=3}}})\r " +
            "rb = count(b{label='x', next=b{label='y'}})";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns(("ra", 3), ("rb", 2));
    }

    // ROADMAP sentinel: linked-list reverse-style accumulator — inferred bound
    // `lastValue(T): N where T <: {v: N, next: T?}`.
    // Same shape carries different value types (int, real).
    // Push reform M2.E (NamedStruct expansion in Fit) enables this end-to-end.
    [Test]
    public void Roadmap_LastValue_Generic_TwoTypes() {
        var script =
            "type intList = {v: int, next: intList? = none}\r " +
            "type realList = {v: real, next: realList? = none}\r " +
            "lastValue(n) = if (n?.next == none) n!.v else lastValue(n?.next)\r " +
            "ri = lastValue(intList{v=1, next=intList{v=2, next=intList{v=3}}})\r " +
            "rr = lastValue(realList{v=1.5, next=realList{v=2.5}})";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns(("ri", 3), ("rr", 2.5));
    }

    // ROADMAP sentinel: tree depth — inferred bound
    // `depth(T): int where T <: {left: T?, right: T?}`.
    // Pure structural: no value field accessed in body.
    // Push reform M1.4 + #68 enables this end-to-end.
    [Test]
    public void Roadmap_TreeDepth_Generic_TwoTypes() {
        var script =
            "type t1 = {value: int, left: t1? = none, right: t1? = none}\r " +
            "type t2 = {tag: text, left: t2? = none, right: t2? = none}\r " +
            "max2(a, b) = if (a > b) a else b\r " +
            "depth(t) = if (t == none) 0 else 1 + max2(depth(t?.left), depth(t?.right))\r " +
            "r1 = depth(t1{value=1, left=t1{value=2, left=t1{value=4}}, right=t1{value=3}})\r " +
            "r2 = depth(t2{tag='a', left=t2{tag='b'}, right=t2{tag='c'}})";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns(("r1", 3), ("r2", 2));
    }

    // ROADMAP sentinel: heterogeneous use — `count` on linked list AND on
    // a tree (different shapes both have `next`-like child).
    // This tests that two DIFFERENT bounds can coexist, each F-bounded
    // independently. Probably needs separate inferred bounds per call group.
    [Test]
    public void Roadmap_CountField_DifferentShapes() {
        var script =
            "type list = {next: list? = none}\r " +
            "countNext(n) = if (n == none) 0 else 1 + countNext(n?.next)\r " +
            "y = countNext(list{next=list{next=list{}}})";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 3);
    }

    // ROADMAP sentinel for Theorem PT-F (principal F-bound). Body has BOTH a
    // primitive Desc constraint (n.v + 1 forces v to be numeric) AND a
    // structural F-bound (recursion forces n to have shape {v:N, next:T?}).
    // The principal type is the meet on every dimension: GcdStruct on bound,
    // LCA on Desc. This tests that the three-way SimplifyOrNull check accepts
    // primitive-D + struct-S when D fits inside one of S's field types.
    [Test]
    public void Roadmap_PrincipalType_PrimitiveAndStructBound() {
        var script =
            "type a = {v: int, next: a? = none}\r " +
            "f(n) = n.v + 1\r " +
            "g(n) = f(n) + n.v\r " +
            "y = g(a{v=10, next=a{v=20}})";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("y", 21);
    }

    // ROADMAP sentinel: mutual recursion between two functions on two named
    // types. Each function infers its own F-bound. The bounds must be
    // co-recursively consistent: T_a <: {bs: T_b[]}, T_b <: {a: T_a?}.
    [Test]
    [Ignore("Mutual recursion rejected at FindFunctionSolvingOrderOrThrow — needs SCC solver for function-level mutual recursion (separate feature, not TIC).")]
    public void Roadmap_MutualRecursion_TwoFunctions_TwoTypes() {
        var script =
            "type a = {bs: b[] = []}\r " +
            "type b = {a_: a? = none}\r " +
            "countA(x) = 1 + sum(x.bs.map(rule countB(it)))\r " +
            "countB(y) = if(y.a_ == none) 0 else countA(y.a_!)\r " +
            "r = countA(a{bs = [b{a_ = a{bs = []}}, b{}]})";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("r", 2);
    }

    // ROADMAP sentinel non-contractive-bound rejection moved to
    // ImpossibleRecursiveTypeDefinitionsTest.cs (Declared_NonContractiveFBound_FailsOnParse).

    // ROADMAP sentinel: nominal carry. F-bound has no nominal info — but at
    // call site `getLast(a:typed)` the runtime should preserve the named-type
    // identity through the body and back to the result, so accessing
    // `.v` on the result works even though the body's signature is purely
    // structural.
    [Test]
    public void Roadmap_NominalCarry_GenericReturnPreservesTypeName() {
        var script =
            "type node = {v: int, next: node? = none}\r " +
            "getLast(n) = if (n == none) n else (getLast(n?.next) ?? n)\r " +
            "lastVal = getLast(node{v=1, next=node{v=2}})!.v";
        var result = script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);
        result.AssertReturns("lastVal", 2);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Recursive function-type aliases (μX. rule(...)->X?)
    // ════════════════════════════════════════════════════════════════════════
    // Theoretically valid: function arrow is a constructor (contravariant in
    // args, covariant in return) — satisfies contractivity per Cardelli-
    // Mitchell '89. Examples: lazy streams, parser combinators, continuation
    // chains.

    private static CalculationResult RunNamed(string script) =>
        script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);

    [Test]
    public void RecFun_DeclareOnly_NoUse() =>
        // Declaration alone must parse; no annotation references the type.
        Assert.DoesNotThrow(() => RunNamed("type x = rule()->x?"));

    [Test]
    public void RecFun_AsAnnotation_NoArgs() {
        // type x = rule()->x? — annotate a value with the recursive type.
        // The terminal `rule none` produces a value compatible with x
        // (none ≤ x? for any x, so a fun returning none fits rule()->x?).
        var script =
            "type x = rule()->x?\r " +
            "f:x = rule none";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_OneArg_DeclareAndUse() {
        // Declaring a recursive rule with one arg, and using it as a struct
        // field annotation. Direct `f:x = rule none` would fail arity check
        // (`rule none` is 0-arg) — but the type itself must be usable.
        var script =
            "type x = rule(int)->x?\r " +
            "type wrap = {f: x}\r " +
            "y = 1";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_StructField_LazyStream_HeadAccess() {
        // Indirect recursion through a struct field carrying the recursive
        // function. Already works — sentinel that direct-alias fix doesn't
        // regress the working struct-field path.
        var script =
            "type stream = {head: int, tail: rule()->stream?}\r " +
            "s = stream{head=1, tail=rule none}\r " +
            "y = s.head";
        RunNamed(script).AssertResultHas("y", 1);
    }

    [Test]
    public void RecFun_StructField_TailReturnsNone() {
        // tail() returns none — coalesce gives back the struct itself.
        var script =
            "type stream = {head: int, tail: rule()->stream?}\r " +
            "s = stream{head=1, tail=rule none}\r " +
            "y = (s.tail() ?? s).head";
        RunNamed(script).AssertResultHas("y", 1);
    }

    [Test]
    public void RecFun_DirectAlias_CallReturnsNone() {
        // Use the alias to type a function variable, call it, observe none.
        var script =
            "type x = rule()->x?\r " +
            "f:x = rule none\r " +
            "y = f() == none";
        RunNamed(script).AssertResultHas("y", true);
    }

    // ─── Edge cases ────────────────────────────────────────────────────────────

    [Test]
    public void RecFun_MutualRecursion_TwoAliases() {
        // type a = rule()->b?; type b = rule()->a? — mutual recursion.
        // Both alias names appear in each other's body — the resolver must
        // pre-register BOTH placeholders before resolving either body.
        var script =
            "type a = rule()->b?\r " +
            "type b = rule()->a?\r " +
            "fa:a = rule none\r " +
            "fb:b = rule none";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_OptionalOfRecursive_AsAnnotation() {
        // f:x? = none — the alias is recursive; the annotation wraps it in Optional.
        var script =
            "type x = rule()->x?\r " +
            "f:x? = none";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_ArrayOfRecursive_AsAnnotation() {
        // arr:x[] — array of recursive function values. Empty array is the
        // terminal value.
        var script =
            "type x = rule()->x?\r " +
            "arr:x[] = []";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_NestedFunArrow_DeclareOnly() {
        // type x = rule()->rule()->x? — nested function arrows in the recursive
        // chain. Contractivity satisfied via Optional + two arrow constructors.
        Assert.DoesNotThrow(() => RunNamed("type x = rule()->rule()->x?"));
    }

    [Test]
    public void RecFun_RecReturnsArrayOfSelf_DeclareOnly() {
        // type x = rule()->x[] — recursive returns array of self (no Optional;
        // contractive through arrow + array constructors).
        Assert.DoesNotThrow(() => RunNamed("type x = rule()->x[]"));
    }

    [Test]
    public void RecFun_AliasViaNonRecAlias() {
        // type y = int; type x = rule(y)->x? — chain through a non-recursive
        // alias mixed with the recursive one. Resolver must order correctly.
        var script =
            "type y = int\r " +
            "type x = rule(y)->x?\r " +
            "type wrap = {f: x}";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_AsFunctionParameter() {
        // process(f:x) = f() — recursive function type used in a user-function
        // signature. Calling process with a 0-arg rule returning none.
        var script =
            "type x = rule()->x?\r " +
            "process(f:x) = f()\r " +
            "y = process(rule none)";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_ForwardReference_TypeAfterVariable() {
        // Variable annotation precedes type declaration. NFun resolves declarations
        // before resolving annotations, so order shouldn't matter.
        var script =
            "f:x = rule none\r " +
            "type x = rule()->x?";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_TwoIndependentSelfRecursive_NoMutualRef() {
        // Two separate self-recursive aliases that don't reference each other.
        // Each pre-registers itself, then resolves independently.
        var script =
            "type a = rule()->a?\r " +
            "type b = rule()->b?\r " +
            "fa:a = rule none\r " +
            "fb:b = rule none";
        Assert.DoesNotThrow(() => RunNamed(script));
    }

    [Test]
    public void RecFun_DeclareOnly_NonContractiveAccepted() {
        // type t = rule()->t — recursion through function arrow only, no Optional.
        // Under classical iso-recursive types this IS contractive (arrow is a
        // constructor per Cardelli-Mitchell '89). NFun accepts the declaration.
        // Note: there is no useful terminal value, so the type is effectively
        // uninhabited — but the declaration itself is well-formed.
        Assert.DoesNotThrow(() => RunNamed("type t = rule()->t"));
    }
}
