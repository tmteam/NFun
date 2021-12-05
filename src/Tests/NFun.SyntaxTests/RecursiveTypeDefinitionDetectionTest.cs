using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests {

[TestFixture]
public class RecursiveTypeDefinitionDetectionTest {
    [TestCase("r(x) = r(x.i)")]
    [TestCase("r(x) = {f = r(x)}")]
    [TestCase("f(x) = x.age; y1 = f(user); y2 = f(user.child)")]
    public void ObviouslyFailsWithRecursiveTypeDefinitionOfStruct(string expr)
        => expr.AssertObviousFailsOnParse();

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
    public void ObviouslyFailsWithRecursiveTypeDefinitionOfArray(string expr) => expr.AssertObviousFailsOnParse();

    [TestCase("g(f) = f(f)")]
    [TestCase("g(f) = f(f(f))")]
    [TestCase("g(f) = f(f(f(f)))")]
    [TestCase("g(f) = f(f[0])")]
    [TestCase("g(f) = f(f[0])")]
    [TestCase("g(f) = f(f())")]
    public void ObviouslyFailsWithRecursiveTypeDefinitionOfFunctionalVar(string expr) =>
        expr.AssertObviousFailsOnParse();
}

}