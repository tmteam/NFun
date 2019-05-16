using System.Collections.Generic;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Types;
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
            var fun = dic.GetOrNull(new MaxOfIntFunction().Name, new[] {VarType.Int32, VarType.Int32});
            Assert.IsInstanceOf<MaxOfIntFunction>(fun);
        }


        [Test]
        public void HasAllMaxFuns_GetOrNullWithIntArgs_returnsOne()
        {
            AddAllMaxFuns();
            var maxIntInt = dic.GetOrNull(maxId, VarType.Int32, VarType.Int32);
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
            var maxIntArr = dic.GetOrNull(maxId, VarType.ArrayOf(VarType.Int32));
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
            var origin    = new FunMock("some", VarType.Int32, VarType.Bool);
            var overrided = new FunMock(origin.Name, VarType.Anything, origin.ArgTypes);

            dic.Add(origin);
            Assert.IsFalse( dic.Add(overrided));
        }
        [Test]
        public void HasFunWithSingleArg_GetOrNullWithWrongTypeReturnsnull()
        {
            var origin = new FunMock("some", VarType.Int32, VarType.Bool);
            
            dic.Add(origin);
            
            var fun = dic.GetOrNull(origin.Name, VarType.Text);
            Assert.IsNull(fun);
        }
        
        [Test]
        public void HasFunWithSingleArg_GetOrNullWithCast_ReturnsOne()
        {
            var origin = new FunMock("some", VarType.Int32, VarType.Real);
            
            dic.Add(origin);
            
            var fun = dic.GetOrNull(origin.Name, VarType.Int32);
            Assert.IsNotNull(fun);
        }
        [Test]
        public void HasOverloadFuns_GetOrNull_ReturnsWithStrictCast()
        {
            var realFun = new FunMock("some", VarType.Real, VarType.Real);
            var intFun = new FunMock("some", VarType.Int32, VarType.Int32);
            
            dic.Add(realFun);
            dic.Add(intFun);
            
            var fun = dic.GetOrNull(intFun.Name, VarType.Int32);
            Assert.AreEqual(intFun.ReturnType, fun.ReturnType);
        }
        [Test]
        public void HasOverloadsWithGenerics_ConcreteNeedToBeCasted_GetOrNull_ReturnsGeneric()
        {
            var concreteFun = new FunMock("some", VarType.Real, VarType.Real);
            var genericFun = new GenericFunMock("some", VarType.Text, VarType.Generic(0));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            
            var fun = dic.GetOrNull(concreteFun.Name, VarType.Int32);
            Assert.AreEqual(VarType.Text, fun.ReturnType);
        }
        
        [Test]
        public void HasOverloadsWithGenerics_ConcreteIsStrict_GetOrNull_ReturnsConcrete()
        {
            var concreteFun = new FunMock("some", VarType.Real, VarType.Real);
            var genericFun = new GenericFunMock("some", VarType.Text, VarType.Generic(0));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            
            var fun = dic.GetOrNull(concreteFun.Name, VarType.Real);
            Assert.AreEqual(VarType.Real, fun.ReturnType);
        }
        
        [Test]
        public void HasOverloadsWithGenericArray_ConcreteNeedToBeCasted_GetOrNull_ReturnsGeneric()
        {
            var concreteFun = new FunMock("some", VarType.Text, VarType.Anything);
            var genericFun = new GenericFunMock("some", VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0)));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            var someConcreteType = VarType.Int32;
            var fun = dic.GetOrNull(concreteFun.Name, VarType.ArrayOf(someConcreteType));
            Assert.AreEqual(someConcreteType, fun.ReturnType);
        }
        [Test]
        public void HasOverloadsWithGenericArray_ConcreteNeedCovariantCast_GetOrNull_ReturnsGeneric()
        {
            var concreteFun = new FunMock("some", VarType.Text, VarType.ArrayOf(VarType.Anything));
            var genericFun = new GenericFunMock("some", VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0)));
            dic.Add(concreteFun);
            dic.Add(genericFun);
            var someConcreteType = VarType.Int32;
            var fun = dic.GetOrNull(concreteFun.Name, VarType.ArrayOf(someConcreteType));
            Assert.AreEqual(someConcreteType, fun.ReturnType);
        }
        [Test]
        public void AddHasOverloadsForRealIntText_ArgsAreTextInt_ReturnsTextAnythingOverload()
        {
            dic.Add(new AddIntFunction());
            dic.Add(new AddRealFunction());
            dic.Add(new AddTextFunction());
           
            var fun = dic.GetOrNull(CoreFunNames.Add,  VarType.Text,VarType.Int32);
            Assert.IsNotNull(fun);
            Assert.IsInstanceOf<AddTextFunction>(fun);
            
        }
        [Test]
        public void ContainsHasOverloads_ArgsAreAtomic_ReturnsAtomicOverload()
        {
            dic.Add(new IsInSingleGenericFunctionDefenition());
            dic.Add(new IsInMultipleGenericFunctionDefenition());
           
            var fun = dic.GetOrNull(CoreFunNames.In,  VarType.Int32,VarType.ArrayOf(VarType.Int32));
            Assert.IsNotNull(fun);
            Assert.AreEqual(VarType.Int32, fun.ArgTypes[0]);
        }
        [Test]
        public void ContainsHasOverloads_ArgsAreArray_ReturnsArrayOverload()
        {
            dic.Add(new IsInSingleGenericFunctionDefenition());
            dic.Add(new IsInMultipleGenericFunctionDefenition());
           
            var fun = dic.GetOrNull(CoreFunNames.In,  VarType.ArrayOf(VarType.Int32),VarType.ArrayOf(VarType.Int32));
            Assert.IsNotNull(fun);
            Assert.AreEqual(VarType.ArrayOf(VarType.Int32), fun.ArgTypes[0]);
        }
        [Test]
        public void ContainsHasOverloads_ArrayArgsAreInvalid_ReturnsNull()
        {
            dic.Add(new IsInSingleGenericFunctionDefenition());
            dic.Add(new IsInMultipleGenericFunctionDefenition());
           
            var fun = dic.GetOrNull(CoreFunNames.In,  VarType.ArrayOf(VarType.Text),VarType.ArrayOf(VarType.Int32));
            Assert.IsNull(fun);
        }
        [Test]
        public void ContainsWithoutOveloads_AtomicArgsAreInvalid_ReturnsNull()
        {
            dic.Add(new IsInSingleGenericFunctionDefenition());
           
            var fun = dic.GetOrNull(CoreFunNames.In,  VarType.Text,VarType.Int32);
            Assert.IsNull(fun);
        }
        
        [Test]
        public void AddHasOverloadsForRealIntText_ArgsAreIntReal_ReturnsRealRealOverload()
        {
            dic.Add(new AddIntFunction());
            dic.Add(new AddRealFunction());
            dic.Add(new AddTextFunction());
           
            var fun = dic.GetOrNull(CoreFunNames.Add, VarType.Int32, VarType.Real);
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
            VarType specifiedType, 
            params VarType[] argTypes) : base(name, specifiedType, argTypes)
        {
        }

        public override object Calc(object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
    class FunMock : FunctionBase
    {
        public FunMock(string name, VarType returnType, params VarType[] argTypes) 
            : base(name, returnType, argTypes)
        {
        }

        public override object Calc(object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}