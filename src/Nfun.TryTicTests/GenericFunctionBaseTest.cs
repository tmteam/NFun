using System;
using System.Linq;
using NFun.Functions;
using NFun.Interpretation.Functions;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests
{
    [TestFixture]
    public class GenericFunctionBaseTest
    {
        [Test]
        public void ReturnSelfFunction_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new ReturnSelfGenericFunctionDefinition();
            var function = rpt.CreateConcreteOrNull(FunnyType.Bool,FunnyType.Bool);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(FunnyType.Bool, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{FunnyType.Bool}, function.ArgTypes);
            });
        }
        
        [Test]
        public void ReturnSelfFunction_ArrayType_ResultTypesAreCorrect()
        {
            var arrayOfBool = FunnyType.ArrayOf(FunnyType.Bool);
            var rpt = new ReturnSelfGenericFunctionDefinition();
            var function = rpt.CreateConcreteOrNull(arrayOfBool,arrayOfBool);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(arrayOfBool, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{arrayOfBool}, function.ArgTypes);
            });
        }
        
        [Test]
        public void Repeat_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new RepeatGenericFunctionDefinition();
            var function = rpt.CreateConcreteOrNull(FunnyType.ArrayOf(FunnyType.Bool), FunnyType.Bool, FunnyType.Int32);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(FunnyType.ArrayOf(FunnyType.Bool), function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{FunnyType.Bool, FunnyType.Int32}, function.ArgTypes);
            });
        }
        
        [Test]
        public void Map_FunReturnsText_ResultTypesAreCorrect()
        {
            var function = new MapFunction()
                .CreateConcreteOrNull(
                    FunnyType.ArrayOf(FunnyType.Text), 
                    FunnyType.ArrayOf(FunnyType.Int32), 
                     FunnyType.Fun(FunnyType.Text, FunnyType.Int32));

            Assert.IsNotNull(function);
            
            Assert.Multiple(()=>{
                Assert.AreEqual(
                    expected: FunnyType.ArrayOf(FunnyType.Text),
                    actual: function.ReturnType);
                CollectionAssert.AreEqual(
                    expected: new[]
                    {
                        FunnyType.ArrayOf(FunnyType.Int32), 
                        FunnyType.Fun(FunnyType.Text, FunnyType.Int32)
                    },
                    actual: function.ArgTypes);
            });
        }    
        
        [Test]
        public void Take_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new TakeGenericFunctionDefinition();
            var function = rpt.CreateConcreteOrNull(
                 FunnyType.ArrayOf(FunnyType.Bool),
                 FunnyType.ArrayOf(FunnyType.Bool), 
                 FunnyType.Int32);
            Assert.IsNotNull(function);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(FunnyType.ArrayOf(FunnyType.Bool), function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{FunnyType.ArrayOf(FunnyType.Bool), FunnyType.Int32}, function.ArgTypes);
            });
        }
        
        [Test]
        public void Take_ArrayType_ResultTypesAreCorrect()
        {
            var rpt = new TakeGenericFunctionDefinition();
            var arrayOfBool = FunnyType.ArrayOf(FunnyType.Bool);
            var function = rpt.CreateConcreteOrNull(
                FunnyType.ArrayOf(arrayOfBool),
                FunnyType.ArrayOf(arrayOfBool), 
                FunnyType.Int32);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(FunnyType.ArrayOf(arrayOfBool), function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{FunnyType.ArrayOf(arrayOfBool), FunnyType.Int32}, function.ArgTypes);
            });
        }
        [TestCase(BaseFunnyType.Real,BaseFunnyType.Int32,BaseFunnyType.Int32)]
        [TestCase(BaseFunnyType.Real,BaseFunnyType.Real,BaseFunnyType.Int32)]
        [TestCase(BaseFunnyType.Real,BaseFunnyType.Int32,BaseFunnyType.Real)]

        [TestCase(BaseFunnyType.Int32,BaseFunnyType.Int32,BaseFunnyType.Int32)]
        [TestCase(BaseFunnyType.Int64,BaseFunnyType.Int32,BaseFunnyType.Int32)]
        [TestCase(BaseFunnyType.Any,  BaseFunnyType.Int32,BaseFunnyType.Int32)]
        public void GetRnd_GenericEqualsOutputType(
            BaseFunnyType returnType, BaseFunnyType firstArg, BaseFunnyType secondArg)
        {
            var rpt = new GetRandomElementFuncDefinition();
            var function = rpt.CreateConcreteOrNull(
                FunnyType.PrimitiveOf(returnType),
                FunnyType.PrimitiveOf(firstArg), 
                FunnyType.PrimitiveOf(secondArg));
            var generic = FunnyType.PrimitiveOf(returnType);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(generic, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{generic, generic}, function.ArgTypes);
            });
        }
        [TestCase(BaseFunnyType.Int32,BaseFunnyType.Real,BaseFunnyType.Int32)]
        [TestCase(BaseFunnyType.Int32,BaseFunnyType.Int32,BaseFunnyType.Real)]
        [TestCase(BaseFunnyType.Int64,BaseFunnyType.Int32,BaseFunnyType.Real)]
        [TestCase(BaseFunnyType.Real,BaseFunnyType.Any,BaseFunnyType.Real)]
        public void GetRnd_ArgAreIncostistent_ReturnsNull(
            BaseFunnyType returnType, BaseFunnyType firstArg, BaseFunnyType secondArg)
        {
            var rpt = new GetRandomElementFuncDefinition();
            var function = rpt.CreateConcreteOrNull(
                FunnyType.PrimitiveOf(returnType),
                FunnyType.PrimitiveOf(firstArg), 
                FunnyType.PrimitiveOf(secondArg));
            Assert.IsNull(function);
        }
        
        [Test]
        public void GetRnd_RealInt_Real_GenericEqualsReal()
        {
            var rpt = new GetRandomElementFuncDefinition();
            var function = rpt.CreateConcreteOrNull(
                FunnyType.Real,
                FunnyType.Int32, 
                FunnyType.Int32);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(FunnyType.Real, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{FunnyType.Real, FunnyType.Real}, function.ArgTypes);
            });
        }
    }

    public class GetRandomElementFuncDefinition : GenericFunctionBase
    {
        public GetRandomElementFuncDefinition() : base("__retSelf",
            FunnyType.Generic(0), FunnyType.Generic(0),FunnyType.Generic(0))
        {
        }

        protected override object Calc(object[] args) => throw new NotImplementedException();
    }
    public class ReturnSelfGenericFunctionDefinition : GenericFunctionBase
    {
        public ReturnSelfGenericFunctionDefinition() : base("__retSelf", FunnyType.Generic(0), FunnyType.Generic(0))
        {
        }

        protected override object Calc(object[] args) => args.First();
    }
}