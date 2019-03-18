using System.Collections.Generic;
using Funny.BuiltInFunctions;
using Funny.Interpritation;
using Funny.Interpritation.Functions;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
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
        public void AddOrReplaceFun_GetOrNull_returnsOne()
        {
            var sin = new SinFunction();
            dic.AddOrReplace(sin);
            var fun = dic.GetOrNull(sin.Name, sin.ArgTypes);
            Assert.IsInstanceOf<SinFunction>(fun);
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
            var fun = dic.GetOrNull(new MaxOfIntFunction().Name, new[] {VarType.IntType, VarType.IntType});
            Assert.IsInstanceOf<MaxOfIntFunction>(fun);
        }


        [Test]
        public void HasAllMaxFuns_GetOrNullWithIntArgs_returnsOne()
        {
            AddAllMaxFuns();
            var maxIntInt = dic.GetOrNull(maxId, VarType.IntType, VarType.IntType);
            Assert.IsInstanceOf<MaxOfIntFunction>(maxIntInt, "Max int->int->int not found");
        }

        [Test]
        public void HasAllMaxFuns_GetOrNullWithRealArgs_returnsOne()
        {
            AddAllMaxFuns();
            var maxRealReal = dic.GetOrNull(maxId, VarType.RealType, VarType.RealType);
            Assert.IsInstanceOf<MaxOfIntFunction>(maxRealReal, "Max real->real->real not found");
        }

        [Test]
        public void HasAllMaxFuns_GetOrNullWithIntArrArg_returnsOne()
        {
            AddAllMaxFuns();
            var maxIntArr = dic.GetOrNull(maxId, VarType.ArrayOf(VarType.IntType));
            Assert.IsInstanceOf<MultiMaxIntFunction>(maxIntArr, "Max int[]->int not found");
        }

        [Test]
        public void HasAllMaxFuns_GetOrNullWithRealArrArg_returnsOne()
        {
            AddAllMaxFuns();
            var maxRealArr = dic.GetOrNull(maxId, VarType.ArrayOf(VarType.RealType));
            Assert.IsInstanceOf<MaxOfIntFunction>(maxRealArr, "Max real[]->real not found");
        }
        
        [Test]
        public void HasAllMaxFuns_GetOrNullWithBoolArg_returnsNull()
        {
            AddAllMaxFuns();
            var maxRealArr = dic.GetOrNull(maxId, VarType.BoolType);
            Assert.IsNull(maxRealArr);
        }

        [Test]
        public void HasSomeFun_AddOrReplaceSome_GetOrNullReturnsReplaced()
        {
            var origin = new FunMock("some", VarType.IntType, VarType.BoolType);
            var overrided = new FunMock(origin.Name, VarType.AnyType, origin.ArgTypes);

            dic.Add(origin);
            dic.AddOrReplace(overrided);
            
            var fun = dic.GetOrNull(overrided.Name, overrided.ArgTypes);
            Assert.AreEqual(overrided,fun);
        }
        
        private void AddAllMaxFuns()
        {
            dic.Add(new MaxOfIntFunction());
            dic.Add(new MaxOfRealFunction());
            dic.Add(new MultiMaxIntFunction());
            dic.Add(new MultiMaxRealFunction());
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