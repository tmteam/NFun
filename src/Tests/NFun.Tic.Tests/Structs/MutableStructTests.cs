using System.Collections.Generic;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// TIC-level tests for StateMutableStruct.
///
/// Algebraic rules:
///   MutStruct <: Struct  (read-only view is always safe)
///   MutStruct fields are invariant  (MutStruct{a:int} != MutStruct{a:real})
///   Width subtyping works  (MutStruct{a,b} <: MutStruct{a})
///   Struct cannot fit into MutStruct  (can't upgrade immutable to mutable)
/// </summary>
public class MutableStructTests {

    #region Basic MutableStruct creation and solving

    [Test]
    public void MutStruct_SingleField_SolvesToCorrectType() {
        //    1      0
        // y = mut{ a = 42i }
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();

        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
        var mutStruct = (StateMutableStruct)yState;
        Assert.AreEqual(1, mutStruct.FieldsCount);
        Assert.AreEqual(I32, mutStruct.GetFieldOrNull("a").GetNonReference().State);
    }

    [Test]
    public void MutStruct_TwoFields_SolvesToCorrectTypes() {
        //    2        0        1
        // y = mut{ a = 42i, b = 1.0 }
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Real);
        graph.SetMutableStructInit(new[] { "a", "b" }, new[] { 0, 1 }, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
        var mutStruct = (StateMutableStruct)yState;
        Assert.AreEqual(2, mutStruct.FieldsCount);
        Assert.AreEqual(I32, mutStruct.GetFieldOrNull("a").GetNonReference().State);
        Assert.AreEqual(Real, mutStruct.GetFieldOrNull("b").GetNonReference().State);
    }

    #endregion

    #region MutStruct <: Struct subtyping

    [Test]
    public void MutStruct_SubtypesStruct_AssignToStructVar() {
        // MutStruct{a:I32} should be assignable where Struct{a:I32} is expected
        //    1         0
        // x = mut{ a = 42i }
        // y:{ a: int } = x
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("x", 1);

        // y expects immutable struct with field a:I32
        graph.SetVarType("y", StateStruct.Of(("a", I32)));
        graph.SetVar("x", 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();

        // x should be MutStruct
        var xState = result.GetVariableNode("x").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(xState);

        // y should resolve to the declared struct type
        result.AssertNamed(StateStruct.Of(("a", I32)), "y");
    }

    [Test]
    public void MutStruct_SubtypesStruct_FieldAccess() {
        // Reading a field from MutStruct is the same as from Struct
        //    1         0
        // x = mut{ a = 42i }
        // y = x.a
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("x", 1);

        graph.SetVar("x", 2);
        graph.SetFieldAccess(2, 3, "a");
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region Field invariance

    [Test]
    public void MutStruct_FieldInvariance_SameTypesMerge() {
        // Two MutStruct{a:I32} should merge fine
        //    1         0
        // x = mut{ a = 42i }
        //    3         2
        // y = mut{ a = 24i }
        // z merges x and y (e.g., if-else)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        graph.SetConst(2, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 2 }, 3);

        // if-else: result is LCA(MutStruct{a:I32}, MutStruct{a:I32})
        graph.SetIfElse(new[] { 4 }, new[] { 1, 3 }, 5);
        graph.SetDef("z", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var zState = result.GetVariableNode("z").GetNonReference().State;
        // LCA of identical MutStructs = MutStruct
        Assert.IsInstanceOf<StateMutableStruct>(zState);
    }

    [Test]
    public void MutStruct_FieldInvariance_DifferentTypes_DowngradesToImmutable() {
        // LCA(MutStruct{a:I32}, MutStruct{a:Real}) = Struct{a:Real}
        // Because Unify(I32, Real) fails, it downgrades to immutable with LCA per field.
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        graph.SetConst(2, Real);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 2 }, 3);

        // if-else: result is LCA(MutStruct{a:I32}, MutStruct{a:Real})
        graph.SetIfElse(new[] { 4 }, new[] { 1, 3 }, 5);
        graph.SetDef("z", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var zState = result.GetVariableNode("z").GetNonReference().State;
        // Downgraded to immutable struct
        Assert.IsInstanceOf<StateStruct>(zState);
        Assert.IsNotInstanceOf<StateMutableStruct>(zState);
        // Field type is LCA(I32, Real) = Real
        var structState = (StateStruct)zState;
        Assert.AreEqual(Real, structState.GetFieldOrNull("a").GetNonReference().State);
    }

    #endregion

    #region LCA: MutStruct x Struct

    [Test]
    public void LCA_MutStruct_Struct_UpcastsToImmutable() {
        // LCA(MutStruct{a:I32}, Struct{a:I32}) = Struct{a:I32}
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        graph.SetConst(2, I32);
        graph.SetStructInit(new[] { "a" }, new[] { 2 }, 3);

        // if-else: result is LCA(MutStruct{a:I32}, Struct{a:I32})
        graph.SetIfElse(new[] { 4 }, new[] { 1, 3 }, 5);
        graph.SetDef("z", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var zState = result.GetVariableNode("z").GetNonReference().State;
        // Should be immutable struct (LCA of Mut+Immut = Immut)
        Assert.IsInstanceOf<StateStruct>(zState);
        Assert.IsNotInstanceOf<StateMutableStruct>(zState);
    }

    #endregion

    #region Width subtyping

    [Test]
    public void MutStruct_WidthSubtyping_ExtraFieldsOk() {
        // MutStruct{a:I32, b:Real} should be usable where MutStruct{a:I32} is expected
        // (width subtyping: hiding fields is safe)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Real);
        graph.SetMutableStructInit(new[] { "a", "b" }, new[] { 0, 1 }, 2);
        graph.SetDef("x", 2);

        // Read field a (which exists in the wider struct)
        graph.SetVar("x", 3);
        graph.SetFieldAccess(3, 4, "a");
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region Struct cannot fit into MutStruct

    [Test]
    public void Struct_CannotFitIntoMutStruct_MergeReturnsNull() {
        // Verify at algebra level: Struct and MutStruct cannot merge
        var structState = StateStruct.Of(("a", I32));
        var mutStructState = StateMutableStruct.Of(("a", I32));

        var merged = SolvingFunctions.GetMergedStateOrNull(structState, mutStructState);
        Assert.IsNull(merged, "Struct should not merge with MutStruct");

        merged = SolvingFunctions.GetMergedStateOrNull(mutStructState, structState);
        Assert.IsNull(merged, "MutStruct should not merge with Struct");
    }

    #endregion

    #region Algebra unit tests

    [Test]
    public void FitsInto_MutStruct_IntoStruct_IsTrue() {
        // MutStruct{a:I32} fits into Struct{a:I32} (read-only view)
        ITicNodeState mutStruct = StateMutableStruct.Of(("a", I32));
        ITicNodeState immutableStruct = StateStruct.Of(("a", I32));
        Assert.IsTrue(mutStruct.FitsInto(immutableStruct));
    }

    [Test]
    public void FitsInto_Struct_IntoMutStruct_IsFalse() {
        // Struct{a:I32} cannot fit into MutStruct{a:I32}
        ITicNodeState immutableStruct = StateStruct.Of(("a", I32));
        ITicNodeState mutStruct = StateMutableStruct.Of(("a", I32));
        Assert.IsFalse(immutableStruct.FitsInto(mutStruct));
    }

    [Test]
    public void FitsInto_MutStruct_IntoMutStruct_SameFields_IsTrue() {
        ITicNodeState a = StateMutableStruct.Of(("x", I32));
        ITicNodeState b = StateMutableStruct.Of(("x", I32));
        Assert.IsTrue(a.FitsInto(b));
    }

    [Test]
    public void FitsInto_MutStruct_IntoMutStruct_DifferentFieldTypes_IsFalse() {
        // Invariant: MutStruct{a:I32} does NOT fit into MutStruct{a:Real}
        ITicNodeState a = StateMutableStruct.Of(("a", I32));
        ITicNodeState b = StateMutableStruct.Of(("a", Real));
        Assert.IsFalse(a.FitsInto(b));
    }

    [Test]
    public void FitsInto_MutStruct_WidthSubtyping() {
        // MutStruct{a:I32, b:Real} fits into MutStruct{a:I32} (width subtyping)
        ITicNodeState wider = StateMutableStruct.Of(("a", I32), ("b", Real));
        ITicNodeState narrower = StateMutableStruct.Of(("a", I32));
        Assert.IsTrue(wider.FitsInto(narrower));
    }

    [Test]
    public void Unify_MutStruct_MutStruct_SameFields() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", I32));
        var result = Algebra.StateExtensions.UnifyOrNull(a, b);
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<StateMutableStruct>(result);
    }

    [Test]
    public void Unify_MutStruct_Struct_ReturnsNull() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateStruct.Of(("a", I32));
        var result = Algebra.StateExtensions.UnifyOrNull(a, b);
        Assert.IsNull(result, "MutStruct and Struct are different type constructors — cannot unify");
    }

    [Test]
    public void Unify_MutStruct_MutStruct_DifferentFieldTypes_ReturnsNull() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", Real));
        var result = Algebra.StateExtensions.UnifyOrNull(a, b);
        Assert.IsNull(result, "Invariant fields: I32 != Real");
    }

    [Test]
    public void LCA_MutStruct_MutStruct_SameFields() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", I32));
        var result = Algebra.StateExtensions.Lca(a, b);
        Assert.IsInstanceOf<StateMutableStruct>(result);
    }

    [Test]
    public void LCA_MutStruct_MutStruct_DifferentFields_DowngradesToStruct() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", Real));
        var result = Algebra.StateExtensions.Lca(a, b);
        // Unify(I32, Real) fails → downgrade to immutable Struct with LCA per field
        Assert.IsInstanceOf<StateStruct>(result);
        Assert.IsNotInstanceOf<StateMutableStruct>(result);
    }

    [Test]
    public void LCA_MutStruct_Struct_AlwaysImmutable() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateStruct.Of(("a", I32));
        var result = Algebra.StateExtensions.Lca(a, b);
        Assert.IsInstanceOf<StateStruct>(result);
        Assert.IsNotInstanceOf<StateMutableStruct>(result);
    }

    [Test]
    public void GCD_MutStruct_MutStruct_SameFields() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", I32));
        var result = Algebra.StateExtensions.Gcd(a, b);
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<StateMutableStruct>(result);
    }

    [Test]
    public void GCD_MutStruct_MutStruct_DifferentFieldTypes_ReturnsNull() {
        // GCD with invariant fields: Unify(I32, Real) fails → null
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", Real));
        var result = Algebra.StateExtensions.Gcd(a, b);
        Assert.IsNull(result);
    }

    [Test]
    public void GCD_Struct_MutStruct_ReturnsMutStruct() {
        // GCD(Struct{a:I32}, MutStruct{a:I32}) = MutStruct (more specific)
        var a = StateStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", I32));
        var result = Algebra.StateExtensions.Gcd(a, b);
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<StateMutableStruct>(result);
    }

    [Test]
    public void MutStruct_Equals_OnlySameType() {
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", I32));
        var c = StateStruct.Of(("a", I32));

        Assert.IsTrue(a.Equals(b));
        Assert.IsFalse(a.Equals(c), "MutStruct should not equal Struct");
    }

    [Test]
    public void MutStruct_GetNonReferenced_PreservesMutability() {
        var fields = new Dictionary<string, TicNode>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "a", TicNode.CreateTypeVariableNode(I32) }
        };
        var mutStruct = new StateMutableStruct(fields, false);
        var nonRef = mutStruct.GetNonReferenced();
        Assert.IsInstanceOf<StateMutableStruct>(nonRef);
    }

    #endregion

    #region Pull/Push through constraint graph

    [Test]
    public void Pull_MutStruct_FieldAccessThroughFunction() {
        // f(s) = s.a  where s is MutStruct
        // Models: user function taking a struct arg and reading a field
        //
        //   s (input) → [0] → fieldAccess(.a) → [1] → f (return)
        //
        var graph = new GraphBuilder();
        var funType = graph.SetFunDef("f", 1, null, "s");

        graph.SetVar("s", 0);
        graph.SetFieldAccess(0, 1, "a");

        // Call site: f(mut{a = 42i})
        graph.SetConst(10, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 10 }, 11);

        // call f(11) → result at 12
        graph.SetCall("f", 11, 12);
        graph.SetDef("y", 12);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void Pull_MutStruct_IfElseBranches() {
        // z = if(cond) mut{a=1i, b=true} else mut{a=2i, b=false}
        // Pull merges two MutStruct branches
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Bool);
        graph.SetMutableStructInit(new[] { "a", "b" }, new[] { 0, 1 }, 2);

        graph.SetConst(3, I32);
        graph.SetConst(4, Bool);
        graph.SetMutableStructInit(new[] { "a", "b" }, new[] { 3, 4 }, 5);

        graph.SetIfElse(new[] { 6 }, new[] { 2, 5 }, 7);
        graph.SetDef("z", 7);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var zState = result.GetVariableNode("z").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(zState);
        var ms = (StateMutableStruct)zState;
        Assert.AreEqual(I32, ms.GetFieldOrNull("a").GetNonReference().State);
        Assert.AreEqual(Bool, ms.GetFieldOrNull("b").GetNonReference().State);
    }

    [Test]
    public void Push_StructAncestor_MutStructDescendant() {
        // y:{a:int} = mut{a = 42i}
        // Struct ancestor accepts MutStruct descendant (MutStruct <: Struct)
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateStruct.Of(("a", I32)));
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of(("a", I32)), "y");
    }

    #endregion

    #region MutStruct inside Optional

    [Test]
    public void MutStruct_InsideOptional() {
        // x:opt(MutStruct{a:int}) = mut{a=42i}
        // Optional wrapping of a mutable struct
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetVarType("x", StateOptional.Of(StateMutableStruct.Of(("a", I32))));
        graph.SetDef("x", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateMutableStruct.Of(("a", I32))), "x");
    }

    [Test]
    public void LCA_MutStruct_None_ProducesOptional() {
        // z = if(cond) mut{a=42i} else none
        // LCA(MutStruct{a:int}, None) = opt(MutStruct{a:int})
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        graph.SetConst(2, None);

        graph.SetIfElse(new[] { 3 }, new[] { 1, 2 }, 4);
        graph.SetDef("z", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var zState = result.GetVariableNode("z").GetNonReference().State;
        Assert.IsInstanceOf<StateOptional>(zState);
        var opt = (StateOptional)zState;
        var inner = opt.ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(inner);
    }

    #endregion

    #region MutStruct inside Array

    [Test]
    public void MutStruct_InsideArray() {
        // y:arr(MutStruct{a:int}) — array of mutable structs via type annotation
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        // [mut{a=42i}] — single-element array
        graph.SetSoftArrayInit(2, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateArray>(yState);
        var arr = (StateArray)yState;
        var elem = arr.ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(elem);
    }

    [Test]
    public void ArrayOfMutStructs() {
        // [mut{a=1i}, mut{a=2i}] — array literal of mutable structs
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        graph.SetConst(2, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 2 }, 3);

        graph.SetSoftArrayInit(4, 1, 3);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateArray>(yState);
        var arr = (StateArray)yState;
        var elem = arr.ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(elem);
        var ms = (StateMutableStruct)elem;
        Assert.AreEqual(I32, ms.GetFieldOrNull("a").GetNonReference().State);
    }

    #endregion

    #region MutStruct with generic fields

    [Test]
    public void MutStruct_GenericField() {
        // y = mut{a = x} where x is unconstrained
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null);
        result.AssertAreGenerics(generic, "x");
    }

    [Test]
    public void MutStruct_FieldConstrainedByCall() {
        // x = mut{a = 42i}
        // y:real = x.a + 1.0   (constrains field 'a' upward via arithmetic)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("x", 1);

        graph.SetVar("x", 2);
        graph.SetFieldAccess(2, 3, "a");

        graph.SetConst(4, Real);
        graph.SetArith(3, 4, 5);
        graph.SetVarType("y", Real);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }

    #endregion

    #region MutStruct in functions

    [Test]
    public void Function_TakesMutStruct_ReadsField() {
        // getA(s) = s.a
        // y = getA(mut{a = 42i})
        var graph = new GraphBuilder();
        graph.SetFunDef("getA", 1, null, "s");
        graph.SetVar("s", 0);
        graph.SetFieldAccess(0, 1, "a");

        graph.SetConst(10, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 10 }, 11);
        graph.SetCall("getA", 11, 12);
        graph.SetDef("y", 12);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    [Test]
    public void Function_ReturnsMutStruct() {
        // mkStruct(v) = mut{a = v}
        // y = mkStruct(42i)
        var graph = new GraphBuilder();
        graph.SetFunDef("mkStruct", 1, null, "v");
        graph.SetVar("v", 0);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);

        graph.SetConst(10, I32);
        graph.SetCall("mkStruct", 10, 11);
        graph.SetDef("y", 11);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
        var ms = (StateMutableStruct)yState;
        Assert.AreEqual(I32, ms.GetFieldOrNull("a").GetNonReference().State);
    }

    [Test]
    public void GenericFunction_PreservesMutStruct() {
        // identity(x) = x
        // y = identity(mut{a = 42i})
        var graph = new GraphBuilder();
        graph.SetFunDef("identity", 0, null, "x");
        graph.SetVar("x", 0);

        graph.SetConst(10, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 10 }, 11);
        graph.SetCall("identity", 11, 12);
        graph.SetDef("y", 12);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
    }

    #endregion

    #region Destruction and constraint propagation

    [Test]
    public void Destruction_StructAncestor_MutStructDescendant_Compatible() {
        // y:{a:real} = mut{a = 42i}
        // Struct{a:real} ancestor + MutStruct{a:int} descendant → OK
        // MutStruct <: Struct, and I32 <: Real (covariant in immutable view)
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateStruct.Of(("a", Real)));
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateStruct.Of(("a", Real)), "y");
    }

    [Test]
    public void Destruction_MutStructAncestor_FieldTypeWidens() {
        // y:mut{a:real} = mut{a = 42i}
        // Ancestor MutStruct{a:real} pushes Real constraint into field;
        // I32 widens to Real since field node is fresh
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateMutableStruct.Of(("a", Real)));
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
    }

    [Test]
    public void Destruction_MutStruct_InvariantFields_AlgebraError() {
        // Algebra level: LCA(MutStruct{a:I32}, MutStruct{a:I32}) = MutStruct (OK)
        // But Unify(MutStruct{a:I32}, MutStruct{a:Real}) = null (invariant failure)
        var a = StateMutableStruct.Of(("a", I32));
        var b = StateMutableStruct.Of(("a", Real));
        var result = Algebra.StateExtensions.UnifyOrNull(a, b);
        Assert.IsNull(result, "Invariant fields: I32 != Real, unify must fail");
    }

    #endregion

    #region Edge cases

    [Test]
    public void MutStruct_Empty() {
        // y = mut{} — empty mutable struct (no fields)
        var graph = new GraphBuilder();
        graph.SetMutableStructInit(new string[] { }, new int[] { }, 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
        var ms = (StateMutableStruct)yState;
        Assert.AreEqual(0, ms.FieldsCount);
    }

    [Test]
    public void MutStruct_ManyFields() {
        // y = mut{a=1i, b=2.0, c=true, d='x', e=1i}
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Real);
        graph.SetConst(2, Bool);
        graph.SetConst(3, Char);
        graph.SetConst(4, I64);
        graph.SetMutableStructInit(
            new[] { "a", "b", "c", "d", "e" },
            new[] { 0, 1, 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
        var ms = (StateMutableStruct)yState;
        Assert.AreEqual(5, ms.FieldsCount);
        Assert.AreEqual(I32, ms.GetFieldOrNull("a").GetNonReference().State);
        Assert.AreEqual(Real, ms.GetFieldOrNull("b").GetNonReference().State);
        Assert.AreEqual(Bool, ms.GetFieldOrNull("c").GetNonReference().State);
        Assert.AreEqual(Char, ms.GetFieldOrNull("d").GetNonReference().State);
        Assert.AreEqual(I64, ms.GetFieldOrNull("e").GetNonReference().State);
    }

    [Test]
    public void MutStruct_FieldNameCaseInsensitive() {
        // MutStruct fields use OrdinalIgnoreCase — "A" and "a" are the same field
        // y = mut{a = 42i}
        // z = y.A   (uppercase access)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("x", 1);

        graph.SetVar("x", 2);
        graph.SetFieldAccess(2, 3, "A");
        graph.SetDef("z", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "z");
    }

    [Test]
    public void MutStruct_Nested() {
        // y = mut{inner = mut{x = 42i}}
        // z = y.inner.x
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetMutableStructInit(new[] { "x" }, new[] { 0 }, 1);
        graph.SetMutableStructInit(new[] { "inner" }, new[] { 1 }, 2);
        graph.SetDef("y", 2);

        graph.SetVar("y", 3);
        graph.SetFieldAccess(3, 4, "inner");
        graph.SetFieldAccess(4, 5, "x");
        graph.SetDef("z", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "z");

        // Verify outer is MutStruct
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.IsInstanceOf<StateMutableStruct>(yState);
        // Verify inner is MutStruct
        var innerNode = ((StateMutableStruct)yState).GetFieldOrNull("inner").GetNonReference();
        Assert.IsInstanceOf<StateMutableStruct>(innerNode.State);
    }

    #endregion
}
