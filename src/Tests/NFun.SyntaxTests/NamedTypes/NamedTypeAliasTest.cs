using System.Text;
using NFun.Runtime;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests for named type definitions covering all possible type combinations
/// within the current syntax: type name = {field:type, ...}
/// </summary>
public class NamedTypeVariantsTest {

    static object Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled)
        .Get("out");

    static FunnyRuntime Build(string expr) =>
        Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build(expr);

    // ═══════════════════════════════════════════════════════════
    // PRIMITIVE FIELD TYPES
    // ═══════════════════════════════════════════════════════════

    [TestCase("type t = {x:int}; out = t{x=42}.x", 42)]
    [TestCase("type t = {x:real}; out = t{x=3.14}.x", 3.14)]
    [TestCase("type t = {x:bool}; out = t{x=true}.x", true)]
    [TestCase("type t = {x:text}; out = t{x='hello'}.x", "hello")]
    public void PrimitiveFieldType(string expr, object expected) =>
        Assert.AreEqual(expected, Calc(expr));

    // ═══════════════════════════════════════════════════════════
    // COMPOSITE FIELD TYPES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ArrayField() =>
        Assert.AreEqual(3, Calc("type t = {items:int[]}; out = t{items=[1,2,3]}.items.count()"));

    [Test]
    public void OptionalField_WithValue() =>
        Assert.AreEqual(42, Calc("type t = {x:int? = none}; out = t{x=42}.x ?? -1"));

    [Test]
    public void OptionalField_WithNone() =>
        Assert.AreEqual(-1, Calc("type t = {x:int? = none}; out = t{}.x ?? -1"));

    [Test]
    public void ArrayOfArrayField() =>
        Assert.AreEqual(2, Calc("type t = {m:int[][]}; out = t{m=[[1,2],[3]]}.m.count()"));

    [Test] // FIXED: ?.method() now supported
    public void OptionalArrayField() =>
        Assert.AreEqual(3, Calc("type t = {items:int[]? = none}; v = t{items=[1,2,3]}; out = v.items?.count() ?? 0"));

    // ═══════════════════════════════════════════════════════════
    // MULTIPLE FIELDS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TwoFields() =>
        Assert.AreEqual(42, Calc("type t = {x:int, y:int}; out = t{x=40, y=2}.x + t{x=40, y=2}.y"));

    [Test]
    public void ThreeFields_MixedTypes() {
        var rt = Build("type person = {name:text, age:int, active:bool}; out = person{name='Alice', age=30, active=true}");
        rt.Run();
        var dict = (System.Collections.Generic.IReadOnlyDictionary<string, object>)rt["out"].Value;
        Assert.AreEqual("Alice", dict["name"]);
        Assert.AreEqual(30, dict["age"]);
        Assert.AreEqual(true, dict["active"]);
    }

    [Test]
    public void ManyFields() =>
        Assert.AreEqual(15, Calc(
            "type t = {a:int, b:int, c:int, d:int, e:int}; " +
            "out = t{a=1,b=2,c=3,d=4,e=5}.a + t{a=1,b=2,c=3,d=4,e=5}.b + t{a=1,b=2,c=3,d=4,e=5}.c + t{a=1,b=2,c=3,d=4,e=5}.d + t{a=1,b=2,c=3,d=4,e=5}.e"));

    // ═══════════════════════════════════════════════════════════
    // DEFAULTS
    // ═══════════════════════════════════════════════════════════

    [TestCase("type t = {x:int = 0}; out = t{}.x", 0)]
    [TestCase("type t = {x:int = 42}; out = t{}.x", 42)]
    [TestCase("type t = {x:bool = true}; out = t{}.x", true)]
    [TestCase("type t = {x:text = 'hi'}; out = t{}.x", "hi")]
    public void DefaultValues(string expr, object expected) =>
        Assert.AreEqual(expected, Calc(expr));

    [Test]
    public void DefaultArray() =>
        Assert.AreEqual(0, Calc("type t = {items:int[] = []}; out = t{}.items.count()"));

    [Test]
    public void PartialDefaults() =>
        Assert.AreEqual(142, Calc("type t = {x:int, y:int = 100}; out = t{x=42}.x + t{x=42}.y"));

    // ═══════════════════════════════════════════════════════════
    // TYPE REFERENCES (non-recursive)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void FieldRefToAnotherType() =>
        Assert.AreEqual(42, Calc("type inner = {v:int}; type outer = {i:inner}; out = outer{i=inner{v=42}}.i.v"));

    [Test]
    public void ThreeLevelNesting() =>
        Assert.AreEqual(99, Calc(
            "type a = {v:int}; type b = {a:a}; type c = {b:b}; " +
            "out = c{b=b{a=a{v=99}}}.b.a.v"));

    [Test]
    public void ArrayOfNamedType() =>
        Assert.AreEqual(3, Calc(
            "type item = {v:int}; type container = {items:item[]}; " +
            "out = container{items=[item{v=1},item{v=2},item{v=3}]}.items.count()"));

    [Test] // Works — Optional named type field access
    public void OptionalNamedType() =>
        Assert.AreEqual(42, Calc(
            "type inner = {v:int}; type outer = {i:inner? = none}; " +
            "out = outer{i=inner{v=42}}.i?.v ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // RECURSIVE TYPES — Optional self-reference
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void RecursiveOptional_Leaf() =>
        Assert.AreEqual(42, Calc("type node = {v:int, next:node? = none}; out = node{v=42}.v"));

    [Test]
    public void RecursiveOptional_OneLevel() =>
        Assert.AreEqual(2, Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = node{v=1, next=node{v=2}}.next?.v ?? -1"));

    [Test]
    public void RecursiveOptional_SafeAccessNone() =>
        Assert.AreEqual(-1, Calc(
            "type node = {v:int, next:node? = none}; " +
            "out = node{v=1}.next?.v ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // RECURSIVE TYPES — Array self-reference
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void RecursiveArray_LeafEmptyChildren() =>
        Assert.AreEqual(0, Calc("type t = {v:int, children:t[] = []}; out = t{v=1}.children.count()"));

    [Test]
    public void RecursiveArray_WithChildren() =>
        Assert.AreEqual(2, Calc(
            "type t = {v:int, children:t[] = []}; " +
            "out = t{v=0, children=[t{v=1}, t{v=2}]}.children.count()"));

    [Test]
    public void RecursiveArray_MapChildValues() {
        Calc("type t = {v:int, children:t[] = []}; " +
             "root = t{v=0, children=[t{v=10}, t{v=20}]}; " +
             "out = root.children.map(rule it.v)");
    }

    // ═══════════════════════════════════════════════════════════
    // RECURSIVE TYPES — Binary tree
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void BinaryTree_LeftAccess() =>
        Assert.AreEqual(2, Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=1, left=tree{v=2}}; out = t.left?.v ?? -1"));

    [Test]
    public void BinaryTree_RightNone() =>
        Assert.AreEqual(-1, Calc(
            "type tree = {v:int, left:tree? = none, right:tree? = none}; " +
            "t = tree{v=1, left=tree{v=2}}; out = t.right?.v ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // MUTUAL RECURSION
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void MutualRecursion_TwoTypes() =>
        Assert.AreEqual(3, Calc(
            "type a = {x:int, b:b? = none}; type b = {y:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2, a=a{x=3}}}.b?.a?.x ?? -1"));

    [Test]
    public void MutualRecursion_ThreeTypes() =>
        Assert.AreEqual(4, Calc(
            "type a = {x:int, b:b? = none}; type b = {y:int, c:c? = none}; type c = {z:int, a:a? = none}; " +
            "out = a{x=1, b=b{y=2, c=c{z=3, a=a{x=4}}}}.b?.c?.a?.x ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // MIXED: Optional + Array in same type
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void MixedOptionalAndArray() =>
        Assert.AreEqual(42, Calc(
            "type t = {v:int, children:t[] = [], parent:t? = none}; " +
            "out = t{v=42}.v"));

    [Test]
    public void OptionalArrayOfSelf() =>
        Assert.AreEqual(1, Calc(
            "type t = {v:int, items:t[]? = none}; " +
            "out = t{v=1}.v"));

    // ═══════════════════════════════════════════════════════════
    // DEEP NESTING (constructor depth)
    // ═══════════════════════════════════════════════════════════

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepNesting_NonRecursive(int depth) {
        // type t1 = {v:int}; type t2 = {inner:t1}; ... type tN = {inner:tN-1}
        var sb = new StringBuilder();
        sb.Append("type t1 = {v:int}; ");
        for (int i = 2; i <= depth; i++)
            sb.Append($"type t{i} = {{inner:t{i - 1}}}; ");
        // Build nested constructors
        sb.Append($"x = t{depth}{{inner=");
        for (int i = depth - 1; i >= 2; i--)
            sb.Append($"t{i}{{inner=");
        sb.Append("t1{v=42}");
        for (int i = 1; i < depth; i++)
            sb.Append('}');
        sb.Append("; ");
        // Access through chain
        sb.Append("out = x");
        for (int i = 0; i < depth - 1; i++)
            sb.Append(".inner");
        sb.Append(".v");

        Assert.AreEqual(42, Calc(sb.ToString()));
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepNesting_RecursiveOptional_AccessChain(int depth) {
        // Build linked list and access at given depth
        var sb = new StringBuilder();
        sb.Append("type node = {v:int, next:node? = none}; n = ");
        for (int i = 1; i <= depth; i++) {
            sb.Append($"node{{v={i}");
            if (i < depth) sb.Append(", next=");
        }
        for (int i = 1; i <= depth; i++) sb.Append('}');
        sb.Append("; out = n");
        for (int i = 1; i < depth; i++) sb.Append(".next?");
        sb.Append(".v ?? -1");

        Assert.AreEqual(depth, Calc(sb.ToString()));
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void DeepNesting_RecursiveArray_FlatChildren(int depth) {
        // Build tree with `depth` children at root, each a leaf
        var sb = new StringBuilder();
        sb.Append("type t = {v:int, children:t[] = []}; root = t{v=0, children=[");
        for (int i = 1; i <= depth; i++) {
            if (i > 1) sb.Append(", ");
            sb.Append($"t{{v={i}}}");
        }
        sb.Append("]}; out = root.children.count()");

        Assert.AreEqual(depth, Calc(sb.ToString()));
    }

    // ═══════════════════════════════════════════════════════════
    // USAGE IN EXPRESSIONS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void NamedTypeInIfElse() =>
        Assert.AreEqual(1, Calc(
            "type t = {v:int}; " +
            "out = if(true) t{v=1}.v else t{v=2}.v"));

    [Test]
    public void NamedTypeInArray() =>
        Assert.AreEqual(3, Calc(
            "type t = {v:int}; " +
            "out = [t{v=1}, t{v=2}, t{v=3}].count()"));

    [Test]
    public void NamedTypeInFunction() =>
        Assert.AreEqual(42, Calc(
            "type t = {v:int}; " +
            "getV(x:t):int = x.v; " +
            "out = getV(t{v=42})"));

    [Test]
    public void NamedTypeFieldArithmetic() =>
        Assert.AreEqual(30, Calc(
            "type point = {x:int, y:int}; " +
            "p = point{x=10, y=20}; " +
            "out = p.x + p.y"));
}
