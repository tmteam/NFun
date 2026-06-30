using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters;

public class VarTypeConverterTest {
    [TestCase((int)1, (double)1, BaseFunnyType.Int32, BaseFunnyType.Real)]
    [TestCase((long)1, (double)1, BaseFunnyType.Int64, BaseFunnyType.Real)]
    [TestCase((byte)1, (UInt32)1, BaseFunnyType.UInt8, BaseFunnyType.UInt32)]
    [TestCase((byte)1, (Int32)1, BaseFunnyType.UInt8, BaseFunnyType.Int32)]
    [TestCase((sbyte)-5, (Int16)(-5), BaseFunnyType.Int8, BaseFunnyType.Int16)]
    [TestCase((sbyte)-5, (Int32)(-5), BaseFunnyType.Int8, BaseFunnyType.Int32)]
    [TestCase((sbyte)-5, (Int64)(-5), BaseFunnyType.Int8, BaseFunnyType.Int64)]
    [TestCase((sbyte)-5, (double)(-5), BaseFunnyType.Int8, BaseFunnyType.Real)]
    // Float32 widening targets
    [TestCase((int)5,    5.0f,         BaseFunnyType.Int32, BaseFunnyType.Float32, Ignore = "Float32 phase 4: int→f32 var convert")]
    [TestCase((sbyte)-5, -5.0f,        BaseFunnyType.Int8,  BaseFunnyType.Float32, Ignore = "Float32 phase 4: i8→f32 var convert")]
    [TestCase((byte)42,  42.0f,        BaseFunnyType.UInt8, BaseFunnyType.Float32, Ignore = "Float32 phase 4: u8→f32 var convert")]
    [TestCase(1.5f,      1.5,          BaseFunnyType.Float32, BaseFunnyType.Real,  Ignore = "Float32 phase 4: f32→real var convert")]
    [TestCase(1.5f,      1.5f,         BaseFunnyType.Float32, BaseFunnyType.Float32,Ignore = "Float32 phase 4: f32→f32 identity")]
    public void ConvertPrimitives(object from, object expected, BaseFunnyType typeFrom, BaseFunnyType typeTo) {
        Assert.IsTrue(
            VarTypeConverter.CanBeConverted(
                @from: FunnyType.PrimitiveOf(typeFrom),
                to: FunnyType.PrimitiveOf(typeTo)));
        var converter = VarTypeConverter.GetConverterOrNull(TypeBehaviour.RealIsDouble, FunnyType.PrimitiveOf(typeFrom),
            FunnyType.PrimitiveOf(typeTo));
        var converted = converter(from);
        Assert.AreEqual(converted, expected);
    }

    [Test]
    public void ConvertIntArrayToRealArray() {
        var intArray = new ImmutableFunnyArray(new[] { 1, 2, 3, 4 });
        var typeFrom = FunnyType.ArrayOf(FunnyType.Int32);
        var typeTo = FunnyType.ArrayOf(FunnyType.Real);
        Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom, typeTo));
        var coverter = VarTypeConverter.GetConverterOrNull(TypeBehaviour.RealIsDouble, typeFrom, typeTo);
        var actual = coverter(intArray) as IFunnyArray;
        Assert.IsNotNull(actual);
        Assert.AreEqual(typeTo.GetGenericArgument(0), actual.ElementType);
        CollectionAssert.AreEqual(new double[] { 1, 2, 3, 4 }, actual);
    }

    [Test]
    public void ConvertFun() {
        var typeFrom = FunnyType.FunOf(FunnyType.Bool, FunnyType.Int32);
        var typeTo = FunnyType.FunOf(FunnyType.Text, FunnyType.UInt8);
        Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom, typeTo));
        var coverter = VarTypeConverter.GetConverterOrNull(TypeBehaviour.RealIsDouble, typeFrom, typeTo);
        Func<int, bool> funcFrom = (input) => input > 0;
        var convertedFunc = coverter(new LambdaToFunWrapper<int, bool>(funcFrom)) as IConcreteFunction;
        var result = convertedFunc.Calc(new object[] { ((byte)12) });
        Assert.IsInstanceOf<IFunnyArray>(result, $"Actual result is {result}");
        Assert.AreEqual((result as IFunnyArray).ToText(), "True");
    }

    [Test]
    public void ConvertStruct() {
        var typeTo = FunnyType.StructOf(("name", FunnyType.Text));
        var typeFrom = FunnyType.StructOf(("name", FunnyType.Text), ("age", FunnyType.Int32));
        Assert.IsTrue(VarTypeConverter.CanBeConverted(typeFrom, typeTo));
        var coverter = VarTypeConverter.GetConverterOrNull(TypeBehaviour.RealIsDouble, typeFrom, typeTo);
        var fromStruct = FunnyStruct.Create(("name", new TextFunnyArray("vasa")), ("age", 42));
        var convertedStruct = coverter(fromStruct) as FunnyStruct;
        Assert.IsNotNull(convertedStruct);
        var val = convertedStruct.GetValue("name") as TextFunnyArray;
        Assert.AreEqual(val, new TextFunnyArray("vasa"));
    }

    class LambdaToFunWrapper<Tin, Tout> : IConcreteFunction {
        private readonly Func<Tin, Tout> _func;
        public LambdaToFunWrapper(Func<Tin, Tout> func) => _func = func;
        public string Name { get; }
        public FunnyType[] ArgTypes { get; }
        public FunnyType ReturnType { get; }
        public object Calc(object[] parameters) => _func((Tin)parameters[0]);

        public IConcreteFunction Clone(ICloneContext context)
            => throw new NotImplementedException();

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children, TypeBehaviour typeBehaviour,
            Interval interval)
            => throw new NotImplementedException();
    }
}
