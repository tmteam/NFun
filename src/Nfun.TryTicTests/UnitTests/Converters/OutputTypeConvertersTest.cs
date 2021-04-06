using System;
using System.Linq;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.ModuleTests.UnitTests.Converters
{
    public class OutputTypeConvertersTest
    {
        [TestCase((byte)1, BaseVarType.UInt8)]
        [TestCase((ushort)2, BaseVarType.UInt16)]
        [TestCase((uint)3, BaseVarType.UInt32)]
        [TestCase((ulong)4, BaseVarType.UInt64)]
        [TestCase((Int16)1, BaseVarType.Int16)]
        [TestCase((int)2, BaseVarType.Int32)]
        [TestCase((long)3, BaseVarType.Int64)]
        [TestCase((double)-15.1, BaseVarType.Real)]
        [TestCase(true, BaseVarType.Bool)]
        public void ConvertPrimitiveType(object primitiveValue, BaseVarType expectedTypeName)
        {
            var clrType = primitiveValue.GetType();
            var converter = FunnyTypeConverters.GetOutputConverter(clrType);
            Assert.AreEqual(expectedTypeName, converter.FunnyType.BaseType);
            var convertedValue = converter.ToClrObject(primitiveValue);
            Assert.AreEqual(primitiveValue, convertedValue);
        }
        
        [TestCase(new byte[]  {1,2,3},      BaseVarType.UInt8)]
        [TestCase(new UInt16[]{1,2,3},      BaseVarType.UInt16)]
        [TestCase(new UInt32[]{1,2,3},      BaseVarType.UInt32)]
        [TestCase(new UInt64[]{1,2,3},      BaseVarType.UInt64)]
        [TestCase(new Int16[]{1,2,3},       BaseVarType.Int16)]
        [TestCase(new Int32[]{1,2,3},       BaseVarType.Int32)]
        [TestCase(new Int64[]{1,2,3},       BaseVarType.Int64)]
        [TestCase(new Double[]{1,2,3},      BaseVarType.Real)]
        [TestCase(new[]{true, false},       BaseVarType.Bool)]
        public void ConvertPrimitiveTypeArrays(object primitiveValue, BaseVarType expectedTypeName)
        {
            var clrType = primitiveValue.GetType();
            var converter = FunnyTypeConverters.GetOutputConverter(clrType);

            Assert.AreEqual(VarType.ArrayOf(VarType.PrimitiveOf(expectedTypeName)), converter.FunnyType);
            var convertedValue = converter.ToClrObject(
                new ImmutableFunArray(primitiveValue as Array, VarType.PrimitiveOf(expectedTypeName)));
            Assert.AreEqual(primitiveValue, convertedValue);
        }
        [TestCase("")]
        [TestCase("v")]
        [TestCase("value")]
        public void ConvertString(string value)
        {
            var converter = FunnyTypeConverters.GetOutputConverter(typeof(string));
            Assert.AreEqual(VarType.Text, converter.FunnyType);
            
            Assert.AreEqual(value, converter.ToClrObject(new TextFunArray(value)));
        }
        [Test]
        public void ConvertArrayOfStrings()
        {
            string[] clrValue = {"vasa", "kata", ""};
            
            var converter = FunnyTypeConverters.GetOutputConverter(clrValue.GetType());

            Assert.AreEqual(VarType.ArrayOf(VarType.Text), converter.FunnyType);
            var funValue = new ImmutableFunArray(
                clrValue.Select(i => new TextFunArray(i)).ToArray(), VarType.Text);

            Assert.AreEqual(clrValue, converter.ToClrObject(funValue));

        }
        [Test]
        public void ArrayOfArrayOfAnything()
        {
            object[][] inputValue = {
                new object[] {1, 2, "kate"},
                new object[] {2, 1, "kate"},
                new object[] { }
            };
            var converter = FunnyTypeConverters.GetOutputConverter(inputValue.GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.ArrayOf(VarType.Anything)), converter.FunnyType);
        }
        
        [Test]
        public void StructType()
        {
            var inputUser = new UserMoqOutputType("vasa", 42, 17.1);
            var converter = FunnyTypeConverters.GetOutputConverter(inputUser.GetType());
            Assert.AreEqual(VarType.StructOf(
                ("name",VarType.Text),
                ("age",VarType.Int32), 
                ("size", VarType.Real)), converter.FunnyType);
        }
        
        [Test]
        public void ArrayOfStructTypesWithoutNewContructor()
        {
            var inputUsers = new[]
            {
                new UserMoqType("vasa", 42, 17.1),
                new UserMoqType("peta", 41, 17.0),
                new UserMoqType("kata", 40, -17.1)
            };
            
            Assert.Throws<InvalidOperationException>(()=> FunnyTypeConverters.GetOutputConverter(inputUsers.GetType()));
        }
        [Test]
        public void ArrayOfStructTypes()
        {
            var inputUsers = new[]
            {
                new UserMoqOutputType("vasa", 42, 17.1),
                new UserMoqOutputType("peta", 41, 17.0),
                new UserMoqOutputType("kata", 40, -17.1)
            };
            
            var converter = FunnyTypeConverters.GetOutputConverter(inputUsers.GetType());
            Assert.AreEqual(
                VarType.ArrayOf(VarType.StructOf(
                    ("name",VarType.Text),
                    ("age",VarType.Int32), 
                    ("size", VarType.Real))), converter.FunnyType);
        }

        [Test]
        public void RequrisiveType_Throws() 
            => Assert.Throws<ArgumentException>(()=> FunnyTypeConverters.GetOutputConverter(typeof(NodeMoqOutputType)));
    }

    class NodeMoqOutputType
    {
        public string Name { get; set; }
        public NodeMoqType[] Children { get; set; }
    }
    class UserMoqOutputType   
    {
        public UserMoqOutputType(string name, int age, double size)
        {
            Name = name;
            Age = age;
            Size = size;
        }

        public UserMoqOutputType() { }
        public string Name { get; set; }
        public int Age { get; set; }
        public double Size { get; set; }
        public bool State { get; }
    }
}