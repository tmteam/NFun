using System;
using System.Linq;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests
{
   /* [TestFixture]
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
            Assert.AreEqual(typeof(uint), ((IFunArray) varVal.Value).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.UInt32), varVal.Type);
        }
        
        [Test] public void IntArrayNewObjectInitialization()
        {
            object val = new int[] { 0, 1, 2 };
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(int), ((IFunArray) varVal.Value).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.Int32), varVal.Type);
        }
        
        [Test] public void UintArrayGenericInitialization()
        {
            var val = new uint[] { 0, 1, 2 };
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(uint), ((IFunArray) varVal.Value).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.UInt32), varVal.Type);
        }
        
        [Test] public void IntArrayGenericInitialization()
        {
            var val = new int[] { 0, 1, 2 };
            var varVal = VarVal.New("y", val);
            Assert.AreEqual(typeof(int), ((IFunArray) varVal.Value).ToArray()[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.Int32), varVal.Type);
        }
        
        [Test] public void TwinIntArrayGenericInitialization()
        {
            var val = new [] {new int[]{ 0, 1, 2 },new int[]{ 3, 4, 5 } };
            var varVal = VarVal.New("y", val);
            var firstArray = ((IFunArray) varVal.Value).ToArray();
            var secondArray = ((IFunArray) firstArray[0]).ToArray();
            Assert.AreEqual(typeof(int), secondArray[0].GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.ArrayOf(VarType.Int32)), varVal.Type);
        }
      
        [Test] public void Int16TypeMapping() => AssertVarTypeMapping((short)1, VarType.Int16);
        [Test] public void Int32TypeMapping() => AssertVarTypeMapping((int)1, VarType.Int32);
        [Test] public void Int64TypeMapping() => AssertVarTypeMapping((long)1, VarType.Int64);
        [Test] public void Uint8TypeMapping() => AssertVarTypeMapping((byte)1, VarType.UInt8);
        [Test] public void Uint16TypeMapping() => AssertVarTypeMapping((UInt16)1, VarType.UInt16);
        [Test] public void Uint32TypeMapping() => AssertVarTypeMapping((UInt32)1, VarType.UInt32);
        [Test] public void Uint64TypeMapping() => AssertVarTypeMapping((UInt64)1, VarType.UInt64);
        [Test] public void RealTypeMapping()   => AssertVarTypeMapping((double)1, VarType.Real);
        [Test] public void AnyTypeMapping()   => AssertVarTypeMapping(new object(), VarType.Anything);
        [Test] public void CharTypeMapping()   => AssertVarTypeMapping('a', VarType.Char);
        [Test] public void BoolTypeMapping()   => AssertVarTypeMapping(true, VarType.Bool);
        [Test] public void TextTypeMapping()   => AssertVarTypeMapping("vasa", VarType.Text);
        [Test] public void TextFromCharArrayTypeMapping()  
            => AssertVarTypeMapping(new char[]{'a','b','c'}, VarType.Text);

        [Test] public void ArrayOfTextsFromCharArrayTypeMapping()  
            => AssertVarTypeMapping(new[]{new[]{'a','b','c'}}, VarType.ArrayOf(VarType.Text));
        [Test] public void ArrayOfTextsFromStringArrayTypeMapping()  
            => AssertVarTypeMapping(new[]{new[]{"abc"}}, VarType.ArrayOf(VarType.ArrayOf(VarType.Text)));
        [Test] public void ArrayOfAnythingTypeMapping()  
            => AssertVarTypeMapping(new[]{new[]{new object(), "abc"}}, 
                VarType.ArrayOf(VarType.ArrayOf(VarType.Anything)));
            
        [Test] public void ArrayOfInt16TypeMapping()  
            => AssertVarTypeMapping(new Int16[]{1,2,3}, 
                VarType.ArrayOf(VarType.Int16));

        /*[Test] public void ArrayOfAnythingWithTwoSubarraysTypeMapping()  
            => AssertVarTypeMapping(new object[]{ new  Int16[]{1,2,3}, new Int32[]{1,2,3}},
                VarType.ArrayOf(VarType.ArrayOf(VarType.Anything)));*/
/*
        private void AssertVarTypeMapping(object value, VarType expected)
        {
            var variable = VarVal.New("a", value);
            Assert.AreEqual(expected,variable.Type);
        }
    }*/
}
