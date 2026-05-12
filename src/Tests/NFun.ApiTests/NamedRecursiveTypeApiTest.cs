using System.Collections.Generic;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

/// <summary>
/// API-level tests for named recursive types.
/// Tests the public API surface: variable types, runtime values, FunnyType structure.
/// </summary>
public class NamedRecursiveTypeApiTest {

    static FunnyRuntime Build(string expr) =>
        Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build(expr);

    // ─── Variable types ───

    [Test]
    public void LinkedList_OutputVariable_IsStruct() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        var outVar = rt["out"];
        Assert.IsTrue(outVar.IsOutput);
        Assert.AreEqual(BaseFunnyType.Struct, outVar.Type.BaseType);
    }

    // ─── TypeRegistry ───

    [Test]
    public void TypeRegistry_HasNamedType() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        Assert.AreEqual(1, rt.TypeRegistry.Count);
        Assert.IsTrue(rt.TypeRegistry.TryGetType("node", out var info));
        Assert.AreEqual("node", info.Name);
        Assert.IsFalse(info.IsAlias);
        Assert.AreEqual(2, info.Fields.Count);
    }

    [Test]
    public void TypeRegistry_StructFields() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        var info = rt.TypeRegistry["node"];
        Assert.AreEqual("v", info.Fields[0].Name);
        Assert.AreEqual(BaseFunnyType.Int32, info.Fields[0].Type.BaseType);
        Assert.IsFalse(info.Fields[0].HasDefault);
        Assert.AreEqual("next", info.Fields[1].Name);
        Assert.AreEqual(BaseFunnyType.Optional, info.Fields[1].Type.BaseType);
        Assert.IsTrue(info.Fields[1].HasDefault);
    }

    [Test]
    public void TypeRegistry_RecursiveFieldType_IsNamedStruct() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        var nextType = rt.TypeRegistry["node"].Fields[1].Type; // node?
        var innerType = nextType.OptionalTypeSpecification.ElementType;
        Assert.AreEqual(BaseFunnyType.NamedStruct, innerType.BaseType);
        Assert.AreEqual("node", innerType.NamedStructTypeName);
    }

    [Test]
    public void TypeRegistry_Alias() {
        var rt = Build("type age = int; type node = {v:age}; out = node{v=42}");
        Assert.AreEqual(2, rt.TypeRegistry.Count);
        var ageInfo = rt.TypeRegistry["age"];
        Assert.IsTrue(ageInfo.IsAlias);
        Assert.AreEqual(BaseFunnyType.Int32, ageInfo.Type.BaseType);
    }

    [Test]
    public void TypeRegistry_Empty_WhenNoTypes() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("out = 42");
        Assert.AreEqual(0, rt.TypeRegistry.Count);
    }

    [Test]
    public void LinkedList_FieldTypes_Correct() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        var fields = rt["out"].Type.StructTypeSpecification;
        Assert.AreEqual(BaseFunnyType.Int32, fields["v"].BaseType);
        Assert.AreEqual(BaseFunnyType.Optional, fields["next"].BaseType);
    }

    [Test]
    public void LinkedList_InnerStructType_IsNamed() {
        // The `next: node?` field's inner type IS the named type `node` itself.
        // Recursive references preserve named identity (NamedStruct, not anonymous Struct) —
        // see TypeRegistry_RecursiveFieldType_IsNamedStruct for the canonical assertion.
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        var nextType = rt["out"].Type.StructTypeSpecification["next"];
        var inner = nextType.OptionalTypeSpecification.ElementType;
        Assert.AreEqual(BaseFunnyType.NamedStruct, inner.BaseType);
        Assert.AreEqual("node", inner.NamedStructTypeName);
    }

    [Test]
    public void ArrayTree_FieldTypes_Correct() {
        // The `children: t[]` field's array element type IS the named type `t` itself.
        // Recursive references preserve named identity.
        var rt = Build("type t = {v:int, children:t[] = []}; out = t{v=1}");
        var fields = rt["out"].Type.StructTypeSpecification;
        Assert.AreEqual(BaseFunnyType.Int32, fields["v"].BaseType);
        Assert.AreEqual(BaseFunnyType.ArrayOf, fields["children"].BaseType);
        var elemType = fields["children"].ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.NamedStruct, elemType.BaseType);
        Assert.AreEqual("t", elemType.NamedStructTypeName);
    }

    // ─── Runtime values ───

    [Test]
    public void LinkedList_RuntimeValue_IsFunnyStruct() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        rt.Run();
        var val = rt["out"].Value;
        Assert.IsInstanceOf<IReadOnlyDictionary<string, object>>(val);
        var dict = (IReadOnlyDictionary<string, object>)val;
        Assert.AreEqual(42, dict["v"]);
    }

    [Test]
    public void LinkedList_Nested_RuntimeValue() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=1, next=node{v=2}}");
        rt.Run();
        var outer = (IReadOnlyDictionary<string, object>)rt["out"].Value;
        Assert.AreEqual(1, outer["v"]);
        var inner = (IReadOnlyDictionary<string, object>)outer["next"];
        Assert.AreEqual(2, inner["v"]);
    }

    [Test]
    public void ArrayTree_RuntimeValue_ChildrenArray() {
        var rt = Build("type t = {v:int, children:t[] = []}; out = t{v=0, children=[t{v=1}, t{v=2}]}");
        rt.Run();
        var root = (IReadOnlyDictionary<string, object>)rt["out"].Value;
        Assert.AreEqual(0, root["v"]);
        var children = (object[])root["children"];
        Assert.AreEqual(2, children.Length);
        var child0 = (IReadOnlyDictionary<string, object>)children[0];
        Assert.AreEqual(1, child0["v"]);
    }

    [Test]
    public void DefaultValue_None_RuntimeValue() {
        var rt = Build("type node = {v:int, next:node? = none}; out = node{v=42}");
        rt.Run();
        var dict = (IReadOnlyDictionary<string, object>)rt["out"].Value;
        Assert.IsTrue(dict["next"] is null or FunnyNone);
    }

    // ─── Computed outputs ───

    [Test]
    public void LinkedList_SafeAccess_ReturnsValue() {
        var rt = Build("type node = {v:int, next:node? = none}; n = node{v=1, next=node{v=2}}; out = n.next?.v ?? -1");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void LinkedList_SafeAccess_ReturnsDefault() {
        var rt = Build("type node = {v:int, next:node? = none}; n = node{v=1}; out = n.next?.v ?? -1");
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    [Test]
    public void ArrayTree_Count_Works() {
        var rt = Build("type t = {v:int, children:t[] = []}; root = t{v=0, children=[t{v=1}, t{v=2}]}; out = root.children.count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void GenericFunction_OverRecursiveType() {
        var rt = Build("type node = {v:int, next:node? = none}; getVal(n:node):int = n.v; out = getVal(node{v=99})");
        rt.Run();
        Assert.AreEqual(99, rt["out"].Value);
    }

    [Test]
    public void MutualRecursion_DeepAccess() {
        var rt = Build(
            "type a = {x:int, b:b? = none}; " +
            "type b = {y:int, a:a? = none}; " +
            "v = a{x=1, b=b{y=2, a=a{x=3}}}; " +
            "out = v.b?.a?.x ?? -1");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // ─── Multiple variables ───

    [Test]
    public void MultipleOutputs_FromSameRecursiveType() {
        var rt = Build(
            "type node = {v:int, next:node? = none}; " +
            "n = node{v=10, next=node{v=20}}; " +
            "headV = n.v; " +
            "nextV = n.next?.v ?? 0");
        rt.Run();
        Assert.AreEqual(10, rt["headV"].Value);
        Assert.AreEqual(20, rt["nextV"].Value);
    }
}
