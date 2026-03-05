using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// Invariant and boundary-condition tests for struct type inference.
/// Written test-first from the algorithm specification, NOT from the implementation.
///
/// Covers:
/// 1. Nested struct construction + field access
/// 2. Struct LCA in if-else and arrays
/// 3. Chained field access through multiple levels
/// 4. Struct with arrays inside fields
/// 5. Multiple references to the same struct variable
/// </summary>
public class StructInvariantsTest {
    // ================================================================
    // GROUP 1: Nested struct construction and field access
    // ================================================================

    [Test]
    public void NestedStructFieldAccess_ConstantFields() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   first = {b = 24i, c = 25i}
        //   second = {d = first}
        //   y:int = second.d.b
        //
        // Constraint graph:
        //
        //   [0:I32]─┐
        //   [1:I32]─┤
        //           ▼
        //   [2:{b=0,c=1}]───▶ first
        //                        │
        //   first ──▶[3]─┐      │(ancestor)
        //                ▼      │
        //   [4:{d=3}]────▶ second
        //                    │
        //   second──▶[5]     │(ancestor)
        //             │.d    │
        //            [6]     │
        //             │.b    │
        //            [7]───▶ y:I32
        //

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "b", "c" }, new[] { 0, 1 }, 2);
        graph.SetDef("first", 2);

        graph.SetVar("first", 3);
        graph.SetStructInit(new[] { "d" }, new[] { 3 }, 4);
        graph.SetDef("second", 4);

        graph.SetVar("second", 5);
        graph.SetFieldAccess(5, 6, "d");
        graph.SetFieldAccess(6, 7, "b");
        graph.SetVarType("y", I32);
        graph.SetDef("y", 7);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void NestedStructFieldAccess_TwoFieldsFromNested() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   first = {b = 24i, c = 25i}
        //   second = {d = first, e = first.c}
        //   y:int = second.d.b + second.e
        //
        // Constraint graph:
        //
        //   [0:I32]─┐
        //   [1:I32]─┤
        //           ▼
        //   [2:{b=0,c=1}]──▶ first
        //                      │
        //   first──▶[3].c──▶[4]──┐
        //   first──▶[5]──────────┤
        //                        ▼
        //   [6:{d=5,e=4}]──▶ second
        //                      │
        //   second──▶[7].d──▶[8].b──▶[9]───┐ arith
        //   second──▶[10].e──▶[11]──────────┤
        //                                   ▼
        //                     [12]──▶ y

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "b", "c" }, new[] { 0, 1 }, 2);
        graph.SetDef("first", 2);

        graph.SetVar("first", 3);
        graph.SetFieldAccess(3, 4, "c");

        graph.SetVar("first", 5);
        graph.SetStructInit(new[] { "d", "e" }, new[] { 5, 4 }, 6);
        graph.SetDef("second", 6);

        graph.SetVar("second", 7);
        graph.SetFieldAccess(7, 8, "d");
        graph.SetFieldAccess(8, 9, "b");

        graph.SetVar("second", 10);
        graph.SetFieldAccess(10, 11, "e");

        graph.SetArith(9, 11, 12);
        graph.SetDef("y", 12);

        var result = graph.Solve();
        result.AssertNoGenerics();
    }

    [Test]
    public void TripleNestedStructFieldAccess() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   a = {x = 1i}
        //   b = {y = a}
        //   c = {z = b}
        //   out:int = c.z.y.x
        //
        // Constraint graph:
        //
        //   [0:I32]──▶[1:{x=0}]──▶ a
        //                            │
        //   a──▶[2]──▶[3:{y=2}]──▶ b
        //                            │
        //   b──▶[4]──▶[5:{z=4}]──▶ c
        //                            │
        //   c──▶[6].z──▶[7].y──▶[8].x──▶[9]──▶ out:I32

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] { "x" }, new[] { 0 }, 1);
        graph.SetDef("a", 1);

        graph.SetVar("a", 2);
        graph.SetStructInit(new[] { "y" }, new[] { 2 }, 3);
        graph.SetDef("b", 3);

        graph.SetVar("b", 4);
        graph.SetStructInit(new[] { "z" }, new[] { 4 }, 5);
        graph.SetDef("c", 5);

        graph.SetVar("c", 6);
        graph.SetFieldAccess(6, 7, "z");
        graph.SetFieldAccess(7, 8, "y");
        graph.SetFieldAccess(8, 9, "x");
        graph.SetVarType("out", I32);
        graph.SetDef("out", 9);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "out");
    }

    [Test]
    public void NestedStructWithGenericConstants() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   a = {b = 24, c = 25}   (generic int consts [U8..Real] pref I32)
        //   second = {d = a}
        //   y = second.d.b

        var graph = new GraphBuilder();
        graph.SetGenericConst(0, U8, Real, I32);
        graph.SetGenericConst(1, U8, Real, I32);
        graph.SetStructInit(new[] { "b", "c" }, new[] { 0, 1 }, 2);
        graph.SetDef("a", 2);

        graph.SetVar("a", 3);
        graph.SetStructInit(new[] { "d" }, new[] { 3 }, 4);
        graph.SetDef("second", 4);

        graph.SetVar("second", 5);
        graph.SetFieldAccess(5, 6, "d");
        graph.SetFieldAccess(6, 7, "b");
        graph.SetDef("y", 7);

        var result = graph.Solve();
        // y = second.d.b  where b is a generic const [U8..Re]I32!
        // Output generics are resolved by the runtime, not TIC solver
        // (same as simple "y = 24" staying generic at TIC level).
        // Here we verify the graph solves and y has correct constraints.
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<ConstraintsState>(yState);
        var c = (ConstraintsState)yState;
        Assert.AreEqual(U8, c.Descendant);
        Assert.AreEqual(Real, c.Ancestor);
        Assert.AreEqual(I32, c.Preferred);
    }

    [Test]
    public void GenericConst_OutputStaysGenericAtTicLevel() {
        using var _ = TraceLog.Scope;
        // y = 24 — generic const [U8..Re] preferred I32.
        // TIC solver keeps output generics unresolved; the runtime resolves them.
        var graph = new GraphBuilder();
        graph.SetGenericConst(0, U8, Real, I32);
        graph.SetDef("y", 0);

        var result = graph.Solve();

        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<ConstraintsState>(yState);
        var c = (ConstraintsState)yState;
        Assert.AreEqual(U8, c.Descendant);
        Assert.AreEqual(Real, c.Ancestor);
        Assert.AreEqual(I32, c.Preferred);
    }

    // ================================================================
    // GROUP 2: Multiple references to the same struct
    // ================================================================

    [Test]
    public void TwinAccessToSameNestedField() {
        //
        // Script:
        //   a = {f = 1i}
        //   y:int = a.f + a.f
        //
        // Graph:
        //
        //   [0:I32]──▶[1:{f=0}]──▶ a
        //                            │
        //   a──▶[2].f──▶[3]─────────┤ arith
        //   a──▶[4].f──▶[5]─────────┤
        //                            ▼
        //                  [6]──▶ y:I32

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] { "f" }, new[] { 0 }, 1);
        graph.SetDef("a", 1);

        graph.SetVar("a", 2);
        graph.SetFieldAccess(2, 3, "f");

        graph.SetVar("a", 4);
        graph.SetFieldAccess(4, 5, "f");

        graph.SetArith(3, 5, 6);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 6);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void StructUsedInTwoNestings() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   a1 = {af = 24i, ag = 1i}
        //   b2 = {bf = a1, bg = a1.ag}
        //   y:int = a1.af + b2.bf.ag + b2.bg
        //
        // Graph (simplified):
        //
        //   [0:I32]─┐ [1:I32]─┐
        //            ▼         ▼
        //   [2:{af=0, ag=1}]──▶ a1
        //                        │
        //   a1──▶[3].ag──▶[4]───┤
        //   a1──▶[5]─────────────┤
        //                        ▼
        //   [6:{bf=5, bg=4}]──▶ b2
        //
        //   a1──▶[7].af──▶[8]─────────┐
        //   b2──▶[9].bf──▶[10].ag──▶[11]──┤ arith
        //   b2──▶[12].bg──▶[13]────────────┤ arith
        //                                  ▼
        //                        [15]──▶ y:I32

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "af", "ag" }, new[] { 0, 1 }, 2);
        graph.SetDef("a1", 2);

        graph.SetVar("a1", 3);
        graph.SetFieldAccess(3, 4, "ag");

        graph.SetVar("a1", 5);
        graph.SetStructInit(new[] { "bf", "bg" }, new[] { 5, 4 }, 6);
        graph.SetDef("b2", 6);

        graph.SetVar("a1", 7);
        graph.SetFieldAccess(7, 8, "af");

        graph.SetVar("b2", 9);
        graph.SetFieldAccess(9, 10, "bf");
        graph.SetFieldAccess(10, 11, "ag");

        graph.SetVar("b2", 12);
        graph.SetFieldAccess(12, 13, "bg");

        graph.SetArith(8, 11, 14);
        graph.SetArith(14, 13, 15);

        graph.SetVarType("y", I32);
        graph.SetDef("y", 15);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ================================================================
    // GROUP 3: Struct with array fields
    // ================================================================

    [Test]
    public void StructWithArrayField_NestedAccess() {
        //
        // Script:
        //   a = {b = [1i, 2i, 3i]}
        //   y:int = a.b[1]
        //
        // Graph:
        //
        //   [0:I32] [1:I32] [2:I32]
        //       │      │      │
        //       ▼      ▼      ▼
        //   [3: arr(V0)]──▶[4:{b=3}]──▶ a
        //                                │
        //   a──▶[5].b──▶[6]             │
        //                │               │
        //        [7:I32] │ arrGet        │
        //           │    ▼               │
        //          [8]──▶ y:I32

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetConst(2, I32);
        graph.SetSoftArrayInit(3, 0, 1, 2);
        graph.SetStructInit(new[] { "b" }, new[] { 3 }, 4);
        graph.SetDef("a", 4);

        graph.SetVar("a", 5);
        graph.SetFieldAccess(5, 6, "b");

        graph.SetConst(7, I32);
        graph.SetArrGetCall(6, 7, 8);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 8);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void DoubleNestedStructWithArrayField() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   a = {b = [1i]}
        //   d = {f = a}
        //   y:int = d.f.b[0]
        //
        // Graph:
        //
        //   [0:I32]──▶[1:arr(V0)]──▶[2:{b=1}]──▶ a
        //                                          │
        //   a──▶[3]──▶[4:{f=3}]──▶ d              │
        //                            │              │
        //   d──▶[5].f──▶[6].b──▶[7]               │
        //                         │                 │
        //             [8:I32]     │ arrGet
        //                │        ▼
        //               [9]──▶ y:I32

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetSoftArrayInit(1, 0);
        graph.SetStructInit(new[] { "b" }, new[] { 1 }, 2);
        graph.SetDef("a", 2);

        graph.SetVar("a", 3);
        graph.SetStructInit(new[] { "f" }, new[] { 3 }, 4);
        graph.SetDef("d", 4);

        graph.SetVar("d", 5);
        graph.SetFieldAccess(5, 6, "f");
        graph.SetFieldAccess(6, 7, "b");

        graph.SetConst(8, I32);
        graph.SetArrGetCall(7, 8, 9);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 9);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ================================================================
    // GROUP 4: Struct LCA in if-else
    // ================================================================

    [Test]
    public void IfElse_SameStructType_PreservesAllFields() {
        //
        // Script:
        //   y = if(true) {age=1i, name=true} else {age=2i, name=false}
        //
        // Both branches identical field sets => result preserves all fields
        //
        // Graph:
        //
        //   [0:Bool]
        //       │ cond
        //   [1:I32][2:Bool]──▶[3:{age=1,name=2}]──┐ if-else
        //   [4:I32][5:Bool]──▶[6:{age=4,name=5}]──┤
        //                                          ▼
        //                                    [7]──▶ y

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);

        graph.SetConst(1, I32);
        graph.SetConst(2, Bool);
        graph.SetStructInit(new[] { "age", "name" }, new[] { 1, 2 }, 3);

        graph.SetConst(4, I32);
        graph.SetConst(5, Bool);
        graph.SetStructInit(new[] { "age", "name" }, new[] { 4, 5 }, 6);

        graph.SetIfElse(new[] { 0 }, new[] { 3, 6 }, 7);
        graph.SetDef("y", 7);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.IsNotNull(y, "y should be a struct");
        Assert.AreEqual(2, y.Fields.Count());
        Assert.IsNotNull(y.GetFieldOrNull("age"));
        Assert.IsNotNull(y.GetFieldOrNull("name"));
    }

    [Test]
    public void IfElse_DifferentFieldSets_OnlyCommonFieldsSurvive() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   y = if(true) {age=1i, name=true} else {age=2i, size=3i}
        //
        // LCA: only field 'age' is common and has same type (I32)
        //
        // Graph:
        //
        //   [0:Bool]
        //   [1:I32][2:Bool]──▶[3:{age=1,name=2}]───┐ if-else
        //   [4:I32][5:I32]───▶[6:{age=4,size=5}]───┤
        //                                           ▼
        //                                     [7]──▶ y
        //
        // Expected: y = {age:I32} (1 field)

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);

        graph.SetConst(1, I32);
        graph.SetConst(2, Bool);
        graph.SetStructInit(new[] { "age", "name" }, new[] { 1, 2 }, 3);

        graph.SetConst(4, I32);
        graph.SetConst(5, I32);
        graph.SetStructInit(new[] { "age", "size" }, new[] { 4, 5 }, 6);

        graph.SetIfElse(new[] { 0 }, new[] { 3, 6 }, 7);
        graph.SetDef("y", 7);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.IsNotNull(y, "y should be a struct");
        Assert.AreEqual(1, y.Fields.Count());
        Assert.IsNotNull(y.GetFieldOrNull("age"));
    }

    [Test]
    public void IfElse_NoCommonFields_EmptyStruct() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   y = if(true) {a=1i} else {b=2i}
        //
        // No common fields => LCA = empty struct {}

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);

        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 1 }, 2);

        graph.SetConst(3, I32);
        graph.SetStructInit(new[] { "b" }, new[] { 3 }, 4);

        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.IsNotNull(y, "y should be a struct");
        Assert.AreEqual(0, y.Fields.Count());
    }

    [Test]
    public void IfElse_CommonFieldWithDifferentConcreteTypes() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   y = if(true) {age = 1i32} else {age = 1.0real}
        //
        // Struct fields are COVARIANT (immutable struct).
        // LCA(I32, Real) = Real => {age:Real}

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);

        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 1 }, 2);

        graph.SetConst(3, Real);
        graph.SetStructInit(new[] { "age" }, new[] { 3 }, 4);

        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.IsNotNull(y, "y should be a struct");
        Assert.AreEqual(1, y.Fields.Count());
        Assert.AreEqual(Real, y.GetFieldOrNull("age").State);
    }

    [Test]
    public void IfElse_StructWithAccessToCommonField() {
        //
        // Script:
        //   x = if(true) {age=42i, size=100i} else {age=42i}
        //   out:int = x.age
        //
        // LCA => {age:I32}. Accessing 'age' should work.

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);

        graph.SetConst(1, I32);
        graph.SetConst(2, I32);
        graph.SetStructInit(new[] { "age", "size" }, new[] { 1, 2 }, 3);

        graph.SetConst(4, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 4 }, 5);

        graph.SetIfElse(new[] { 0 }, new[] { 3, 5 }, 6);
        graph.SetDef("x", 6);

        graph.SetVar("x", 7);
        graph.SetFieldAccess(7, 8, "age");
        graph.SetVarType("out", I32);
        graph.SetDef("out", 8);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "out");
    }

    [Test]
    public void IfElse_ThreeBranches_StructLca() {
        //
        // Script:
        //   x = if(c1) {a=1i, b=2i, c=3i}
        //       else if(c2) {a=4i, c=5i}
        //       else {a=6i}
        //   out:int = x.a
        //
        // LCA of 3 branches: only 'a' is common to all

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetConst(1, Bool);

        graph.SetConst(10, I32);
        graph.SetConst(11, I32);
        graph.SetConst(12, I32);
        graph.SetStructInit(new[] { "a", "b", "c" }, new[] { 10, 11, 12 }, 13);

        graph.SetConst(20, I32);
        graph.SetConst(21, I32);
        graph.SetStructInit(new[] { "a", "c" }, new[] { 20, 21 }, 22);

        graph.SetConst(30, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 30 }, 31);

        graph.SetIfElse(new[] { 0, 1 }, new[] { 13, 22, 31 }, 40);
        graph.SetDef("x", 40);

        graph.SetVar("x", 41);
        graph.SetFieldAccess(41, 42, "a");
        graph.SetVarType("out", I32);
        graph.SetDef("out", 42);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "out");
    }

    // ================================================================
    // GROUP 5: Struct LCA in arrays
    // ================================================================

    [Test]
    public void Array_StructsWithSameFields_ElementHasAllFields() {
        //
        // Script:
        //   y = [{age=1i, name=true}, {age=2i, name=false}]
        //
        // Both elements identical field sets => element type has both fields

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Bool);
        graph.SetStructInit(new[] { "age", "name" }, new[] { 0, 1 }, 2);

        graph.SetConst(3, I32);
        graph.SetConst(4, Bool);
        graph.SetStructInit(new[] { "age", "name" }, new[] { 3, 4 }, 5);

        graph.SetSoftArrayInit(6, 2, 5);
        graph.SetDef("y", 6);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        Assert.IsNotNull(y);
        var elem = y.Element as StateStruct;
        Assert.IsNotNull(elem);
        Assert.AreEqual(2, elem.Fields.Count());
    }

    [Test]
    public void Array_StructsWithDifferentFields_ElementHasOnlyCommon() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   y = [{age=1i, name=true}, {age=2i, size=3i}]
        //
        // LCA element: only 'age' is common (and same type I32)

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Bool);
        graph.SetStructInit(new[] { "age", "name" }, new[] { 0, 1 }, 2);

        graph.SetConst(3, I32);
        graph.SetConst(4, I32);
        graph.SetStructInit(new[] { "age", "size" }, new[] { 3, 4 }, 5);

        graph.SetSoftArrayInit(6, 2, 5);
        graph.SetDef("y", 6);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        Assert.IsNotNull(y);
        var elem = y.Element as StateStruct;
        Assert.IsNotNull(elem);
        Assert.AreEqual(1, elem.Fields.Count());
        Assert.IsNotNull(elem.GetFieldOrNull("age"));
    }

    [Test]
    public void Array_ThreeStructs_LcaKeepsOnlyUniversalFields() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   y = [{a=1i,b=2i,c=3i}, {a=4i,c=5i}, {a=6i}]
        //
        // Only 'a' is common to all three elements

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetConst(2, I32);
        graph.SetStructInit(new[] { "a", "b", "c" }, new[] { 0, 1, 2 }, 3);

        graph.SetConst(4, I32);
        graph.SetConst(5, I32);
        graph.SetStructInit(new[] { "a", "c" }, new[] { 4, 5 }, 6);

        graph.SetConst(7, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 7 }, 8);

        graph.SetSoftArrayInit(9, 3, 6, 8);
        graph.SetDef("y", 9);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        Assert.IsNotNull(y);
        var elem = y.Element as StateStruct;
        Assert.IsNotNull(elem);
        Assert.AreEqual(1, elem.Fields.Count());
        Assert.IsNotNull(elem.GetFieldOrNull("a"));
    }

    [Test]
    public void Array_StructsThenFieldAccess() {
        //
        // Script:
        //   arr = [{age=42i}, {age=42i, size=15i}]
        //   out:int = arr[0].age
        //
        // LCA element = {age:I32}. Access .age => I32.

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 0 }, 1);

        graph.SetConst(2, I32);
        graph.SetConst(3, I32);
        graph.SetStructInit(new[] { "age", "size" }, new[] { 2, 3 }, 4);

        graph.SetSoftArrayInit(5, 1, 4);
        graph.SetDef("arr", 5);

        graph.SetVar("arr", 6);
        graph.SetConst(7, I32);
        graph.SetArrGetCall(6, 7, 8);

        graph.SetFieldAccess(8, 9, "age");
        graph.SetVarType("out", I32);
        graph.SetDef("out", 9);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "out");
    }

    // ================================================================
    // GROUP 6: Function LCA with struct arguments
    // ================================================================

    [Test]
    public void FunLca_TwoRulesWithDifferentStructFields() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   f1 = rule it.age + it.size    (needs {age, size})
        //   f2 = rule it.age              (needs {age})
        //   f3 = if(true) f1 else f2
        //   out = f3({age=42, size=15})
        //
        // f3 arg type = GCD({age,size}, {age}) = {age,size}
        // f3 ret type = LCA(arith, arith)

        var graph = new GraphBuilder();

        graph.SetVar("it1", 0);
        graph.SetFieldAccess(0, 1, "age");
        graph.SetVar("it1", 2);
        graph.SetFieldAccess(2, 3, "size");
        graph.SetArith(1, 3, 4);
        graph.CreateLambda(4, 5, "it1");

        graph.SetVar("it2", 6);
        graph.SetFieldAccess(6, 7, "age");
        graph.CreateLambda(7, 8, "it2");

        graph.SetConst(9, Bool);
        graph.SetIfElse(new[] { 9 }, new[] { 5, 8 }, 10);
        graph.SetDef("f3", 10);

        graph.SetConst(11, I32);
        graph.SetConst(12, I32);
        graph.SetStructInit(new[] { "age", "size" }, new[] { 11, 12 }, 13);

        graph.SetVar("f3", 14);
        graph.SetCall(14, new[] { 13, 15 });
        graph.SetDef("out", 15);

        var result = graph.Solve();
        result.AssertNoGenerics();
    }

    // ================================================================
    // GROUP 7: Struct field access on variable (not constructed)
    // ================================================================

    [Test]
    public void FieldAccessOnInputVariable_InfersStructType() {
        //
        // Script:
        //   y:int = a.name + a.age
        //
        // 'a' not constructed => inferred as {name:?, age:?}

        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetFieldAccess(0, 1, "name");

        graph.SetVar("a", 2);
        graph.SetFieldAccess(2, 3, "age");

        graph.SetArith(1, 3, 4);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");

        var a = result.GetVariableNode("a").State as StateStruct;
        Assert.IsNotNull(a, "a should be inferred as struct");
        Assert.AreEqual(2, a.Fields.Count());
        Assert.IsNotNull(a.GetFieldOrNull("name"));
        Assert.IsNotNull(a.GetFieldOrNull("age"));
    }

    // ================================================================
    // GROUP 8: Edge cases
    // ================================================================

    [Test]
    public void EmptyStructInit() {
        // y = {}
        var graph = new GraphBuilder();
        graph.SetStructInit(new string[] { }, new int[] { }, 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.IsNotNull(y);
        Assert.AreEqual(0, y.Fields.Count());
    }

    [Test]
    public void StructFieldAccessThenArrayGet_ConcreteResult() {
        using var _ = TraceLog.Scope;
        //
        // Script:
        //   inner = {arr = [true, false]}
        //   outer = {nested = inner}
        //   y:bool = outer.nested.arr[0]

        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetConst(1, Bool);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetStructInit(new[] { "arr" }, new[] { 2 }, 3);
        graph.SetDef("inner", 3);

        graph.SetVar("inner", 4);
        graph.SetStructInit(new[] { "nested" }, new[] { 4 }, 5);
        graph.SetDef("outer", 5);

        graph.SetVar("outer", 6);
        graph.SetFieldAccess(6, 7, "nested");
        graph.SetFieldAccess(7, 8, "arr");

        graph.SetConst(9, I32);
        graph.SetArrGetCall(8, 9, 10);
        graph.SetVarType("y", Bool);
        graph.SetDef("y", 10);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    [Test]
    public void StructFieldAccess_SameFieldNameDifferentLevels() {
        //
        // Script:
        //   y = {f = {f = {f = 2.0}}}.f.f.f

        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetStructInit(new[] { "f" }, new[] { 0 }, 1);
        graph.SetStructInit(new[] { "f" }, new[] { 1 }, 2);
        graph.SetStructInit(new[] { "f" }, new[] { 2 }, 3);

        graph.SetFieldAccess(3, 4, "f");
        graph.SetFieldAccess(4, 5, "f");
        graph.SetFieldAccess(5, 6, "f");
        graph.SetDef("y", 6);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }

    // ================================================================
    // GROUP 9: Negative tests — should fail type inference
    // ================================================================

    [Test]
    public void AccessNonExistentFieldAfterLca_ShouldFail() {
        //
        // Script:
        //   arr = [{age=42i}, {age=42i, size=15i}]
        //   out:int = arr[0].size
        //
        // LCA element = {age:I32}. 'size' not in LCA => should fail.

        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();

            graph.SetConst(0, I32);
            graph.SetStructInit(new[] { "age" }, new[] { 0 }, 1);

            graph.SetConst(2, I32);
            graph.SetConst(3, I32);
            graph.SetStructInit(new[] { "age", "size" }, new[] { 2, 3 }, 4);

            graph.SetSoftArrayInit(5, 1, 4);
            graph.SetDef("arr", 5);

            graph.SetVar("arr", 6);
            graph.SetConst(7, I32);
            graph.SetArrGetCall(6, 7, 8);

            graph.SetFieldAccess(8, 9, "size");
            graph.SetVarType("out", I32);
            graph.SetDef("out", 9);

            graph.Solve();
        });
    }
}
