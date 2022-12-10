using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

public class ExplicitConvertationTest {
    [Ignore("TODO: toXXX functions are not implemented")]
    [TestCase("y = ~0.toByte()", (byte)255)]
    [TestCase("y = ~1.toByte()", (byte)254)]
    [TestCase("y = ~5.toByte()", (byte)250)]
    [TestCase("y = ~~1.toByte()", (byte)1)]
    [TestCase("y = ~~5.toByte()", (byte)5)]
    [TestCase("y = ~0.toUint32()", (uint)4294967295)]
    [TestCase("y = ~1.toUint32()", (uint)4294967294)]
    [TestCase("y = ~5.toUint32()", (uint)4294967290)]
    [TestCase("y = ~~1.toUint32()", (uint)1)]
    [TestCase("y = ~~5.toUint32()", (uint)5)]
    [TestCase("y = ~0.toUint64()", (ulong)18446744073709551615)]
    [TestCase("y = ~1.toUint64()", (ulong)18446744073709551614)]
    [TestCase("y = ~5.toUint64()", (ulong)18446744073709551610)]
    [TestCase("y = ~~1.toUint64()", (ulong)1)]
    [TestCase("y = ~~5.toUint64()", (ulong)5)]
    [TestCase("y = ~0.toUint16()", (ushort)65535)]
    [TestCase("y = ~1.toUint16()", (ushort)65534)]
    [TestCase("y = ~5.toUint16()", (ushort)65530)]
    [TestCase("y = ~~1.toUint16()", (ushort)1)]
    [TestCase("y = ~~5.toUint16()", (ushort)5)]
    [TestCase("y = ~0.toInt16()", (short)-1)]
    [TestCase("y = ~1.toInt16()", (short)-2)]
    [TestCase("y = ~5.toInt16()", (short)-6)]
    [TestCase("y = ~~1.toInt16()", (short)1)]
    [TestCase("y = ~~5.toInt16()", (short)5)]
    [TestCase("y = ~0.toInt32()", -1)]
    [TestCase("y = ~1.toInt32()", -2)]
    [TestCase("y = ~5.toInt32()", -6)]
    [TestCase("y = ~~1.toInt32()", 1)]
    [TestCase("y = ~~5.toInt32()", 5)]
    [TestCase("y = ~0.toInt64()", (long)-1)]
    [TestCase("y = ~1.toInt64()", (long)-2)]
    [TestCase("y = ~5.toInt64()", (long)-6)]
    [TestCase("y = ~~1.toInt64()", (long)1)]
    [TestCase("y = ~~5.toInt64()", (long)5)]
    public void ExplicitConvertation(string expr, object expected)
        => expr.AssertAnonymousOut(expected);
}
