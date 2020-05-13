using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NFun.ParseErrors;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class RecursiveTypeDefenitionDetectionTest
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
        public void ObviouslyFailsWithRecursiveTypeDefenitionOfArray(string expr) =>
          Assert.Throws<FunParseException>(
              () => FunBuilder.BuildDefault(expr));

        [TestCase("g(f) = f(f)")]
        [TestCase("g(f) = f(f(f))")]
        [TestCase("g(f) = f(f(f(f)))")]
        public void ObviouslyFailsWithRecursiveTypeDefenitionOfFunctionalVar(string expr) =>
            Assert.Throws<FunParseException>(
                () => FunBuilder.BuildDefault(expr));

    }
}
