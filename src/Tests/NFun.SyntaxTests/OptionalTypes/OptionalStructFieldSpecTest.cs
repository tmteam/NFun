namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalStructFieldSpecTest {

    [Test]
    public void OptionalStruct_WithValue() =>
        Assert.DoesNotThrow(() =>
            "x:{a:int}? = {a = 1}"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalStruct_WithNone() {
        var result = "x:{a:int}? = none\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalStructOptionalField_InnerNone() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);

    [Test]
    [Ignore("Struct field type specification not supported yet")]
    public void ArrayOfStructsWithOptionalField() =>
        Assert.DoesNotThrow(() =>
            "y = [{n:int? = 1}, {n:int? = none}, {n:int? = 3}]"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Stress_OptionalStructFieldCoalesce() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 99);

    [Test]
    public void Stress_OptionalStructFieldCoalesce_HasValue() =>
        "s:{n:int?} = {n = 42}\r y = s.n ?? 99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);
}
