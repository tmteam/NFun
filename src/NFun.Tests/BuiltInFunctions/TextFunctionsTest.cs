using NFun.Types;
using NUnit.Framework;

namespace NFun.Tests.BuiltInFunctions
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
        [TestCase("y = ' hi world '.trimStart().trimEnd().split(' ')", new[]{"hi","world"})]
        [TestCase("y = '  hi world  '.trimEnd()", "  hi world")]
        [TestCase("y = 'hi world'.trim()", "hi world")]

        public void ConstantEquationWithGenericPredefinedFunction(string expr, object expected)
        {
            var runtime = FunBuilder.Build(expr);
            runtime.Calculate()
                .AssertReturns(VarVal.New("y", expected));
        }
    }
}
