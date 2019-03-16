using System.Linq;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ArraysTest
    {
        [TestCase("y = [1.0,1.2,2.4]", new[]{1.0,1.2, 2.4})]
        [TestCase("y = [1.0]", new[]{1.0})]
        [TestCase("y = []", new double[0])]
        [TestCase("y = [1.0,2.0]::[3.0,4.0]", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = ([1.0]::[2.0])::[3.0,4.0]", new []{1.0,2.0,3.0,4.0})]
        
        [TestCase("y = [1.0]==[]", false)]
        [TestCase("y = [1.0]==[2.0]", false)]
        [TestCase("y = []==[]", true)]
        [TestCase("y = [1.0]==[1.0]", true)]
        [TestCase("y = [1.0,2.0]==[1.0,2.0]", true)]
        [TestCase("y = [1.0,2.0]==([1.0]::[2.0])", true)]

        public void ConstantArrayTest(string expr, object expected)
        {
            Interpreter.BuildOrThrow(expr).Calculate().AssertReturns(Var.New("y", expected));
        }
        [TestCase("y = [1.0,a,b] a = 2.0 \r b=3.0 \r ", new[]{1.0,2.0,3.0})]
        [TestCase("y = [a,b] a = 2.0 \r b=3.0 \r ", new[]{2.0,3.0})]
        [TestCase("y = [a+1,b+2] a = 2.0 \r b=3.0 \r ", new[]{3.0,5.0})]
        [TestCase("y = [a*0,b*0] a = 2.0 \r b=3.0 \r ", new[]{0.0,0.0})]

        public void ConstantCalculableArrayTest(string expr, object expected)
        {
            Interpreter.BuildOrThrow(expr).Calculate().AssertHas(Var.New("y", expected));
        }
        
        [TestCase("y = [")]
        [TestCase("y = [1.0::")]
        [TestCase("y = [1.0::]")]
        [TestCase("y = [1.0]::]")]
        [TestCase("y = [1.0]::[")]
        [TestCase("y = [1.0]::")]
        [TestCase("y = [1.0]::::[2.0]")]
        [TestCase("y = ::[2.0]")]
        [TestCase("y = [2.0 3.0]")]
        [TestCase("y = [2.0,,3.0]")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
    }
}