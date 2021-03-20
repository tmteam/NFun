using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class AprioriTypesTest
    {
        [Test]
        public void AprioriInputCalcs()
        {
            string	expression = "y = x";
            var runtime = FunBuilder
                .With(expression)
                .WithAprioriInput("x", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void AprioriOutputCalcs()
        {
            string	expression = "y = x";
            var runtime = FunBuilder
                .With(expression)
                .WithAprioriOutput("y", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");

        }
    }
}