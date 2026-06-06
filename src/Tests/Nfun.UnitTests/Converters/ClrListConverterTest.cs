using System.Collections.Generic;
using System.Linq;
using NFun.Runtime.Lists;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters;

/// <summary>
/// Stage 2.2 CLR converter tests for <c>List&lt;T&gt;</c> ↔ <c>list&lt;T&gt;</c>.
/// Verifies that the converter detects <c>System.Collections.Generic.List&lt;T&gt;</c>
/// CLR types, routes them to <see cref="ClrListInputTypeFunnyConverter"/> /
/// <see cref="ClrListOutputFunnyConverter"/>, and roundtrips values intact.
/// </summary>
[TestFixture]
public class ClrListConverterTest {
    [Test]
    public void InputConverterFromClrType_DetectsListAsList() {
        var converter = FunnyConverter.RealIsDouble.GetInputConverterFor(typeof(List<int>));
        Assert.AreEqual(FunnyType.ListOf(FunnyType.Int32), converter.FunnyType);
    }

    [Test]
    public void OutputConverterFromClrType_DetectsListAsList() {
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(typeof(List<int>));
        Assert.AreEqual(FunnyType.ListOf(FunnyType.Int32), converter.FunnyType);
    }

    [Test]
    public void OutputConverterFromFunnyType_TargetsGenericList() {
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(FunnyType.ListOf(FunnyType.Int32));
        Assert.AreEqual(typeof(List<int>), converter.ClrType);
    }

    [Test]
    public void InputConverter_WrapsClrListIntoMutableFunnyList() {
        var clrInput = new List<int> { 1, 2, 3 };
        var converter = FunnyConverter.RealIsDouble.GetInputConverterFor(typeof(List<int>));
        var funObj = converter.ToFunObject(clrInput);
        Assert.IsInstanceOf<MutableFunnyList>(funObj);
        var fun = (MutableFunnyList)funObj;
        Assert.AreEqual(3, fun.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, fun.As<int>().ToArray());
    }

    [Test]
    public void OutputConverter_UnwrapsMutableFunnyListIntoClrList() {
        var fun = new MutableFunnyList(FunnyType.Int32, new object[] { 1, 2, 3 });
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(typeof(List<int>));
        var clrObj = converter.ToClrObject(fun);
        Assert.IsInstanceOf<List<int>>(clrObj);
        CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, (List<int>)clrObj);
    }

    [Test]
    public void Roundtrip_ListOfInt_PreservesElements() {
        var origin = new List<int> { 7, 14, 21 };
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        var inConv = converter.GetInputConverterFor(typeof(List<int>));
        var outConv = converter.GetOutputConverterFor(typeof(List<int>));
        var fun = inConv.ToFunObject(origin);
        var back = outConv.ToClrObject(fun);
        CollectionAssert.AreEqual(origin, (List<int>)back);
    }

    [Test]
    public void Roundtrip_ListOfReal_PreservesElements() {
        var origin = new List<double> { 1.5, 2.5, 3.5 };
        var converter = FunnyConverter.RealIsDouble;
        var inConv = converter.GetInputConverterFor(typeof(List<double>));
        var outConv = converter.GetOutputConverterFor(typeof(List<double>));
        var fun = inConv.ToFunObject(origin);
        var back = outConv.ToClrObject(fun);
        CollectionAssert.AreEqual(origin, (List<double>)back);
    }

    [Test]
    public void EmptyList_RoundtripsEmpty() {
        var origin = new List<int>();
        var converter = FunnyConverter.RealIsDouble;
        var inConv = converter.GetInputConverterFor(typeof(List<int>));
        var outConv = converter.GetOutputConverterFor(typeof(List<int>));
        var fun = inConv.ToFunObject(origin);
        var back = (List<int>)outConv.ToClrObject(fun);
        Assert.AreEqual(0, back.Count);
    }
}
