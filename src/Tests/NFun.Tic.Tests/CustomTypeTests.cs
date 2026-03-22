using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

class CustomTypeTests {

    private class TestCustomTypeDef : Types.IFunnyCustomTypeDefinition {
        public static readonly TestCustomTypeDef Instance = new();
        public string Name => "my_type";
        public object DefaultValue => new object();
        public bool Equals(object a, object b) => ReferenceEquals(a, b);
        public string ToText(object value) => value?.ToString() ?? "null";
    }

    private static StatePrimitiveCustom MyType =>
        new("my_type", FunnyType.CustomOf(TestCustomTypeDef.Instance));

    // y = a (where a: custom)
    [Test]
    public void SingleVar_Custom() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", MyType);
        graph.SetVar("a", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(MyType, "a", "y");
    }

    // y = if(true) a else a (where a: custom)
    [Test]
    public void IfElse_SameCustomVar() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", MyType);
        graph.SetConst(0, Bool);
        graph.SetVar("a", 1);
        graph.SetVar("a", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(MyType, "a", "y");
    }

    // y = if(true) a else b (where a,b: same custom)
    [Test]
    public void IfElse_TwoDifferentCustomVars_SameType() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", MyType);
        graph.SetVarType("b", MyType);
        graph.SetConst(0, Bool);
        graph.SetVar("a", 1);
        graph.SetVar("b", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(MyType, "a", "b", "y");
    }

    // For comparison: same thing with Bool (should work identically)
    [Test]
    public void IfElse_TwoBoolVars_Reference() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", Bool);
        graph.SetVarType("b", Bool);
        graph.SetConst(0, Bool);
        graph.SetVar("a", 1);
        graph.SetVar("b", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(Bool, "a", "b", "y");
    }

    // y = a == b (where a,b: custom)
    [Test]
    public void Equality_Custom() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", MyType);
        graph.SetVarType("b", MyType);
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        // == function: (T,T) -> Bool
        graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNamed(MyType, "a", "b");
        result.AssertNamed(Bool, "y");
    }

}
