using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

/// <summary>
/// Fluent API tests for custom types.
/// Note: Fluent API (Calc/BuildForCalc) auto-deconstructs TInput/TOutput CLR types
/// into struct fields. Custom types used as PROPERTIES of input/output work correctly.
/// Custom types AS the entire TInput/TOutput require Hardcore API with WithApriori.
/// </summary>
[TestFixture]
public class CustomTypeFluentTest {

    // Wrapper — fluent API maps properties to script variables
    public class FoobaHolder { public Fooba F { get; set; } = new(); public Booba B { get; set; } = new(); }

    [Test]
    public void FluentCalc_FoobaToInt_ViaFunction() {
        // Custom type as property of input → script variable 'f' is Fooba
        var builder = Funny.WithCustomType(FoobaDef.Instance)
            .WithFunction<Fooba, int>("getVal", f => f.Value);

        var calculator = builder.BuildForCalc<FoobaHolder, int>();
        var result = calculator.Calc("out = getVal(f)", new FoobaHolder { F = new Fooba(42) });
        Assert.AreEqual(42, result);
    }

    [Test]
    public void FluentCalc_TwoCustomTypes_Combine() {
        var calculator = Funny.WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithFunction<Fooba, Booba, int>("combine", (f, b) => f.Value + b.Str.Length)
            .BuildForCalc<FoobaHolder, int>();

        var result = calculator.Calc("out = combine(f, b)",
            new FoobaHolder { F = new Fooba(10), B = new Booba("abc") });
        Assert.AreEqual(13, result);
    }

    [Test]
    public void FluentCalc_FunctionChain() {
        var calculator = Funny.WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithFunction<Fooba, Booba>("toBooba", f => new Booba("v" + f.Value))
            .WithFunction<Booba, int>("blen", b => b.Str.Length)
            .BuildForCalc<FoobaHolder, int>();

        var result = calculator.Calc("out = blen(toBooba(f))",
            new FoobaHolder { F = new Fooba(123) });
        Assert.AreEqual(4, result); // "v123".Length
    }

    [Test]
    public void FluentCalc_Reuse() {
        var calculator = Funny.WithCustomType(FoobaDef.Instance)
            .WithFunction<Fooba, int>("getVal", f => f.Value)
            .BuildForCalc<FoobaHolder, int>();

        Assert.AreEqual(1, calculator.Calc("out = getVal(f)", new FoobaHolder { F = new Fooba(1) }));
        Assert.AreEqual(42, calculator.Calc("out = getVal(f)", new FoobaHolder { F = new Fooba(42) }));
        Assert.AreEqual(0, calculator.Calc("out = getVal(f)", new FoobaHolder { F = new Fooba(0) }));
    }

    [Test]
    public void FluentCalc_Equality() {
        var calculator = Funny.WithCustomType(FoobaDef.Instance)
            .BuildForCalc<FoobaHolder, bool>();

        var result = calculator.Calc("out = f == f", new FoobaHolder { F = new Fooba(5) });
        Assert.AreEqual(true, result);
    }
}
