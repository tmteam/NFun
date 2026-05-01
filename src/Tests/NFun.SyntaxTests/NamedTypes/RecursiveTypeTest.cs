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

    // ═══════════════════════════════════════════════════════════════
    // NON-OPTIONAL DIRECT RECURSION — must error (infinite size)
    // ═══════════════════════════════════════════════════════════════

    #region Non-optional direct recursion — error

    [Test]
    public void Direct_NonOpt_SingleField() =>
        Assert.Throws<FunnyParseException>(() => Build("type t = {self:t}"));

    [Test]
    public void Direct_NonOpt_WithOtherFields() =>
        Assert.Throws<FunnyParseException>(() => Build("type node = {v:int, next:node}"));

    [Test]
    public void Direct_NonOpt_TwoRecursiveFields() =>
        Assert.Throws<FunnyParseException>(() => Build("type t = {left:t, right:t}"));

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // NON-OPTIONAL INDIRECT RECURSION — must error
    // ═══════════════════════════════════════════════════════════════

    #region Non-optional indirect recursion — error

    [Test]
    public void Indirect_NonOpt_TwoTypes() =>
        Assert.Throws<FunnyParseException>(() =>
            Build("type a = {b:b}; type b = {a:a}"));

    [Test]
    public void Indirect_NonOpt_ThreeTypes() =>
        Assert.Throws<FunnyParseException>(() =>
            Build("type a = {b:b}; type b = {c:c}; type c = {a:a}"));

    [Test]
    public void Indirect_NonOpt_FourTypes() =>
        Assert.Throws<FunnyParseException>(() =>
            Build("type a = {b:b}; type b = {c:c}; type c = {d:d}; type d = {a:a}"));

    [Test]
    public void Indirect_NonOpt_WithPayload() =>
        Assert.Throws<FunnyParseException>(() =>
            Build("type a = {x:int, b:b}; type b = {y:text, a:a}"));

    #endregion

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

    [Test]
    public void RecursiveDefaultConstructor_CompileError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "type node = {v:int, next:node? = node{v=0}}; n = node{v=1}; out = n.v"
                .CalcWithDialect(
                    optionalTypesSupport: OptionalTypesSupport.Enabled,
                    namedTypesSupport: NamedTypesSupport.Enabled));
    }

    [Test]
    public void TreeArrayMap_NoStackOverflow() {
        Assert.DoesNotThrow(() =>
            "type tree = {v:int, children:tree[] = []}; forest = [tree{v=1, children=[tree{v=10}]}, tree{v=2}]; out = forest.map(rule it.children.count())"
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled));
    }
}
