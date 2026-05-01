using System.Text;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Edge case tests for type aliases covering all possible interactions
/// with NFun features: operators, functions, lambdas, coercion, etc.
/// </summary>
public class TypeAliasEdgeCasesTest {

    static object Calc(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled)
        .Get("out");

    static void CalcNoThrow(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled);

    // ═══════════════════════════════════════════════════════════
    // ALIAS IN ARITHMETIC / COMPARISON
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void IntAlias_Addition() =>
        Assert.AreEqual(3, Calc("type n = int; a:n = 1; b:n = 2; out = a + b"));

    [Test]
    public void IntAlias_Multiplication() =>
        Assert.AreEqual(6, Calc("type n = int; a:n = 2; b:n = 3; out = a * b"));

    [Test]
    public void IntAlias_Comparison() =>
        Assert.AreEqual(true, Calc("type n = int; a:n = 5; out = a > 3"));

    [Test]
    public void RealAlias_Arithmetic() =>
        Assert.AreEqual(5.5, Calc("type r = real; a:r = 2.5; b:r = 3.0; out = a + b"));

    [Test]
    public void TextAlias_Concat() =>
        Assert.AreEqual("helloworld", Calc("type s = text; a:s = 'hello'; b:s = 'world'; out = a.concat(b)"));

    [Test]
    public void BoolAlias_Logic() =>
        Assert.AreEqual(false, Calc("type flag = bool; a:flag = true; b:flag = false; out = a and b"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS IN IF-ELSE
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void IntAlias_InIfElse() =>
        Assert.AreEqual(10, Calc("type n = int; x:n = 5; out = if(x > 3) 10 else 20"));

    [Test]
    public void ArrayAlias_InIfElse() =>
        Assert.AreEqual(2, Calc("type nums = int[]; out = if(true) [1,2].count() else [3,4,5].count()"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS WITH ARRAY FUNCTIONS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ArrayAlias_Map() =>
        Assert.AreEqual(3, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.map(rule it*2).count()"));

    [Test]
    public void ArrayAlias_Filter() =>
        Assert.AreEqual(2, Calc("type nums = int[]; x:nums = [1,2,3,4]; out = x.filter(rule it > 2).count()"));

    [Test]
    public void ArrayAlias_Fold() =>
        Assert.AreEqual(10, Calc("type nums = int[]; x:nums = [1,2,3,4]; out = x.fold(rule it1+it2)"));

    [Test]
    public void ArrayAlias_Sort() =>
        CalcNoThrow("type nums = int[]; x:nums = [3,1,2]; out = x.sort()");

    [Test]
    public void ArrayAlias_Reverse() =>
        Assert.AreEqual(3, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.reverse()[0]"));

    [Test]
    public void ArrayAlias_Count() =>
        Assert.AreEqual(3, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.count()"));

    [Test]
    public void ArrayAlias_Any() =>
        Assert.AreEqual(true, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.any(rule it > 2)"));

    [Test]
    public void ArrayAlias_All() =>
        Assert.AreEqual(false, Calc("type nums = int[]; x:nums = [1,2,3]; out = x.all(rule it > 2)"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS IN LAMBDA / RULE
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructAlias_InMapLambda() =>
        CalcNoThrow("type p = {x:int, y:int}; arr = [p{x=1,y=2}, p{x=3,y=4}]; out = arr.map(rule it.x)");

    [Test]
    public void StructAlias_InFilterLambda() =>
        Assert.AreEqual(2, Calc(
            "type p = {x:int}; arr = [p{x=1}, p{x=5}, p{x=10}]; out = arr.filter(rule it.x > 3).count()"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS IN FUNCTION SIGNATURES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void AliasAsArgType() =>
        Assert.AreEqual(43, Calc("type age = int; inc(x:age):int = x + 1; out = inc(42)"));

    [Test]
    public void AliasAsReturnType() =>
        Assert.AreEqual(42, Calc("type age = int; getAge():age = 42; out = getAge()"));

    [Test]
    public void AliasAsArgAndReturnType() =>
        Assert.AreEqual(43, Calc("type age = int; inc(x:age):age = x + 1; out = inc(42)"));

    [Test]
    public void StructAliasAsArgType() =>
        Assert.AreEqual(42, Calc("type p = {v:int}; getV(x:p):int = x.v; out = getV(p{v=42})"));

    [Test]
    public void ArrayAliasAsArgType() =>
        Assert.AreEqual(3, Calc("type nums = int[]; len(x:nums):int = x.count(); out = len([1,2,3])"));

    // ═══════════════════════════════════════════════════════════
    // TYPE COERCION WITH ALIAS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void IntAlias_WidensToReal() =>
        Assert.AreEqual(42.0, Calc("type n = int; x:n = 42; out:real = x"));

    [Test]
    public void IntAlias_InRealExpression() =>
        Assert.AreEqual(42.5, Calc("type n = int; x:n = 42; out = x + 0.5"));

    // ═══════════════════════════════════════════════════════════
    // OPTIONAL ALIAS OPERATIONS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void OptionalAlias_Coalesce() =>
        Assert.AreEqual(42, Calc("type m = int?; x:m = 42; out = x ?? 0"));

    [Test]
    public void OptionalAlias_CoalesceNone() =>
        Assert.AreEqual(0, Calc("type m = int?; x:m = none; out = x ?? 0"));

    [Test]
    public void OptionalStructAlias_SafeAccess() =>
        Assert.AreEqual(42, Calc("type p = {v:int}; x:p? = p{v=42}; out = x?.v ?? -1"));

    [Test]
    public void OptionalStructAlias_SafeAccessNone() =>
        Assert.AreEqual(-1, Calc("type p = {v:int}; x:p? = none; out = x?.v ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // MULTIPLE ALIASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TwoAliasesToSameType() =>
        Assert.AreEqual(3, Calc("type a = int; type b = int; x:a = 1; y:b = 2; out = x + y"));

    [Test]
    public void ManyAliasesInOneScript() =>
        Assert.AreEqual(42, Calc(
            "type age = int; type name = text; type flag = bool; type score = real; " +
            "a:age = 42; n:name = 'test'; f:flag = true; s:score = 1.0; " +
            "out = a"));

    // ═══════════════════════════════════════════════════════════
    // CASE INSENSITIVITY
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TypeName_CaseInsensitive() =>
        Assert.AreEqual(42, Calc("type Age = int; x:age = 42; out = x"));

    [Test]
    public void TypeName_MixedCase() =>
        Assert.AreEqual(42, Calc("type MyType = int; x:mytype = 42; out = x"));

    // ═══════════════════════════════════════════════════════════
    // STRUCT ALIAS VARIATIONS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void EmptyStruct() =>
        CalcNoThrow("type t = {}; out = t{}");

    [Test]
    public void SingleFieldStruct_Int() =>
        Assert.AreEqual(42, Calc("type t = {v:int}; out = t{v=42}.v"));

    [Test]
    public void SingleFieldStruct_Text() =>
        Assert.AreEqual("hello", Calc("type t = {s:text}; out = t{s='hello'}.s"));

    [Test]
    public void SingleFieldStruct_Array() =>
        Assert.AreEqual(3, Calc("type t = {items:int[]}; out = t{items=[1,2,3]}.items.count()"));

    [Test]
    public void SingleFieldStruct_Optional() =>
        Assert.AreEqual(42, Calc("type t = {v:int? = none}; out = t{v=42}.v ?? -1"));

    [Test]
    public void SingleFieldStruct_Bool() =>
        Assert.AreEqual(true, Calc("type t = {f:bool}; out = t{f=true}.f"));

    // ═══════════════════════════════════════════════════════════
    // ALIAS OF ALIAS OF STRUCT
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void AliasOfStruct_FieldAccess() =>
        Assert.AreEqual(42, Calc("type p = {v:int}; type q = p; x:q = p{v=42}; out = x.v"));

    [Test]
    public void AliasOfStruct_Array() =>
        Assert.AreEqual(2, Calc("type p = {v:int}; type ps = p[]; x:ps = [p{v=1}, p{v=2}]; out = x.count()"));

    // ═══════════════════════════════════════════════════════════
    // STRUCT WITH ALIAS FIELD TYPES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructField_AliasType() =>
        Assert.AreEqual(42, Calc("type age = int; type person = {a:age}; out = person{a=42}.a"));

    [Test]
    public void StructField_ArrayAliasType() =>
        Assert.AreEqual(3, Calc(
            "type nums = int[]; type container = {items:nums}; " +
            "out = container{items=[1,2,3]}.items.count()"));

    [Test]
    public void StructField_OptionalAliasType() =>
        Assert.AreEqual(42, Calc(
            "type age = int; type person = {a:age? = none}; " +
            "out = person{a=42}.a ?? -1"));

    [Test]
    public void StructField_StructAliasType() =>
        Assert.AreEqual(42, Calc(
            "type inner = {v:int}; type outer = {i:inner}; " +
            "out = outer{i=inner{v=42}}.i.v"));

    // ═══════════════════════════════════════════════════════════
    // FORWARD REFERENCE
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructField_ForwardRef_OptionalType() =>
        Assert.AreEqual(42, Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int}; " +
            "out = a{x=42}.x"));

    [Test]
    public void StructField_ForwardRef_Used() =>
        Assert.AreEqual(99, Calc(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int}; " +
            "v = a{x=1, b=b{y=99}}; " +
            "out = v.b?.y ?? -1"));

    // ═══════════════════════════════════════════════════════════
    // ARRAY LITERAL WITH ALIAS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ArrayLiteral_OfAlias() =>
        Assert.AreEqual(3, Calc("type p = {v:int}; out = [p{v=1}, p{v=2}, p{v=3}].count()"));

    [Test]
    public void ArrayLiteral_MapAlias() =>
        CalcNoThrow("type p = {v:int}; out = [p{v=10}, p{v=20}].map(rule it.v)");

    // ═══════════════════════════════════════════════════════════
    // DEFAULT VALUES — ALL TYPES
    // ═══════════════════════════════════════════════════════════

    [TestCase("type t = {v:int = 0}; out = t{}.v", 0)]
    [TestCase("type t = {v:int = -1}; out = t{}.v", -1)]
    [TestCase("type t = {v:int = 100}; out = t{}.v", 100)]
    [TestCase("type t = {v:real = 3.14}; out = t{}.v", 3.14)]
    [TestCase("type t = {v:bool = false}; out = t{}.v", false)]
    [TestCase("type t = {v:bool = true}; out = t{}.v", true)]
    [TestCase("type t = {v:text = 'default'}; out = t{}.v", "default")]
    public void DefaultValues_AllTypes(string expr, object expected) =>
        Assert.AreEqual(expected, Calc(expr));

    [Test]
    public void DefaultValue_EmptyArray() =>
        Assert.AreEqual(0, Calc("type t = {items:int[] = []}; out = t{}.items.count()"));

    [Test]
    public void DefaultValue_None() =>
        Assert.AreEqual(-1, Calc("type t = {v:int? = none}; out = t{}.v ?? -1"));

    [Test]
    public void DefaultValue_AllFieldsHaveDefaults() =>
        Assert.AreEqual(0, Calc("type t = {a:int = 0, b:text = '', c:bool = false}; out = t{}.a"));

    [Test]
    public void DefaultValue_Override() =>
        Assert.AreEqual(99, Calc("type t = {v:int = 0}; out = t{v=99}.v"));

    // ═══════════════════════════════════════════════════════════
    // MULTIPLE TYPE DECLARATIONS
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ThreeTypes_AllUsed() =>
        Assert.AreEqual(6, Calc(
            "type a = {x:int}; type b = {y:int}; type c = {z:int}; " +
            "out = a{x=1}.x + b{y=2}.y + c{z=3}.z"));

    [Test]
    public void TypesDeclaredBetweenEquations() =>
        Assert.AreEqual(42, Calc(
            "type t = {v:int}; x = t{v=42}; out = x.v"));

    // ═══════════════════════════════════════════════════════════
    // ERROR CASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void DuplicateTypeName_Throws() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Calc("type t = {x:int}; type t = {y:int}; out = 1"));

    [Test]
    public void UnknownFieldInConstructor_Throws() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Calc("type t = {x:int}; out = t{y=1}.x"));

    [Test]
    public void MissingRequiredField_Throws() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Calc("type t = {x:int}; out = t{}.x"));

    [Test]
    public void WrongFieldType_Throws() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            Calc("type t = {x:int}; out = t{x='text'}.x"));
}
