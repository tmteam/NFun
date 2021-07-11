using System;
using System.Linq;
using NFun.Interpretation;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestHardcoreApiAddConstant
    {
        [Test]
        public void UseConstant_inputNotAppears()
        {
            var runtime = Funny.Hardcore
                .WithConstants(("pipi", Math.PI))
                .WithFunction(new LogFunction())
                .Build("y = pipi");
            Assert.IsTrue(runtime.Variables.All(v=>v.IsOutput));
            runtime.Calc().AssertReturns("y", Math.PI);
        }

        [Test]
        public void UseConstantWithOp() =>
            Funny
                .Hardcore
                .WithConstants(("one", 1))
                .Build("y = -one")
                .Calc()
                .AssertReturns("y", -1);

        [Test]
        public void UseClrConstant_inputNotAppears()
        {
            var runtime = Funny.Hardcore
                .WithConstant("pipi", Math.PI)
                .WithFunction(new LogFunction())
                .Build("y = pipi");
            runtime.AssertInputsCount(0);

            runtime.Calc().AssertReturns("y", Math.PI);
        }

        [Test]
        public void UseTwoClrConstants()
        {
            var runtime = Funny.Hardcore
                .WithConstant("one", 1)
                .WithConstant("two", 2)
                .WithFunction(new LogFunction())
                .Build("y = one+two");
            runtime.AssertInputsCount(0);
            runtime.Calc().AssertReturns("y", 3);
        }

        [Test]
        public void UseTwoNfunConstants()
        {
            var runtime = Funny.Hardcore
                .WithConstants(("one",1),("two",2))
                .Build("y = one+two");
            runtime.AssertInputsCount(0);
            runtime.Calc().AssertReturns("y",3);
        }

        [Test]
        public void OverrideConstantWithOutputVariable_constantNotUsed()
        {
            var runtime = Funny.Hardcore
                .WithConstant("pi", Math.PI)
                .WithFunction(new LogFunction())
                .Build("pi = 3; y = pi");

            runtime.AssertInputsCount(0);
            runtime.Calc().AssertReturns(("y", 3.0),("pi",3.0));
        }

        [Test]
        public void OverrideConstantWithInputVariable_constantNotUsed()
        {
            var constants = new ConstantList();
            constants.AddConstant("pi", Math.PI);
            var runtime = Funny.Hardcore
                .WithConstant("pi", Math.PI)
                .Build("pi:int; y = pi");
            runtime.AssertInputsCount(1);
            runtime.Calc("pi", 2).AssertReturns("y",2);
        }   
    }
}