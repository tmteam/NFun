using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.BuiltInFunctions
{
    class TextFunctionsTest
    {
        [TestCase("y = ' hi world '.trim()", "hi world")]
        [TestCase("y = ' hi world'.trim()", "hi world")]
        [TestCase("y = 'hi world  '.trim()", "hi world")]
        [TestCase("y = '  hi world  '.trim()", "hi world")]
        [TestCase("y = 'hi world'.trim()", "hi world")]

        [TestCase("y = ' hi world '.trimStart()", "hi world ")]
        [TestCase("y = ' hi world'.trimStart()", "hi world")]
        [TestCase("y = 'hi world  '.trimStart()", "hi world  ")]
        [TestCase("y = '  hi world  '.trimStart()", "hi world  ")]
        [TestCase("y = 'hi world'.trim()", "hi world")]

        [TestCase("y = ' hi world '.trimEnd()", " hi world")]
        [TestCase("y = ' hi world'.trimEnd()", " hi world")]
        [TestCase("y = 'hi world  '.trimEnd()", "hi world")]
        [TestCase("y = '  hi world  '.trimEnd()", "  hi world")]
        [TestCase("y = 'hi world'.trim()", "hi world")]
        public void ConstantEquationWithGenericPredefinedFunction(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate()
                .AssertReturns(VarVal.New("y", expected));
        }
    }
}
