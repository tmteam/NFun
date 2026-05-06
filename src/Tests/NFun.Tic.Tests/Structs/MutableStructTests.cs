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
}
