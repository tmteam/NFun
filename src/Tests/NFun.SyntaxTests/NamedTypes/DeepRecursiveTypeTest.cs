using System;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Deep recursive type tests — testing limits of recursion through Optional and Array.
/// These require TIC to handle recursive struct references without stack overflow.
/// </summary>
public class DeepRecursiveTypeTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    static CalculationResult Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled);

    // ═══════════════════════════════════════════════════════════════
    // LINKED LIST — deep access chains
    // ═══════════════════════════════════════════════════════════════

    #region Linked list — depth 2

    [Test]
    public void LinkedList_Depth2_LastNone() {
        // n = node{v=1, next=node{v=2}}
        // n.next?.next?.v → -1 (next of second node is none)
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2}}; " +
            "out = n.next?.next?.v ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    [Test]
    public void LinkedList_Depth2_AccessMiddle() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2}}; " +
            "out = n.next?.v ?? -1");
        Assert.AreEqual(2, r.Get("out"));
    }

    #endregion

    #region Linked list — depth 3

    [Test]
    public void LinkedList_Depth3_AccessLast() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2, next=node{v=3}}}; " +
            "out = n.next?.next?.v ?? -1");
        Assert.AreEqual(3, r.Get("out"));
    }

    [Test]
    public void LinkedList_Depth3_PastEnd() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=1, next=node{v=2, next=node{v=3}}}; " +
            "out = n.next?.next?.next?.v ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // BINARY TREE — deep access
    // ═══════════════════════════════════════════════════════════════

    #region Binary tree — depth 2

    [Test]
    public void BinaryTree_Depth2_LeftLeft() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=1, left=tree{v=2, left=tree{v=3}}}; " +
            "out = t.left?.left?.v ?? -1");
        Assert.AreEqual(3, r.Get("out"));
    }

    [Test]
    public void BinaryTree_Depth2_LeftRight() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=1, left=tree{v=2, right=tree{v=4}}}; " +
            "out = t.left?.right?.v ?? -1");
        Assert.AreEqual(4, r.Get("out"));
    }

    [Test]
    public void BinaryTree_Depth2_RightNone() {
        var r = Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=1, left=tree{v=2}}; " +
            "out = t.left?.right?.v ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // ARRAY-OF-SELF — nested children
    // ═══════════════════════════════════════════════════════════════

    #region Array of self — nested

    [Test]
    public void ArrayOfSelf_TwoLevelChildren() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=1, children=[t{v=3}, t{v=4}]}, t{v=2}]}; " +
            "out = root.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void ArrayOfSelf_ChildCount() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=1}, t{v=2}, t{v=3}]}; " +
            "out = root.children.count()");
        Assert.AreEqual(3, r.Get("out"));
    }

    [Test]
    public void ArrayOfSelf_MapValues() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "root = t{v=0, children=[t{v=10}, t{v=20}]}; " +
            "out = root.children.map(rule it.v)");
    }

    [Test]
    public void ArrayOfSelf_EmptyChildren() {
        var r = Calc(
            "type t = {v:int, children:t[] = []}; " +
            "leaf = t{v=42}; " +
            "out = leaf.children.count()");
        Assert.AreEqual(0, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // INDIRECT RECURSION — deep access
    // ═══════════════════════════════════════════════════════════════

    #region Indirect recursion

    [Test]
    public void Indirect_TwoTypes_DeepAccess() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int, a:a? = none}; " +
            "v = a{x=1, b=b{y=2, a=a{x=3}}}; " +
            "out = v.b?.a?.x ?? -1");
        Assert.AreEqual(3, r.Get("out"));
    }

    [Test]
    public void Indirect_TwoTypes_DeepAccess_None() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int, a:a? = none}; " +
            "v = a{x=1, b=b{y=2}}; " +
            "out = v.b?.a?.x ?? -1");
        Assert.AreEqual(-1, r.Get("out"));
    }

    [Test]
    public void Indirect_ThreeTypes_DeepAccess() {
        var r = Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int, c:c? = none}; " +
            "type c = {z:int, a:a? = none}; " +
            "v = a{x=1, b=b{y=2, c=c{z=3, a=a{x=4}}}}; " +
            "out = v.b?.c?.a?.x ?? -1");
        Assert.AreEqual(4, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // MIXED: Optional + Array in recursive type
    // ═══════════════════════════════════════════════════════════════

    #region Mixed Optional + Array

    [Test]
    public void Mixed_TreeWithChildrenArray_And_OptionalParent() {
        var r = Calc(
            "type t = {v:int, children:t[] = [], parent:t? = none}; " +
            "leaf = t{v=42}; " +
            "out = leaf.v");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void Mixed_StructWithOptArray() {
        // Optional array of self
        var r = Calc(
            "type t = {v:int, items:t[]? = none}; " +
            "out = t{v=1}.v");
        Assert.AreEqual(1, r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // PRACTICAL: JSON-like tree, config, etc.
    // ═══════════════════════════════════════════════════════════════

    #region Practical use cases

    [Test]
    public void Practical_MenuTree() {
        var r = Calc(
            "type menu = {label:text, submenu:menu[] = []}; " +
            "m = menu{label='File', submenu=[menu{label='New'}, menu{label='Open'}]}; " +
            "out = m.label");
        Assert.AreEqual("File", r.Get("out"));
    }

    [Test]
    public void Practical_LinkedListSum() {
        // Sum first two values of a linked list using ??
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=10, next=node{v=20}}; " +
            "out = n.v + (n.next?.v ?? 0)");
        Assert.AreEqual(30, r.Get("out"));
    }

    [Test]
    public void Practical_OrgChart() {
        var r = Calc(
            "type emp = {name:text, reports:emp[] = []}; " +
            "boss = emp{name='CEO', reports=[emp{name='CTO'}, emp{name='CFO'}]}; " +
            "out = boss.reports.count()");
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void Practical_CommentThread() {
        var r = Calc(
            "type comment = {msg:text, replies:comment[] = []}; " +
            "c = comment{msg='hello', replies=[comment{msg='world'}]}; " +
            "out = c.msg");
        Assert.AreEqual("hello", r.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // EDGE CASES
    // ═══════════════════════════════════════════════════════════════

    #region Edge cases

    [Test]
    public void Edge_SelfOptionalOnly() {
        // Only field is self-referencing optional
        Calc("type t = {self:t? = none}; out = t{}");
    }

    [Test] // FIXED: MergeInplace implicit lift T ≤ opt(T)
    public void Edge_ArrayOfRecursiveInIfElse() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "arr = if(true) [node{v=1}] else [node{v=2, next=node{v=3}}]; " +
            "out = arr.count()");
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void Edge_RecursiveTypeInFunction() {
        var r = Calc(
            "type node = {v:int, next:node? = none}; " +
            "getVal(n:node):int = n.v; " +
            "out = getVal(node{v=42, next=node{v=99}})");
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void Edge_RecursiveAllDefaults() {
        var r = Calc(
            "type node = {v:int = 0, next:node? = none}; " +
            "out = node{}.v");
        Assert.AreEqual(0, r.Get("out"));
    }

    [Test]
    public void Edge_PartialCycleBreak_AtoB_NonOpt_BtoA_Opt() {
        // a→b (non-optional), b→a (optional) — one-directional break
        var r = Calc(
            "type a = {x:int, b:b}; " +
            "type b = {y:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2}}.b.y");
        Assert.AreEqual(2, r.Get("out"));
    }

    #endregion
}
