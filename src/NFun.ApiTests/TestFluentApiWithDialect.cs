using System;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestFluentApiWithDialect {

        [Test]
        public void PreferedIntTypeIsI32()
        {
            var result = Funny
                .WithDialect(Dialects.ModifyClassic(integerPreferedType: IntegerPreferedType.I32))
                .ForCalc<ModelWithInt, Object>()
                .Calc("42", new ModelWithInt());
            Assert.IsInstanceOf<int>(result);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void PreferedIntTypeIsI64()
        {
            var result = Funny
                .WithDialect(Dialects.ModifyClassic(integerPreferedType: IntegerPreferedType.I64))
                .ForCalc<ModelWithInt, Object>()
                .Calc("42", new ModelWithInt());
            Assert.IsInstanceOf<long>(result);
            Assert.AreEqual(42L, result);
        }
        
        [Test]
        public void PreferedIntTypeIsReal()
        {
            var result = Funny
                .WithDialect(Dialects.ModifyClassic(integerPreferedType: IntegerPreferedType.Real))
                .ForCalc<ModelWithInt, Object>()
                .Calc("42", new ModelWithInt());
            Assert.IsInstanceOf<double>(result);
            Assert.AreEqual(42.0, result);
        }
        
        [Test]
        public void DenyIf_EquationWithIfThrows() =>
            Assert.Throws<FunParseException>(()=> Funny
                .WithDialect(Dialects.ModifyClassic(IfExpressionSetup.Deny))
                .ForCalc<ModelWithInt, Object>()
                .Calc("if(true) false else true", new ModelWithInt()));
        
        [Test]
        public void DenyIfIfElse_EquationWithIfIfElseThrows() =>
            Assert.Throws<FunParseException>(()=> Funny
                .WithDialect(Dialects.ModifyClassic(IfExpressionSetup.IfIfElse))
                .ForCalc<ModelWithInt, Object>()
                .Calc("if(true) false if(false) true else true", new ModelWithInt()));
        
        
        [TestCase(IfExpressionSetup.Deny)]
        [TestCase(IfExpressionSetup.IfElseIf)]
        [TestCase(IfExpressionSetup.IfIfElse)]
        public void EquationWithoutIfsCalculates(IfExpressionSetup setup) =>
            Assert.AreEqual(12, Funny
                .WithDialect(Dialects.ModifyClassic(setup, IntegerPreferedType.I32))
                .ForCalc<ModelWithInt, Object>()
                .Calc("12", new ModelWithInt()));
        
        [TestCase(IfExpressionSetup.IfElseIf)]
        [TestCase(IfExpressionSetup.IfIfElse)]
        public void EquationWithIfCalculates(IfExpressionSetup setup) =>
            Assert.AreEqual(false, Funny
                .WithDialect(Dialects.ModifyClassic(setup))
                .ForCalc<ModelWithInt, Object>()
                .Calc("if(true) false else true", new ModelWithInt()));
    }
}