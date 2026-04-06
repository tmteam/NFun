using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

/// <summary>
/// Tests for the "stale snapshot" bug family.
///
/// Root cause: ConstraintsState.AddDescendant() calls Concretest() which
/// snapshots types before PullNoneNode Phase 2 wraps them in Optional.
/// The stale snapshot propagates up through LCA, causing type errors
/// when none and concrete values are in DIFFERENT containers that get LCA'd.
///
/// Note: mixing none with concrete in a SINGLE container works fine
/// (e.g. [1, none] -> int?[]), because PullNoneNode wraps the element
/// before the container's element type is snapshotted. The bug only
/// manifests when two separate containers are LCA'd (if-else branches,
/// array literal elements).
/// </summary>
[TestFixture]
class StaleSnapshotTests {

    // ---------------------------------------------------------------
    // Bug 1: if(true) [1] else [none]
    //
    // LCA of int[] vs none[]. The [none] array's element gets wrapped
    // in Optional by PullNoneNode Phase 2, but AddDescendant already
    // snapshotted the element type before wrapping.
    //
    // Graph:
    //   0: true (Bool)
    //   1: 1 (IntConst U8)
    //   2: [1] = arr(node1)            -- StrictArrayInit
    //   3: none (None)
    //   4: [none] = arr(node3)         -- StrictArrayInit
    //   5: if(0) then 2 else 4         -- IfElse
    //   y = node5
    //
    // Expected: y = opt(int)[].  Each branch produces an array,
    // elements are LCA'd: LCA(int, none) = opt(int), so result is opt(int)[].
    // ---------------------------------------------------------------
    [Test]
    public void IfElse_ArrayInt_vs_ArrayNone() {
        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetIntConst(1, U8);
        graph.SetStrictArrayInit(2, 1);
        graph.SetConst(3, None);
        graph.SetStrictArrayInit(4, 3);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        // y should be an array of opt(T) where T is in [U8..Real]
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State,
            "y should be an array type");
        var elemState = ((StateArray)yNode.State).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateOptional>(elemState,
            "Array element should be Optional (LCA of int and none)");
    }

    // ---------------------------------------------------------------
    // Bug 2: if(true) {a=1} else {a=none}
    //
    // LCA of struct fields: field 'a' is int in one branch, none in the other.
    // AddDescendant snapshots the field type before PullNoneNode wraps it.
    //
    // Graph:
    //   0: true (Bool)
    //   1: 1 (IntConst U8)
    //   2: {a=node1}                   -- StructInit
    //   3: none (None)
    //   4: {a=node3}                   -- StructInit
    //   5: if(0) then 2 else 4         -- IfElse
    //   y = node5
    //
    // Expected: y = {a: opt(int)}. Field 'a' LCA is LCA(int, none) = opt(int).
    // ---------------------------------------------------------------
    [Test]
    public void IfElse_StructFieldInt_vs_StructFieldNone() {
        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetIntConst(1, U8);
        graph.SetStructInit(new[] { "a" }, new[] { 1 }, 2);
        graph.SetConst(3, None);
        graph.SetStructInit(new[] { "a" }, new[] { 3 }, 4);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        // y should be a struct with field 'a' of type opt(T) where T in [U8..Real]
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateStruct>(yNode.State,
            "y should be a struct type");
        var fieldNode = ((StateStruct)yNode.State).GetFieldOrNull("a").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(fieldNode.State,
            "Field 'a' should be Optional (LCA of int and none)");
    }

    // ---------------------------------------------------------------
    // Bug 3: [{a=1},{a=none}]
    //
    // Array of structs where one struct has a concrete field and the other
    // has none. Array init LCAs the element types (the structs), which
    // in turn LCAs the struct fields.
    //
    // Graph:
    //   0: 1 (IntConst U8)
    //   1: {a=node0}                   -- StructInit
    //   2: none (None)
    //   3: {a=node2}                   -- StructInit
    //   4: [node1, node3]              -- StrictArrayInit
    //   y = node4
    //
    // Expected: y = {a: opt(int)}[]. Each element struct's 'a' field
    // gets LCA'd: LCA(int, none) = opt(int).
    // ---------------------------------------------------------------
    [Test]
    public void ArrayLiteral_StructsWithNoneField() {
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetConst(2, None);
        graph.SetStructInit(new[] { "a" }, new[] { 2 }, 3);
        graph.SetStrictArrayInit(4, 1, 3);
        graph.SetDef("y", 4);

        var result = graph.Solve();

        // y should be an array of struct with optional field
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State,
            "y should be an array type");
        var elemNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateStruct>(elemNode.State,
            "Array element should be a struct");
        var fieldNode = ((StateStruct)elemNode.State).GetFieldOrNull("a").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(fieldNode.State,
            "Struct field 'a' should be Optional (LCA of int and none)");
    }

    // ---------------------------------------------------------------
    // Bug 4: if(true) [[1]] else [[none]]
    //
    // Nested arrays: the outer if-else LCAs two nested arrays.
    // Inner arrays are [1] (int[]) vs [none] (none[]).
    // LCA should produce opt(int)[][].
    //
    // Graph:
    //   0: true (Bool)
    //   1: 1 (IntConst U8)
    //   2: [node1]                     -- inner array [1]
    //   3: [node2]                     -- outer array [[1]]
    //   4: none (None)
    //   5: [node4]                     -- inner array [none]
    //   6: [node5]                     -- outer array [[none]]
    //   7: if(0) then 3 else 6         -- IfElse
    //   y = node7
    //
    // Expected: y = opt(int)[][]. The innermost element LCA
    // is LCA(int, none) = opt(int), so inner arrays are opt(int)[],
    // and outer is opt(int)[][].
    // ---------------------------------------------------------------
    [Test]
    public void IfElse_NestedArrayInt_vs_NestedArrayNone() {
        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetIntConst(1, U8);
        graph.SetStrictArrayInit(2, 1);       // [1]
        graph.SetStrictArrayInit(3, 2);       // [[1]]
        graph.SetConst(4, None);
        graph.SetStrictArrayInit(5, 4);       // [none]
        graph.SetStrictArrayInit(6, 5);       // [[none]]
        graph.SetIfElse(new[] { 0 }, new[] { 3, 6 }, 7);
        graph.SetDef("y", 7);

        var result = graph.Solve();

        // y should be array of array of opt(T)
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State,
            "y should be an array type (outer)");
        var innerArrayNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateArray>(innerArrayNode.State,
            "Outer element should be an array type (inner)");
        var innerElemState = ((StateArray)innerArrayNode.State).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateOptional>(innerElemState,
            "Inner array element should be Optional (LCA of int and none)");
    }

    // ---------------------------------------------------------------
    // Bug 5: [1,2,3].map(rule if(it>1) [it] else [none])
    //
    // Lambda returns either [it] (int[]) or [none] (none[]).
    // The if-else inside the lambda LCAs the two branches.
    //
    // Graph (simplified — lambda body):
    //   Lambda arg "it" is fed from map's input array element.
    //   0: it (Var, comparable for >)
    //   1: 1 (IntConst U8)
    //   2: it > 1 → Bool (Comparable call)
    //   3: it (Var)
    //   4: [node3]                     -- [it]
    //   5: none (None)
    //   6: [node5]                     -- [none]
    //   7: if(2) then 4 else 6         -- IfElse (lambda body result)
    //   8: lambda(7, "it")
    //
    //   Input array:
    //   9:  1 (IntConst U8)
    //   10: 2 (IntConst U8)
    //   11: 3 (IntConst U8)
    //   12: [9,10,11]                  -- [1,2,3]
    //   13: map(12, 8, result)
    //   y = node13
    //
    // Expected: y = opt(int)[][]. map produces an array of the lambda return
    // type, and the lambda returns opt(int)[] (from LCA of [int] and [none]).
    // ---------------------------------------------------------------
    [Test]
    public void Map_LambdaIfElse_ArrayInt_vs_ArrayNone() {
        var graph = new GraphBuilder();

        // Input array: [1, 2, 3]
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, U8);
        graph.SetIntConst(2, U8);
        graph.SetSoftArrayInit(3, 0, 1, 2);    // [1,2,3]

        // Lambda body: if(it > 1) [it] else [none]
        graph.SetVar("it", 4);                   // it (for comparison)
        graph.SetIntConst(5, U8);                // 1
        graph.SetComparable(4, 5, 6);            // it > 1 → Bool (node 6)

        graph.SetVar("it", 7);                   // it (for array)
        graph.SetSoftArrayInit(8, 7);            // [it]

        graph.SetConst(9, None);
        graph.SetSoftArrayInit(10, 9);           // [none]

        graph.SetIfElse(new[] { 6 }, new[] { 8, 10 }, 11); // if-else → node 11

        graph.CreateLambda(11, 12, "it");         // lambda → node 12

        // map([1,2,3], lambda) → result
        graph.SetMap(3, 12, 13);
        graph.SetDef("y", 13);

        var result = graph.Solve();

        // y should be array of arrays with optional elements
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State,
            "y should be an array type (map result)");
        var innerArrayNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateArray>(innerArrayNode.State,
            "Map result element should be an array (lambda returns array)");
        var innerElemState = ((StateArray)innerArrayNode.State).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateOptional>(innerElemState,
            "Inner array element should be Optional (LCA of int and none in lambda)");
    }

    // ---------------------------------------------------------------
    // Bug 6: [[1,2,3],[none]]
    //
    // Partial array with none: the outer array has two elements:
    //   [1,2,3] (int[]) and [none] (none[]).
    // StrictArrayInit LCAs the element types.
    //
    // Graph:
    //   0: 1 (IntConst U8)
    //   1: 2 (IntConst U8)
    //   2: 3 (IntConst U8)
    //   3: [0,1,2]                     -- [1,2,3]
    //   4: none (None)
    //   5: [node4]                     -- [none]
    //   6: [node3, node5]              -- [[1,2,3],[none]]
    //   y = node6
    //
    // Expected: y = opt(int)[][].  The inner arrays get LCA'd:
    // LCA(int[], none[]) element-wise is LCA(int, none) = opt(int),
    // so inner arrays are opt(int)[], and outer is opt(int)[][].
    // ---------------------------------------------------------------
    [Test]
    public void ArrayLiteral_NestedIntArray_and_NoneArray() {
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, U8);
        graph.SetIntConst(2, U8);
        graph.SetSoftArrayInit(3, 0, 1, 2);    // [1,2,3]
        graph.SetConst(4, None);
        graph.SetSoftArrayInit(5, 4);            // [none]
        graph.SetSoftArrayInit(6, 3, 5);        // [[1,2,3],[none]]
        graph.SetDef("y", 6);

        var result = graph.Solve();

        // y should be array of array of opt(T)
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State,
            "y should be an array type (outer)");
        var innerArrayNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateArray>(innerArrayNode.State,
            "Outer element should be an array type (inner)");
        var innerElemState = ((StateArray)innerArrayNode.State).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateOptional>(innerElemState,
            "Inner array element should be Optional (LCA of int and none)");
    }
}
