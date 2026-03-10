namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class LcaOptionalTest {

    // ===================================================================
    // None x None
    // ===================================================================

    [Test]
    public void None_LCA_None_ReturnsNone() =>
        AssertLca(None, None, None);

    // ===================================================================
    // None x Primitives (via Lca extension: LcaWithNone)
    // ===================================================================

    [Test]
    public void None_LCA_Any_ReturnsAny() =>
        // none <: any, so LCA(None, Any) = Any
        AssertLca(None, Any, Any);

    [Test]
    public void None_LCA_Bool_ReturnsOptBool() =>
        AssertLca(None, Bool, Optional(Bool));

    [Test]
    public void None_LCA_Char_ReturnsOptChar() =>
        AssertLca(None, Char, Optional(Char));

    [Test]
    public void None_LCA_I32_ReturnsOptI32() =>
        AssertLca(None, I32, Optional(I32));

    [Test]
    public void None_LCA_Real_ReturnsOptReal() =>
        AssertLca(None, Real, Optional(Real));

    [Test]
    public void None_LCA_U8_ReturnsOptU8() =>
        AssertLca(None, U8, Optional(U8));

    [Test]
    public void None_LCA_I16_ReturnsOptI16() =>
        AssertLca(None, I16, Optional(I16));

    [Test]
    public void None_LCA_I64_ReturnsOptI64() =>
        AssertLca(None, I64, Optional(I64));

    [Test]
    public void None_LCA_U64_ReturnsOptU64() =>
        AssertLca(None, U64, Optional(U64));

    [Test]
    public void None_LCA_U32_ReturnsOptU32() =>
        AssertLca(None, U32, Optional(U32));

    [Test]
    public void None_LCA_U16_ReturnsOptU16() =>
        AssertLca(None, U16, Optional(U16));

    [Test]
    public void None_LCA_Ip_ReturnsOptIp() =>
        AssertLca(None, Ip, Optional(Ip));

    [Test]
    public void None_LCA_I96_ReturnsOptI96() =>
        AssertLca(None, I96, Optional(I96));

    [Test]
    public void None_LCA_I48_ReturnsOptI48() =>
        AssertLca(None, I48, Optional(I48));

    [Test]
    public void None_LCA_I24_ReturnsOptI24() =>
        AssertLca(None, I24, Optional(I24));

    // ===================================================================
    // None x Optional
    // ===================================================================

    [Test]
    public void None_LCA_OptI32_ReturnsOptI32() =>
        AssertLca(None, Optional(I32), Optional(I32));

    [Test]
    public void None_LCA_OptReal_ReturnsOptReal() =>
        AssertLca(None, Optional(Real), Optional(Real));

    [Test]
    public void None_LCA_OptBool_ReturnsOptBool() =>
        AssertLca(None, Optional(Bool), Optional(Bool));

    [Test]
    public void None_LCA_OptChar_ReturnsOptChar() =>
        AssertLca(None, Optional(Char), Optional(Char));

    [Test]
    public void None_LCA_OptU8_ReturnsOptU8() =>
        AssertLca(None, Optional(U8), Optional(U8));

    [Test]
    public void None_LCA_OptArrayI32_ReturnsOptArrayI32() =>
        AssertLca(None, Optional(Array(I32)), Optional(Array(I32)));

    // ===================================================================
    // None x Composites
    // ===================================================================

    [Test]
    public void None_LCA_ArrayI32_ReturnsOptArrayI32() =>
        AssertLca(None, Array(I32), Optional(Array(I32)));

    [Test]
    public void None_LCA_ArrayReal_ReturnsOptArrayReal() =>
        AssertLca(None, Array(Real), Optional(Array(Real)));

    [Test]
    public void None_LCA_ArrayBool_ReturnsOptArrayBool() =>
        AssertLca(None, Array(Bool), Optional(Array(Bool)));

    [Test]
    public void None_LCA_ArrayOfArray_ReturnsOptArrayOfArray() =>
        AssertLca(None, Array(Array(I32)), Optional(Array(Array(I32))));

    [Test]
    public void None_LCA_Struct_ReturnsOptStruct() =>
        AssertLca(None, Struct("a", I32), Optional(Struct("a", I32)));

    [Test]
    public void None_LCA_StructMultiField_ReturnsOptStruct() =>
        AssertLca(None,
            Struct(("a", I32), ("b", Real)),
            Optional(Struct(("a", I32), ("b", Real))));

    [Test]
    public void None_LCA_Fun_ReturnsOptFun() =>
        AssertLca(None, Fun(I32, Real), Optional(Fun(I32, Real)));

    [Test]
    public void None_LCA_FunNoArgs_ReturnsOptFun() =>
        AssertLca(None, Fun(Real), Optional(Fun(Real)));

    // ===================================================================
    // None x Constraints
    // ===================================================================

    [Test]
    public void None_LCA_EmptyConstraints_ReturnsNone() =>
        AssertLca(None, EmptyConstraints, None);

    [Test]
    public void None_LCA_ConstrainsDescI32_ReturnsOptI32() =>
        AssertLca(None, Constrains(desc: I32), Optional(I32));

    [Test]
    public void None_LCA_ConstrainsDescU8_ReturnsOptU8() =>
        AssertLca(None, Constrains(desc: U8), Optional(U8));

    [Test]
    public void None_LCA_ConstrainsDescReal_ReturnsOptReal() =>
        AssertLca(None, Constrains(desc: Real), Optional(Real));

    [Test]
    public void None_LCA_ConstrainsDescBool_ReturnsOptBool() =>
        AssertLca(None, Constrains(desc: Bool), Optional(Bool));

    [Test]
    public void None_LCA_ConstrainsDescAny_ReturnsAny() =>
        // none <: any, LCA(None, Any) = Any
        AssertLca(None, Constrains(desc: Any), Any);

    // ===================================================================
    // Optional x Optional (covariant)
    // ===================================================================

    [Test]
    public void OptI32_LCA_OptI32_ReturnsOptI32() =>
        AssertLca(Optional(I32), Optional(I32), Optional(I32));

    [Test]
    public void OptI32_LCA_OptReal_ReturnsOptReal() =>
        AssertLca(Optional(I32), Optional(Real), Optional(Real));

    [Test]
    public void OptU8_LCA_OptI32_ReturnsOptI32() =>
        AssertLca(Optional(U8), Optional(I32), Optional(I32));

    [Test]
    public void OptU8_LCA_OptU32_ReturnsOptU32() =>
        AssertLca(Optional(U8), Optional(U32), Optional(U32));

    [Test]
    public void OptI16_LCA_OptU16_ReturnsOptI24() =>
        AssertLca(Optional(I16), Optional(U16), Optional(I24));

    [Test]
    public void OptBool_LCA_OptI32_ReturnsAny() =>
        // LCA(Bool, I32) = Any, opt(any) = any
        AssertLca(Optional(Bool), Optional(I32), Any);

    [Test]
    public void OptChar_LCA_OptBool_ReturnsAny() =>
        // LCA(Char, Bool) = Any, opt(any) = any
        AssertLca(Optional(Char), Optional(Bool), Any);

    [Test]
    public void OptChar_LCA_OptI32_ReturnsAny() =>
        // LCA(Char, I32) = Any, opt(any) = any
        AssertLca(Optional(Char), Optional(I32), Any);

    [Test]
    public void OptBool_LCA_OptBool_ReturnsOptBool() =>
        AssertLca(Optional(Bool), Optional(Bool), Optional(Bool));

    [Test]
    public void OptChar_LCA_OptChar_ReturnsOptChar() =>
        AssertLca(Optional(Char), Optional(Char), Optional(Char));

    [Test]
    public void OptReal_LCA_OptReal_ReturnsOptReal() =>
        AssertLca(Optional(Real), Optional(Real), Optional(Real));

    [Test]
    public void OptU64_LCA_OptI64_ReturnsOptI96() =>
        AssertLca(Optional(U64), Optional(I64), Optional(I96));

    [Test]
    public void OptI32_LCA_OptU32_ReturnsOptI48() =>
        AssertLca(Optional(I32), Optional(U32), Optional(I48));

    [Test]
    public void OptU8_LCA_OptReal_ReturnsOptReal() =>
        AssertLca(Optional(U8), Optional(Real), Optional(Real));

    [Test]
    public void OptArrayI32_LCA_OptArrayReal_ReturnsOptArrayReal() =>
        AssertLca(Optional(Array(I32)), Optional(Array(Real)), Optional(Array(Real)));

    [Test]
    public void OptArrayI32_LCA_OptArrayI32_ReturnsOptArrayI32() =>
        AssertLca(Optional(Array(I32)), Optional(Array(I32)), Optional(Array(I32)));

    [Test]
    public void OptArrayU8_LCA_OptArrayI32_ReturnsOptArrayI32() =>
        AssertLca(Optional(Array(U8)), Optional(Array(I32)), Optional(Array(I32)));

    [Test]
    public void OptArrayChar_LCA_OptArrayBool_ReturnsOptArrayAny() =>
        // LCA(Array(Char), Array(Bool)) = Array(Any), so Opt(Array(Any))
        // Note: inner is Array(Any), not Any itself, so optional doesn't collapse
        AssertLca(Optional(Array(Char)), Optional(Array(Bool)), Optional(Array(Any)));

    [Test]
    public void OptStructA_LCA_OptStructA_ReturnsOptStruct() =>
        AssertLca(
            Optional(Struct("a", I32)),
            Optional(Struct("a", I32)),
            Optional(Struct("a", I32)));

    [Test]
    public void OptStructA_I32_LCA_OptStructA_Real_ReturnsOptStructA_Real() =>
        AssertLca(
            Optional(Struct("a", I32)),
            Optional(Struct("a", Real)),
            Optional(Struct("a", Real)));

    // ===================================================================
    // Optional x Primitives
    // ===================================================================

    [Test]
    public void OptI32_LCA_I32_ReturnsOptI32() =>
        AssertLca(Optional(I32), I32, Optional(I32));

    [Test]
    public void OptI32_LCA_Real_ReturnsOptReal() =>
        // LCA(I32, Real) = Real, so Opt(Real)
        AssertLca(Optional(I32), Real, Optional(Real));

    [Test]
    public void OptI32_LCA_U8_ReturnsOptI32() =>
        // LCA(I32, U8) = I32, so Opt(I32)
        AssertLca(Optional(I32), U8, Optional(I32));

    [Test]
    public void OptI32_LCA_Bool_ReturnsAny() =>
        // LCA(I32, Bool) = Any, opt(any) = any
        AssertLca(Optional(I32), Bool, Any);

    [Test]
    public void OptI32_LCA_Any_ReturnsAny() =>
        // opt(T) <: any, so LCA(Opt(I32), Any) = Any
        AssertLca(Optional(I32), Any, Any);

    [Test]
    public void OptBool_LCA_Bool_ReturnsOptBool() =>
        AssertLca(Optional(Bool), Bool, Optional(Bool));

    [Test]
    public void OptChar_LCA_Char_ReturnsOptChar() =>
        AssertLca(Optional(Char), Char, Optional(Char));

    [Test]
    public void OptReal_LCA_I32_ReturnsOptReal() =>
        // LCA(Real, I32) = Real, so Opt(Real)
        AssertLca(Optional(Real), I32, Optional(Real));

    [Test]
    public void OptReal_LCA_U8_ReturnsOptReal() =>
        AssertLca(Optional(Real), U8, Optional(Real));

    [Test]
    public void OptU8_LCA_I32_ReturnsOptI32() =>
        // LCA(U8, I32) = I32, so Opt(I32)
        AssertLca(Optional(U8), I32, Optional(I32));

    [Test]
    public void OptI64_LCA_U64_ReturnsOptI96() =>
        // LCA(I64, U64) = I96, so Opt(I96)
        AssertLca(Optional(I64), U64, Optional(I96));

    [Test]
    public void OptI16_LCA_U16_ReturnsOptI24() =>
        // LCA(I16, U16) = I24, so Opt(I24)
        AssertLca(Optional(I16), U16, Optional(I24));

    [Test]
    public void OptBool_LCA_Char_ReturnsAny() =>
        // LCA(Bool, Char) = Any, opt(any) = any
        AssertLca(Optional(Bool), Char, Any);

    [Test]
    public void OptChar_LCA_I32_ReturnsAny() =>
        // LCA(Char, I32) = Any, opt(any) = any
        AssertLca(Optional(Char), I32, Any);

    [Test]
    public void OptIp_LCA_I32_ReturnsAny() =>
        // LCA(Ip, I32) = Any, opt(any) = any
        AssertLca(Optional(Ip), I32, Any);

    [Test]
    public void OptBool_LCA_Any_ReturnsAny() =>
        AssertLca(Optional(Bool), Any, Any);

    [Test]
    public void OptChar_LCA_Any_ReturnsAny() =>
        AssertLca(Optional(Char), Any, Any);

    // ===================================================================
    // Optional x Composites (mismatched inner kind)
    // ===================================================================

    [Test]
    public void OptI32_LCA_ArrayI32_ReturnsAny() =>
        // LCA(I32, Array(I32)) = Any, opt(any) = any
        AssertLca(Optional(I32), Array(I32), Any);

    [Test]
    public void OptI32_LCA_StructA_I32_ReturnsAny() =>
        // LCA(I32, Struct) = Any, opt(any) = any
        AssertLca(Optional(I32), Struct("a", I32), Any);

    [Test]
    public void OptI32_LCA_FunI32Real_ReturnsAny() =>
        // LCA(I32, Fun(I32,Real)) = Any, opt(any) = any
        AssertLca(Optional(I32), Fun(I32, Real), Any);

    [Test]
    public void OptBool_LCA_ArrayBool_ReturnsAny() =>
        // LCA(Bool, Array(Bool)) = Any, opt(any) = any
        AssertLca(Optional(Bool), Array(Bool), Any);

    // ===================================================================
    // Optional x Composites (matching inner kind)
    // ===================================================================

    [Test]
    public void OptArrayI32_LCA_ArrayI32_ReturnsOptArrayI32() =>
        // LCA(Array(I32), Array(I32)) = Array(I32), so Opt(Array(I32))
        AssertLca(Optional(Array(I32)), Array(I32), Optional(Array(I32)));

    [Test]
    public void OptArrayI32_LCA_ArrayReal_ReturnsOptArrayReal() =>
        // LCA(Array(I32), Array(Real)) = Array(Real), so Opt(Array(Real))
        AssertLca(Optional(Array(I32)), Array(Real), Optional(Array(Real)));

    [Test]
    public void OptArrayU8_LCA_ArrayI32_ReturnsOptArrayI32() =>
        // LCA(Array(U8), Array(I32)) = Array(I32), so Opt(Array(I32))
        AssertLca(Optional(Array(U8)), Array(I32), Optional(Array(I32)));

    [Test]
    public void OptArrayBool_LCA_ArrayChar_ReturnsOptArrayAny() =>
        // LCA(Array(Bool), Array(Char)) = Array(Any), inner is Array(Any) not Any → stays Opt
        AssertLca(Optional(Array(Bool)), Array(Char), Optional(Array(Any)));

    [Test]
    public void OptStructA_I32_LCA_StructA_I32_ReturnsOptStructA_I32() =>
        AssertLca(Optional(Struct("a", I32)), Struct("a", I32), Optional(Struct("a", I32)));

    [Test]
    public void OptStructA_I32_LCA_StructA_Real_ReturnsOptStructA_Real() =>
        // LCA(Struct(a:I32), Struct(a:Real)) = Struct(a:Real), so Opt(Struct(a:Real))
        AssertLca(Optional(Struct("a", I32)), Struct("a", Real), Optional(Struct("a", Real)));

    [Test]
    public void OptStructA_LCA_StructB_ReturnsOptEmptyStruct() =>
        // Struct fields don't overlap: LCA = empty struct
        AssertLca(Optional(Struct("a", I32)), Struct("b", I32), Optional(EmptyStruct()));

    [Test]
    public void OptFun_LCA_SameFun_ReturnsOptFun() =>
        AssertLca(Optional(Fun(I32, Real)), Fun(I32, Real), Optional(Fun(I32, Real)));

    [Test]
    public void OptFun_LCA_DifferentReturnFun_ReturnsOptFunWithLcaReturn() =>
        // LCA(Fun(I32,I16), Fun(I32,U16)) = Fun(I32, I24)
        AssertLca(Optional(Fun(I32, I16)), Fun(I32, U16), Optional(Fun(I32, I24)));

    // ===================================================================
    // Optional x Constraints
    // ===================================================================

    [Test]
    public void OptI32_LCA_EmptyConstraints_ReturnsOptI32() =>
        AssertLca(Optional(I32), EmptyConstraints, Optional(I32));

    [Test]
    public void OptReal_LCA_EmptyConstraints_ReturnsOptReal() =>
        AssertLca(Optional(Real), EmptyConstraints, Optional(Real));

    [Test]
    public void OptBool_LCA_EmptyConstraints_ReturnsOptBool() =>
        AssertLca(Optional(Bool), EmptyConstraints, Optional(Bool));

    [Test]
    public void OptI32_LCA_ConstrainsDescU8_ReturnsOptI32() =>
        // Constrains unwraps to desc=U8, then LCA(Opt(I32), U8) = Opt(LCA(I32,U8)) = Opt(I32)
        AssertLca(Optional(I32), Constrains(desc: U8), Optional(I32));

    [Test]
    public void OptI32_LCA_ConstrainsDescReal_ReturnsOptReal() =>
        // Constrains unwraps to desc=Real, then LCA(Opt(I32), Real) = Opt(Real)
        AssertLca(Optional(I32), Constrains(desc: Real), Optional(Real));

    [Test]
    public void OptI32_LCA_ConstrainsDescBool_ReturnsAny() =>
        // Constrains unwraps to desc=Bool, LCA(I32, Bool) = Any, opt(any) = any
        AssertLca(Optional(I32), Constrains(desc: Bool), Any);

    [Test]
    public void OptI32_LCA_ConstrainsDescI32_ReturnsOptI32() =>
        AssertLca(Optional(I32), Constrains(desc: I32), Optional(I32));

    // ===================================================================
    // Nested Optional compositions
    // ===================================================================

    [Test]
    public void OptArrayOfArrayI32_LCA_ArrayOfArrayReal_ReturnsOptArrayOfArrayReal() =>
        AssertLca(
            Optional(Array(Array(I32))),
            Array(Array(Real)),
            Optional(Array(Array(Real))));

    [Test]
    public void None_LCA_OptOptionalArrayI32() =>
        // None ^ Opt(Array(I32)) = Opt(Array(I32)) — None ≤ any optional
        AssertLca(None, Optional(Array(I32)), Optional(Array(I32)));

    [Test]
    public void OptFunBoolI16_LCA_OptFunBoolI32_ReturnsOptFunBoolI32() =>
        // LCA(Fun(Bool,I16), Fun(Bool,I32)) = Fun(Bool,I32), so Opt(Fun(Bool,I32))
        AssertLca(Optional(Fun(Bool, I16)), Optional(Fun(Bool, I32)), Optional(Fun(Bool, I32)));

    [Test]
    public void OptFunDifferentArgCount_ReturnsAny() =>
        // LCA(Fun(I32,I32,I32), Fun(I32,I32)) = Any, opt(any) = any
        AssertLca(Optional(Fun(I32, I32, I32)), Optional(Fun(I32, I32)), Any);

    [Test]
    public void OptStruct_LCA_OptArray_ReturnsAny() =>
        // LCA(Struct, Array) = Any, opt(any) = any
        AssertLca(Optional(Struct("a", I32)), Optional(Array(I32)), Any);

    [Test]
    public void OptArray_LCA_OptFun_ReturnsAny() =>
        AssertLca(Optional(Array(I32)), Optional(Fun(I32, I32)), Any);

    [Test]
    public void OptStruct_LCA_OptFun_ReturnsAny() =>
        AssertLca(Optional(Struct("a", I32)), Optional(Fun(I32, I32)), Any);

    // ===================================================================
    // Bulk: Optional x Optional for all primitive LCA combinations
    // ===================================================================

    [Test]
    public void OptionalOfAllPrimitivePairs_ReturnsOptionalOfLcaOrAny() {
        foreach (var types in PrimitiveTypesLca)
        {
            // opt(any) collapses to any
            var expected = types.Lca.Equals(Any)
                ? (ITicNodeState)Any
                : (ITicNodeState)Optional(types.Lca);
            AssertLca(Optional(types.Left), Optional(types.Right), expected);
        }
    }

    // ===================================================================
    // Bulk: None x all primitives
    // ===================================================================

    [Test]
    public void NoneAndAllPrimitives_ReturnsOptOrAny() {
        foreach (var primitive in PrimitiveTypes)
        {
            // none <: any, so LCA(None, Any) = Any; for others LCA(None, T) = Opt(T)
            var expected = primitive.Equals(Any)
                ? (ITicNodeState)Any
                : (ITicNodeState)Optional(primitive);
            AssertLca(None, primitive, expected);
        }
    }

    // ===================================================================
    // Bulk: Optional(T) x T for all primitives = Opt(T)
    // ===================================================================

    [Test]
    public void OptionalPrimitive_LCA_SamePrimitive_ReturnsOptSelfOrAny() {
        foreach (var primitive in PrimitiveTypes)
        {
            // opt(any) = any
            var expected = primitive.Equals(Any)
                ? (ITicNodeState)Any
                : (ITicNodeState)Optional(primitive);
            AssertLca(Optional(primitive), primitive, expected);
        }
    }

    // ===================================================================
    // Bulk: Opt(Left) x Right for all primitive pairs
    // ===================================================================

    [Test]
    public void OptionalLeft_LCA_PrimitiveRight_ReturnsOptLcaOrAny() {
        foreach (var types in PrimitiveTypesLca)
        {
            // LCA(Opt(Left), Right) = Opt(LCA(Left, Right)), but opt(any) = any
            var expected = types.Lca.Equals(Any)
                ? (ITicNodeState)Any
                : (ITicNodeState)Optional(types.Lca);
            AssertLca(Optional(types.Left), types.Right, expected);
        }
    }

    // ===================================================================
    // Bulk: None x Opt(T) for all primitives
    // ===================================================================

    [Test]
    public void NoneAndAllOptionalPrimitives_ReturnsOptPrimitive() {
        foreach (var primitive in PrimitiveTypes)
        {
            // None ^ Opt(T) = Opt(T), but opt(any) = any
            // For T=Any: LCA(None, Opt(Any)) = LCA(None, Any) = Any
            var opt = Optional(primitive);
            var expected = primitive.Equals(Any)
                ? (ITicNodeState)Any
                : (ITicNodeState)opt;
            AssertLca(None, opt, expected);
        }
    }

    // ===================================================================
    // Bulk: None x Array(T) for all primitives
    // ===================================================================

    [Test]
    public void NoneAndArrayOfAllPrimitives_ReturnsOptArray() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(None, Array(primitive), Optional(Array(primitive)));
    }

    // ===================================================================
    // Bulk: Opt(Array(Left)) x Array(Right) for all pairs
    // ===================================================================

    [Test]
    public void OptArrayLeft_LCA_ArrayRight_ReturnsOptArrayLca() {
        foreach (var types in PrimitiveTypesLca)
        {
            // LCA(Opt(Arr(L)), Arr(R)) = Opt(LCA(Arr(L), Arr(R))) = Opt(Arr(LCA(L,R)))
            var expected = (ITicNodeState)Optional(Array(types.Lca));
            AssertLca(Optional(Array(types.Left)), Array(types.Right), expected);
        }
    }

    // ===================================================================
    // Bulk: Opt(Arr(L)) x Opt(Arr(R)) for all pairs
    // ===================================================================

    [Test]
    public void OptArrayLeft_LCA_OptArrayRight_ReturnsOptArrayLca() {
        foreach (var types in PrimitiveTypesLca)
        {
            // LCA(Opt(Arr(L)), Opt(Arr(R))) = Opt(LCA(Arr(L),Arr(R))) = Opt(Arr(LCA(L,R)))
            var expected = (ITicNodeState)Optional(Array(types.Lca));
            AssertLca(Optional(Array(types.Left)), Optional(Array(types.Right)), expected);
        }
    }

    // ===================================================================
    // Edge cases
    // ===================================================================

    [Test]
    public void OptAny_LCA_Primitives_ReturnsAny() {
        // opt(any) = any, so LCA(Any, X) = Any
        AssertLca(Optional(Any), I32, Any);
        AssertLca(Optional(Any), Bool, Any);
        AssertLca(Optional(Any), Array(I32), Any);
        AssertLca(Optional(Any), Struct("a", I32), Any);
        AssertLca(Optional(Any), Fun(I32, Real), Any);
    }

    [Test]
    public void OptAny_LCA_None_ReturnsAny() {
        // opt(any) = any, LCA(Any, None) = Any (since none <: any)
        AssertLca(Optional(Any), None, Any);
    }

    [Test]
    public void OptAny_LCA_OptAny_ReturnsAny() =>
        // opt(any) = any
        AssertLca(Optional(Any), Optional(Any), Any);

    [Test]
    public void OptAny_LCA_OptI32_ReturnsAny() =>
        // LCA(Any, I32) = Any, opt(any) = any
        AssertLca(Optional(Any), Optional(I32), Any);

    [Test]
    public void None_LCA_None_Idempotent() {
        // Triple check: None is idempotent
        var result = None;
        AssertLca(result, None, None);
    }

    // ===================================================================
    // MergeOrNull with None: algebra-level behavior
    // ===================================================================

    [Test]
    public void MergeOrNull_NoneDesc_WithEmpty_ReturnsNoneDesc() {
        // At algebra level, [None..].MergeOrNull([..]) = [None..] — correct.
        // The Destruction stage handles this specially to avoid "None infection"
        // (see DestructionFunctions: creates opt(T) instead of unifying to None).
        var noneConstraint = ConstraintsState.Of(desc: None);
        var empty = ConstraintsState.Empty;
        var result = noneConstraint.MergeOrNull(empty);

        Assert.IsNotNull(result);
        Assert.IsInstanceOf<ConstraintsState>(result);
        var cs = (ConstraintsState)result;
        Assert.AreEqual(None, cs.Descendant);
    }

    [Test]
    public void MergeOrNull_NoneDesc_WithI32Desc_ReturnsOptI32() {
        // [None..].MergeOrNull([I32..]) → LCA(None, I32) = opt(I32)
        var noneConstraint = ConstraintsState.Of(desc: None);
        var i32Constraint = ConstraintsState.Of(desc: I32);
        var result = noneConstraint.MergeOrNull(i32Constraint);

        Assert.IsNotNull(result);
        // MergeOrNull collapse: [opt(I32)..] with no ancestor → opt(I32)
        Assert.IsInstanceOf<StateOptional>(result);
        Assert.AreEqual(Optional(I32), result);
    }

    [Test]
    public void MergeOrNull_NoneDesc_WithNoneDesc_StaysNone() {
        // [None..].MergeOrNull([None..]) → [None..] (both None, no Optional wrapping)
        var a = ConstraintsState.Of(desc: None);
        var b = ConstraintsState.Of(desc: None);
        var result = a.MergeOrNull(b);

        Assert.IsNotNull(result);
        Assert.IsInstanceOf<ConstraintsState>(result);
        var cs = (ConstraintsState)result;
        Assert.AreEqual(None, cs.Descendant);
    }

    // ===================================================================

    [Test]
    public void OptI32_LCA_None_ReturnsOptI32() =>
        // Symmetric of None ^ Opt(I32)
        AssertLca(Optional(I32), None, Optional(I32));

    [Test]
    public void OptReal_LCA_None_ReturnsOptReal() =>
        AssertLca(Optional(Real), None, Optional(Real));

    [Test]
    public void OptBool_LCA_None_ReturnsOptBool() =>
        AssertLca(Optional(Bool), None, Optional(Bool));

    [Test]
    public void OptArrayI32_LCA_None_ReturnsOptArrayI32() =>
        AssertLca(Optional(Array(I32)), None, Optional(Array(I32)));

    [Test]
    public void OptStruct_LCA_None_ReturnsOptStruct() =>
        AssertLca(Optional(Struct("a", I32)), None, Optional(Struct("a", I32)));

    [Test]
    public void OptFun_LCA_None_ReturnsOptFun() =>
        AssertLca(Optional(Fun(I32, Real)), None, Optional(Fun(I32, Real)));
}
