using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    class NfunUpdateTest
    {
        [TestCase("y = 2*x", 3, 6)]
        [TestCase("y = 2*x", 3.5, 7.0)]
        [TestCase("y = 4/x", 2, 2)]
        [TestCase("y = x/4", 10, 2.5)]
        [TestCase("y = 4- x", 3, 1)]
        [TestCase("y = x- x", 3, 0)]
        [TestCase("y = 4+ x", 3, 7)]
        [TestCase("y = (x + 4/x)", 2, 4)]
        [TestCase("y = x**3", 2, 8)]
        [TestCase("y = x%3", 2, 2)]
        [TestCase("y = x%4", 5, 1)]
        [TestCase("y = x%-4", 5, 1)]
        [TestCase("y = x%4", -5, -1)]
        [TestCase("y = x%-4", -5, -1)]
        [TestCase("y = x%4", -5, -1)]
        [TestCase("y = -x ", 0.3, -0.3)]
        [TestCase("y = -(-(-x))", 2, -2)]
        [TestCase("y = x/0.2", 1, 5)]

        public void SingleVariableEquation(string expr, double arg, double expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var ySource = runtime.GetAllVariableSources().First(vs => vs.IsOutput && vs.Name == "y");
            var xSource = runtime.GetAllVariableSources().First(vs => !vs.IsOutput && vs.Name == "x");
            xSource.Value = arg;
            runtime.Update();
            Assert.AreEqual(expected, ySource.Value);
        }
    }
}
