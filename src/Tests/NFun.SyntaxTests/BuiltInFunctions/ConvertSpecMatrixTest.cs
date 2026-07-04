using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.BuiltInFunctions;

[TestFixture]
public class ConvertSpecMatrixTest {
    [Test]
    public void BoolToInt32_True_Returns1() =>
        "out:int = convert(true)".AssertResultHas("out", 1);

    [Test]
    public void BoolToInt32_False_Returns0() =>
        "out:int = convert(false)".AssertResultHas("out", 0);

    [Test]
    public void BoolToInt64_True_Returns1() =>
        "out:int64 = convert(true)".AssertResultHas("out", 1L);

    [Test]
    public void BoolToByte_True_Returns1() =>
        "out:byte = convert(true)".AssertResultHas("out", (byte)1);

    [Test]
    public void BoolToUInt64_False_Returns0() =>
        "out:uint64 = convert(false)".AssertResultHas("out", 0UL);

    [Test]
    public void BoolToReal_True_Returns1() =>
        "out:real = convert(true)".AssertResultHas("out", 1.0);

    [Test]
    public void BoolToReal_False_Returns0() =>
        "out:real = convert(false)".AssertResultHas("out", 0.0);

    [Test]
    public void IntToBool_Zero_False() =>
        "out:bool = convert(0)".AssertResultHas("out", false);

    [Test]
    public void IntToBool_Five_True() =>
        "out:bool = convert(5)".AssertResultHas("out", true);

    [Test]
    public void IntToBool_NegativeOne_True() =>
        "out:bool = convert(-1)".AssertResultHas("out", true);

    [Test]
    public void Int64ToBool_Zero_False() =>
        "x:int64 = 0\rout:bool = convert(x)".AssertResultHas("out", false);

    [Test]
    public void Int64ToBool_LargeValue_True() =>
        "x:int64 = 1000000000000\rout:bool = convert(x)".AssertResultHas("out", true);

    [Test]
    public void ByteToBool_Zero_False() =>
        "x:byte = 0\rout:bool = convert(x)".AssertResultHas("out", false);

    [Test]
    public void ByteToBool_NonZero_True() =>
        "x:byte = 42\rout:bool = convert(x)".AssertResultHas("out", true);

    [Test]
    public void UInt32ToBool_Zero_False() =>
        "x:uint = 0\rout:bool = convert(x)".AssertResultHas("out", false);

    [Test]
    public void UInt32ToBool_NonZero_True() =>
        "x:uint = 7\rout:bool = convert(x)".AssertResultHas("out", true);

    [Test]
    public void RealToBool_PositiveZero_False() =>
        "x:real = 0.0\rout:bool = convert(x)".AssertResultHas("out", false);

    [Test]
    public void RealToBool_NegativeZero_False() =>
        "x:real = -0.0\rout:bool = convert(x)".AssertResultHas("out", false);

    [Test]
    public void RealToBool_FiniteNonZero_True() =>
        "x:real = 1.5\rout:bool = convert(x)".AssertResultHas("out", true);

    [Test]
    public void RealToBool_NegativeFinite_True() =>
        "x:real = -3.14\rout:bool = convert(x)".AssertResultHas("out", true);

    [Test]
    public void RealToBool_NaN_False() =>
        // NaN computed as 0.0 / 0.0
        "x:real = 0.0 / 0.0\rout:bool = convert(x)".AssertResultHas("out", false);

    [Test]
    public void RealToBool_Infinity_True() =>
        // +Inf computed as 1.0 / 0.0
        "x:real = 1.0 / 0.0\rout:bool = convert(x)".AssertResultHas("out", true);

    [Test]
    public void TextToIntOpt_BadInput_ReturnsNone() {
        var rt = "out:int? = convert('not-a-number')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void TextToIntOpt_GoodInput_ReturnsValue() {
        var rt = "out:int? = convert('42')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(42, rt.Get("out"));
    }

    [Test]
    public void TextToBoolOpt_BadInput_ReturnsNone() {
        var rt = "out:bool? = convert('invalid')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void TextToIpOpt_BadInput_ReturnsNone() {
        var rt = "out:ip? = convert('not-an-ip')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void NarrowingOverflowOpt_OutOfRange_ReturnsNone() {
        var rt = "x:int64 = 99999999999\rout:int? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void NarrowingOpt_InRange_ReturnsValue() {
        var rt = "x:int64 = 42\rout:int? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(42, rt.Get("out"));
    }

    [Test]
    public void RealToIntOpt_Overflow_ReturnsNone() {
        var rt = "x:real = 1.0e20\rout:int? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void RealToIntOpt_Fractional_TruncatesToSome() {
        // Spec §1.1: real → int truncates toward zero (1.5 → 1, NOT banker's round 2).
        // Test confirms both correct truncation AND that no rescue-to-none fires.
        var rt = "x:real = 1.5\rout:int? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1, rt.Get("out"));
    }

    [Test]
    public void RealToInt_Truncation_BoundaryValues() {
        // Spec §1.1: truncate toward zero. Banker's round (Convert.ToInt32 default)
        // would give different results for half-integers and negative fractions.
        "x:real =  1.5\rout:int = convert(x)".AssertResultHas("out", 1); // truncate: 1
        "x:real = -1.5\rout:int = convert(x)".AssertResultHas("out", -1); // truncate: -1
        "x:real =  3.5\rout:int = convert(x)".AssertResultHas("out", 3); // truncate: 3 (banker's: 4)
        "x:real = -0.7\rout:int = convert(x)".AssertResultHas("out", 0); // truncate: 0 (banker's: -1)
    }

    [Test]
    public void ByteArrayToIntOpt_WrongLength_ReturnsNone() {
        var rt = "x:byte[] = [1, 2]\rout:int? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void RealToInt_ExactInteger_PreservedExactly() =>
        "out:int = convert(2.0)".AssertResultHas("out", 2);

    [Test]
    public void RealToInt_Overflow_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() => "x:real = 1.0e20\rout:int = convert(x)".Calc());

    [Test]
    public void OptIntToInt_NoneSource_Throws() {
        var r = "x:int? = none\rout:int = convert(x)"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.Throws<FunnyRuntimeException>(() => r.Run());
    }

    [Test]
    public void OptIntToIntOpt_NoneSource_PreservesNone() {
        var rt = "x:int? = none\rout:int? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void OptIntToInt_SomeSource_ExtractsValue() {
        var rt = "x:int? = 42\rout:int = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(42, rt.Get("out"));
    }

    [Test]
    public void ArrayIntToReal_LiftsElementWise() =>
        "out:real[] = convert([1,2,3])".AssertResultHas("out", new[] { 1.0, 2.0, 3.0 });

    [Test]
    [Ignore("convert-deferred: complex composite conversions — see Specs/Functions.md Implementation status")]
    public void ArrayTextToInt_AllValid_ReturnsInts() =>
        "out:int[] = convert(['1','2','3'])".AssertResultHas("out", new[] { 1, 2, 3 });

    [Test]
    [Ignore("convert-deferred: complex composite conversions — see Specs/Functions.md Implementation status")]
    public void ArrayTextToInt_OneInvalid_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "out:int[] = convert(['1','foo','3'])".Calc());

    [Test]
    [Ignore("convert-deferred: complex composite conversions — see Specs/Functions.md Implementation status")]
    public void ArrayTextToIntOpt_PartialFails_PerElementNone() {
        var rt = "out:int?[] = convert(['1','foo','3'])"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        var arr = (object[])rt.Get("out");
        Assert.AreEqual(3, arr.Length);
        Assert.AreEqual(1, arr[0]);
        Assert.IsNull(arr[1]);
        Assert.AreEqual(3, arr[2]);
    }

    [Test]
    public void IpToUInt32_NaturalRepresentation_Works() =>
        "out:uint = 127.3.2.1.convert()".AssertResultHas("out", (uint)0x0102037f);

    [Test]
    public void IpToInt64_Widening_Works() =>
        "out:int64 = 127.3.2.1.convert()".AssertResultHas("out", (long)0x0102037f);

    [Test]
    public void IpToUInt64_Widening_Works() =>
        "out:uint64 = 127.3.2.1.convert()".AssertResultHas("out", (ulong)0x0102037f);

    [Test]
    public void IpToInt32_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:int = 127.3.2.1.convert()".Calc());

    [Test]
    public void IpToByte_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:byte = 127.3.2.1.convert()".Calc());

    [Test]
    public void IpToInt16_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:int16 = 127.3.2.1.convert()".Calc());

    [Test]
    public void IpToReal_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:real = 127.3.2.1.convert()".Calc());

    [Test]
    public void IpToBool_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:bool = 127.3.2.1.convert()".Calc());

    [Test]
    public void UInt32ToIp_Works() =>
        "x:uint = 0x0102037f\rout:ip = convert(x)"
            .AssertResultHas("out", System.Net.IPAddress.Parse("127.3.2.1"));

    [Test]
    public void Int32NegativeToIp_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "x:int = -1\rout:ip = convert(x)".Calc());

    [Test]
    public void Int32NegativeToIpOpt_ReturnsNone() {
        var rt = "x:int = -1\rout:ip? = convert(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    [Test]
    public void Int64TooBigToIp_Throws() =>
        Assert.Throws<FunnyRuntimeException>(() =>
            "x:int64 = 5000000000\rout:ip = convert(x)".Calc());

    [Test]
    public void BoolToChar_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:char = convert(true)".Calc());

    [Test]
    public void CharToBool_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:bool = convert('a'[0])".Calc());

    [Test]
    public void BoolToIp_StaticReject() => Assert.Throws<FunnyParseException>(() => "out:ip = convert(true)".Calc());

    [Test]
    public void RealToChar_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "x:real = 65.0\rout:char = convert(x)".Calc());

    [Test]
    public void RealToIp_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "x:real = 1.0\rout:ip = convert(x)".Calc());

    [Test]
    public void StructToInt_StaticReject() =>
        Assert.Throws<FunnyParseException>(() =>
            "x = {a=1,b=2}\rout:int = convert(x)".Calc());

    [Test]
    public void IntToStruct_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => "out:{x:int} = convert(5)".Calc());

    [Test]
    public void OptIntToByteArray_StaticReject() =>
        Assert.Throws<FunnyParseException>(() => {
            var b = "x:int? = none\rout:byte[] = convert(x)"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
            b.Run();
        });

    [Test]
    public void AnyToInt_RuntimeIntTag_Works() {
        var rt = Funny.Hardcore.Build("x:any = 42\rout:int = convert(x)");
        rt.Run();
        Assert.AreEqual(42, rt["out"].Value);
    }

    [Test]
    public void AnyToInt_RuntimeStringTag_Throws() {
        var rt = Funny.Hardcore.Build("x:any = 'hello'\rout:int = convert(x)");
        Assert.Throws<FunnyRuntimeException>(() => rt.Run());
    }

    [Test]
    public void AnyToIntOpt_RuntimeStringTag_ReturnsNone() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("x:any = 'hello'\rout:int? = convert(x)");
        rt.Run();
        Assert.IsNull(rt["out"].Value);
    }
}
