using System;
using System.Linq;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NUnit.Framework;

namespace NFun.UnitTests;

[TestFixture]
public class FunctionRegistryTest {

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ImmutableFunctionRegistry CreateDict(params StubFunction[] funcs) =>
        new(funcs, Array.Empty<GenericFunctionBase>());

    private static ImmutableFunctionRegistry CreateDictFromConcretes(params IConcreteFunction[] funcs) =>
        new(funcs, Array.Empty<GenericFunctionBase>());

    // ── ImmutableFunctionRegistry: GetOrNull ──────────────────────────────

    [Test]
    public void GetOrNull_ExistingFunction_ReturnsIt() {
        var f = new StubFunction("foo", 2);
        var dict = CreateDict(f);
        Assert.AreEqual(f, dict.GetOrNull("foo", 2));
    }

    [Test]
    public void GetOrNull_WrongArity_ReturnsNull() {
        var dict = CreateDict(new StubFunction("foo", 2));
        Assert.IsNull(dict.GetOrNull("foo", 3));
    }

    [Test]
    public void GetOrNull_WrongName_ReturnsNull() {
        var dict = CreateDict(new StubFunction("foo", 2));
        Assert.IsNull(dict.GetOrNull("bar", 2));
    }

    [Test]
    public void GetOrNull_EmptyDict_ReturnsNull() {
        var dict = CreateDict();
        Assert.IsNull(dict.GetOrNull("foo", 1));
    }

    // ── Overloads: same name, different arity ───────────────────────────────

    [Test]
    public void GetOrNull_Overloads_ReturnsCorrectArity() {
        var f1 = new StubFunction("log", 1);
        var f2 = new StubFunction("log", 2);
        var dict = CreateDict(f1, f2);

        Assert.AreEqual(f1, dict.GetOrNull("log", 1));
        Assert.AreEqual(f2, dict.GetOrNull("log", 2));
        Assert.IsNull(dict.GetOrNull("log", 3));
    }

    // ── FindOrNull: no named args → same as GetOrNull ───────────────────────

    [Test]
    public void FindOrNull_NoNamedArgs_SameAsGetOrNull() {
        var f = new StubFunction("foo", 2);
        var dict = CreateDict(f);
        Assert.AreEqual(f, dict.FindOrNull("foo", 2, null));
        Assert.AreEqual(f, dict.FindOrNull("foo", 2, Array.Empty<string>()));
    }

    // ── FindOrNull: exact match with ArgProperties ──────────────────────────

    [Test]
    public void FindOrNull_ExactMatch_WithArgProperties() {
        var f = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f);
        Assert.AreEqual(f, dict.FindOrNull("max", 0, new[] { "a", "b" }));
    }

    [Test]
    public void FindOrNull_PartialNamed_ExactMatch() {
        var f = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f);
        Assert.AreEqual(f, dict.FindOrNull("max", 1, new[] { "b" }));
    }

    [Test]
    public void FindOrNull_AllPositional_NoNamed() {
        var f = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f);
        Assert.AreEqual(f, dict.FindOrNull("max", 2, null));
    }

    // ── FindOrNull: no ArgProperties → returns null for named args ──────────

    [Test]
    public void FindOrNull_NoArgProperties_ReturnsNull() {
        var f = new StubFunction("foo", 2); // no arg names
        var dict = CreateDict(f);
        // exact arity match exists, but no ArgProperties → can't validate named args

        Assert.IsNull(dict.FindOrNull("foo", 0, new[] { "a", "b" }));
    }

    // ── FindOrNull: wrong named arg name ────────────────────────────────────

    [Test]
    public void FindOrNull_UnknownNamedArg_ReturnsNull() {
        var f = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f);
        Assert.IsNull(dict.FindOrNull("max", 0, new[] { "a", "z" }));
    }

    // ── FindOrNull: named arg overlaps positional → returns null ─────────────

    [Test]
    public void FindOrNull_OverlapsPositional_ReturnsNull() {
        var f = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f);
        // 2 positional + named "a" → "a" is slot 0 which overlaps
        Assert.IsNull(dict.FindOrNull("max", 2, new[] { "a" }));
    }

    // ── FindOrNull: overload resolution by named args ───────────────────────

    [Test]
    public void FindOrNull_SelectsCorrectOverload() {
        var f1 = new StubFunction("log", 1, "x");
        var f2 = new StubFunction("log", 2, "value", "newBase");
        var dict = CreateDict(f1, f2);

        // Named call: log(newBase=10, value=100) → should find f2
        Assert.AreEqual(f2, dict.FindOrNull("log", 0, new[] { "newBase", "value" }));
        // Named call: log(x=1) → should find f1
        Assert.AreEqual(f1, dict.FindOrNull("log", 0, new[] { "x" }));
    }

    // ── FindOrNull: case-insensitive named args ─────────────────────────────

    [Test]
    public void FindOrNull_CaseInsensitive() {
        var f = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f);
        Assert.AreEqual(f, dict.FindOrNull("max", 0, new[] { "A", "B" }));
    }

    // ── FindOrNull: with defaults ───────────────────────────────────────────

    [Test]
    public void FindOrNull_WithDefaults_FewerArgsThanParams() {
        var f = new StubFunctionWithDefaults("greet", new[] { "name", "greeting" }, new[] { false, true });
        var dict = CreateDictFromConcretes(f);
        // 1 positional, 0 named → "greeting" has default → should match
        Assert.AreEqual(f, dict.FindOrNull("greet", 1, Array.Empty<string>()));
    }

    [Test]
    public void FindOrNull_WithDefaults_NamedOnlyRequired() {
        var f = new StubFunctionWithDefaults("greet", new[] { "name", "greeting" }, new[] { false, true });
        var dict = CreateDictFromConcretes(f);
        Assert.AreEqual(f, dict.FindOrNull("greet", 0, new[] { "name" }));
    }

    [Test]
    public void FindOrNull_WithDefaults_MissingRequired_ReturnsNull() {
        var f = new StubFunctionWithDefaults("greet", new[] { "name", "greeting" }, new[] { false, true });
        var dict = CreateDictFromConcretes(f);
        // only "greeting" named, "name" has no default → doesn't match
        Assert.IsNull(dict.FindOrNull("greet", 0, new[] { "greeting" }));
    }

    // ── CloneWith ───────────────────────────────────────────────────────────

    [Test]
    public void CloneWith_AddsNewFunction() {
        var f1 = new StubFunction("foo", 1);
        var dict = CreateDict(f1);
        var f2 = new StubFunction("bar", 2);
        var dict2 = dict.CloneWith(f2);

        Assert.AreEqual(f1, dict2.GetOrNull("foo", 1));
        Assert.AreEqual(f2, dict2.GetOrNull("bar", 2));
    }

    [Test]
    public void CloneWith_DoesNotMutateOriginal() {
        var dict = CreateDict(new StubFunction("foo", 1));
        dict.CloneWith(new StubFunction("bar", 2));

        Assert.IsNull(dict.GetOrNull("bar", 2));
    }

    [Test]
    public void CloneWith_FindOrNull_WorksOnNewDict() {
        var f1 = new StubFunction("max", 2, "a", "b");
        var dict = CreateDict(f1);
        var f2 = new StubFunction("min", 2, "a", "b");
        var dict2 = dict.CloneWith(f2);

        Assert.AreEqual(f2, dict2.FindOrNull("min", 0, new[] { "a", "b" }));
    }

    // ── ScopeFunctionRegistry ─────────────────────────────────────────────

    [Test]
    public void Scope_GetOrNull_DelegatesToOrigin() {
        var f = new StubFunction("foo", 1);
        var origin = CreateDict(f);
        var scope = new ScopeFunctionRegistry(origin);

        Assert.AreEqual(f, scope.GetOrNull("foo", 1));
    }

    [Test]
    public void Scope_GetOrNull_LocalOverridesOrigin() {
        var f1 = new StubFunction("foo", 1);
        var origin = CreateDict(f1);
        var scope = new ScopeFunctionRegistry(origin);
        var f2 = new StubFunction("foo", 1);
        scope.Add(f2);

        Assert.AreEqual(f2, scope.GetOrNull("foo", 1));
    }

    [Test]
    public void Scope_GetOrNull_LocalAndOriginCoexist() {
        var f1 = new StubFunction("foo", 1);
        var origin = CreateDict(f1);
        var scope = new ScopeFunctionRegistry(origin);
        var f2 = new StubFunction("bar", 2);
        scope.Add(f2);

        Assert.AreEqual(f1, scope.GetOrNull("foo", 1));
        Assert.AreEqual(f2, scope.GetOrNull("bar", 2));
    }

    [Test]
    public void Scope_FindOrNull_DelegatesToOrigin() {
        var f = new StubFunction("max", 2, "a", "b");
        var origin = CreateDict(f);
        var scope = new ScopeFunctionRegistry(origin);

        Assert.AreEqual(f, scope.FindOrNull("max", 0, new[] { "a", "b" }));
    }

    [Test]
    public void Scope_FindOrNull_PrefersLocal() {
        var f1 = new StubFunction("max", 2, "a", "b");
        var origin = CreateDict(f1);
        var scope = new ScopeFunctionRegistry(origin);
        var f2 = new StubFunction("max", 2, "x", "y");
        scope.Add(f2);

        Assert.AreEqual(f2, scope.FindOrNull("max", 0, new[] { "x", "y" }));
    }

    [Test]
    public void Scope_FindOrNull_FallsBackToOrigin() {
        var f1 = new StubFunction("max", 2, "a", "b");
        var origin = CreateDict(f1);
        var scope = new ScopeFunctionRegistry(origin);
        var f2 = new StubFunction("min", 2, "x", "y");
        scope.Add(f2);

        Assert.AreEqual(f1, scope.FindOrNull("max", 0, new[] { "a", "b" }));
    }

    // ── SearchAllFunctionsIgnoreCase ────────────────────────────────────────

    [Test]
    public void SearchAll_CaseInsensitive() {
        var f = new StubFunction("Foo", 1);
        var dict = CreateDict(f);
        var results = dict.SearchAllFunctionsIgnoreCase("foo", 1);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(f, results[0]);
    }

    [Test]
    public void SearchAll_NoMatch_ReturnsEmpty() {
        var dict = CreateDict(new StubFunction("foo", 1));
        var results = dict.SearchAllFunctionsIgnoreCase("bar", 1);
        Assert.AreEqual(0, results.Count);
    }
}

// ── Test stubs ──────────────────────────────────────────────────────────────

class StubFunction : FunctionWithManyArguments {
    public StubFunction(string name, int argCount, params string[] argNames)
        : base(name, FunnyType.Int32, Enumerable.Repeat(FunnyType.Int32, argCount).ToArray()) {
        if (argNames.Length > 0)
            ArgProperties = FunArgProperty.FromNames(argNames);
    }

    public override object Calc(object[] args) => 0;
}

class StubFunctionWithDefaults : FunctionWithManyArguments {
    public StubFunctionWithDefaults(string name, string[] argNames, bool[] hasDefault)
        : base(name, FunnyType.Int32, Enumerable.Repeat(FunnyType.Int32, argNames.Length).ToArray()) {
        var props = new FunArgProperty[argNames.Length];
        for (int i = 0; i < argNames.Length; i++)
            props[i] = new FunArgProperty { Name = argNames[i], HasDefault = hasDefault[i] };
        ArgProperties = props;
    }

    public override object Calc(object[] args) => 0;
}

class PapaFunction : FunctionWithManyArguments {
    public const string PapaReturn = "papa is here";
    public PapaFunction(string name) : base(name, FunnyType.Text) { }

    public override object Calc(object[] args) => PapaReturn;
}

class MamaFunction : FunctionWithManyArguments {
    public const string MamaReturn = "mama called";

    public MamaFunction(string name) : base(name, FunnyType.Text) { }

    public override object Calc(object[] args) => MamaReturn;
}
