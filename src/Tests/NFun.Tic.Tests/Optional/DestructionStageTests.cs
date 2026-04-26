using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

/// <summary>
/// Tests exercising the Destruction stage of TIC solving.
/// Destruction runs when ancestor and descendant are both concrete (or close to it)
/// and performs final type resolution: Composite x Composite matching, Optional wrapping,
/// stale snapshot handling, and struct width subtyping.
/// </summary>
class DestructionStageTests {

    #region Array x Array Destruction

    [Test(Description = "arr(i32) ancestor, arr(i32) descendant -> both arr(i32)")]
    public void ArrayArray_SameElementType() {
        // y:i32[] = x:i32[]
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVarType("y", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(StateArray.Of(I32), "y");
    }

    [Test(Description = "arr(real) ancestor, arr(i32) descendant -> element widened via Destruction")]
    public void ArrayArray_ElementWidened() {
        // y:real[] = x:i32[]
        // Destruction recurses into element nodes: Destruction(desc.Element, anc.Element)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVarType("y", StateArray.Of(Real));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(StateArray.Of(Real), "y");
    }

    [Test(Description = "arr(opt(i32)) ancestor, arr(i32) descendant -> element wrapped in optional via Destruction wrapping")]
    public void ArrayArray_ElementWrappedInOptional() {
        // y:opt(i32)[] = x:i32[]
        // During Destruction, arr(i32) descendant meets arr(opt(i32)) ancestor.
        // Destruction recurses into elements: i32 desc ≤ opt(i32) anc.
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVarType("y", StateArray.Of(StateOptional.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(StateArray.Of(StateOptional.Of(I32)), "y");
    }

    [Test(Description = "If-else with two arrays: [1i] and [2.0] -> arr(real) via Destruction element-by-element")]
    public void ArrayArray_IfElse_ElementsResolveThroughDestruction() {
        // y = if(a) [1i] else [2.0]
        // Both branches are arrays, Destruction recurses into elements: i32 vs real -> real
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetStrictArrayInit(2, 1);
        graph.SetConst(3, Real);
        graph.SetStrictArrayInit(4, 3);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Real), "y");
    }

    [Test(Description = "arr(real) ancestor, arr(bool) descendant -> error (incompatible element types)")]
    public void ArrayArray_IncompatibleElements_Fails() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateArray.Of(Bool));
            graph.SetVarType("y", StateArray.Of(Real));
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    #endregion

    #region Struct x Struct Destruction

    [Test(Description = "struct{x:i32} ancestor, struct{x:i32} descendant -> match")]
    public void StructStruct_SameFields() {
        // y:{x:i32} = v:{x:i32}
        var graph = new GraphBuilder();
        graph.SetVarType("v", StateStruct.Of("x", I32));
        graph.SetVarType("y", StateStruct.Of("x", I32));
        graph.SetVar("v", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("x", I32), "v");
        result.AssertNamed(StateStruct.Of("x", I32), "y");
    }

    [Test(Description = "struct{x:i32} ancestor, struct{x:i32,y:bool} descendant -> width subtyping")]
    public void StructStruct_WidthSubtyping() {
        // y:{x:i32} = v:{x:i32, y:bool}
        // Destruction: ancestor has fewer fields, descendant has more. Width subtyping allows this.
        var graph = new GraphBuilder();
        graph.SetVarType("v", StateStruct.Of(("x", I32), ("y", Bool)));
        graph.SetVar("v", 0);
        // y only requires field x
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "x" }, new[] { 1 }, 2);
        // v ≤ node2 (struct{x:i32})
        graph.GetOrCreateNode(0).AddAncestor(graph.GetOrCreateNode(2));
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        // v keeps both fields
        result.AssertNamed(StateStruct.Of(("x", I32), ("y", Bool)), "v");
    }

    [Test(Description = "struct{x:real} ancestor, struct{x:i32} descendant -> field widened")]
    public void StructStruct_FieldWidened() {
        // y:{x:real} = v:{x:i32}
        // Destruction recurses into fields: i32 desc ≤ real anc
        var graph = new GraphBuilder();
        graph.SetVarType("v", StateStruct.Of("x", I32));
        graph.SetVarType("y", StateStruct.Of("x", Real));
        graph.SetVar("v", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("x", I32), "v");
        result.AssertNamed(StateStruct.Of("x", Real), "y");
    }

    [Test(Description = "If-else with two structs: {a:i32} and {a:real} -> {a:real}")]
    public void StructStruct_IfElse_FieldsResolveThroughDestruction() {
        // y = if(c) {a=42i} else {a=1.0}
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 1 }, 2);
        graph.SetConst(3, Real);
        graph.SetStructInit(new[] { "a" }, new[] { 3 }, 4);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("a", Real), "y");
    }

    [Test(Description = "struct{x:i32} ancestor, struct{x:bool} descendant -> error (incompatible field types)")]
    public void StructStruct_IncompatibleFields_Fails() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("v", StateStruct.Of("x", Bool));
            graph.SetVarType("y", StateStruct.Of("x", I32));
            graph.SetVar("v", 0);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    #endregion

    #region Optional wrapping during Destruction

    [Test(Description = "opt(i32) ancestor, i32 descendant -> i32 lifted to opt(i32) via WrapDescendantInOptional")]
    public void Destruction_OptAncestor_PrimitiveDescendant_Lifts() {
        // y:opt(i32) = 42i
        // Destruction: opt(i32) ancestor, i32 descendant. WrapDescendantInOptional or fallback unwrap.
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetConst(0, I32);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "i32 ancestor, opt(i32) descendant -> ERROR (can't unwrap)")]
    public void Destruction_PrimitiveAncestor_OptDescendant_Fails() {
        // y:i32 = x:opt(i32)
        // opt(T) ≤ T is invalid — no implicit unwrap
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateOptional.Of(I32));
            graph.SetVarType("y", I32);
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    [Test(Description = "opt(arr(i32)) ancestor, arr(i32) descendant -> arr lifted via WrapDescendantInOptional")]
    public void Destruction_OptArrayAncestor_ArrayDescendant_Lifts() {
        // y:opt(i32[]) = x:i32[]
        // Destruction: opt(arr(i32)) ancestor, arr(i32) descendant -> WrapDescendantInOptional
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVarType("y", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    [Test(Description = "arr(i32) ancestor, opt(arr(i32)) descendant -> ERROR (WrapAncestorInOptional for array)")]
    public void Destruction_ArrayAncestor_OptArrayDescendant_WrapsAncestor() {
        // x:opt(i32[]) flows into context expecting arr(T)
        // During Destruction: arr ancestor, opt(arr) descendant -> WrapAncestorInOptional
        // This wraps ancestor in optional to converge types
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        // y expects array — but x is opt(array), so opt(arr) ≤ arr is invalid
        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateArray.Of(t), I32 }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        TestHelper.AssertThrowsTicError(() => graph.Solve());
    }

    [Test(Description = "struct{x:opt(i32)} ancestor, struct{x:i32} descendant -> field wrapped in optional")]
    public void Destruction_StructWithOptionalField_FieldLifted() {
        // y:{x:opt(i32)} = v:{x:i32}
        // Destruction: struct x struct, then field-by-field: opt(i32) anc vs i32 desc -> lift
        var graph = new GraphBuilder();
        graph.SetVarType("v", StateStruct.Of("x", I32));
        graph.SetVarType("y", StateStruct.Of("x", StateOptional.Of(I32)));
        graph.SetVar("v", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("x", I32), "v");
        result.AssertNamed(StateStruct.Of("x", StateOptional.Of(I32)), "y");
    }

    [Test(Description = "opt(struct{x:i32}) ancestor, struct{x:i32} descendant -> struct lifted via WrapDescendantInOptional")]
    public void Destruction_OptStructAncestor_StructDescendant_Lifts() {
        // y:opt({x:i32}) = v:{x:i32}
        var graph = new GraphBuilder();
        graph.SetVarType("v", StateStruct.Of("x", I32));
        graph.SetVarType("y", StateOptional.Of(StateStruct.Of("x", I32)));
        graph.SetVar("v", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("x", I32), "v");
        result.AssertNamed(StateOptional.Of(StateStruct.Of("x", I32)), "y");
    }

    #endregion

    #region Optional x Optional Destruction

    [Test(Description = "opt(i32) ancestor, opt(i32) descendant -> elements destructed")]
    public void OptionalOptional_SameElement() {
        // y:opt(i32) = x:opt(i32)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "x");
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "opt(real) ancestor, opt(i32) descendant -> element widened")]
    public void OptionalOptional_ElementWidened() {
        // y:opt(real) = x:opt(i32)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVarType("y", StateOptional.Of(Real));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "x");
        result.AssertNamed(StateOptional.Of(Real), "y");
    }

    [Test(Description = "opt(arr(i32)) ancestor, opt(arr(i32)) descendant -> elements destructed recursively")]
    public void OptionalOptional_ArrayElements() {
        // y:opt(i32[]) = x:opt(i32[])
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVarType("y", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "x");
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    #endregion

    #region Fun x Fun Destruction

    [Test(Description = "fun(i32)->bool ancestor, fun(i32)->bool descendant -> match")]
    public void FunFun_SameSignature() {
        // y:(i32)->bool = f:(i32)->bool
        var graph = new GraphBuilder();
        graph.SetVarType("f", StateFun.Of(I32, Bool));
        graph.SetVarType("y", StateFun.Of(I32, Bool));
        graph.SetVar("f", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateFun.Of(I32, Bool), "f");
        result.AssertNamed(StateFun.Of(I32, Bool), "y");
    }

    [Test(Description = "fun(i32)->real ancestor, fun(i32)->i32 descendant -> return widened")]
    public void FunFun_ReturnWidened() {
        // y:(i32)->real = f:(i32)->i32
        // Destruction recurses: args contra, return co
        var graph = new GraphBuilder();
        graph.SetVarType("f", StateFun.Of(I32, I32));
        graph.SetVarType("y", StateFun.Of(I32, Real));
        graph.SetVar("f", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateFun.Of(I32, I32), "f");
        result.AssertNamed(StateFun.Of(I32, Real), "y");
    }

    #endregion

    #region Constrains x Composite Destruction (stale snapshot path)

    [Test(Description = "Constrains with arr descendant snapshot, actual arr(i32) -> adopts arr(i32)")]
    public void ConstrainsComposite_ArrayFitsConstrains() {
        // Models: LCA node with stale constraint, resolved arr descendant
        // This exercises Apply(ConstraintsState, ICompositeState)
        // y = if(a) [1i] else [2i]
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetStrictArrayInit(2, 1);
        graph.SetConst(3, I32);
        graph.SetStrictArrayInit(4, 3);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "y");
    }

    [Test(Description = "Stale snapshot: arr(opt(T)) actual vs arr(U8) snapshot -> DescendantHasOptionalLift adopts actual")]
    public void StaleSnapshot_ArrayWithOptionalLift() {
        // Models the case where Phase 2 wraps array elements in Optional
        // after the constraint's Descendant was already set.
        // y = [42i, none]
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State);
        var elemNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateOptional>(elemNode.State);
        Assert.AreEqual(StateOptional.Of(I32), elemNode.State);
    }

    [Test(Description = "Stale snapshot: struct with optional-lifted field -> DescendantHasOptionalLift adopts actual")]
    public void StaleSnapshot_StructWithOptionalLiftedField() {
        // y = {a = if(c) 1i else none}
        // Field "a" becomes opt(i32) after Phase 2 but the struct snapshot may be stale.
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetStructInit(new[] { "a" }, new[] { 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateStruct>(yNode.State);
        var fieldNode = ((StateStruct)yNode.State).GetFieldOrNull("a").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(fieldNode.State);
        Assert.AreEqual(StateOptional.Of(I32), fieldNode.State);
    }

    #endregion

    #region Composite x Primitive Destruction (ICompositeState ancestor, StatePrimitive descendant)

    [Test(Description = "opt(i32) ancestor, None descendant -> None ≤ opt(T) is valid (no-op)")]
    public void Destruction_OptAncestor_NoneDescendant_Valid() {
        // y:opt(i32) = none
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetConst(0, None);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "opt(i32) ancestor, i32 descendant -> T ≤ opt(T), constrains element")]
    public void Destruction_OptAncestor_I32Descendant_ConstrainsElement() {
        // opt(T) ancestor with concrete i32 descendant
        // Destruction calls Destruction(descendantNode, opt.ElementNode) to constrain T=i32
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetConst(0, I32);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "arr(i32) ancestor, i32 descendant -> ERROR (non-optional composite can't accept primitive)")]
    public void Destruction_ArrayAncestor_PrimitiveDescendant_Fails() {
        // y:i32[] = 42i -> error, i32 is not an array
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("y", StateArray.Of(I32));
            graph.SetConst(0, I32);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    #endregion

    #region Composite x Constrains Destruction

    [Test(Description = "opt(i32) ancestor, unconstrained descendant -> descendant becomes opt(i32)")]
    public void Destruction_OptAncestor_UnconstrainedDescendant() {
        // y:opt(i32) = x (unconstrained)
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
        result.AssertNamed(StateOptional.Of(I32), "x");
    }

    [Test(Description = "struct{x:i32} ancestor, unconstrained descendant -> descendant becomes struct")]
    public void Destruction_StructAncestor_UnconstrainedDescendant() {
        // y:{x:i32} = v (unconstrained)
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateStruct.Of("x", I32));
        graph.SetVar("v", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("x", I32), "v");
        result.AssertNamed(StateStruct.Of("x", I32), "y");
    }

    [Test(Description = "arr(i32) ancestor, unconstrained descendant -> descendant becomes arr(i32)")]
    public void Destruction_ArrayAncestor_UnconstrainedDescendant() {
        // y:i32[] = x (unconstrained)
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(StateArray.Of(I32), "y");
    }

    #endregion

    #region Complex multi-node graphs exercising Destruction

    [Test(Description = "If-else with None branch: if(cond) x:i32 else none -> opt(i32)")]
    public void IfElse_NoneAndConcrete_OptionalResult() {
        // y = if(cond) x else none, x:i32
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("cond", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "Generic function returning Optional: wrap(x) where wrap:T->opt(T), x:i32 -> opt(i32)")]
    public void GenericFun_ReturnsOptional_ConcreteArg() {
        // y = wrap(x), wrap:T->opt(T), x:i32
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        graph.SetCall(new ITicNodeState[] { t, StateOptional.Of(t) }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "Two variables sharing constraint: x:i32, y:real, z = if(c) x else y -> z:real")]
    public void TwoVars_SharedConstraint_ResolvedViaDestruction() {
        // z = if(c) x else y, x:i32, y:real
        // Both x and y flow into LCA node, Destruction resolves element types
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVarType("yv", Real);
        graph.SetVar("c", 0);
        graph.SetVar("x", 1);
        graph.SetVar("yv", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "z");
    }

    [Test(Description = "Chain: x:i32[] -> if(c) x else none -> opt(i32[]) -> via Destruction wrapping")]
    public void Chain_ArrayIntoIfElseNone_OptionalArrayResult() {
        // y = if(c) x else none, x:i32[]
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVar("c", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    [Test(Description = "Nested: if(c) {a=1i} else {a=2i} -> {a:i32}, Destruction field-by-field")]
    public void IfElse_TwoStructs_FieldDestructedElementByElement() {
        // y = if(c) {a=1i} else {a=2i}
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 1 }, 2);
        graph.SetConst(3, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 3 }, 4);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of("a", I32), "y");
    }

    [Test(Description = "opt(opt(i32)) flattens to opt(i32) through Destruction")]
    public void Destruction_NestedOptional_Flattens() {
        // x:opt(opt(i32)); y = x -> opt(i32)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateOptional.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "Constrains x Constrains Destruction: merge two generic intervals")]
    public void ConstrainsConstrains_MergeIntervals() {
        // x:[U8..Real], y:[I32..Real], z = if(c) x else y
        // LCA of both intervals: [LCA(U8,I32)..GCD(Real,Real)] = [I32..Real]
        // But since x has U8 desc and y has I32 desc, merge gives LCA(U8,I32)=I32 desc
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetIntConst(1, U8);    // [U8..Real]
        graph.SetIntConst(2, I32);   // [I32..Real]
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        // Result should be a generic with merged constraints
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(yNode.State,
            $"Expected ConstraintsState but got {yNode.State}");
        var cs = (ConstraintsState)yNode.State;
        Assert.IsTrue(cs.HasDescendant);
        // LCA(U8, I32) = I32
        Assert.AreEqual(I32, cs.Descendant);
        Assert.AreEqual(Real, cs.Ancestor);
    }

    [Test(Description = "Primitive x Constrains Destruction: concrete resolves generic")]
    public void PrimitiveConstrains_ConcreteResolvesGeneric() {
        // x:i32, y = if(c) x else 42
        // One branch is concrete I32, the other is [U8..Real] int literal
        // Destruction resolves: I32 fits into [U8..Real], so constraint becomes I32
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("c", 0);
        graph.SetVar("x", 1);
        graph.SetIntConst(2, U8);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "Constrains x Primitive Destruction: generic resolved by concrete")]
    public void ConstrainsPrimitive_GenericResolvedByConcrete() {
        // x = if(c) 42 else r:real
        // [U8..Real] vs Real concrete -> resolved to Real
        var graph = new GraphBuilder();
        graph.SetVarType("r", Real);
        graph.SetVar("c", 0);
        graph.SetIntConst(1, U8);
        graph.SetVar("r", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }

    #endregion

    #region Destruction with equality (Primitive x Primitive through generic)

    [Test(Description = "Equality: x:i32 == z:i32 -> bool, generics resolve through Destruction")]
    public void Destruction_Equality_SameType() {
        // y = (x:i32) == (z:i32) -> bool
        // Generic T resolves to I32, return is Bool
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVarType("z", I32);
        graph.SetVar("x", 0);
        graph.SetVar("z", 1);
        graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    #endregion

    #region Destruction: Primitive x Primitive (trivial, always returns true)

    [Test(Description = "i32 ancestor, i32 descendant -> both stay i32 (trivial Destruction)")]
    public void PrimitivePrimitive_SameType() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVarType("y", I32);
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "x");
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "real ancestor, i32 descendant -> i32 ≤ real (widening)")]
    public void PrimitivePrimitive_Widened() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVarType("y", Real);
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "x");
        result.AssertNamed(Real, "y");
    }

    #endregion
}
