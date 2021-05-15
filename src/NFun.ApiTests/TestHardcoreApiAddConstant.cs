using System;
using NFun.Interpritation;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestHardcoreApiAddConstant
    {
       [Test]
        public void UseConstant_inputNotAppears()
        {
            var runtime = Funny.Hardcore
                .WithConstants(VarVal.New("pipi", Math.PI))
                .WithFunction(new LogFunction())
                .Build("y = pipi");
            Assert.AreEqual(0, runtime.Inputs.Length);

            var result = runtime.Calculate();
            Assert.AreEqual(Math.PI, result.GetValueOf("y"));
        }
        
        [Test]
        public void UseConstantWithOp()
        {
            var runtime = Funny.Hardcore
                .WithConstants(new VarVal("one", 1, VarType.Int32))
                .Build("y = -one");

            var result = runtime.Calculate();
            Assert.AreEqual(-1, result.GetValueOf("y"));
        }

        [Test]
        public void UseClrConstant_inputNotAppears()
        {
            var runtime = Funny.Hardcore
                .WithConstant("pipi", Math.PI)
                .WithFunction(new LogFunction())
                .Build("y = pipi");
            Assert.AreEqual(0, runtime.Inputs.Length);

            var result = runtime.Calculate();
            Assert.AreEqual(Math.PI, result.GetValueOf("y"));
        }
        
        [Test]
        public void UseTwoClrConstants()
        {
            var runtime = Funny.Hardcore
                .WithConstant("one", 1)
                .WithConstant("two", 2)
                .WithFunction(new LogFunction())
                .Build("y = one+two");
            Assert.AreEqual(0, runtime.Inputs.Length);
            Assert.AreEqual(3, runtime.Calculate().GetValueOf("y"));
        }

        [Test]
        public void UseTwoNfunConstants()
        {
            var runtime = Funny.Hardcore
                .WithConstants(new VarVal("one",1,VarType.Int32),new VarVal("two",2,VarType.Int32))
                .Build("y = one+two");
            Assert.AreEqual(0, runtime.Inputs.Length);
            Assert.AreEqual(3, runtime.Calculate().GetValueOf("y"));
        }

        [Test]
        public void OverrideConstantWithOutputVariable_constantNotUsed()
        {
            var runtime = Funny.Hardcore
                .WithConstant("pi", Math.PI)
                .WithFunction(new LogFunction())
                .Build("pi = 3; y = pi");
            
            Assert.AreEqual(0, runtime.Inputs.Length);

            var result = runtime.Calculate();
            Assert.AreEqual(3.0,  result.GetValueOf("y"));
        }

        [Test]
        public void OverrideConstantWithInputVariable_constantNotUsed()
        {
            var constants = new ConstantList();
            constants.AddConstant(VarVal.New("pi", Math.PI));
            var runtime = Funny.Hardcore
                .WithConstant("pi", Math.PI)
                .Build("pi:int; y = pi");
            Assert.AreEqual(1, runtime.Inputs.Length);

            var result = runtime.Calculate(VarVal.New("pi", 2));
            Assert.AreEqual(2, result.GetValueOf("y"));
        }   
    }
}