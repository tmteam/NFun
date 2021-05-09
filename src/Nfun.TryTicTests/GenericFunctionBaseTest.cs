using System;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation.Functions;
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
            var function = rpt.CreateConcreteOrNull(VarType.Bool,VarType.Bool);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.Bool, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{VarType.Bool}, function.ArgTypes);
            });
        }
        
        [Test]
        public void ReturnSelfFunction_ArrayType_ResultTypesAreCorrect()
        {
            var arrayOfBool = VarType.ArrayOf(VarType.Bool);
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
            var function = rpt.CreateConcreteOrNull(VarType.ArrayOf(VarType.Bool), VarType.Bool, VarType.Int32);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.ArrayOf(VarType.Bool), function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{VarType.Bool, VarType.Int32}, function.ArgTypes);
            });
        }
        
        [Test]
        public void Map_FunReturnsText_ResultTypesAreCorrect()
        {
            var function = new MapFunction()
                .CreateConcreteOrNull(
                    VarType.ArrayOf(VarType.Text), 
                    VarType.ArrayOf(VarType.Int32), 
                     VarType.Fun(VarType.Text, VarType.Int32));

            Assert.IsNotNull(function);
            
            Assert.Multiple(()=>{
                Assert.AreEqual(
                    expected: VarType.ArrayOf(VarType.Text),
                    actual: function.ReturnType);
                CollectionAssert.AreEqual(
                    expected: new[]
                    {
                        VarType.ArrayOf(VarType.Int32), 
                        VarType.Fun(VarType.Text, VarType.Int32)
                    },
                    actual: function.ArgTypes);
            });
        }    
        
        [Test]
        public void Take_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new TakeGenericFunctionDefinition();
            var function = rpt.CreateConcreteOrNull(
                 VarType.ArrayOf(VarType.Bool),
                 VarType.ArrayOf(VarType.Bool), 
                 VarType.Int32);
            Assert.IsNotNull(function);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.ArrayOf(VarType.Bool), function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{VarType.ArrayOf(VarType.Bool), VarType.Int32}, function.ArgTypes);
            });
        }
        
        [Test]
        public void Take_ArrayType_ResultTypesAreCorrect()
        {
            var rpt = new TakeGenericFunctionDefinition();
            var arrayOfBool = VarType.ArrayOf(VarType.Bool);
            var function = rpt.CreateConcreteOrNull(
                VarType.ArrayOf(arrayOfBool),
                VarType.ArrayOf(arrayOfBool), 
                VarType.Int32);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.ArrayOf(arrayOfBool), function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{VarType.ArrayOf(arrayOfBool), VarType.Int32}, function.ArgTypes);
            });
        }
        [TestCase(BaseVarType.Real,BaseVarType.Int32,BaseVarType.Int32)]
        [TestCase(BaseVarType.Real,BaseVarType.Real,BaseVarType.Int32)]
        [TestCase(BaseVarType.Real,BaseVarType.Int32,BaseVarType.Real)]

        [TestCase(BaseVarType.Int32,BaseVarType.Int32,BaseVarType.Int32)]
        [TestCase(BaseVarType.Int64,BaseVarType.Int32,BaseVarType.Int32)]
        [TestCase(BaseVarType.Any,  BaseVarType.Int32,BaseVarType.Int32)]
        public void GetRnd_GenericEqualsOutputType(
            BaseVarType returnType, BaseVarType firstArg, BaseVarType secondArg)
        {
            var rpt = new GetRandomElementFuncDefinition();
            var function = rpt.CreateConcreteOrNull(
                VarType.PrimitiveOf(returnType),
                VarType.PrimitiveOf(firstArg), 
                VarType.PrimitiveOf(secondArg));
            var generic = VarType.PrimitiveOf(returnType);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(generic, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{generic, generic}, function.ArgTypes);
            });
        }
        [TestCase(BaseVarType.Int32,BaseVarType.Real,BaseVarType.Int32)]
        [TestCase(BaseVarType.Int32,BaseVarType.Int32,BaseVarType.Real)]
        [TestCase(BaseVarType.Int64,BaseVarType.Int32,BaseVarType.Real)]
        [TestCase(BaseVarType.Real,BaseVarType.Any,BaseVarType.Real)]
        public void GetRnd_ArgAreIncostistent_ReturnsNull(
            BaseVarType returnType, BaseVarType firstArg, BaseVarType secondArg)
        {
            var rpt = new GetRandomElementFuncDefinition();
            var function = rpt.CreateConcreteOrNull(
                VarType.PrimitiveOf(returnType),
                VarType.PrimitiveOf(firstArg), 
                VarType.PrimitiveOf(secondArg));
            Assert.IsNull(function);
        }
        
        [Test]
        public void GetRnd_RealInt_Real_GenericEqualsReal()
        {
            var rpt = new GetRandomElementFuncDefinition();
            var function = rpt.CreateConcreteOrNull(
                VarType.Real,
                VarType.Int32, 
                VarType.Int32);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.Real, function.ReturnType);
                CollectionAssert.AreEquivalent(new[]{VarType.Real, VarType.Real}, function.ArgTypes);
            });
        }
    }

    public class GetRandomElementFuncDefinition : GenericFunctionBase
    {
        public GetRandomElementFuncDefinition() : base("__retSelf",
            VarType.Generic(0), VarType.Generic(0),VarType.Generic(0))
        {
        }

        protected override object Calc(object[] args) => throw new NotImplementedException();
    }
    public class ReturnSelfGenericFunctionDefinition : GenericFunctionBase
    {
        public ReturnSelfGenericFunctionDefinition() : base("__retSelf", VarType.Generic(0), VarType.Generic(0))
        {
        }

        protected override object Calc(object[] args) => args.First();
    }
}