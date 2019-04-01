using NFun;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class SeveralEquationsTest
    {
        [TestCase("y = 1.0\r z = true", 1.0, true)]
        [TestCase("y = 2\r z=3",2,3)]
        [TestCase("y = 2*3\r z= 1 + (1 + 4)/2 - (3 +2)*(3 -1)",6,-6.5)]
        public void TwinConstantEquations(string expr, object expectedY, object expectedZ)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(
                    Var.New("y", expectedY),
                    Var.New("z", expectedZ));
        }
        
        [TestCase("y = x*0.5\r z=3",2, 1.0,3)]
        [TestCase("y = x/2\r z=2*x",2, 1.0,4.0)]
        public void TwinEquationsWithSingleVariable(string expr, double x, object expectedY, object expectedZ)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x", x))
                .AssertReturns(
                    Var.New("y", expectedY),
                    Var.New("z", expectedZ));        
        }
        
        [TestCase("y = 1\r z=2", new string[0])]        
        [TestCase("y = x\r z=x", new []{"x"})]
        [TestCase("y = x/2\r z=2*x",new []{"x"})]
        [TestCase("y = in1/2\r z=2+in2",new []{"in1","in2"})]
        [TestCase("y = in1/2 + in2\r z=2 + in2",new []{"in1","in2"})]
        public void TwinEquations_inputVarablesListIsCorrect(string expr, string[] inputNames)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            CollectionAssert.AreEquivalent(inputNames, runtime.Variables);
        }
        
        [TestCase("y = 1\r z=y", new string[0])]        
        [TestCase("y = x\r z=y", new []{"x"})]
        [TestCase("y = x/2\r z=2*y",new []{"x"})]
        [TestCase("y = x/2\r z=2*y+x",new []{"x"})]
        [TestCase("y = in1/2\r z=2*y+in2",new []{"in1","in2"})]
        [TestCase("y = in1/2 + in2\r z=2*y + in2",new []{"in1","in2"})]
        public void TwinDependentEquations_inputVarsListIsCorrect(string expr, string[] inputNames)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            CollectionAssert.AreEquivalent(inputNames, runtime.Variables);
        }

        [TestCase("y = z\r z=1.0",       1.0,1.0)]
        [TestCase("y = 1.0\r z=y",       1.0,1.0)]
        [TestCase("y = 1\r z=y/2",       1,0.5)]
        [TestCase("y = true\r z=y",       true,true)]
        [TestCase("y = z\r z=true",       true,true)]
        [TestCase("y = z\r z=1",         1,1)]
        [TestCase("y = 1\r z=y",         1,1)]
        [TestCase("y = 1\r z=y*2",       1,2)]
        public void TwinDependentConstantEquations_CalculatesCorrect(string expr,  object expectedY, object expectedZ)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(
                    Var.New("y", expectedY),
                    Var.New("z", expectedZ));
        }

        
        [TestCase(2, "y = x\r z=y",         2,2)]
        [TestCase(2, "y = x/2\r z=2*y",     1,2)]
        [TestCase(2, "y = x/2\r z=2*y+x",   1,4)]
        public void TwinDependentEquationsWithSingleVariable_CalculatesCorrect(double x, string expr,  double expectedY, double expectedZ)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x", x))
                .AssertReturns(
                    Var.New("y", expectedY),
                    Var.New("z", expectedZ));
        }
        
        [TestCase("o1 = 1\r o2=o1\r o3 = 0", 1, 1, 0)]
        [TestCase("o1 = 1\r o2 = o1+1\r o3=2*o1*o2",1, 2, 4)]
        [TestCase("o1 = 1\r o2 = o3\n o3 = 2.0",1, 2.0, 2.0)]
        [TestCase("o1 = o2*2\r o2 = o3*2\n o3 = 2",8, 4, 2)]
        public void ThreeDependentConstantEquations_CalculatesCorrect(string expr,  object o1, object o2, object o3)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(
                    Var.New("o1", o1),
                    Var.New("o2", o2),
                    Var.New("o3", o3));
        }
        
        [TestCase(2,"o1 = x\r o2=o1\r o3 = 0", 2.0, 2.0, 0)]
        [TestCase(2,"o1 = x/2\r o2 = o1+1\r o3=2*o1*o2",1.0, 2.0, 4.0)]
        [TestCase(2,"o1 = x/2\r o2 = o3\n o3 = x",1.0, 2.0, 2.0)]
        public void ThreeDependentEquationsWithSingleVariable_CalculatesCorrect(double x,string expr,  object o1, object o2, object o3)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x", x))
                .AssertReturns(
                    Var.New("o1", o1),
                    Var.New("o2", o2),
                    Var.New("o3", o3));
        }
        
        [Test]
        public void ComplexDependentConstantsEquations_CalculatesCorrect()
        {
            var runtime = Interpreter.BuildOrThrow(
                @"o1 = 1
                  o2 = o1*2
                  o3 = o2*2
                  o4 = o1/2
                  o5 = 0
                  o6 = o4+o3
                  o7 = true
                  o8 = o7 xor true");
            
            runtime.Calculate()
                .AssertReturns(
                    Var.New("o1", 1),
                    Var.New("o2", 2),
                    Var.New("o3", 4),
                    Var.New("o4", 0.5),
                    Var.New("o5", 0),
                    Var.New("o6", 4.5),
                    Var.New("o7", true),
                    Var.New("o8", false)
                    );
        }
        
        [TestCase("o1 = o2\r o2=o1")]
        [TestCase("o1 = o3\r o2 = o1\r o3 = o2")]
        [TestCase("o0 = 3\r o1 = o3+o0\r o2 = o1\r o3 = o2")]
        public void ObviouslyFails(string expr)
        {
            Assert.Throws<FunParseException>(()=> Interpreter.BuildOrThrow(expr));
        }
       

    }
}