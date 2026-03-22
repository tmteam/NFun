using System;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

public class SomeCustomType {
    public int Id { get; set; }
    public string Label { get; set; }

    public SomeCustomType() { Id = 0; Label = "default"; }
    public SomeCustomType(int id, string label) { Id = id; Label = label; }

    public override bool Equals(object obj) =>
        obj is SomeCustomType other && Id == other.Id && Label == other.Label;

    public override int GetHashCode() => HashCode.Combine(Id, Label);
    public override string ToString() => $"SomeCustomType({Id}, {Label})";
}

public class AnotherCustomType {
    public double Value { get; set; }
    public AnotherCustomType() { Value = 0; }
    public AnotherCustomType(double value) { Value = value; }
    public override bool Equals(object obj) => obj is AnotherCustomType o && Math.Abs(Value - o.Value) < 0.001;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"AnotherCustomType({Value})";
}

// ── Fooba / Booba: real CLR types for integration tests ─────────────────────
public class Fooba {
    public int Value { get; }
    public Fooba() => Value = 0;
    public Fooba(int value) => Value = value;
    public override bool Equals(object obj) => obj is Fooba f && f.Value == Value;
    public override int GetHashCode() => Value;
    public override string ToString() => $"Fooba({Value})";
}

public class Booba {
    public string Str { get; }
    public Booba() => Str = "";
    public Booba(string str) => Str = str;
    public override bool Equals(object obj) => obj is Booba b && b.Str == Str;
    public override int GetHashCode() => Str?.GetHashCode() ?? 0;
    public override string ToString() => $"Booba({Str})";
}

public class FoobaDef : IFunnyCustomTypeDefinition {
    public static readonly FoobaDef Instance = new();
    public string Name => "fooba";
    public object DefaultValue { get; } = new Fooba(0);
    public bool Equals(object a, object b) => a.Equals(b);
    public string ToText(object value) => value.ToString();
}

public class BoobaDef : IFunnyCustomTypeDefinition {
    public static readonly BoobaDef Instance = new();
    public string Name => "booba";
    public object DefaultValue { get; } = new Booba("");
    public bool Equals(object a, object b) => a.Equals(b);
    public string ToText(object value) => value.ToString();
}

// IFunnyCustomTypeDefinition implementations — singletons
public class SomeCustomCustomTypeDef : IFunnyCustomTypeDefinition {
    public static readonly SomeCustomCustomTypeDef Instance = new();
    public string Name => "my_type";
    public object DefaultValue { get; } = new SomeCustomType(0, "default");
    public bool Equals(object a, object b) => a.Equals(b);
    public string ToText(object value) => value.ToString();
}

public class AnotherCustomCustomTypeDef : IFunnyCustomTypeDefinition {
    public static readonly AnotherCustomCustomTypeDef Instance = new();
    public string Name => "other_type";
    public object DefaultValue { get; } = new AnotherCustomType();
    public bool Equals(object a, object b) => a.Equals(b);
    public string ToText(object value) => value.ToString();
}

[TestFixture]
public class CustomTypeTest {

    private static FunnyType MyType => FunnyType.CustomOf(SomeCustomCustomTypeDef.Instance);

    private static HardcoreBuilder Builder =>
        Funny.Hardcore.WithCustomType(SomeCustomCustomTypeDef.Instance);

    [Test]
    public void CustomType_AprioriInput_ReturnsValue() {
        var runtime = Builder
            .WithApriori("x", MyType)
            .Build("y = x");

        var input = new SomeCustomType(42, "hello");
        runtime["x"].Value = input;
        runtime.Run();

        Assert.AreEqual(input, runtime["y"].Value);
    }

    [Test]
    public void CustomType_TypeAnnotation() {
        var runtime = Builder.Build("y:my_type = x");

        var input = new SomeCustomType(1, "test");
        runtime["x"].Value = input;
        runtime.Run();

        Assert.AreEqual(input, runtime["y"].Value);
    }

    [Test]
    public void CustomType_IfElse() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = if(true) a else b");

        var va = new SomeCustomType(1, "a");
        var vb = new SomeCustomType(2, "b");
        runtime["a"].Value = va;
        runtime["b"].Value = vb;
        runtime.Run();

        Assert.AreEqual(va, runtime["y"].Value);
    }

    [Test]
    public void CustomType_Equality() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = a == b");

        runtime["a"].Value = new SomeCustomType(1, "x");
        runtime["b"].Value = new SomeCustomType(1, "x");
        runtime.Run();
        Assert.AreEqual(true, runtime["y"].Value);
    }

    [Test]
    public void CustomType_DefaultValue() {
        var runtime = Builder.Build("y:my_type = default");
        runtime.Run();

        Assert.AreEqual(new SomeCustomType(0, "default"), runtime["y"].Value);
    }

    [Test]
    public void CustomType_InArray() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = [a, b]");

        runtime["a"].Value = new SomeCustomType(1, "x");
        runtime["b"].Value = new SomeCustomType(2, "y");
        runtime.Run();

        var result = runtime["y"].Value;
        Assert.IsNotNull(result);
    }

    [Test]
    public void TwoDifferentCustomTypes_CannotMix() {
        var builder = Funny.Hardcore
            .WithCustomType(SomeCustomCustomTypeDef.Instance)
            .WithCustomType(AnotherCustomCustomTypeDef.Instance);

        Assert.Catch(() => builder.Build("a:my_type = x\r b:other_type = x"));
    }

    // ── CLR integration tests ────────────────────────────────────────────────

    [Test]

    public void CustomType_UserFunction_TwoCustomArgs_ReturnsInt() {
        // fooba carries int, booba carries string
        // userFunction(fooba, booba) => booba.str.Length + fooba.value
        var runtime = Funny.Hardcore
            .WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithFunction<Fooba, Booba, int>("userFunction", (f, b) => b.Str.Length + f.Value)
            .WithApriori("f", FunnyType.CustomOf(FoobaDef.Instance))
            .WithApriori("b", FunnyType.CustomOf(BoobaDef.Instance))
            .Build("y = userFunction(f, b)");

        runtime["f"].Value = new Fooba(42);
        runtime["b"].Value = new Booba("kookoo");
        runtime.Run();

        Assert.AreEqual(48, runtime["y"].Value); // 6 + 42 = 48
    }

    [Test]

    public void CustomType_UserFunction_Converts_FoobaToBooba() {
        // myFun2(fooba) => booba
        var runtime = Funny.Hardcore
            .WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithFunction<Fooba, Booba>("myFun2", f => new Booba("x" + f.Value))
            .WithApriori("f", FunnyType.CustomOf(FoobaDef.Instance))
            .Build("y:booba = myFun2(f)");

        runtime["f"].Value = new Fooba(7);
        runtime.Run();

        var result = (Booba)runtime["y"].Value;
        Assert.AreEqual("x7", result.Str);
    }

    // ── Equality / Inequality ──────────────────────────────────────────────────

    [Test]
    public void CustomType_Equality_True() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = a == b");

        runtime["a"].Value = new SomeCustomType(1, "x");
        runtime["b"].Value = new SomeCustomType(1, "x");
        runtime.Run();
        Assert.AreEqual(true, runtime["y"].Value);
    }

    [Test]
    public void CustomType_Equality_False() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = a == b");

        runtime["a"].Value = new SomeCustomType(1, "x");
        runtime["b"].Value = new SomeCustomType(2, "y");
        runtime.Run();
        Assert.AreEqual(false, runtime["y"].Value);
    }

    [Test]
    public void CustomType_Inequality_True() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = a != b");

        runtime["a"].Value = new SomeCustomType(1, "x");
        runtime["b"].Value = new SomeCustomType(2, "y");
        runtime.Run();
        Assert.AreEqual(true, runtime["y"].Value);
    }

    [Test]
    public void CustomType_Inequality_False() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = a != b");

        runtime["a"].Value = new SomeCustomType(1, "x");
        runtime["b"].Value = new SomeCustomType(1, "x");
        runtime.Run();
        Assert.AreEqual(false, runtime["y"].Value);
    }

    // ── Optional ────────────────────────────────────────────────────────────────

    [Test]
    public void CustomType_Optional_WithValue() {
        var runtime = Builder
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .WithApriori("a", MyType)
            .Build("y = if(true) a else none");

        runtime["a"].Value = new SomeCustomType(5, "opt");
        runtime.Run();
        Assert.AreEqual(new SomeCustomType(5, "opt"), runtime["y"].Value);
    }

    [Test]
    public void CustomType_NullCoalesce() {
        var runtime = Builder
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .WithApriori("a", FunnyType.OptionalOf(MyType))
            .WithApriori("b", MyType)
            .Build("y = a ?? b");

        runtime["a"].Value = new SomeCustomType(1, "val");
        runtime["b"].Value = new SomeCustomType(99, "fallback");
        runtime.Run();
        Assert.AreEqual(new SomeCustomType(1, "val"), runtime["y"].Value);
    }

    // ── If-else with different values ───────────────────────────────────────────

    [Test]
    public void CustomType_IfElse_FalseCondition() {
        var runtime = Builder
            .WithApriori("a", MyType)
            .WithApriori("b", MyType)
            .Build("y = if(false) a else b");

        runtime["a"].Value = new SomeCustomType(1, "a");
        runtime["b"].Value = new SomeCustomType(2, "b");
        runtime.Run();

        Assert.AreEqual(new SomeCustomType(2, "b"), runtime["y"].Value);
    }

    // ── Multiple custom types coexisting ────────────────────────────────────────

    [Test]
    public void TwoCustomTypes_IndependentVariables() {
        var runtime = Funny.Hardcore
            .WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithApriori("f", FunnyType.CustomOf(FoobaDef.Instance))
            .WithApriori("b", FunnyType.CustomOf(BoobaDef.Instance))
            .Build("x = f\r y = b");

        runtime["f"].Value = new Fooba(42);
        runtime["b"].Value = new Booba("hi");
        runtime.Run();

        Assert.AreEqual(new Fooba(42), runtime["x"].Value);
        Assert.AreEqual(new Booba("hi"), runtime["y"].Value);
    }

    // ── UserFunction chaining ───────────────────────────────────────────────────

    [Test]
    public void CustomType_FunctionChain_FoobaToBoobaThenLength() {
        var runtime = Funny.Hardcore
            .WithCustomType(FoobaDef.Instance)
            .WithCustomType(BoobaDef.Instance)
            .WithFunction<Fooba, Booba>("toBooba", f => new Booba("v" + f.Value))
            .WithFunction<Booba, int>("blen", b => b.Str.Length)
            .WithApriori("f", FunnyType.CustomOf(FoobaDef.Instance))
            .Build("y = blen(toBooba(f))");

        runtime["f"].Value = new Fooba(123);
        runtime.Run();

        Assert.AreEqual(4, runtime["y"].Value); // "v123".Length = 4
    }

    [Test]
    public void CustomType_PassThrough() {
        var runtime = Builder
            .WithApriori("x", MyType)
            .Build("y = x");

        var input = new SomeCustomType(99, "const");
        runtime["x"].Value = input;
        runtime.Run();

        Assert.AreEqual(input, runtime["y"].Value);
    }
}
