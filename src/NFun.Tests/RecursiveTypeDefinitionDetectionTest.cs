using NFun;
using NFun.Exceptions;
using NFun.ParseErrors;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class RecursiveTypeDefinitionDetectionTest
    {
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
        public void ObviouslyFailsWithRecursiveTypeDefinitionOfArray(string expr) =>
          Assert.Throws<FunParseException>(
              () => FunBuilder.Build(expr));

        [TestCase("g(f) = f(f)")]
        [TestCase("g(f) = f(f(f))")]
        [TestCase("g(f) = f(f(f(f)))")]
        [TestCase("g(f) = f(f[0])")]
        [TestCase("g(f) = f(f[0])")]
        [TestCase("g(f) = f(f())")]

        public void ObviouslyFailsWithRecursiveTypeDefinitionOfFunctionalVar(string expr) =>
            Assert.Throws<FunParseException>(
                () => FunBuilder.Build(expr));

    }
}
