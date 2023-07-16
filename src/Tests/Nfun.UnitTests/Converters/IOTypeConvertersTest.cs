using System;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace NFun.UnitTests.Converters;

public class IOTypeConvertersTest {
    [TestCase((byte)24)]
    [TestCase((ushort)355)]
    [TestCase((uint)54123)]
    [TestCase((ulong)ulong.MaxValue)]
    [TestCase((short)-1)]
    [TestCase((int)-37651)]
    [TestCase((long)24123123)]
    [TestCase((double)123.1)]
    [TestCase(false)]
    [TestCase("someString")]
    [TestCase("")]
    [TestCase(new byte[] { 24, 2 })]
    [TestCase(new ushort[] { 24, 2 })]
    [TestCase(new uint[] { 24, 2 })]
    [TestCase(new ulong[] { 24, 2, 32 })]
    [TestCase(new short[] { 24, 2, -1 })]
    [TestCase(new int[] { 24, 2 })]
    [TestCase(new long[] { 24, 2 })]
    [TestCase(new Double[] { 24, 2 })]
    [TestCase(new uint[0])]
    [TestCase(new bool[] { true, false, true })]
    [TestCase('c')]
    public void ClrObjectConverts(object clrObject) => AssertFunnyConvert(clrObject);

    [Test]
    public void ArrayOfStringsConverts() => AssertFunnyConvert(new[] { "vasa", "petja", "kata" });

    [Test]
    public void ArrayOfObjectsConverts() => AssertFunnyConvert(new object[] { "vasa", 12, false });

    [Test]
    public void ComplexArrayConverts() => AssertFunnyConvert(
        new[] { new[] { new[] { 1, 2 }, Array.Empty<int>() }, new[] { new[] { 3 }, new[] { 4, 5, 6 } } });

    [Test]
    public void ObjectConverts() => AssertFunnyConvert(new object());

    [Test]
    public void ComplexObject() => AssertFunnyConvert(
        new SomeUser {
            Name = "PussyDog",
            age = 42,
            sizE = 17.5,
            _hAs_money = false,
            objects = new[] { new object(), "foo", false },
            lOveRs = new[] { "mikey", "cow", "spiderman" },
            Friends = new[] {
                new SomeUserFriend { age = 31, lOveRs = Array.Empty<string>(), Name = "DIana  " },
                new SomeUserFriend { age = 14, lOveRs = new[] { "barbie", "Ken", "Putin" }, Name = "" }
            },
            BestFriend = new SomeUserFriend { age = 69, lOveRs = new[] { "a", "b", "c", "" }, Name = "mr. president" }
        });

    class SomeUser {
        public String Name { get; set; }
        public int age { get; set; }
        public double sizE { get; set; }
        public bool _hAs_money { get; set; }
        public object[] objects { get; set; }
        public string[] lOveRs { get; set; }
        public SomeUserFriend[] Friends { get; set; }
        public SomeUserFriend BestFriend { get; set; }
    }

    public class SomeUserFriend {
        public String Name { get; set; }
        public int age { get; set; }
        public string[] lOveRs { get; set; }
    }

    private void AssertFunnyConvert(object originClrObject) {
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        var inputConverter = FunnyConverter.RealIsDouble.GetInputConverterFor(originClrObject.GetType());
        var outputConverter = FunnyConverter.RealIsDouble.GetOutputConverterFor(originClrObject.GetType());
        var size = converter.CacheSize;
        var funObject = inputConverter.ToFunObject(originClrObject);
        var clrObject = outputConverter.ToClrObject(funObject);
        FunnyAssert.AreSame(originClrObject, clrObject);

        FunnyConverter.RealIsDouble.GetInputConverterFor(originClrObject.GetType());
        FunnyConverter.RealIsDouble.GetOutputConverterFor(originClrObject.GetType());
        FunnyConverter.RealIsDouble.GetInputConverterFor(originClrObject.GetType());
        FunnyConverter.RealIsDouble.GetOutputConverterFor(originClrObject.GetType());
        Assert.AreEqual(size, converter.CacheSize);
    }
}
