using System.Linq;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class SingleEquatationsTest
    {
        [TestCase("y = 2",2)]
        [TestCase("y = 2*3",6)]
        [TestCase("y = 4/2",2)]
        [TestCase("y = 4- 3",1)]
        [TestCase("y = 4+ 3",7)]
        [TestCase("z = 1 + 4/2 + 3 +2*3 -1", 11)]
        [TestCase("z = 1 + (1 + 4)/2 - (3 +2)*(3 -1)",-6.5)]
        [TestCase("y = 2 ",2)]
        [TestCase("y = 2  ",2)]
        [TestCase("y = -1",-1)]
        [TestCase("y = -1 ",-1)]
        [TestCase("y = -1+2",1)]
        [TestCase("y = -(1+2)",-3)]
        [TestCase("y = -2*-4",8)]
        [TestCase("y = -2*(-4+2)",4)]
        [TestCase("y = -(-(-1))",-1)]
        [TestCase("y = -(-1)",1)]
        public void Constant(string expr, double expected)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        
        [TestCase("y = x",2,2)]
        [TestCase("y = 2*x",3,6)]
        [TestCase("y = 4/x",2,2)]
        [TestCase("y = x/4",10,2.5)]
        [TestCase("y = 4- x",3,1)]
        [TestCase("y = x- x",3,0)]
        [TestCase("y = 4+ x",3,7)]
        [TestCase("z = (x + 4/x)",2,4)]
        [TestCase("y = -x ",0.3,-0.3)]
        [TestCase("y = -(-(-x))",2,-2)]
        public void SingleVariable(string expr, double arg, double expected)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            var res = runtime.Calculate(Variable.New("x",arg));
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        
        [TestCase("y = x1+x2",2,3,5)]
        [TestCase("y = 2*x1*x2",3,6, 36)]
        [TestCase("y = x1*4/x2",2,2,4)]
        [TestCase("y = (x1+x2)/4",2,2,1)]
        public void TwoVariables(string expr, double arg1, double arg2, double expected)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            var res = runtime.Calculate(
                Variable.New("x1",arg1),
                Variable.New("x2",arg2));

            Assert.AreEqual(expected, res.Results.First().Value);
        }
    }
}