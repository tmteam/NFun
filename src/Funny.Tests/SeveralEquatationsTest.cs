using System.Linq;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class SeveralEquatationsTest
    {
        [TestCase("y = 2\r z=3",2,3)]
        [TestCase("y = 2*3\r z= 1 + (1 + 4)/2 - (3 +2)*(3 -1)",6,-6.5)]
        public void TwinConstantEquatations(string expr, double expectedY, double expectedZ)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(2, res.Results.Length);
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", res.Results[0].Name);
                Assert.AreEqual(expectedY, res.Results[0].Value);

                Assert.AreEqual("z", res.Results[1].Name);
                Assert.AreEqual(expectedZ, res.Results[1].Value);
            });
        }
        
        
        
        [TestCase("y = x\r z=3",2, 2,3)]
        [TestCase("y = x/2\r z=2*x",2, 1,4)]
        public void TwinEquatationsWithSingleVariable(string expr, double x, double expectedY, double expectedZ)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            var res = runtime.Calculate(Variable.New("x", x));
            Assert.AreEqual(2, res.Results.Length);
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", res.Results[0].Name);
                Assert.AreEqual(expectedY, res.Results[0].Value);

                Assert.AreEqual("z", res.Results[1].Name);
                Assert.AreEqual(expectedZ, res.Results[1].Value);

            });        
        }
        
        [TestCase("y = 1\r z=2", new string[0])]        
        [TestCase("y = x\r z=x", new []{"x"})]
        [TestCase("y = x/2\r z=2*x",new []{"x"})]
        [TestCase("y = in1/2\r z=2+in2",new []{"in1","in2"})]
        [TestCase("y = in1/2 + in2\r z=2 + in2",new []{"in1","in2"})]
        public void TwinEquatations_inputVarablesListIsCorrect(string expr, string[] inputNames)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            CollectionAssert.AreEquivalent(inputNames, runtime.Variables);
        }

        
        [TestCase("y = 1\r z=y", new string[0])]        
        [TestCase("y = x\r z=y", new []{"x"})]
        [TestCase("y = x/2\r z=2*y",new []{"x"})]
        [TestCase("y = x/2\r z=2*y+x",new []{"x"})]
        [TestCase("y = in1/2\r z=2*y+in2",new []{"in1","in2"})]
        [TestCase("y = in1/2 + in2\r z=2*y + in2",new []{"in1","in2"})]
        public void TwinDependentEquatations_inputVarablesListIsCorrect(string expr, string[] inputNames)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            CollectionAssert.AreEquivalent(inputNames, runtime.Variables);
        }
        
        [TestCase("y = x\r z=y",2, 2, 2)]
        [TestCase("y = x/2\r z=2*y",2, 1,2)]
        [TestCase("y = x/2\r z=2*y+x",2, 1,4)]
        public void TwinDependentEquatationsWithSingleVariable_CalculatesCorrect(string expr, double x, double expectedY, double expectedZ)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            var res = runtime.Calculate(Variable.New("x", x));
            Assert.AreEqual(2, res.Results.Length);
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", res.Results[0].Name);
                Assert.AreEqual(expectedY, res.Results[0].Value);

                Assert.AreEqual("z", res.Results[1].Name);
                Assert.AreEqual(expectedZ, res.Results[1].Value);
            });        
        }

    }
}