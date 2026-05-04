using System;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>
/// Tests for struct-based trees in lang mode.
///
/// Limitation: recursive struct types (tree node pointing to tree node) require
/// named types. Anonymous structs with recursive functions cause "Recursive type
/// definition" because TIC creates a cyclic struct constraint.
///
/// Named types ('type tree = {value, left: tree?, right: tree?}') work in
/// expression mode but 'type' keyword not yet in LangParser.
///
/// These tests use non-recursive patterns: fixed-depth structs, struct mutation,
/// and struct algorithms that don't require recursive types.
/// </summary>
[TestFixture]
public class LangStructTreeTest {

    [Test]
    public void StructLeaf() {
        var rt = Funny.Hardcore.BuildLang(
            "t = {value = 42, left = none, right = none}\ny = t.value");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void StructNested_TwoLevels() {
        var rt = Funny.Hardcore.BuildLang(
            "t = {value = 1, left = {value = 2, left = none, right = none}, right = {value = 3, left = none, right = none}}\n" +
            "y = t.left.value + t.right.value");
        rt.Run();
        Assert.AreEqual(5, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void StructNested_ThreeLevels() {
        var rt = Funny.Hardcore.BuildLang(
            "t = {v = 1, l = {v = 2, l = {v = 4, l = none, r = none}, r = none}, r = {v = 3, l = none, r = none}}\n" +
            "y = t.l.l.v");
        rt.Run();
        Assert.AreEqual(4, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void StructSafeAccess_NoneChild() {
        var rt = Funny.Hardcore.BuildLang(
            "t = {value = 1, left = none, right = {value = 3, left = none, right = none}}\n" +
            "y = t.left?.value ?? -1");
        rt.Run();
        Assert.AreEqual(-1, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void StructFieldMutation() {
        var rt = Funny.Hardcore.BuildLang(
            "p = {x = 0, y = 0, label = 'origin'}\n" +
            "p.x = 10\np.y = 20\np.label = 'moved'\n" +
            "result = p.x + p.y");
        rt.Run();
        Assert.AreEqual(30, Convert.ToInt32(rt["result"].Value));
    }

    [Test]
    public void StructInArray() {
        var rt = Funny.Hardcore.BuildLang(
            "points = [{x = 1, y = 2}, {x = 3, y = 4}, {x = 5, y = 6}]\n" +
            "total = 0\nfor p in points:\n    total += p.x + p.y\ny = total");
        rt.Run();
        Assert.AreEqual(21, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void StructAsAccumulator() {
        var rt = Funny.Hardcore.BuildLang(
            "stats = {sum = 0, count = 0}\n" +
            "for item in [10, 20, 30, 40]:\n" +
            "    stats.sum += item\n" +
            "    stats.count += 1\n" +
            "avg = stats.sum // stats.count");
        rt.Run();
        Assert.AreEqual(25, Convert.ToInt32(rt["avg"].Value));
    }

    [Test]
    public void NamedTypeTree_ExpressionMode() {
        // Named types work in expression mode
        var rt = Funny.Hardcore
            .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled,
                         optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build(
                "type tree = {value: int, left: tree?, right: tree?}\r" +
                "t = {value = 1, left = {value = 2, left = none, right = none}, right = none}\r" +
                "y = t.left?.value ?? 0");
        rt.Run();
        Assert.AreEqual(2, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void NamedTypeTree_LangMode() {
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n" +
            "t = {value = 1, left = {value = 2, left = none, right = none}, right = none}\n" +
            "y = t.left?.value ?? 0");
        rt.Run();
        Assert.AreEqual(2, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void NamedTypeTree_LangMode_RecursiveFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n\n" +
            "fun treeSize(t):\n" +
            "    if t == none: return 0\n" +
            "    return 1 + treeSize(t?.left) + treeSize(t?.right)\n\n" +
            "y = treeSize({value=1, left={value=2, left=none, right=none}, right=none})");
        rt.Run();
        Assert.AreEqual(2, Convert.ToInt32(rt["y"].Value));
    }

    [Test][Ignore("Lang mode parser does not support expression-style function definitions")]
    public void NamedTypeTree_LangMode_ExpressionFunction() {
        // Same but with expression-style function (no fun/return)
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n\n" +
            "treeSize(t) = if(t==none) 0 else 1 + treeSize(t?.left) + treeSize(t?.right)\n\n" +
            "y = treeSize({value=1, left={value=2, left=none, right=none}, right=none})");
        rt.Run();
        Assert.AreEqual(2, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void NamedTypeTree_ExprMode_RecursiveFunction() {
        var rt = Funny.Hardcore
            .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled,
                         optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build(
                "type tree = {value: int, left: tree?, right: tree?}\r" +
                "treeSize(t) = if(t==none) 0 else 1 + treeSize(t?.left) + treeSize(t?.right)\r" +
                "y = treeSize({value=1, left={value=2, left=none, right=none}, right=none})");
        rt.Run();
        Assert.AreEqual(2, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void BlockNarrowing_SimpleNoneCheck() {
        // Simplest block narrowing: early-exit guard narrows optional to non-optional
        var rt = Funny.Hardcore.BuildLang(
            "fun getValue(x: int?):\n" +
            "    if x == none: return -1\n" +
            "    return x + 1\n" +
            "y = getValue(42)");
        rt.Run();
        Assert.AreEqual(43, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void BlockNarrowing_NoneCheck_WithNone() {
        // Guard triggers on none input: returns -1
        var rt = Funny.Hardcore.BuildLang(
            "fun getValue(x: int?):\n" +
            "    if x == none: return -1\n" +
            "    return x + 1\n" +
            "y = getValue(none)");
        rt.Run();
        Assert.AreEqual(-1, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void BlockNarrowing_MultipleGuards() {
        // Multiple early-exit guards narrow different variables
        var rt = Funny.Hardcore.BuildLang(
            "fun add(a: int?, b: int?):\n" +
            "    if a == none: return -1\n" +
            "    if b == none: return -2\n" +
            "    return a + b\n" +
            "y = add(10, 20)");
        rt.Run();
        Assert.AreEqual(30, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void NamedTypeTree_LangMode_DeepAccess() {
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n" +
            "t = {value = 1, left = {value = 2, left = {value = 4, left = none, right = none}, right = none}, right = {value = 3, left = none, right = none}}\n" +
            "y = t.left?.left?.value ?? -1");
        rt.Run();
        Assert.AreEqual(4, Convert.ToInt32(rt["y"].Value));
    }

    // ─── #120: Block-level narrowing — direct field access after early-exit guard ───
    // After `if t == none: return ...`, the parameter `t` is narrowed so that
    // direct field access (`t.value`, no `?.`) works in subsequent statements.

    [Test]
    public void BlockNarrowing_RecursiveTree_DirectFieldAccess() {
        // Direct .value (no ?.) on narrowed recursive named type after early-return guard.
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n" +
            "fun treeSum(t):\n" +
            "    if t == none: return 0\n" +
            "    return t.value + treeSum(t.left) + treeSum(t.right)\n" +
            "y = treeSum({value=1, left={value=2, left=none, right=none}, right={value=3, left=none, right=none}})");
        rt.Run();
        Assert.AreEqual(6, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void BlockNarrowing_RecursiveTree_NoneInput() {
        // Same function with none input — guard fires, returns 0.
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n" +
            "fun treeSum(t):\n" +
            "    if t == none: return 0\n" +
            "    return t.value + treeSum(t.left) + treeSum(t.right)\n" +
            "y = treeSum(none)");
        rt.Run();
        Assert.AreEqual(0, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void BlockNarrowing_RecursiveTree_LeafOnly() {
        // Leaf-only tree: guard fires at every recursive call into none children.
        var rt = Funny.Hardcore.BuildLang(
            "type tree = {value: int, left: tree?, right: tree?}\n" +
            "fun treeSum(t):\n" +
            "    if t == none: return 0\n" +
            "    return t.value + treeSum(t.left) + treeSum(t.right)\n" +
            "y = treeSum({value=42, left=none, right=none})");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }
}
