using System.Linq;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.Structs;

public class StructGenericFunctionTest {
    [Test]
    public void CallGenericFunctionFieldAccess() =>
        ("f(x) = x.age;" +
         "x1= {age = 12};" +
         "x2= {age = true};" +
         "r = f(x1); b = f(x2);")
        .AssertResultHas("r", 12)
        .AssertResultHas("b", true);

    [Test]
    public void CallToAllowedReqTypeDef() {
        var runtime = Funny.Hardcore.Build("f(x) = x.age; y1 = f(user); y2 = f(user.child[0])");
        var childItem = new System.Collections.Generic.Dictionary<string, object> { { "age", 7 } };
        var userValue = new System.Collections.Generic.Dictionary<string, object> {
            { "age", 42 },
            { "child", new[] { childItem } }
        };
        runtime["user"].Value = userValue;
        runtime.Run();
        Assert.AreEqual(42, runtime["y1"].Value);
        Assert.AreEqual(7, runtime["y2"].Value);
    }

    [Test]
    public void CallGenericFunctionFieldNegate() =>
        ("f(x) = -x.age;" +
         "x1= {age = 12};" +
         "r = f(x1); ")
        .AssertResultHas("r", -12);

    [Test]
    public void CallGenericFunctionMultipleFieldOfArrayAccess_PipeForward() =>
        ("f(x) = x.a.concat(x.b);" +
         "x1= {a = 'mama'; b = 'popo'};" +
         "t = f(x1);" +
         "iarr = f({a=[1,2,3]; b = [4,5,6]})")
        .AssertResultHas("t", "mamapopo")
        .AssertResultHas("iarr", new[] { 1, 2, 3, 4, 5, 6 });

    [Test]
    public void CallGenericFunctionMultipleFieldOfArrayAccess() =>
        ("f(x) = concat(x.a, x.b);" +
         "x1= {a = 'mama'; b = 'popo'};" +
         "t = f(x1);" +
         "iarr = f({a=[1,2,3]; b = [4,5,6]})")
        .AssertResultHas("t", "mamapopo")
        .AssertResultHas("iarr", new[] { 1, 2, 3, 4, 5, 6 });

    [Test]
    public void CallGenericFunctionMultipleFieldOfTextAccess() =>
        ("f(x) = concat(x.a, x.b);" +
         "t = f({a = 'mama'; b = 'popo'});").AssertResultHas("t", "mamapopo");

    [Test]
    public void CallGenericFunctionMultipleFieldOfRealArrayAccess() =>
        ("f(x) = concat(x.a, x.b);" +
         "x1= {a = 'mama'; b = 'popo'};" +
         "iarr = f({a=[1,2,3]; b = [4,5,6]})").AssertResultHas("iarr", new[] { 1, 2, 3, 4, 5, 6 });

    [Test]
    public void CallGenericFunctionMultipleFieldOfConcreteIntArrayAccess() =>
        ("f(x) = concat(x.a, x.b);" +
         "iarr:int[] = f({a=[1,2,3]; b = [4,5,6]})").AssertResultHas("iarr", new[] { 1, 2, 3, 4, 5, 6 });

    [Test]
    public void CallGenericFunctionMultipleFieldOfConcreteRealsArrayAccess() =>
        ("f(x) = concat(x.a, x.b);" +
         "iarr:real[] = f( {a=[1,2.0,3.0]; b = [4.0,5.0,6.0]} )")
        .AssertResultHas("iarr", new[] { 1.0, 2, 3, 4, 5, 6 });

    [Test]
    public void CallGenericFunctionSingleFieldOfConcreteRealsArrayAccess() =>
        ("f(x) = reverse(x.a);" +
         "y:real[] = f( {a=[1.0,2.0]} )").AssertResultHas("y", new[] { 2.0, 1.0 });

    [Test]
    public void CallGenericFunctionWithFieldIndexing() =>
        ("f(x) = x.item[1];" +
         "x1= {item = [1,2,3] };" +
         "r = f(x1);").AssertResultHas("r", 2);

    [Test]
    public void CallGenericFunctionWithAdditionalFields() =>
        ("f(x) = x.size;" +
         "x1= {age = 12; size = 24; name = 'vasa'};" +
         "r = f(x1);").AssertResultHas("r", 24);

    [Test]
    public void CallComplexFunction() =>
        ("f(x) = x[1].name;" +
         "a = [{age = 42; name = 'vasa'}, {age = 21; name = 'peta'}];" +
         "y = f(a);").AssertResultHas("y", "peta");

    [Test]
    public void GenericStructFunctionReturn() =>
        ("f(x) = {res = x}; " +
         "r = f(42).res;" +
         "txt = f('try').res").AssertResultHas("r", 42)
        .AssertResultHas("txt", "try");

    [Test]
    public void SingleGenericStructFunctionReturn() =>
        ("f(x) = {res = x}; " +
         "r = f(42).res;").AssertResultHas("r", 42);

    [Test]
    public void SingleGenericStructFunction_WithConcrete_ReturnsCouncrete() =>
        ("f(x) = {res = x}; " +
         "r = f(42.0).res;").AssertResultHas("r", 42.0);

    [Test]
    public void ConstrainedGenericStructFunctionReturn() =>
        ("f(x) = {twice = x+x; dec = x-1}; " +
         "t = f(42).twice;" +
         "d = f(123).dec").AssertResultHas("t", 84)
        .AssertResultHas("d", 122);

    // Body literal `1` gives generic T preferred = Int32; with no caller-side type
    // context, T resolves to Int32 (PreferredFromBody rule). To exercise Real-mode
    // the caller must annotate the result type — see RecursiveUserFunctionsTest's
    // FactorialGeneric_OutReal for the same pattern on a non-struct fact.

    [TestCase(1, 1)]
    [TestCase(3, 6)]
    [TestCase(6, 720)]
    public void GenericFactorialReq_ReturnStruct(int x, int y) =>
        @"fact(n) = if(n<=1) {res = 1} else {res = fact(n-1).res*n}
                  y = fact(x).res".Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(3, 6)]
    [TestCase(6, 720)]
    public void GenericFactorialReq_ArgIsStruct(int x, int y) =>
        @"fact(n) = if(n.field<=1) 1 else fact({field=n.field-1})*n.field
                  x:int
                  y = fact({field=x})".Calc("x", x)
            .AssertReturns("y", y);

    [Test]
    public void GenericUserFunction_StructFieldsPreserved_FoldReturnAccessesExtraField() =>
        ("getMax(items) = items.fold(rule if(it1.v > it2.v) it1 else it2);" +
         "out = getMax([{v=3,name='a'},{v=1,name='b'},{v=5,name='c'}]).name")
        .AssertResultHas("out", "c");

    [Test]
    public void GenericUserFunction_StructFieldsPreserved_FoldReturnAccessesOriginalField() =>
        ("getMax(items) = items.fold(rule if(it1.v > it2.v) it1 else it2);" +
         "out = getMax([{v=3,name='a'},{v=1,name='b'},{v=5,name='c'}]).v")
        .AssertResultHas("out", 5);

    [Test]
    public void GenericUserFunction_StructFieldsPreserved_ViaIntermediateVariable() =>
        ("getMax(items) = items.fold(rule if(it1.v > it2.v) it1 else it2)\r\n" +
        "best = getMax([{v=3,name='a'},{v=1,name='b'},{v=5,name='c'}])\r\n" +
        "out = best.name")
        .AssertResultHas("out", "c");

    [TestCase("f(x) = x.a; y = f({nonExistField = 1})")]
    [TestCase("f(x) = x.a; y = f({})")]
    [TestCase("f(x) = x.a; y:bool = f({x = 1})")]
    [TestCase(
        @"fact(n) = if(n.field<=1) 1 else fact({field=n.field-1}) * n.field;
                  y = fact({a=x})")]
    [TestCase(
        @"f(n) = n.field;
                  y = fact({nonExistingField=x})")]
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    [Test]
    public void StructFieldMap_PreservesIntType() {
        var r = "s = {m = [[1,2],[3,4]]}\r out = s.m.map(rule it.sum())".Calc();
        var outVal = r.Get("out");
        Assert.AreEqual(typeof(int[]), outVal.GetType(), $"Expected int[] but got {outVal.GetType()}");
        Assert.AreEqual(new[] { 3, 7 }, outVal);
    }

    [Test]
    public void DirectVariableMap_PreservesIntType() {
        var r = "m = [[1,2],[3,4]]\r out = m.map(rule it.sum())".Calc();
        Assert.AreEqual(new[] { 3, 7 }, r.Get("out"));
        r.AssertResultHas("out", new[] { 3, 7 });
    }

    [Test]
    public void Sort_PreservesAllFields() {
        var runtime = Funny.Hardcore.Build("[{a=2,b=20},{a=1,b=10}].sort(rule it.a)");
        runtime.Run();
        var outType = runtime["out"].Type;
        Assert.AreEqual(BaseFunnyType.ArrayOf, outType.BaseType, "out should be array");
        var elemType = outType.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType, "element should be struct");
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b" }, fields, "struct must have both fields a and b");
    }

    [Test]
    public void Filter_PreservesAllFields() {
        var runtime = Funny.Hardcore.Build("[{a=1,b=10},{a=2,b=20}].filter(rule it.a > 1)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b" }, fields);
    }

    [Test]
    public void SortDescending_PreservesAllFields() {
        var runtime = Funny.Hardcore.Build("[{a=1,b=10},{a=2,b=20}].sortDescending(rule it.a)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b" }, fields);
    }

    [Test]
    public void Sort_ThreeFields() {
        var runtime = Funny.Hardcore.Build("[{x=2,y=20,z=200},{x=1,y=10,z=100}].sort(rule it.x)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "x", "y", "z" }, fields);
    }

    [Test]
    public void Filter_TwoFieldsAccessed_PreservesThird() {
        var runtime = Funny.Hardcore.Build("[{a=1,b=2,c=3},{a=4,b=5,c=6}].filter(rule it.a > 0 and it.b > 0)");
        runtime.Run();
        var elemType = runtime["out"].Type.ArrayTypeSpecification.FunnyType;
        Assert.AreEqual(BaseFunnyType.Struct, elemType.BaseType);
        var fields = elemType.StructTypeSpecification.Select(f => f.Key).OrderBy(f => f).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, fields);
    }

    [Test]
    public void Sort_ThenAccessField_Preserves() =>
        "[{a=2,b=20},{a=1,b=10}].sort(rule it.a).map(rule it.b)"
            .AssertReturns("out", new[] { 10, 20 });

    [Test]
    public void GenericFuncLosesStructFields_Fixed() {
        "getMax(items) = items.fold(rule if(it1.v > it2.v) it1 else it2); out = getMax([{v=3,name='a'},{v=1,name='b'},{v=5,name='c'}]).name"
            .AssertReturns("out", "c");
    }

    [Test]
    public void LambdaStructCovariance() {
        "f(p) = p.x + 0.5\r arr:{x:int}[] = [{x=1},{x=2}]\r out = arr.map(rule f(it))"
            .Calc().AssertResultHas("out", new[] { 1.5, 2.5 });
    }

    #region FloatFamily dialect

    [Test]
    public void Float32_Generic_StructAccessor_ReturnsF32() {
        var rt = "getter(p) = p.x\r a:{x:float32}={x=1.5}\r out=getter(a)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Generic_TwoCallsSameShape() {
        var rt = "getter(p) = p.x\r a:{x:float32}={x=1.5}\r b:{x:float32}={x=2.5}\r out=getter(a)+getter(b)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(4.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Generic_FunctionReturnsStruct_WithF32() {
        var rt = "mk(x:float32):{v:float32} = {v=x}\r r = mk(1.5)\r out = r.v".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Generic_FoldOnStructArray_SumF32Field() {
        var rt = "sumX(items:{x:float32}[]):float32 = items.fold(0.0, rule it1 + it2.x)\r arr:{x:float32}[]=[{x=1.0},{x=2.0},{x=3.0}]\r out = sumX(arr)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(6.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Generic_GetMax_StructWithF32_PreservesOtherFields() {
        var rt = "getMax(items) = items.fold(rule if(it1.v > it2.v) it1 else it2)\r arr:{v:float32,name:text}[] = [{v=3.0,name='a'},{v=5.0,name='b'}]\r out = getMax(arr).name".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("b", rt["out"].Value);
    }

    [Test]
    public void Float32_Generic_MapBuildsStructArray_WithF32Field() {
        var rt = "arr:float32[]=[1.0,2.0,3.0]\r out = arr.map(rule {v=it})".BuildWithFloats();
        rt.Run();
        var mapped = ((System.Collections.IEnumerable)rt["out"].Value);
        int count = 0;
        foreach (var _ in mapped) count++;
        Assert.AreEqual(3, count);
    }

    [Test]
    public void Float32_Generic_SortStructs_ByF32Field() {
        var rt = "arr:{v:float32,name:text}[] = [{v=3.0,name='c'},{v=1.0,name='a'},{v=2.0,name='b'}]\r out = arr.sort(rule it.v).map(rule it.name)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { "a", "b", "c" }, rt["out"].Value);
    }

    [Test]
    public void Float32_Generic_FilterStructs_ByF32Field() {
        var rt = "arr:{v:float32}[] = [{v=1.0},{v=3.0},{v=2.0}]\r out = arr.filter(rule it.v > 1.5).map(rule it.v)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 3.0f, 2.0f }, rt["out"].Value);
    }
    #endregion
}
