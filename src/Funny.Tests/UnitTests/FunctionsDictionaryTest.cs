using System.Collections.Generic;
using Funny.BuiltInFunctions;
using Funny.Interpritation;
using Funny.Interpritation.Functions;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class FunctionsDictionaryTest
    {
        private FunctionsDictionary dic;
        private const string maxId = "max";

        [SetUp]
        public void Setup()
        {
            dic = new FunctionsDictionary();
        }

        [Test]
        public void HasOneArglessFun_GetOrNull_returnsOne()
        {
            dic.Add(new EFunction());
            var fun = dic.GetOrNull(new EFunction().Name, new VarType[0]);
            Assert.IsInstanceOf<EFunction>(fun);
        }
        
        [Test]
        public void Empty_GetOrNull_returnsNull()
        {
            var fun = dic.GetOrNull(new EFunction().Name, new VarType[0]);
            Assert.IsNull(fun);
        }

        [Test]
        public void HasMaxOfIntFun_GetOrNullWithCorrectArgs_returnsOne()
        {
            dic.Add(new MaxOfIntFunction());
            var fun = dic.GetOrNull(new MaxOfIntFunction().Name, new[] {VarType.Int, VarType.Int});
            Assert.IsInstanceOf<MaxOfIntFunction>(fun);
        }


        [Test]
        public void HasAllMaxFuns_GetOrNullWithIntArgs_returnsOne()
        {
            AddAllMaxFuns();
            var maxIntInt = dic.GetOrNull(maxId, VarType.Int, VarType.Int);
            Assert.IsInstanceOf<MaxOfIntFunction>(maxIntInt, "Max int->int->int not found");
        }

        [Test]
        public void HasAllMaxFuns_GetOrNullWithRealArgs_returnsOne()
        {
            AddAllMaxFuns();
            var maxRealReal = dic.GetOrNull(maxId, VarType.Real, VarType.Real);
            Assert.IsInstanceOf<MaxOfRealFunction>(maxRealReal, "Max real->real->real not found");
        }

        [Test]
        public void HasAllMaxFuns_GetOrNullWithIntArrArg_returnsOne()
        {
            AddAllMaxFuns();
            var maxIntArr = dic.GetOrNull(maxId, VarType.ArrayOf(VarType.Int));
            Assert.IsInstanceOf<MultiMaxIntFunction>(maxIntArr, "Max int[]->int not found");
        }

        [Test]
        public void HasAllMaxFuns_GetOrNullWithRealArrArg_returnsOne()
        {
            AddAllMaxFuns();
            var maxRealArr = dic.GetOrNull(maxId, VarType.ArrayOf(VarType.Real));
            Assert.IsInstanceOf<MultiMaxRealFunction>(maxRealArr, "Max real[]->real not found");
        }
        
        [Test]
        public void HasAllMaxFuns_GetOrNullWithBoolArg_returnsNull()
        {
            AddAllMaxFuns();
            var maxRealArr = dic.GetOrNull(maxId, VarType.Bool);
            Assert.IsNull(maxRealArr);
        }

        [Test]
        public void HasSomeFun_AddSome_returnsFalse()
        {
            var origin    = new FunMock("some", VarType.Int, VarType.Bool);
            var overrided = new FunMock(origin.Name, VarType.Anything, origin.ArgTypes);

            dic.Add(origin);
            Assert.IsFalse( dic.Add(overrided));
        }
        [Test]
        public void HasFunWithSingleArg_GetOrNullWithWrongTypeReturnsnull()
        {
            var origin = new FunMock("some", VarType.Int, VarType.Bool);
            
            dic.Add(origin);
            
            var fun = dic.GetOrNull(origin.Name, VarType.Text);
            Assert.IsNull(fun);
        }
        
        [Test]
        public void HasFunWithSingleArg_GetOrNullWithCast_ReturnsOne()
        {
            var origin = new FunMock("some", VarType.Int, VarType.Real);
            
            dic.Add(origin);
            
            var fun = dic.GetOrNull(origin.Name, VarType.Int);
            Assert.IsNotNull(fun);
        }
        [Test]
        public void HasOverloadFuns_GetOrNull_ReturnsWithStrictCast()
        {
            var realFun = new FunMock("some", VarType.Real, VarType.Real);
            var intFun = new FunMock("some", VarType.Int, VarType.Int);
            
            dic.Add(realFun);
            dic.Add(intFun);
            
            var fun = dic.GetOrNull(intFun.Name, VarType.Int);
            Assert.AreEqual(intFun.OutputType, fun.OutputType);
        }
        [Test]
        public void HasOverloadsWithGenerics_ConcreteNeedToBeCasted_GetOrNull_ReturnsGeneric()
        {
            var concreteFun = new FunMock("some", VarType.Real, VarType.Real);
            var genericFun = new GenericFunMock("some", VarType.Text, VarType.Generic(0));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            
            var fun = dic.GetOrNull(concreteFun.Name, VarType.Int);
            Assert.AreEqual(VarType.Text, fun.OutputType);
        }
        
        [Test]
        public void HasOverloadsWithGenerics_ConcreteIsStrict_GetOrNull_ReturnsConcrete()
        {
            var concreteFun = new FunMock("some", VarType.Real, VarType.Real);
            var genericFun = new GenericFunMock("some", VarType.Text, VarType.Generic(0));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            
            var fun = dic.GetOrNull(concreteFun.Name, VarType.Real);
            Assert.AreEqual(VarType.Real, fun.OutputType);
        }
        
        [Test]
        public void HasOverloadsWithGenericArray_ConcreteNeedToBeCasted_GetOrNull_ReturnsGeneric()
        {
            var concreteFun = new FunMock("some", VarType.Text, VarType.Anything);
            var genericFun = new GenericFunMock("some", VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0)));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            var someConcreteType = VarType.Int;
            var fun = dic.GetOrNull(concreteFun.Name, VarType.ArrayOf(someConcreteType));
            Assert.AreEqual(someConcreteType, fun.OutputType);
        }
        [Test]
        public void HasOverloadsWithGenericArray_ConcreteNeedCovariantCast_GetOrNull_ReturnsGeneric()
        {
            var concreteFun = new FunMock("some", VarType.Text, VarType.ArrayOf(VarType.Anything));
            var genericFun = new GenericFunMock("some", VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0)));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            var someConcreteType = VarType.Int;
            var fun = dic.GetOrNull(concreteFun.Name, VarType.ArrayOf(someConcreteType));
            Assert.AreEqual(someConcreteType, fun.OutputType);
        }
        [Test]
        public void AddHasOverloadsForRealIntText_ArgsAreTextInt_ReturnsTextAnythingOverload()
        {
            dic.Add(new AddIntFunction());
            dic.Add(new AddRealFunction());
            dic.Add(new AddTextFunction());
           
            var fun = dic.GetOrNull(CoreFunNames.Add,  VarType.Text,VarType.Int);
            Assert.IsNotNull(fun);
            Assert.IsInstanceOf<AddTextFunction>(fun);
            
        }
        [Test]
        public void AddHasOverloadsForRealIntText_ArgsAreIntReal_ReturnsRealRealOverload()
        {
            dic.Add(new AddIntFunction());
            dic.Add(new AddRealFunction());
            dic.Add(new AddTextFunction());
           
            var fun = dic.GetOrNull(CoreFunNames.Add, VarType.Int, VarType.Real);
            Assert.IsNotNull(fun);
            Assert.IsInstanceOf<AddRealFunction>(fun);
            
        }
        private void AddAllMaxFuns()
        {
            dic.Add(new MaxOfIntFunction());
            dic.Add(new MaxOfRealFunction());
            dic.Add(new MultiMaxIntFunction());
            dic.Add(new MultiMaxRealFunction());
        }
    }

    class GenericFunMock : GenericFunctionBase
    {
        public GenericFunMock(
            string name, 
            VarType outputType, 
            params VarType[] argTypes) : base(name, outputType, argTypes)
        {
        }

        public override object Calc(object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
    class FunMock : FunctionBase
    {
        public FunMock(string name, VarType outputType, params VarType[] argTypes) 
            : base(name, outputType, argTypes)
        {
        }

        public override object Calc(object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}