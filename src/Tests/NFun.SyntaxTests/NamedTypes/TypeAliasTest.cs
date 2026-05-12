using System.Text;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests for type aliases: type name = any_type
/// Not just structs — primitives, arrays, optionals, functions, nested combinations.
/// </summary>
public class TypeAliasTest {

    static object Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled)
        .Get("out");

    // ═══════════════════════════════════════════════════════════
    // PRIMITIVE ALIASES
    // ═══════════════════════════════════════════════════════════

    [TestCase("type age = int; x:age = 42; out = x", 42)]
    [TestCase("type score = real; x:score = 3.14; out = x", 3.14)]
    [TestCase("type flag = bool; x:flag = true; out = x", true)]
    [TestCase("type name = text; x:name = 'hello'; out = x", "hello")]
    public void PrimitiveAlias(string expr, object expected) =>
        Assert.AreEqual(expected, Calc(expr));

    // ═══════════════════════════════════════════════════════════
    // ARRAY ALIASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ArrayAlias() =>
        Assert.AreEqual(3, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.count()"));

    [Test]
    public void ArrayAlias_Operations() =>
        Assert.AreEqual(6, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.fold(rule it1+it2)"));

    [Test]
    public void NestedArrayAlias() =>
        Assert.AreEqual(2, Calc("type matrix = int[][]; x:matrix = [[1,2],[3,4]]; out = x.count()"));

    [Test]
    public void ArrayOfTextAlias() =>
        Assert.AreEqual(3, Calc("type words = text[]; x:words = ['a','b','c']; out = x.count()"));

    // ═══════════════════════════════════════════════════════════
    // OPTIONAL ALIASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void OptionalAlias_WithValue() =>
        Assert.AreEqual(42, Calc("type maybeInt = int?; x:maybeInt = 42; out = x ?? -1"));

    [Test]
    public void OptionalAlias_WithNone() =>
        Assert.AreEqual(-1, Calc("type maybeInt = int?; x:maybeInt = none; out = x ?? -1"));

    [Test] // FIXED: ?.method() now supported
    public void OptionalArrayAlias() =>
        Assert.AreEqual(3, Calc("type maybeNums = int[]?; x:maybeNums = [1,2,3]; out = x?.count() ?? 0"));

    // ═══════════════════════════════════════════════════════════
    // STRUCT ALIASES (existing syntax — should still work)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructAlias() =>
        Assert.AreEqual(42, Calc("type point = {x:int, y:int}; p:point = point{x=40, y=2}; out = p.x + p.y"));

    [Test]
    public void StructAlias_InArray() =>
        Assert.AreEqual(2, Calc("type point = {x:int, y:int}; arr:point[] = [point{x=1,y=2}, point{x=3,y=4}]; out = arr.count()"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS TO ALIAS (chaining)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void AliasToAlias() =>
        Assert.AreEqual(42, Calc("type a = int; type b = a; x:b = 42; out = x"));

    [Test]
    public void AliasChain_Three() =>
        Assert.AreEqual(42, Calc("type a = int; type b = a; type c = b; x:c = 42; out = x"));

    [Test]
    public void AliasToArrayOfAlias() =>
        Assert.AreEqual(3, Calc("type id = int; type ids = id[]; x:ids = [1,2,3]; out = x.count()"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS USED AS FIELD TYPE IN STRUCT
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void AliasAsFieldType() =>
        Assert.AreEqual(42, Calc("type age = int; type person = {a:age}; out = person{a=42}.a"));

    [Test]
    public void ArrayAliasAsFieldType() =>
        Assert.AreEqual(3, Calc("type nums = int[]; type container = {items:nums}; out = container{items=[1,2,3]}.items.count()"));

    // ═══════════════════════════════════════════════════════════
    // RECURSIVE ALIASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void RecursiveArrayAlias() =>
        Assert.AreEqual(3, Calc(
            "type tree = {v:int, children:tree[] = []}; " +
            "out = tree{v=0, children=[tree{v=1},tree{v=2},tree{v=3}]}.children.count()"));

    [Test]
    public void RecursiveOptionalAlias() =>
        Assert.AreEqual(2, Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = node{v=1, next=node{v=2}}.next?.v ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // COMPLEX COMBINATIONS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void OptionalOfStructAlias() =>
        Assert.AreEqual(42, Calc(
            "type point = {x:int, y:int}; " +
            "p:point? = point{x=42, y=0}; " +
            "out = p?.x ?? -1"));

    [Test]
    public void ArrayOfOptionalAlias() =>
        Assert.AreEqual(2, Calc(
            "type maybeInt = int?; " +
            "arr:maybeInt[] = [1, none, 3]; " +
            "out = arr.filter(rule it != none).count()"));

    [Test] // FIXED: ?.method() now supported
    public void StructWithOptionalArrayFieldAlias() =>
        Assert.AreEqual(0, Calc(
            "type ids = int[]; " +
            "type container = {items:ids? = none}; " +
            "c = container{}; out = c.items?.count() ?? 0"));

    [Test]
    public void AliasUsedInFunction() =>
        Assert.AreEqual(42, Calc(
            "type age = int; " +
            "getAge(x:age):age = x; " +
            "out = getAge(42)"));

    [Test]
    public void AliasUsedInFunctionWithStruct() =>
        Assert.AreEqual(42, Calc(
            "type point = {x:int, y:int}; " +
            "getX(p:point):int = p.x; " +
            "out = getX(point{x=42, y=0})"));

    // ═══════════════════════════════════════════════════════════
    // DEEP NESTING
    // ═══════════════════════════════════════════════════════════

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepAliasChain(int depth) {
        // type t1 = int; type t2 = t1; ... type tN = tN-1; x:tN = 42; out = x
        var sb = new StringBuilder();
        sb.Append("type t1 = int; ");
        for (int i = 2; i <= depth; i++)
            sb.Append($"type t{i} = t{i - 1}; ");
        sb.Append($"x:t{depth} = 42; out = x");
        Assert.AreEqual(42, Calc(sb.ToString()));
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepArrayAliasChain(int depth) {
        // type t1 = int[]; type t2 = t1[]; ... type tN = tN-1[]
        // each level wraps in another array
        // t1 = int[], t2 = int[][], t3 = int[][][], ...
        var sb = new StringBuilder();
        sb.Append("type t1 = int[]; ");
        for (int i = 2; i <= depth; i++)
            sb.Append($"type t{i} = t{i - 1}[]; ");
        // Build nested array literal: [[...[1]...]]
        sb.Append($"x:t{depth} = ");
        for (int i = 1; i < depth; i++) sb.Append('[');
        sb.Append("[1]");
        for (int i = 1; i < depth; i++) sb.Append(']');
        sb.Append("; out = x.count()");
        Assert.AreEqual(1, Calc(sb.ToString()));
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepStructNesting_DifferentTypes(int depth) {
        // type t1 = {v:int}; type t2 = {inner:t1}; ... type tN = {inner:tN-1}
        var sb = new StringBuilder();
        sb.Append("type t1 = {v:int}; ");
        for (int i = 2; i <= depth; i++)
            sb.Append($"type t{i} = {{inner:t{i - 1}}}; ");
        // Build nested constructor
        sb.Append($"x = t{depth}{{inner=");
        for (int i = depth - 1; i >= 2; i--)
            sb.Append($"t{i}{{inner=");
        sb.Append("t1{v=42}");
        for (int i = 1; i < depth; i++) sb.Append('}');
        sb.Append("; out = x");
        for (int i = 0; i < depth - 1; i++) sb.Append(".inner");
        sb.Append(".v");
        Assert.AreEqual(42, Calc(sb.ToString()));
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepOptionalChain(int depth) {
        // type t1 = int?; type t2 = t1?; ... type tN = tN-1?
        // nested optionals should flatten
        var sb = new StringBuilder();
        sb.Append("type t1 = int?; ");
        for (int i = 2; i <= depth; i++)
            sb.Append($"type t{i} = t{i - 1}?; ");
        sb.Append($"x:t{depth} = 42; out = x ?? -1");
        Assert.AreEqual(42, Calc(sb.ToString()));
    }

    // ═══════════════════════════════════════════════════════════
    // ERROR CASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void UndefinedTypeAlias_Throws() =>
        Assert.Throws<Exceptions.FunnyParseException>(() =>
            Calc("type a = nonexistent; x:a = 1; out = x"));

    // Circular alias detection is consolidated in ImpossibleRecursiveTypeDefinitionsTest.cs.
}
