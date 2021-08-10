using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters
{
    public class VarTypeConverterTest
    {
        [TestCase((int)1, (double)1, BaseFunnyType.Int32, BaseFunnyType.Real)]
        [TestCase((long)1,(double)1, BaseFunnyType.Int64, BaseFunnyType.Real)]
        [TestCase((byte)1,(UInt32)1, BaseFunnyType.UInt8, BaseFunnyType.UInt32)]
        [TestCase((byte)1,(Int32)1, BaseFunnyType.UInt8, BaseFunnyType.Int32)]
        public void ConvertPrimitives(object from, object expected, BaseFunnyType typeFrom, BaseFunnyType typeTo)
        {
           Assert.IsTrue(VarTypeConverter.CanBeConverted(
               @from: FunnyType.PrimitiveOf(typeFrom), 
               to: FunnyType.PrimitiveOf(typeTo)));
           var converter = VarTypeConverter.GetConverterOrNull(
               FunnyType.PrimitiveOf(typeFrom),
               FunnyType.PrimitiveOf(typeTo));
           var converted = converter(from);
           Assert.AreEqual(converted, expected);
        }
        [Test]
        public void ConvertIntArrayToRealArray()
        {
            var intArray = new ImmutableFunnyArray(new[] {1, 2, 3, 4});
            var typeFrom = FunnyType.ArrayOf(FunnyType.Int32);
            var typeTo   = FunnyType.ArrayOf(FunnyType.Real);
            Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom,typeTo));
            var coverter = VarTypeConverter.GetConverterOrNull(typeFrom, typeTo);
            var actual =  coverter(intArray) as IFunnyArray;
            Assert.IsNotNull(actual);
            Assert.AreEqual(typeTo.ArrayTypeSpecification.FunnyType, actual.ElementType);
            CollectionAssert.AreEqual(new double[]{1,2,3,4}, actual);
        }
        [Test]
        public void ConvertFun()
        {
            var typeFrom = FunnyType.Fun(FunnyType.Bool, FunnyType.Int32);
            var typeTo   = FunnyType.Fun(FunnyType.Text, FunnyType.UInt8);
            Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom,typeTo));
            var coverter = VarTypeConverter.GetConverterOrNull(typeFrom, typeTo);
            Func<int,bool> funcFrom = (input) => input > 0;
            var convertedFunc = coverter(new LambdaToFunWrapper<int, bool>(funcFrom)) as IConcreteFunction;
            var result = convertedFunc.Calc(new object[] {((byte) 12)});
            Assert.IsInstanceOf<IFunnyArray>(result,$"Actual result is {result}");
            Assert.AreEqual((result as IFunnyArray).ToText(),"True");
        }
        [Test]
        public void ConvertStruct()
        {
            var typeTo     = FunnyType.StructOf(("name", FunnyType.Text));
            var typeFrom   = FunnyType.StructOf(("name", FunnyType.Text),("age", FunnyType.Int32));
            Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom,typeTo));
            var coverter = VarTypeConverter.GetConverterOrNull(typeFrom, typeTo);
            var fromStruct = FunnyStruct.Create(("name", new TextFunnyArray("vasa")), ("age", 42));
            var convertedStruct = coverter(fromStruct) as FunnyStruct;
            Assert.IsNotNull(convertedStruct);
            var val = convertedStruct.GetValue("name") as TextFunnyArray;
            Assert.AreEqual(val,new TextFunnyArray("vasa"));
        }
    
        class LambdaToFunWrapper<Tin,Tout> : IConcreteFunction
        {
            private readonly Func<Tin,Tout> _func;
            public LambdaToFunWrapper(Func<Tin,Tout> func) => _func = func;
            public string Name { get; }
            public FunnyType[] ArgTypes { get; }
            public FunnyType ReturnType { get; }
            public object Calc(object[] parameters) => _func((Tin) parameters[0]);
            public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, Interval interval) 
                => throw new NotImplementedException();
        }
    }
}