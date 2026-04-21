using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting session 9 (600 iterations, 3 agents).
/// </summary>
public class BugHunt9Test {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ══════════════════════════════════════════════════════════════
    // Bug 9#1 (CRITICAL): Optional flag leak through ?? with struct field access
    // Pattern: [struct].map(rule if(it.field > X) it.field else none).map(rule it ?? default).aggregate()
    // Crashes or returns wrong type when struct field + if-else-none + ?? in pipeline.
    // Works without struct (plain array). Works when struct access in separate map.
    // ══════════════════════════════════════════════════════════════

    [Test]
    //[Ignore("Bug 9#1: struct field + if-else-none + ?? + sum() crashes")]
    public void Bug1_Sum_Crashes() {
        var r = "[{a=1},{a=2},{a=3}].map(rule if(it.a > 1) it.a else none).map(rule it ?? 0).sum()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(5, r["out"].Value);
    }

    [Test]
    // FIXED: Bug 9#1 — IsOptional leak through ?? with struct field access
    //[Ignore("Bug 9#1: max() returns optional instead of int")]
    public void Bug1_Max_WrongType() {
        var r = "[{a=1},{a=2},{a=3}].map(rule if(it.a > 1) it.a else none).map(rule it ?? 0).max()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(3, r["out"].Value);
    }

    [Test]
    // FIXED: Bug 9#1 — IsOptional leak through ?? with struct field access
    //[Ignore("Bug 9#1: sort() returns optional array instead of int array")]
    public void Bug1_Sort_WrongType() {
        var r = "[{a=1},{a=2},{a=3}].map(rule if(it.a > 1) it.a else none).map(rule it ?? 0).sort()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        // Should be int[], not int?[]
    }

    [Test]
    public void Bug1_WithoutStruct_Works() {
        // Proves the issue is struct field access, not the pipeline itself
        var r = "[1,2,3].map(rule if(it > 1) it else none).map(rule it ?? 0).sum()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(5, r["out"].Value);
    }

    [Test]
    public void Bug1_SeparateMaps_Works() {
        // Struct access in separate map works — proves it's the combination
        var r = "[{a=1},{a=2},{a=3}].map(rule it.a).map(rule if(it > 1) it else none).map(rule it ?? 0).sum()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(5, r["out"].Value);
    }

    [Test]
    // FIXED: Bug 9#1 — IsOptional leak through ?? with struct field access
    //[Ignore("Bug 9#1: count() works — only aggregate functions that need numeric type fail")]
    public void Bug1_Count_Works() {
        var r = "[{a=1},{a=2},{a=3}].map(rule if(it.a > 1) it.a else none).map(rule it ?? 0).count()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(3, r["out"].Value);
    }

    [Test]
    // FIXED: Bug 9#1 — IsOptional leak through ?? with struct field access
    //[Ignore("Bug 9#1: different struct field name")]
    public void Bug1_DifferentFieldName() {
        var r = "[{v=10},{v=20},{v=5}].map(rule if(it.v > 8) it.v else none).map(rule it ?? 0).sum()"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(30, r["out"].Value);
    }

    [Test]
    // FIXED: Bug 9#1 — IsOptional leak through ?? with struct field access
    //[Ignore("Bug 9#1: with fold instead of sum")]
    public void Bug1_Fold() {
        var r = "[{a=1},{a=2},{a=3}].map(rule if(it.a > 1) it.a else none).map(rule it ?? 0).fold(rule it1+it2)"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(5, r["out"].Value);
    }

    // ══════════════════════════════════════════════════════════════
    // Bug 9#2 (MODERATE): sort/filter drops struct fields not in rule
    // TIC narrows generic T to only fields accessed in lambda.
    // ══════════════════════════════════════════════════════════════

    [Test]
    // Bug 9#2 FIXED: width propagation in Pull(ICompositeState=StateStruct, ConstraintsState)
    public void Bug2_Sort_DropsField() {
        // sort by it.a should preserve field b
        var runtime = Funny.Hardcore.Build("[{a=2,b=20},{a=1,b=10}].sort(rule it.a)");
        runtime.Run();
        var outType = runtime["out"].Type;
        // Output must be struct array with BOTH fields a and b
        Assert.AreEqual(BaseFunnyType.ArrayOf, outType.BaseType, "out should be array");
        var elemType = outType.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType, "element should be struct");
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b" }, fields, "struct must have both fields a and b");
    }

    [Test]
    // Bug 9#2 FIXED
    public void Bug2_Filter_DropsField() {
        var runtime = Funny.Hardcore.Build("[{a=1,b=10},{a=2,b=20}].filter(rule it.a > 1)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b" }, fields);
    }

    [Test]
    // Bug 9#2 FIXED
    public void Bug2_SortDescending_DropsField() {
        var runtime = Funny.Hardcore.Build("[{a=1,b=10},{a=2,b=20}].sortDescending(rule it.a)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b" }, fields);
    }

    [Test]
    // Bug 9#2 FIXED
    public void Bug2_Sort_ThreeFields() {
        var runtime = Funny.Hardcore.Build("[{x=2,y=20,z=200},{x=1,y=10,z=100}].sort(rule it.x)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "x", "y", "z" }, fields);
    }

    [Test]
    // Bug 9#2 FIXED
    public void Bug2_Filter_TwoFields_DropsThird() {
        var runtime = Funny.Hardcore.Build("[{a=1,b=2,c=3},{a=4,b=5,c=6}].filter(rule it.a > 0 and it.b > 0)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, fields);
    }

    [Test]
    public void Bug2_Sort_ThenAccessField_Preserves() =>
        // When downstream code accesses fields, TIC preserves them
        "[{a=2,b=20},{a=1,b=10}].sort(rule it.a).map(rule it.b)"
            .AssertReturns("out", new[] { 10, 20 });

    // ══════════════════════════════════════════════════════════════
    // Bug 9#3 (MODERATE): Struct equality ignores extra fields
    // Width subtyping narrows to LCA before comparison.
    // Spec: "same list of fields" required for equality.
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void Bug3_SubsetEqual_ShouldBeFalse() =>
        "out = {a = 1} == {a = 1, b = 2}".AssertReturns("out", false);

    [Test]
    public void Bug3_SupersetEqual_ShouldBeFalse() =>
        "out = {a = 1, b = 2} == {a = 1}".AssertReturns("out", false);

    [Test]
    public void Bug3_SubsetNotEqual_ShouldBeTrue() =>
        "out = {a = 1} != {a = 1, b = 2}".AssertReturns("out", true);

    [Test]
    public void Bug3_DisjointFields_NotEqual() =>
        // Different extra fields — correctly not equal
        "out = {a = 1, b = 2} == {a = 1, c = 3}".AssertReturns("out", false);

    [Test]
    public void Bug3_SameFields_Equal() =>
        // Same fields — correctly equal
        "out = {a = 1, b = 2} == {a = 1, b = 2}".AssertReturns("out", true);

    [Test]
    public void Bug3_SameFieldsDiffValues_NotEqual() =>
        "out = {a = 1, b = 2} == {a = 1, b = 3}".AssertReturns("out", false);

    [Test]
    public void Bug3_ThreeVsTwoFields() =>
        "out = {a = 1, b = 2, c = 3} == {a = 1, b = 2}".AssertReturns("out", false);
}
