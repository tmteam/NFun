using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// All impossible / ill-typed recursive type definitions — must be rejected at parse time.
/// Two flavors:
///   • TIC-inferred: an arbitrary expression that would force a self-referential type
///     (e.g. <c>r(x) = r(x.i)</c>, <c>t[t]</c>, <c>f(f)</c>) — default dialect.
///   • Named-type-declared: an explicit <c>type ...</c> declaration that is itself
///     infinite — non-optional self-reference, circular alias, non-contractive
///     F-bound, default value that loops forever — requires NamedTypes dialect.
/// Valid recursive forms (linked list, binary tree, array-of-self, optional self-ref,
/// indirect-via-optional, recursive function-type aliases, etc.) live in
/// <c>NamedTypes/RecursiveTypeTest.cs</c> alongside the rest of the happy-path
/// recursive-type coverage.
/// </summary>
[TestFixture]
public class ImpossibleRecursiveTypeDefinitionsTest {

    // ─── Inferred from expression — recursive struct shape ───
    [TestCase("r(x) = r(x.i)")]
    [TestCase("r(x) = {f = r(x)}")]
    public void Inferred_Struct_FailsOnParse(string expr) => expr.AssertObviousFailsOnParse();

    // ─── Inferred from expression — recursive array shape ───
    [TestCase("y = t.concat(t[0])")]
    [TestCase("y = t.concat(t[0][0])")]
    [TestCase("y = t.concat(t[0][0][0])")]
    [TestCase("y = t.concat(t[0][0][0][0])")]
    [TestCase("y = t[0].concat(t[0][0])")]
    [TestCase("y = t[0].concat(t[0][0][0])")]
    [TestCase("y = t[0].concat(t[0][0][0][0])")]
    [TestCase("y = t[0][0].concat(t[0][0][0])")]
    [TestCase("y = t[0][0].concat(t[0][0][0][0])")]
    [TestCase("y = t[0][0][0].concat(t[0][0][0][0])")]
    [TestCase("y = t[t])")]
    [TestCase("y = t[0][t])")]
    [TestCase("y = t[0][0][t])")]
    [TestCase("y = t[0][0][t[0]])")]
    [TestCase("y = t[0][0][0][t[0]])")]
    [TestCase("y = t[0][0][0][t[0][0]])")]
    [TestCase("y = if(t.count() < 2) t else t[1:].reverse().concat(t[0])")]
    [TestCase("f(t) = if(t.count() < 2) t else t[1:].reverse().concat(t[0])")]
    [TestCase("f(t) = t[1:].reverse().concat(t[0])")]
    [TestCase("f(t) = t.reverse().concat(t[0])")]
    [TestCase("f(t) = t.concat(t[0])")]
    [TestCase("f(t) = t.concat(t[0][0])")]
    [TestCase("f(t) = t.concat(t[0][0][0])")]
    [TestCase("f(t) = t.concat(t[0][0][0][0])")]
    [TestCase("f(t) = t[0].concat(t[0][0])")]
    [TestCase("f(t) = t[0].concat(t[0][0][0])")]
    [TestCase("f(t) = t[0].concat(t[0][0][0][0])")]
    [TestCase("f(t) = t[0][0].concat(t[0][0][0])")]
    [TestCase("f(t) = t[0][0].concat(t[0][0][0][0])")]
    [TestCase("f(t) = t[0][0][0].concat(t[0][0][0][0])")]
    [TestCase("f(t) = t[t]")]
    [TestCase("f(t) = t[0][t]")]
    [TestCase("f(t) = t[0][0][t]")]
    [TestCase("f(t) = t[0][0][t[0]]")]
    [TestCase("f(t) = t[0][0][0][t[0]]")]
    [TestCase("f(t) = t[0][0][0][t[0][0]]")]
    public void Inferred_Array_FailsOnParse(string expr) => expr.AssertObviousFailsOnParse();

    // ─── Inferred from expression — recursive functional value ───
    [TestCase("g(f) = f(f)")]
    [TestCase("g(f) = f(f(f))")]
    [TestCase("g(f) = f(f(f(f)))")]
    [TestCase("g(f) = f(f[0])")]
    [TestCase("g(f) = f(f())")]
    public void Inferred_FunctionalVar_FailsOnParse(string expr) => expr.AssertObviousFailsOnParse();

    /// <summary>
    /// Negative test for the inferred-recursion detector: same-name access on
    /// distinct values produces a finite (non-recursive) structural type, not a
    /// false-positive cycle. user and user.child both satisfy {age:_} independently.
    /// </summary>
    [Test]
    public void Inferred_SameNameAccessOnDistinctValues_NotFlaggedAsRecursive() {
        var runtime = Funny.Hardcore.Build("f(x) = x.age; y1 = f(user); y2 = f(user.child)");
        var userType = runtime["user"].Type;
        Assert.AreEqual(BaseFunnyType.Struct, userType.BaseType);
        Assert.IsTrue(userType.StructTypeSpecification.ContainsKey("age"));
        Assert.IsTrue(userType.StructTypeSpecification.ContainsKey("child"));
    }

    // ─── Declared `type ...` — non-optional direct self-reference (infinite size) ───
    [TestCase("type t = {self:t}")]
    [TestCase("type node = {v:int, next:node}")]
    [TestCase("type t = {left:t, right:t}")]
    public void Declared_NonOptDirectSelfRef_FailsOnParse(string expr) =>
        Assert.Throws<FunnyParseException>(() => BuildWithDialect(expr));

    // ─── Declared `type ...` — non-optional indirect self-reference (mutual & longer cycles) ───
    [TestCase("type a = {b:b}; type b = {a:a}")]
    [TestCase("type a = {b:b}; type b = {c:c}; type c = {a:a}")]
    [TestCase("type a = {b:b}; type b = {c:c}; type c = {d:d}; type d = {a:a}")]
    [TestCase("type a = {x:int, b:b}; type b = {y:text, a:a}")]
    public void Declared_NonOptIndirectSelfRef_FailsOnParse(string expr) =>
        Assert.Throws<FunnyParseException>(() => BuildWithDialect(expr));

    // ─── Declared `type ... = ...` — circular alias chains ───
    [TestCase("type a = a\r out = 1")]
    [TestCase("type a = b\r type b = a\r out = 1")]
    [TestCase("type a = b\r type b = c\r type c = a\r out = 1")]
    public void Declared_CircularAlias_FailsOnParse(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.BuildWithNamedTypes());

    /// <summary>
    /// A valid recursive type whose default value would invoke its own constructor —
    /// the default expression itself is the recursion, not the type shape.
    /// </summary>
    [Test]
    public void Declared_RecursiveDefaultConstructor_FailsOnParse() =>
        Assert.Throws<FunnyParseException>(() =>
            "type node = {v:int, next:node? = node{v=0}}; n = node{v=1}; out = n.v"
                .CalcWithDialect(
                    optionalTypesSupport: OptionalTypesSupport.Enabled,
                    namedTypesSupport: NamedTypesSupport.Enabled));

    /// <summary>
    /// Non-contractive F-bound: self appears in negative (argument) position of a
    /// non-optional function field. The cycle has no Optional / Array indirection
    /// to break it, so the bound is structurally bottomless and must be rejected.
    /// </summary>
    [Test]
    public void Declared_NonContractiveFBound_FailsOnParse() =>
        Assert.Throws<FunnyParseException>(() =>
            ("type fnode = {f: rule(fnode):int}\r " +
             "selfFunc(n) = n.f(n)\r " +
             "y = selfFunc(fnode{f = rule it.f(it)})")
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled));

    private static void BuildWithDialect(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled);
}
