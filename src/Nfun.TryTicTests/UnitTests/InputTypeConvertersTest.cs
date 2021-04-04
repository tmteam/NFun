using System;
using System.Linq;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    public class InputTypeConvertersTest
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
            var converter = FunnyTypeConverters.GetInputConverter(clrType);
            Assert.AreEqual(expectedTypeName, converter.FunnyType.BaseType);
            var convertedValue = converter.ToFunObject(primitiveValue);
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
            var converter = FunnyTypeConverters.GetInputConverter(clrType);

            Assert.AreEqual(VarType.ArrayOf(VarType.PrimitiveOf(expectedTypeName)), converter.FunnyType);
            var convertedValue = converter.ToFunObject(primitiveValue);
            Assert.AreEqual(primitiveValue, convertedValue);
        }
        [TestCase("")]
        [TestCase("v")]
        [TestCase("value")]
        public void ConvertString(string value)
        {
            var converter = FunnyTypeConverters.GetInputConverter(typeof(string));

            Assert.AreEqual(VarType.Text, converter.FunnyType);
            Assert.AreEqual(new TextFunArray(value), converter.ToFunObject(value));
        }
        [Test]
        public void ConvertArrayOfStrings()
        {
            string[] inputValue = {
                "vasa", "kata", ""
            };
            
            var converter = FunnyTypeConverters.GetInputConverter(inputValue.GetType());

            Assert.AreEqual(VarType.ArrayOf(VarType.Text), converter.FunnyType);
            Assert.AreEqual(new ImmutableFunArray(
                inputValue.Select(i => new TextFunArray(i)).ToArray(),VarType.Text),
                converter.ToFunObject(inputValue));
        }
        [Test]
        public void ArrayOfArrayOfAnything()
        {
            object[][] inputValue = {
                new object[] {1, 2, "kate"},
                new object[] {2, 1, "kate"},
                new object[] { }
            };
            var converter = FunnyTypeConverters.GetInputConverter(inputValue.GetType());
            Assert.AreEqual(VarType.ArrayOf(VarType.ArrayOf(VarType.Anything)), converter.FunnyType);
            var value = converter.ToFunObject(inputValue);
            Assert.IsInstanceOf<ImmutableFunArray>(value);
            Assert.AreEqual(3,((ImmutableFunArray) value).Count);
        }
        
        [Test]
        public void StructType()
        {
            var inputUser = new UserMoqType("vasa", 42, 17.1);
            var converter = FunnyTypeConverters.GetInputConverter(inputUser.GetType());
            Assert.AreEqual(VarType.StructOf(
                ("name",VarType.Text),
                ("age",VarType.Int32), 
                ("size", VarType.Real)), converter.FunnyType);
            var value = converter.ToFunObject(inputUser);
            Assert.IsInstanceOf<FunnyStruct>(value);
            var converted = (FunnyStruct) value;
            Assert.AreEqual(inputUser.Name, (converted.GetValue("name") as TextFunArray).ToText());
            Assert.AreEqual(inputUser.Age, converted.GetValue("age"));
            Assert.AreEqual(inputUser.Size, converted.GetValue("size"));
        }
        
        [Test]
        public void ArrayOfStructTypes()
        {
            var inputUsers = new[]
            {
                new UserMoqType("vasa", 42, 17.1),
                new UserMoqType("peta", 41, 17.0),
                new UserMoqType("kata", 40, -17.1)
            };
            
            var converter = FunnyTypeConverters.GetInputConverter(inputUsers.GetType());
            Assert.AreEqual(
                VarType.ArrayOf(VarType.StructOf(
                ("name",VarType.Text),
                ("age",VarType.Int32), 
                ("size", VarType.Real))), converter.FunnyType);
            var value = converter.ToFunObject(inputUsers);
            Assert.IsInstanceOf<ImmutableFunArray>(value);
            var secondElememt = ((ImmutableFunArray) value).GetElementOrNull(1) as FunnyStruct;
            Assert.IsNotNull(secondElememt,((ImmutableFunArray) value).GetElementOrNull(1).GetType().Name +" is wrong type");
            Assert.AreEqual(inputUsers[1].Name, (secondElememt.GetValue("name") as TextFunArray).ToText());
            Assert.AreEqual(inputUsers[1].Age, secondElememt.GetValue("age"));
            Assert.AreEqual(inputUsers[1].Size, secondElememt.GetValue("size"));
        }
        
        [Test]
        public void RequrisiveType_Throws()
        {
            NodeMoqType obj = new NodeMoqType("vasa", new NodeMoqType("peta"));
            Assert.Throws<ArgumentException>(()=> FunnyTypeConverters.GetInputConverter(obj.GetType()));
        }
    }

    class NodeMoqType
    {
        public NodeMoqType(string name, params NodeMoqType[] children)
        {
            Name = name;
            Children = children;
        }

        public string Name { get; }
        public NodeMoqType[] Children { get; }
    }
    class UserMoqType   
    {
        public UserMoqType(string name, int age, double size)
        {
            Name = name;
            Age = age;
            Size = size;
        }

        public string Name { get; }
        public int Age { get; }
        public double Size { get; }
        public bool State { set; private get; }
    }
}