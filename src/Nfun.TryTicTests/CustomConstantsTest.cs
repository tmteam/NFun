using System;
using NFun;
using NFun.Interpritation;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    [TestFixture]
    class CustomConstantsTest
    {
        [Test]
        public void UseConstant_inputNotAppears()
        {
            var constants = new ConstantList();
            constants.AddConstant(VarVal.New("pi", Math.PI));

            var runtime = FunBuilder
                .With("y = pi")
                .With(constants)
                .WithFunctions(new LogFunction()).Build();
            Assert.AreEqual(0, runtime.Inputs.Length);

            var result = runtime.Calculate();
            Assert.AreEqual(Math.PI, result.GetValueOf("y"));
        }
        
        [Test]
        public void UseConstant_WithDefaultBuiderConstantAppears()
        {
            var constants = new ConstantList();
            constants.AddConstant(VarVal.New("pi", Math.PI));

            var runtime = FunBuilder
                .With("y = pi")
                .With(constants)
                .Build();

            Assert.AreEqual(0, runtime.Inputs.Length);

            var result = runtime.Calculate();
            Assert.AreEqual(Math.PI, result.GetValueOf("y"));
        }

        [Test]
        public void OverrideConstantWithOutputVariable_constantNotUsed()
        {
            var constants = new ConstantList();
            constants.AddConstant(VarVal.New("pi", Math.PI));

            var runtime = FunBuilder
                .With("pi = 3; y = pi")
                .With(constants)
                .WithFunctions(new LogFunction()).Build();
            Assert.AreEqual(0, runtime.Inputs.Length);

            var result = runtime.Calculate();
            Assert.AreEqual(3.0,  result.GetValueOf("y"));
        }

        [Test]
        public void OverrideConstantWithInputVariable_constantNotUsed()
        {
            var constants = new ConstantList();
            constants.AddConstant(VarVal.New("pi", Math.PI));

            var runtime = FunBuilder
                .With("pi:int; y = pi")
                .With(constants)
                .WithFunctions(new LogFunction()).Build();
            Assert.AreEqual(1, runtime.Inputs.Length);

            var result = runtime.Calculate(VarVal.New("pi", 2));
            Assert.AreEqual(2, result.GetValueOf("y"));
        }
    }
}
