using System.Collections.Generic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using System.Linq;
using static StatePrimitive;

public class StructTest {
    [Test]
    public void SingleStrictStructMember() {
        //        0 2  1
        //y:int = a . name
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetFieldAccess(0, 2, "name");
        graph.SetVarType("y", I32);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.WithField("name", I32), "a");
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void SeveralStrictStructMembers() {
        //        0 2  1
        //y:int = a . name
        //         3 5  4
        //z:real = a . age
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetFieldAccess(0, 2, "name");
        graph.SetVarType("y", I32);
        graph.SetDef("y", 2);

        graph.SetVar("a", 3);
        graph.SetFieldAccess(3, 5, "age");
        graph.SetVarType("z", Real);
        graph.SetDef("z", 5);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(
            new StateStruct(
                new Dictionary<string, TicNode> {
                    { "name", TicNode.CreateTypeVariableNode(I32) }, { "age", TicNode.CreateTypeVariableNode(Real) }
                }, false),
            "a");
        result.AssertNamed(I32, "y");
        result.AssertNamed(Real, "z");
    }

    [Test]
    public void StructConstructor_WithStrictFields() {
        //    2       0       1
        //y = { a = 12i, b = 1.0}
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Real);
        graph.SetStructInit(new[] { "a", "b" }, new[] { 0, 1 }, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(
            new StateStruct(
                new Dictionary<string, TicNode> {
                    { "a", TicNode.CreateNamedNode("a", I32) }, { "b", TicNode.CreateNamedNode("b", Real) }
                }, false), "y");
    }

    [Test]
    public void StructConstructor_WithGenericField() {
        TraceLog.IsEnabled = true;
        //    1      0
        //y = { a = x,}
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null);
        result.AssertAreGenerics(generic, "x");
        result.AssertNamed(
            new StateStruct("a", generic, false), "y");
    }

    [Test]
    public void NestedStructConstructor() {
        //    4       0        3     1       2
        //y = { a = 12i, b = {c = true,d = 1.0 } }
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Bool);
        graph.SetConst(2, Real);
        graph.SetStructInit(new[] { "c", "d" }, new[] { 1, 2 }, 3);
        graph.SetStructInit(new[] { "a", "b" }, new[] { 0, 3 }, 4);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();

        var yStruct = result.GetVariableNode("y").State as StateStruct;
        var aField = yStruct.GetFieldOrNull("a").State as StatePrimitive;
        Assert.AreEqual(I32.Name, aField.Name);
        var bField = yStruct.GetFieldOrNull("b").State as StateStruct;
        var cField = bField.GetFieldOrNull("c").State as StatePrimitive;
        Assert.AreEqual(Bool.Name, cField.Name);
        var dField = bField.GetFieldOrNull("d").State as StatePrimitive;
        Assert.AreEqual(Real.Name, dField.Name);
    }

    [Test]
    public void TwinFieldAccess() {
        //    1 2   0
        //y = a . b . c"

        var graph = new GraphBuilder();

        graph.SetVar("a", 1);
        graph.SetFieldAccess(1, 2, "b");
        graph.SetFieldAccess(2, 0, "f");
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null);
        Assert.AreEqual("y", generic.Name);
    }

    [Test]
    public void ConcreteTwinFieldAccess() {
        //         1 2   0
        //y:int = a . b . c"

        var graph = new GraphBuilder();

        graph.SetVar("a", 1);
        graph.SetFieldAccess(1, 2, "b");
        graph.SetFieldAccess(2, 0, "f");
        graph.SetVarType("y", I32);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void TwinNestedStructCrossAccess() {
        //    1     2      3
        //d = {f = {b= true}};
        //
        //    5 6   4
        //y = d . f . b"
        TraceLog.IsEnabled = true;
        var graph = new GraphBuilder();
        graph.SetConst(3, Bool);
        graph.SetStructInit(new[] { "b" }, new[] { 3 }, 2);
        graph.SetStructInit(new[] { "f" }, new[] { 2 }, 1);
        graph.SetDef("d", 1);

        graph.SetVar("d", 5);
        graph.PrintTrace("after D");
        graph.SetFieldAccess(5, 6, "f");
        graph.PrintTrace("after f");

        graph.SetFieldAccess(6, 4, "b");
        graph.PrintTrace("after b");

        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    [Test]
    public void FunCallWithStruct_StructTypeSolved() {
        // f( input:{field:int} ):bool
        //
        //     1         2
        // x = @(field = 1)
        //     4 3
        // y = f(x)

        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();

        graph.SetGenericConst(2, U8, Real, Real);
        graph.SetStructInit(new[] { "field" }, new[] { 2 }, 1);
        graph.SetDef("x", 1);

        graph.SetVar("x", 3);
        graph.SetCall(
            new ITicNodeState[] { new StateStruct("field", TicNode.CreateTypeVariableNode(I32), false), Bool },
            new[] { 3, 4 });
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");

        var xStruct = result.GetVariableNode("x").State as StateStruct;
        var field = xStruct.GetFieldOrNull("field").State as StatePrimitive;
        Assert.AreEqual(I32, field);
    }

    [Test]
    public void FunCallReturnsGenericStruct_WithGenericConstant() {
        // f(x) = {res = x}
        //        1 0   2
        //    y = f(42).res
        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();
        graph.SetGenericConst(0, U8, Real, Real);
        var varnode = graph.InitializeVarNode();

        graph.SetCall(
            new ITicNodeState[] { varnode, new StateStruct("res", varnode.Node, false) }, new[] { 0, 1 });
        graph.SetFieldAccess(1, 2, "res");
        graph.SetDef("y", 2);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U8, Real, false);
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    public void FunCallReturnsGenericStruct_WithConcreteConstant() {
        // f(x) = {res = x}
        //        1 0     2
        //    y = f(42.0).res
        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        var varnode = graph.InitializeVarNode();

        graph.SetCall(
            new ITicNodeState[] { varnode, new StateStruct("res", varnode.Node, false) }, new[] { 0, 1 });
        graph.SetFieldAccess(1, 2, "res");
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }


    [Test]
    public void SingleFieldGenericExpression() {
        //        1  0
        // y = x.field

        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();

        graph.SetVar("x", 1);
        graph.SetFieldAccess(1, 0, "field");
        graph.SetDef("y", 0);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null, false);
        var xStruct = result.GetVariableNode("x").State as StateStruct;
        var fieldNode = xStruct.GetFieldOrNull("field").GetNonReference();
        Assert.AreEqual(generic, fieldNode);
    }

    [Test]
    public void UserFunDefinition_SingleFieldExpression() {
        //        2  1
        // f(x) = x.field

        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();

        var fun = graph.SetFunDef(
            name: "f'1",
            returnId: 1,
            returnType: null,
            varNames: new[] { "x" });

        graph.SetVar("x", 2);
        graph.SetFieldAccess(2, 1, "field");

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null, false).GetNonReference();
        var xStruct = result.GetVariableNode("x").State as StateStruct;
        var fieldNode = xStruct.GetFieldOrNull("field").GetNonReference();
        Assert.AreEqual(generic, fieldNode);

        Assert.AreEqual(fun.RetNode, generic);
        Assert.AreEqual(fun.ArgNodes[0].State, xStruct);
    }

    [Test]
    public void ComparableFieldsGenericExpression() {
        //     1 0   4    3 2
        // y = x.a   <    x.b

        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();

        graph.SetVar("x", 1);
        graph.SetFieldAccess(1, 0, "a");

        graph.SetVar("x", 3);
        graph.SetFieldAccess(3, 2, "b");

        graph.SetComparable(0, 2, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null, true);

        var xStruct = result.GetVariableNode("x").State as StateStruct;
        var aFieldNode = xStruct!.GetFieldOrNull("a").GetNonReference();
        var bFieldNode = xStruct!.GetFieldOrNull("b").GetNonReference();

        Assert.AreEqual(generic, aFieldNode);
        Assert.AreEqual(generic, bFieldNode);
        result.AssertNamed(Bool, "y");
    }

    [Test]
    public void StructArrayLca1() {
        using var _ = TraceLog.Scope;
        // Ta: {age:int}, Tb: {}
        //     2 0     1
        // y = [a:Ta, b:Tb]
        TraceLog.IsEnabled = true;

        var graph = new GraphBuilder();

        graph.SetVarType("a", StateStruct.Of("age", I32));
        graph.SetVarType("b", StateStruct.Empty());

        graph.SetVar("a", 0);
        graph.SetVar("b", 0);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        var eType = y.Element as StateStruct;
        Assert.AreEqual(0, eType.Fields.Count());
    }

    [Test]
    public void StructArrayLca2() {
        using var _ = TraceLog.Scope;
        // Ta: {age:int}, Tb: {age:int, size:int}
        //     2 0     1
        // y = [a:Ta, b:Tb]

        var graph = new GraphBuilder();

        graph.SetVarType("a", StateStruct.Of("age", I32));
        graph.SetVarType("b", StateStruct.Of(("age", I32), ("size", I32)));

        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        var eType = y.Element as StateStruct;

        Assert.AreEqual(1, eType.Fields.Count());
        Assert.AreEqual(I32, eType.GetFieldOrNull("age").State);
    }

    [Test]
    public void StructArrayLca3() {
        using var _ = TraceLog.Scope;
        // Ta: {age:int}, Tb: {age:real, size:int}
        //     2 0     1
        // y = [a:Ta, b:Tb]

        var graph = new GraphBuilder();

        graph.SetVarType("a", StateStruct.Of("age", I32));
        graph.SetVarType("b", StateStruct.Of(("age", Real), ("size", I32)));

        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        var eType = y.Element as StateStruct;
        Assert.AreEqual(0, eType.Fields.Count());
    }


    [Test]
    public void StructArrayLca4() {
        using var _ = TraceLog.Scope;
        // Ta: {age:int}, Tb: {}
        //     2 0     1
        // y = [b:Tb, a:Ta]

        var graph = new GraphBuilder();

        graph.SetVarType("a", StateStruct.Of("age", I32));
        graph.SetVarType("b", StateStruct.Empty());

        graph.SetVar("a", 1);
        graph.SetVar("b", 0);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        var eType = y.Element as StateStruct;
        Assert.AreEqual(0, eType.Fields.Count());
    }

    [Test]
    public void StructArrayLca5() {
        using var _ = TraceLog.Scope;
        // Ta: {age:int}, Tb: {age:int, size:int}
        //     2 0     1
        // y = [b:Tb, a:Ta]

        var graph = new GraphBuilder();

        graph.SetVarType("a", StateStruct.Of("age", I32));
        graph.SetVarType("b", StateStruct.Of(("age", I32), ("size", I32)));

        graph.SetVar("a", 1);
        graph.SetVar("b", 0);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        var eType = y.Element as StateStruct;

        Assert.AreEqual(1, eType.Fields.Count());
        Assert.AreEqual(I32, eType.GetFieldOrNull("age").State);
    }

    [Test]
    public void StructArrayLca6() {
        using var _ = TraceLog.Scope;
        // Ta: {age:int}, Tb: {age:real, size:int}
        //     2 0     1
        // y = [b:Tb, a:Ta]

        var graph = new GraphBuilder();

        graph.SetVarType("a", StateStruct.Of("age", I32));
        graph.SetVarType("b", StateStruct.Of(("age", Real), ("size", I32)));

        graph.SetVar("a", 1);
        graph.SetVar("b", 0);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateArray;
        var eType = y.Element as StateStruct;
        Assert.AreEqual(0, eType.Fields.Count());
    }

    [Test]
    public void StructIfLca1() {
        using var _ = TraceLog.Scope;
        // Tb: {age:Real}
        //
        //    4    0    2       1        3
        // y = if(true) { age = 1 } else b:Tb

        var graph = new GraphBuilder();

        graph.SetVarType("b", StateStruct.Of("age", Real));
        graph.SetVar("b", 3);
        graph.SetConst(0, Bool);
        graph.SetGenericConst(1, U8, Real, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 1 }, 2);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.AreEqual(1, y.Fields.Count());
        var age = y.GetFieldOrNull("age");
        Assert.AreEqual(Real, age.State);
    }

    [Test]
    public void StructIfLca2() {
        using var _ = TraceLog.Scope;
        // Tb: {age:Any}
        //
        //    4    0    2       1        3
        // y = if(true) { age = 1 } else b:Tb

        var graph = new GraphBuilder();

        graph.SetVarType("b", StateStruct.Of("age", Any));
        graph.SetVar("b", 3);
        graph.SetConst(0, Bool);
        graph.SetGenericConst(1, U8, Real, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 1 }, 2);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();

        var y = result.GetVariableNode("y").State as StateStruct;
        Assert.AreEqual(1, y.Fields.Count());
        var age = y.GetFieldOrNull("age");
        Assert.AreEqual(Any, age.State);
    }
}
