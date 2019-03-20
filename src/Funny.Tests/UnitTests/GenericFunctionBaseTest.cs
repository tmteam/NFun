using System;
using System.Linq;
using Funny.BuiltInFunctions;
using Funny.Interpritation.Functions;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class GenericFunctionBaseTest
    {
        [Test]
        public void ReturnSelfFunction_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new ReturnSelfGenericFunctionDefenition();
            var function = rpt.MakeConcreteOrNull(VarType.Bool);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.Bool, function.OutputType);
                CollectionAssert.AreEquivalent(new[]{VarType.Bool}, function.ArgTypes);
            });
        }
        [Test]
        public void ReturnSelfFunction_ArrayType_ResultTypesAreCorrect()
        {
            var genericArgument = VarType.ArrayOf(VarType.Bool);
            var rpt = new ReturnSelfGenericFunctionDefenition();
            var function = rpt.MakeConcreteOrNull(genericArgument);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(genericArgument, function.OutputType);
                CollectionAssert.AreEquivalent(new[]{genericArgument}, function.ArgTypes);
            });
        }
        [Test]
        public void Repeat_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new RepeatGenericFunctionDefenition();
            var function = rpt.MakeConcreteOrNull(VarType.Bool, VarType.Int);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.ArrayOf(VarType.Bool), function.OutputType);
                CollectionAssert.AreEquivalent(new[]{VarType.Bool, VarType.Int}, function.ArgTypes);
            });
        }
        [Test]
        public void Take_PrimitiveType_ResultTypesAreCorrect()
        {
            var rpt = new TakeGenericFunctionDefenition();
            var function = rpt.MakeConcreteOrNull(VarType.ArrayOf(VarType.Bool), VarType.Int);
            Assert.IsNotNull(function);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.ArrayOf(VarType.Bool), function.OutputType);
                CollectionAssert.AreEquivalent(new[]{VarType.ArrayOf(VarType.Bool), VarType.Int}, function.ArgTypes);
            });
        }
        [Test]
        public void Take_ArrayType_ResultTypesAreCorrect()
        {
            var rpt = new TakeGenericFunctionDefenition();
            var genericArgument = VarType.ArrayOf(VarType.Bool);
            var function = rpt.MakeConcreteOrNull(VarType.ArrayOf(genericArgument), VarType.Int);
            Assert.IsNotNull(function);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(VarType.ArrayOf(genericArgument), function.OutputType);
                CollectionAssert.AreEquivalent(new[]{VarType.ArrayOf(genericArgument), VarType.Int}, function.ArgTypes);
            });
        }
    }
    public class ReturnSelfGenericFunctionDefenition : GenericFunctionBase
    {
        public ReturnSelfGenericFunctionDefenition() : base("__retSelf", VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override object Calc(object[] args) => args.First();
    }
}