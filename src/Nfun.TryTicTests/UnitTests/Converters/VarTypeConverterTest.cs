using System;
using System.Collections.Generic;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ModuleTests.UnitTests.Converters
{
    public class VarTypeConverterTest
    {
        [TestCase((int)1, (double)1, BaseVarType.Int32, BaseVarType.Real)]
        [TestCase((long)1,(double)1, BaseVarType.Int64, BaseVarType.Real)]
        [TestCase((byte)1,(UInt32)1, BaseVarType.UInt8, BaseVarType.UInt32)]
        [TestCase((byte)1,(Int32)1, BaseVarType.UInt8, BaseVarType.Int32)]
        public void ConvertPrimitives(object from, object expected, BaseVarType typeFrom, BaseVarType typeTo)
        {
           Assert.IsTrue(VarTypeConverter.CanBeConverted(
               @from: VarType.PrimitiveOf(typeFrom), 
               to: VarType.PrimitiveOf(typeTo)));
           var converter = VarTypeConverter.GetConverterOrNull(
               VarType.PrimitiveOf(typeFrom),
               VarType.PrimitiveOf(typeTo));
           var converted = converter(from);
           Assert.AreEqual(converted, expected);
        }
        [Test]
        public void ConvertIntArrayToRealArray()
        {
            var intArray = new ImmutableFunArray(new[] {1, 2, 3, 4});
            var typeFrom = VarType.ArrayOf(VarType.Int32);
            var typeTo   = VarType.ArrayOf(VarType.Real);
            Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom,typeTo));
            var coverter = VarTypeConverter.GetConverterOrNull(typeFrom, typeTo);
            var actual =  coverter(intArray) as IFunArray;
            Assert.IsNotNull(actual);
            Assert.AreEqual(typeTo.ArrayTypeSpecification.VarType, actual.ElementType);
            CollectionAssert.AreEqual(new double[]{1,2,3,4}, actual);
        }
        [Test]
        public void ConvertFun()
        {
            var typeFrom = VarType.Fun(VarType.Bool, VarType.Int32);
            var typeTo   = VarType.Fun(VarType.Text, VarType.UInt8);
            Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom,typeTo));
            var coverter = VarTypeConverter.GetConverterOrNull(typeFrom, typeTo);
            Func<int,bool> funcFrom = (input) => input > 0;
            var convertedFunc = coverter(new LambdaToFunWrapper<int, bool>(funcFrom)) as IConcreteFunction;
            var result = convertedFunc.Calc(new object[] {((byte) 12)});
            Assert.IsInstanceOf<IFunArray>(result,$"Actual result is {result}");
            Assert.AreEqual((result as IFunArray).ToText(),"True");
        }
        [Test]
        public void ConvertStruct()
        {
            var typeTo     = VarType.StructOf(("name", VarType.Text));
            var typeFrom   = VarType.StructOf(("name", VarType.Text),("age", VarType.Int32));
            Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom,typeTo));
            var coverter = VarTypeConverter.GetConverterOrNull(typeFrom, typeTo);
            var fromStruct = FunnyStruct.Create(("name", new TextFunArray("vasa")), ("age", 42));
            var convertedStruct = coverter(fromStruct) as FunnyStruct;
            Assert.IsNotNull(convertedStruct);
            var val = convertedStruct.GetValue("name") as TextFunArray;
            Assert.AreEqual(val,new TextFunArray("vasa"));
        }
    
        class LambdaToFunWrapper<Tin,Tout> : IConcreteFunction
        {
            private readonly Func<Tin,Tout> _func;
            public LambdaToFunWrapper(Func<Tin,Tout> func) => _func = func;
            public string Name { get; }
            public VarType[] ArgTypes { get; }
            public VarType ReturnType { get; }
            public object Calc(object[] parameters) => _func((Tin) parameters[0]);
            public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval) 
                => throw new NotImplementedException();
        }
    }
}