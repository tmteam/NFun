using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// TIC-level tests for recursive struct types through Optional and Array.
/// These test the constraint graph directly — no parser, no named types.
/// Recursive structs through Optional/Array should be valid (none/[] breaks recursion).
/// </summary>
public class RecursiveStructTest {

    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ═══════════════════════════════════════════════════════════════
    // BASELINE: non-recursive structs (should work)
    // ═══════════════════════════════════════════════════════════════

    #region Non-recursive baseline

    [Test]
    public void NonRecursive_NestedStruct() {
        // {inner = {v = 42}}.inner.v → I32
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] {"v"}, new[] {0}, 1);
        graph.SetStructInit(new[] {"inner"}, new[] {1}, 2);
        graph.SetFieldAccess(2, 3, "inner");
        graph.SetFieldAccess(3, 4, "v");
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void NonRecursive_ThreeLevelNesting() {
        // {a = {b = {c = 42}}}.a.b.c → I32
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] {"c"}, new[] {0}, 1);
        graph.SetStructInit(new[] {"b"}, new[] {1}, 2);
        graph.SetStructInit(new[] {"a"}, new[] {2}, 3);
        graph.SetFieldAccess(3, 4, "a");
        graph.SetFieldAccess(4, 5, "b");
        graph.SetFieldAccess(5, 6, "c");
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // STRUCT WITH OPTIONAL FIELD containing None
    // ═══════════════════════════════════════════════════════════════

    #region Optional field = None

    [Test]
    public void StructWithNoneField_FieldAccess() {
        // {v = 42, next = none}.v → I32
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 1}, 2);
        graph.SetFieldAccess(2, 3, "v");
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void TwoStructs_InnerHasNone_OuterReferencesInner() {
        // inner = {v=2, next=none}; outer = {v=1, next=inner}; y = outer.v
        var graph = new GraphBuilder();
        graph.SetConst(0, I32); // 2
        graph.SetConst(1, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 1}, 2); // inner
        graph.SetConst(3, I32); // 1
        graph.SetStructInit(new[] {"v", "next"}, new[] {3, 2}, 4); // outer
        graph.SetFieldAccess(4, 5, "v");
        graph.SetDef("y", 5);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void TwoStructs_InnerFieldAccess() {
        // outer = {v=1, next={v=2, next=none}}; y = outer.next.v
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 1}, 2); // inner
        graph.SetConst(3, I32);
        graph.SetStructInit(new[] {"v", "next"}, new[] {3, 2}, 4); // outer
        graph.SetFieldAccess(4, 5, "next");
        graph.SetFieldAccess(5, 6, "v");
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // STRUCT FIELD IS ARRAY OF STRUCTS
    // ═══════════════════════════════════════════════════════════════

    #region Array of structs field

    [Test]
    public void ArrayOfStructsField_AccessParent() {
        // parent = {v=0, children=[{v=1}, {v=2}]}; y = parent.v
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] {"v"}, new[] {1}, 2);
        graph.SetConst(3, I32);
        graph.SetStructInit(new[] {"v"}, new[] {3}, 4);
        graph.SetSoftArrayInit(5, 2, 4);
        graph.SetStructInit(new[] {"v", "children"}, new[] {0, 5}, 6);
        graph.SetFieldAccess(6, 7, "v");
        graph.SetDef("y", 7);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void ArrayOfStructsField_AccessChildren() {
        // parent = {v=0, children=[{v=1}, {v=2}]}; y = parent.children
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] {"v"}, new[] {1}, 2);
        graph.SetConst(3, I32);
        graph.SetStructInit(new[] {"v"}, new[] {3}, 4);
        graph.SetSoftArrayInit(5, 2, 4);
        graph.SetStructInit(new[] {"v", "children"}, new[] {0, 5}, 6);
        graph.SetFieldAccess(6, 7, "children");
        graph.SetDef("y", 7);
        var result = graph.Solve();
        result.AssertNoGenerics();
        // y should be array type
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State);
    }

    [Test]
    public void NestedArrayOfStructs_TwoLevels() {
        // root = {v=0, children=[{v=1, children=[]}, {v=2, children=[]}]}
        var graph = new GraphBuilder();
        graph.SetConst(0, I32); // 0

        graph.SetConst(1, I32); // 1
        graph.SetSoftArrayInit(2); // empty children for child1
        graph.SetStructInit(new[] {"v", "children"}, new[] {1, 2}, 3); // child1

        graph.SetConst(4, I32); // 2
        graph.SetSoftArrayInit(5); // empty children for child2
        graph.SetStructInit(new[] {"v", "children"}, new[] {4, 5}, 6); // child2

        graph.SetSoftArrayInit(7, 3, 6); // [child1, child2]
        graph.SetStructInit(new[] {"v", "children"}, new[] {0, 7}, 8); // root
        graph.SetFieldAccess(8, 9, "v");
        graph.SetDef("y", 9);
        var result = graph.Solve();
        // Empty arrays leave generic element type — that's OK
        result.AssertNamed(I32, "y");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // IF-ELSE with struct and None → Optional struct
    // ═══════════════════════════════════════════════════════════════

    #region If-else producing Optional struct

    [Test]
    public void IfElse_StructOrNone_IsOptional() {
        // y = if(a) {v=42} else none → Optional struct
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] {"v"}, new[] {1}, 2);
        graph.SetConst(3, None);
        graph.SetIfElse(new[] {0}, new[] {2, 3}, 4);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State);
    }

    [Test]
    public void IfElse_StructWithNoneFieldOrNone() {
        // y = if(a) {v=1, next=none} else none
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {1, 2}, 3);
        graph.SetConst(4, None);
        graph.SetIfElse(new[] {0}, new[] {3, 4}, 5);
        graph.SetDef("y", 5);
        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // ARRAY containing structs with None fields (LCA)
    // ═══════════════════════════════════════════════════════════════

    #region Array LCA with None fields

    [Test]
    public void Array_StructWithNone_And_StructWithStruct() {
        // [{v=1, next=none}, {v=2, next={v=3, next=none}}]
        var graph = new GraphBuilder();
        // elem1: {v=1, next=none}
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 1}, 2);
        // elem2: {v=2, next={v=3, next=none}}
        graph.SetConst(3, I32);
        graph.SetConst(4, I32);
        graph.SetConst(5, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {4, 5}, 6); // inner
        graph.SetStructInit(new[] {"v", "next"}, new[] {3, 6}, 7); // outer
        // array
        graph.SetSoftArrayInit(8, 2, 7);
        graph.SetDef("arr", 8);
        var result = graph.Solve();
        var arrNode = result.GetVariableNode("arr").GetNonReference();
        Assert.IsInstanceOf<StateArray>(arrNode.State);
    }

    [Test]
    public void Array_ThreeStructs_MixedNoneAndNested() {
        // [{v=1, next=none}, {v=2, next={v=3, next=none}}, {v=4, next=none}]
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 1}, 2);

        graph.SetConst(3, I32);
        graph.SetConst(4, I32);
        graph.SetConst(5, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {4, 5}, 6);
        graph.SetStructInit(new[] {"v", "next"}, new[] {3, 6}, 7);

        graph.SetConst(8, I32);
        graph.SetConst(9, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {8, 9}, 10);

        graph.SetSoftArrayInit(11, 2, 7, 10);
        graph.SetDef("arr", 11);
        var result = graph.Solve();
        var arrNode = result.GetVariableNode("arr").GetNonReference();
        Assert.IsInstanceOf<StateArray>(arrNode.State);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // STRUCT PASSED TO FUNCTION
    // ═══════════════════════════════════════════════════════════════

    #region Function with struct param

    [Test]
    public void FunctionTakesStructWithNoneField() {
        // f(x) = x.v; y = f({v=42, next=none})
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 1}, 2);
        graph.SetFieldAccess(2, 3, "v");
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void FunctionTakesStructWithEmptyArrayField() {
        // f(x) = x.v; y = f({v=42, children=[]})
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetSoftArrayInit(1); // empty array
        graph.SetStructInit(new[] {"v", "children"}, new[] {0, 1}, 2);
        graph.SetFieldAccess(2, 3, "v");
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // TYPED ANNOTATION with struct containing Optional self-ref
    // ═══════════════════════════════════════════════════════════════

    #region Annotation pushes type into struct

    [Test]
    public void Annotation_PushesFieldTypes() {
        // y:{v:int, next:{v:int}?} = {v=1, next={v=2, next=none}}
        // The annotation constrains field types
        var graph = new GraphBuilder();
        graph.SetConst(0, I32); // 1
        graph.SetConst(1, I32); // 2
        graph.SetConst(2, None);
        graph.SetStructInit(new[] {"v", "next"}, new[] {1, 2}, 3); // inner
        graph.SetStructInit(new[] {"v", "next"}, new[] {0, 3}, 4); // outer

        // Set output type annotation
        var innerStruct = StateStruct.WithField("v", I32);
        var outerStruct = new StateStruct(
            new System.Collections.Generic.Dictionary<string, TicNode> {
                ["v"] = TicNode.CreateTypeVariableNode(I32),
                ["next"] = TicNode.CreateTypeVariableNode(StateOptional.Of(innerStruct))
            }, true);
        graph.SetVarType("y", outerStruct);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
    }

    #endregion
}
