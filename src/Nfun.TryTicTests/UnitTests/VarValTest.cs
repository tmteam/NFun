using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.ModuleTests.UnitTests
{
    [TestFixture]
    public class VarValTest
    {
        [Test]
        public void UintNewObjectInitialization()
        {
            object val = (uint)0;
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(uint), varVal.Value.GetType());
            Assert.AreEqual(VarType.UInt32, varVal.Type);
        }

        [Test]
        public void IntNewObjectInitialization()
        {
            object val = (int)0;
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(int), varVal.Value.GetType());
            Assert.AreEqual(VarType.Int32, varVal.Type);
        }
        [Test]
        public void UintArrayNewObjectInitialization()
        {
            object val = new uint[] {0, 1, 2};
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(uint), (varVal.Value as IFunArray).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.UInt32), varVal.Type);
        }
        [Test]
        public void IntArrayNewObjectInitialization()
        {
            object val = new int[] { 0, 1, 2 };
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(int), (varVal.Value as IFunArray).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.Int32), varVal.Type);
        }
        [Test]
        public void UintArrayGenericInitialization()
        {
            var val = new uint[] { 0, 1, 2 };
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(uint), (varVal.Value as IFunArray).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.UInt32), varVal.Type);
        }
        [Test]
        public void IntArrayGenericInitialization()
        {
            var val = new int[] { 0, 1, 2 };
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(int), (varVal.Value as IFunArray).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.Int32), varVal.Type);
        }
    }
}
