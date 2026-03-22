using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

[TestFixture]
public class CustomTypeStringTemplateTest {

    [Test]
    public void StringTemplate_CustomTypeInInterpolation() {
        var template = Funny.Hardcore
            .WithCustomType(FoobaDef.Instance)
            .WithFunction<Fooba, int>("getVal", f => f.Value)
            .WithApriori("f", FunnyType.CustomOf(FoobaDef.Instance))
            .BuildStringTemplate("value is {getVal(f)}!");

        template["f"].Value = new Fooba(42);
        var result = template.Calculate();
        Assert.AreEqual("value is 42!", result);
    }

    [Test]
    public void StringTemplate_TwoCustomTypes() {
        var template = Funny.Hardcore
            .WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithFunction<Fooba, int>("fval", f => f.Value)
            .WithFunction<Booba, string>("bstr", b => b.Str)
            .WithApriori("f", FunnyType.CustomOf(FoobaDef.Instance))
            .WithApriori("b", FunnyType.CustomOf(BoobaDef.Instance))
            .BuildStringTemplate("{fval(f)} and {bstr(b)}");

        template["f"].Value = new Fooba(7);
        template["b"].Value = new Booba("hello");
        var result = template.Calculate();
        Assert.AreEqual("7 and hello", result);
    }
}
