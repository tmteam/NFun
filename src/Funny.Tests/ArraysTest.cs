using System.Linq;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ArraysTest
    {
        [TestCase("y = [1.0,1.2,2.3]", new[]{1.0,1.2,2,3})]
        [TestCase("y = [1.0]", new[]{1.0})]
        [TestCase("y = []", new double[0])]
        [TestCase("y = [1.0,2.0]::[3.0,4.0]", new []{1.0,2.0,3.0,4.0})]
        public void ConstantArrayTest(string expr, double[] expected)
        {
            Interpreter.BuildOrThrow(expr).Calculate().AssertReturns(Var.New("y", expected));
        }
    }
}