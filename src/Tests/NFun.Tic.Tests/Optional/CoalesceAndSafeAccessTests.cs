using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

/// <summary>
/// TIC-level tests for NullCoalesce (??) operator and SafeArrayAccess (?[]) operator.
/// NullCoalesce uses generic function signature (opt(T), T) -> T via SetCall.
/// SafeArrayAccess uses the dedicated SetSafeArrayAccess TIC special form.
/// Also covers additional SetSafeFieldAccess scenarios not in OptionalCompositeTests.
/// </summary>
class CoalesceAndSafeAccessTests {

    // Helper: models the ?? operator as generic function (opt(T), T) -> T
    private static void SetNullCoalesce(GraphBuilder graph, int leftId, int rightId, int resultId) {
        var t = graph.InitializeVarNode();
        graph.SetCall(
            new ITicNodeState[] { StateOptional.Of(t), t, t },
            new[] { leftId, rightId, resultId });
    }

    #region NullCoalesce: opt(int) ?? int -> int

    [Test(Description = "x:opt(i32); y = x ?? 0i -> y = i32")]
    public void Coalesce_OptI32_WithI32_ReturnsI32() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region NullCoalesce: opt(int) ?? real -> real (LCA)

    [Test(Description = "x:opt(i32); y = x ?? 1.0 -> y = real")]
    public void Coalesce_OptI32_WithReal_ReturnsReal() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetConst(1, Real);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }

    #endregion

    #region NullCoalesce: opt(int) ?? opt(int)

    [Test(Description = "x:opt(i32); z:opt(i32); y = x ?? z -> y = i32")]
    public void Coalesce_OptI32_WithOptI32_ReturnsI32() {
        // NullCoalesce signature: (opt(T), T) -> T
        // left:opt(i32) -> opt(T) constrains T=i32.
        // right:opt(i32) is assigned to position T=i32.
        // TIC allows implicit lift: i32 <= opt(i32), so opt(i32) in T=i32 position
        // resolves via opt flattening: left = opt(opt(i32)) flattens to opt(i32), T=i32.
        // Result: T = i32.
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetVarType("z", StateOptional.Of(I32));
        graph.SetVar("z", 1);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region NullCoalesce: opt(T) ?? T where T is constrained

    [Test(Description = "x:opt(i32); y = x ?? intConst -> y = i32")]
    public void Coalesce_OptI32_WithIntConst_ReturnsI32() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetIntConst(1, U8);  // [U8..Real]
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region NullCoalesce: opt(arr(int)) ?? arr(int) -> arr(int)

    [Test(Description = "x:opt(int[]); y = x ?? [1i,2i] -> y = int[]")]
    public void Coalesce_OptIntArray_WithIntArray_ReturnsIntArray() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        // right = [1i, 2i] as int[]
        graph.SetConst(1, I32);
        graph.SetConst(2, I32);
        graph.SetStrictArrayInit(3, 1, 2);
        SetNullCoalesce(graph, 0, 3, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "y");
    }

    #endregion

    #region NullCoalesce: opt(struct) ?? struct -> struct

    [Test(Description = "x:opt({a:i32}); y = x ?? {a=0i} -> y = {a:i32}")]
    public void Coalesce_OptStruct_WithStruct_ReturnsStruct() {
        var graph = new GraphBuilder();
        var s = StateStruct.Of("a", I32);
        graph.SetVarType("x", StateOptional.Of(s));
        graph.SetVar("x", 0);
        // right = {a = 0i}
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 1 }, 2);
        SetNullCoalesce(graph, 0, 2, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(s, "y");
    }

    #endregion

    #region NullCoalesce: none ?? int -> int

    [Test(Description = "y = none ?? 42i -> y = i32")]
    public void Coalesce_None_WithI32_ReturnsI32() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetConst(1, I32);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region NullCoalesce: chained opt(int) ?? opt(int) ?? int

    [Test(Description = "a:opt(i32); b:opt(i32); y = a ?? b ?? 0i -> y = i32")]
    public void Coalesce_Chained_OptI32_OptI32_I32() {
        // a ?? b ?? 0i  =  a ?? (b ?? 0i) (right-associative)
        // Inner: b ?? 0i = coalesce(b:opt(i32), 0i:i32) -> i32
        // Outer: a ?? inner = coalesce(a:opt(i32), inner:i32) -> i32
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateOptional.Of(I32));
        graph.SetVar("a", 0);
        graph.SetVarType("b", StateOptional.Of(I32));
        graph.SetVar("b", 1);
        graph.SetConst(2, I32);  // 0i

        // Inner coalesce: b ?? 0i -> node 3
        SetNullCoalesce(graph, 1, 2, 3);
        // Outer coalesce: a ?? (b ?? 0i) -> node 4
        SetNullCoalesce(graph, 0, 3, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region SetSafeArrayAccess: arr(int)?[i] -> opt(int)

    [Test(Description = "x:opt(int[]); y = x?[0] -> y = opt(int)")]
    public void SafeArrayAccess_OptIntArray_ReturnsOptInt() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);  // index
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region SetSafeArrayAccess: arr(opt(int))?[i] -> opt(int) (no double optional)

    [Test(Description = "x:opt(opt(int)[]); y = x?[0] -> y = opt(int) (flattened)")]
    public void SafeArrayAccess_OptArrayOfOptInt_ReturnsOptInt() {
        // x: opt(arr(opt(int))), x?[i] -> LCA(opt(int), None) = opt(int) (flatten)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(StateOptional.Of(I32))));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);  // index
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region SetSafeArrayAccess: arr(arr(int))?[i] -> opt(arr(int))

    [Test(Description = "x:opt(int[][]); y = x?[0] -> y = opt(int[])")]
    public void SafeArrayAccess_OptNestedArray_ReturnsOptInnerArray() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(StateArray.Of(I32))));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);  // index
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    #endregion

    #region SetSafeArrayAccess: if-else produced optional array

    [Test(Description = "x = if(c) [1i,2i] else none; y = x?[0] -> y = opt(i32)")]
    public void SafeArrayAccess_IfElseArrayOrNone() {
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, I32);
        graph.SetStrictArrayInit(3, 1, 2);
        graph.SetConst(4, None);
        graph.SetIfElse(new[] { 0 }, new[] { 3, 4 }, 5);
        graph.SetDef("x", 5);

        graph.SetVar("x", 6);
        graph.SetConst(7, I32);  // index
        graph.SetSafeArrayAccess(6, 7, 8);
        graph.SetDef("y", 8);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region SetSafeFieldAccess: struct{x:int}?.x -> opt(int)

    [Test(Description = "x:opt({name:i32}); y = x?.name -> y = opt(i32)")]
    public void SafeFieldAccess_OptStruct_SingleField() {
        // This is similar to OptionalCompositeTests but tests a different field name
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateStruct.Of("name", I32)));
        graph.SetVar("x", 0);
        graph.SetSafeFieldAccess(0, 1, "name");
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region SetSafeFieldAccess: struct{x:opt(int)}?.x -> opt(int) (flatten)

    [Test(Description = "x:opt({val:opt(i32)}); y = x?.val -> y = opt(i32) (flattened)")]
    public void SafeFieldAccess_OptStructWithOptField_Flattens() {
        // x?.val: field type is opt(i32), wrapping in optional gives opt(opt(i32)) -> flattened to opt(i32)
        var graph = new GraphBuilder();
        graph.SetVarType("x",
            StateOptional.Of(StateStruct.Of("val", StateOptional.Of(I32))));
        graph.SetVar("x", 0);
        graph.SetSafeFieldAccess(0, 1, "val");
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region SetSafeFieldAccess: opt(struct{x:int})?.x -> opt(int)

    [Test(Description = "x:opt({score:real}); y = x?.score -> y = opt(real)")]
    public void SafeFieldAccess_OptStructRealField() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateStruct.Of("score", Real)));
        graph.SetVar("x", 0);
        graph.SetSafeFieldAccess(0, 1, "score");
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Real), "y");
    }

    #endregion

    #region SetSafeFieldAccess: struct with array field

    [Test(Description = "x:opt({items:int[]}); y = x?.items -> y = opt(int[])")]
    public void SafeFieldAccess_OptStructWithArrayField() {
        var graph = new GraphBuilder();
        graph.SetVarType("x",
            StateOptional.Of(StateStruct.Of("items", StateArray.Of(I32))));
        graph.SetVar("x", 0);
        graph.SetSafeFieldAccess(0, 1, "items");
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    #endregion

    #region Combined: coalesce + safe access

    [Test(Description = "x:opt(int[]); y = (x ?? [1i])?[0] — coalesce then safe access is invalid (result not optional)")]
    public void Coalesce_ThenSafeAccess_CoalesceReturnsNonOptional() {
        // x:opt(int[]); x ?? [1i] -> int[] (not optional!)
        // Then ?[] on non-optional array should fail since SetSafeArrayAccess expects opt(arr(T))
        // Actually, SetSafeArrayAccess constrains source to opt(arr(T)), so it would
        // push int[] -> opt(arr(T)), meaning T constrained but source gets opt wrapping.
        // Let me just test: y = x?[0] ?? 0i (safe access then coalesce)
        // x:opt(int[]); x?[0] -> opt(int); opt(int) ?? 0i -> int
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);  // index
        graph.SetSafeArrayAccess(0, 1, 2);  // x?[0] -> node 2: opt(int)
        graph.SetConst(3, I32);  // default value 0i
        SetNullCoalesce(graph, 2, 3, 4);  // opt(int) ?? 0i -> node 4: int
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region Combined: safe field access then coalesce

    [Test(Description = "x:opt({age:i32}); y = x?.age ?? 0i -> y = i32")]
    public void SafeFieldAccess_ThenCoalesce() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateStruct.Of("age", I32)));
        graph.SetVar("x", 0);
        graph.SetSafeFieldAccess(0, 1, "age");  // x?.age -> node 1: opt(i32)
        graph.SetConst(2, I32);  // default 0i
        SetNullCoalesce(graph, 1, 2, 3);  // opt(i32) ?? 0i -> node 3: i32
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region NullCoalesce: error case — non-optional left

    [Test(Description = "x:i32; y = x ?? 0i -> x fits opt(T) via implicit lift, y = i32")]
    public void Coalesce_NonOptionalLeft_ImplicitLift() {
        // x:i32 passed as opt(T) — implicit lift i32 -> opt(i32), T=i32
        // right: 0i fits T=i32. Result: T=i32.
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region NullCoalesce: opt(bool) ?? false -> bool

    [Test(Description = "flag:opt(bool); y = flag ?? false -> y = bool")]
    public void Coalesce_OptBool_WithBool() {
        var graph = new GraphBuilder();
        graph.SetVarType("flag", StateOptional.Of(Bool));
        graph.SetVar("flag", 0);
        graph.SetConst(1, Bool);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    #endregion

    #region SafeArrayAccess: non-optional array fails

    [Test(Description = "x:int[]; x?[0] — SetSafeArrayAccess constrains source to opt(arr(T)), int[] lifts")]
    public void SafeArrayAccess_NonOptionalArray_ImplicitLift() {
        // SetSafeArrayAccess constrains source to opt(arr(T)).
        // int[] can lift into opt(arr(T)) with T=int.
        // Result = LCA(T, None) = opt(int)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);  // index
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region SafeArrayAccess with bool element type

    [Test(Description = "x:opt(bool[]); y = x?[0] -> y = opt(bool)")]
    public void SafeArrayAccess_OptBoolArray() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(Bool)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Bool), "y");
    }

    #endregion

    #region SafeArrayAccess with struct element type

    [Test(Description = "x:opt({a:i32}[]); y = x?[0] -> y = opt({a:i32})")]
    public void SafeArrayAccess_OptArrayOfStruct() {
        var graph = new GraphBuilder();
        var s = StateStruct.Of("a", I32);
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(s)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(s), "y");
    }

    #endregion
}
