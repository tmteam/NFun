namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static GcdTest;
using static LcaTestTools;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class GcdOptionalTest {

    [Test]
    public void None_GCD_None_ReturnsNone() =>
        AssertGcd(None, None, None);

    [Test]
    public void None_GCD_Any_ReturnsNone() =>
        // none <: any, so GCD(None, Any) = None
        AssertGcd(None, Any, None);

    [Test]
    public void None_GCD_Bool_ReturnsNull() =>
        AssertGcd(None, Bool, null);

    [Test]
    public void None_GCD_Char_ReturnsNull() =>
        AssertGcd(None, Char, null);

    [Test]
    public void None_GCD_I32_ReturnsNull() =>
        AssertGcd(None, I32, null);

    [Test]
    public void None_GCD_Real_ReturnsNull() =>
        AssertGcd(None, Real, null);

    [Test]
    public void None_GCD_U8_ReturnsNull() =>
        AssertGcd(None, U8, null);

    [Test]
    public void None_GCD_I16_ReturnsNull() =>
        AssertGcd(None, I16, null);

    [Test]
    public void None_GCD_I64_ReturnsNull() =>
        AssertGcd(None, I64, null);

    [Test]
    public void None_GCD_U64_ReturnsNull() =>
        AssertGcd(None, U64, null);

    [Test]
    public void None_GCD_U32_ReturnsNull() =>
        AssertGcd(None, U32, null);

    [Test]
    public void None_GCD_U16_ReturnsNull() =>
        AssertGcd(None, U16, null);

    [Test]
    public void None_GCD_Ip_ReturnsNull() =>
        AssertGcd(None, Ip, null);

    [Test]
    public void None_GCD_I96_ReturnsNull() =>
        AssertGcd(None, I96, null);

    [Test]
    public void None_GCD_I48_ReturnsNull() =>
        AssertGcd(None, I48, null);

    [Test]
    public void None_GCD_I24_ReturnsNull() =>
        AssertGcd(None, I24, null);

    // ===================================================================
    // None x Optional
    // ===================================================================

    [Test]
    public void None_GCD_OptI32_ReturnsNone() =>
        AssertGcd(None, Optional(I32), None);

    [Test]
    public void None_GCD_OptReal_ReturnsNone() =>
        AssertGcd(None, Optional(Real), None);

    [Test]
    public void None_GCD_OptBool_ReturnsNone() =>
        AssertGcd(None, Optional(Bool), None);

    [Test]
    public void None_GCD_OptChar_ReturnsNone() =>
        AssertGcd(None, Optional(Char), None);

    [Test]
    public void None_GCD_OptU8_ReturnsNone() =>
        AssertGcd(None, Optional(U8), None);

    [Test]
    public void None_GCD_OptAny_ReturnsNone() =>
        AssertGcd(None, Optional(Any), None);

    [Test]
    public void None_GCD_OptArrayI32_ReturnsNone() =>
        AssertGcd(None, Optional(Array(I32)), None);

    [Test]
    public void None_GCD_ArrayI32_ReturnsNull() =>
        AssertGcd(None, Array(I32), null);

    [Test]
    public void None_GCD_ArrayReal_ReturnsNull() =>
        AssertGcd(None, Array(Real), null);

    [Test]
    public void None_GCD_Struct_ReturnsNull() =>
        AssertGcd(None, Struct("a", I32), null);

    [Test]
    public void None_GCD_Fun_ReturnsNull() =>
        AssertGcd(None, Fun(I32, Real), null);

    [Test]
    public void OptI32_GCD_OptI32_ReturnsOptI32() =>
        AssertGcd(Optional(I32), Optional(I32), Optional(I32));

    [Test]
    public void OptI32_GCD_OptReal_ReturnsOptI32() =>
        // GCD(I32, Real) = I32 (I32 is subtype of Real)
        AssertGcd(Optional(I32), Optional(Real), Optional(I32));

    [Test]
    public void OptU8_GCD_OptI32_ReturnsOptU8() =>
        // GCD(U8, I32) = U8
        AssertGcd(Optional(U8), Optional(I32), Optional(U8));

    [Test]
    public void OptU8_GCD_OptU32_ReturnsOptU8() =>
        // GCD(U8, U32) = U8
        AssertGcd(Optional(U8), Optional(U32), Optional(U8));

    [Test]
    public void OptI16_GCD_OptU16_ReturnsOptU12() =>
        // GCD(I16, U16) = U12
        AssertGcd(Optional(I16), Optional(U16), Optional(U12));

    [Test]
    public void OptBool_GCD_OptI32_ReturnsNull() =>
        // GCD(Bool, I32) = null
        AssertGcd(Optional(Bool), Optional(I32), null);

    [Test]
    public void OptChar_GCD_OptBool_ReturnsNull() =>
        // GCD(Char, Bool) = null
        AssertGcd(Optional(Char), Optional(Bool), null);

    [Test]
    public void OptBool_GCD_OptBool_ReturnsOptBool() =>
        AssertGcd(Optional(Bool), Optional(Bool), Optional(Bool));

    [Test]
    public void OptChar_GCD_OptChar_ReturnsOptChar() =>
        AssertGcd(Optional(Char), Optional(Char), Optional(Char));

    [Test]
    public void OptReal_GCD_OptReal_ReturnsOptReal() =>
        AssertGcd(Optional(Real), Optional(Real), Optional(Real));

    [Test]
    public void OptU64_GCD_OptI64_ReturnsOptU48() =>
        // GCD(U64, I64) = U48
        AssertGcd(Optional(U64), Optional(I64), Optional(U48));

    [Test]
    public void OptI32_GCD_OptU32_ReturnsOptU24() =>
        // GCD(I32, U32) = U24
        AssertGcd(Optional(I32), Optional(U32), Optional(U24));

    [Test]
    public void OptU8_GCD_OptReal_ReturnsOptU8() =>
        // GCD(U8, Real) = U8
        AssertGcd(Optional(U8), Optional(Real), Optional(U8));

    [Test]
    public void OptArrayI32_GCD_OptArrayReal_ReturnsOptArrayI32() =>
        // GCD(Array(I32), Array(Real)) = Array(GCD(I32,Real)) = Array(I32)
        AssertGcd(Optional(Array(I32)), Optional(Array(Real)), Optional(Array(I32)));

    [Test]
    public void OptArrayI32_GCD_OptArrayI32_ReturnsOptArrayI32() =>
        AssertGcd(Optional(Array(I32)), Optional(Array(I32)), Optional(Array(I32)));

    [Test]
    public void OptArrayChar_GCD_OptArrayBool_ReturnsNull() =>
        // GCD(Array(Char), Array(Bool)) = null (GCD(Char,Bool)=null)
        AssertGcd(Optional(Array(Char)), Optional(Array(Bool)), null);

    [Test]
    public void OptStructA_GCD_OptStructA_ReturnsOptStruct() =>
        AssertGcd(
            Optional(Struct("a", I32)),
            Optional(Struct("a", I32)),
            Optional(Struct("a", I32)));

    [Test]
    public void OptStructA_I32_GCD_OptStructA_Real_ReturnsOptStructA_I32() =>
        // GCD(Struct(a:I32), Struct(a:Real)) = Struct(a:I32)
        AssertGcd(
            Optional(Struct("a", I32)),
            Optional(Struct("a", Real)),
            Optional(Struct("a", I32)));

    [Test]
    public void OptI32_GCD_I32_ReturnsI32() =>
        // GCD(Opt(I32), I32) = GCD(I32, I32) = I32
        AssertGcd(Optional(I32), I32, I32);

    [Test]
    public void OptI32_GCD_U8_ReturnsU8() =>
        // GCD(Opt(I32), U8) = GCD(I32, U8) = U8
        AssertGcd(Optional(I32), U8, U8);

    [Test]
    public void OptI32_GCD_Real_ReturnsI32() =>
        // GCD(Opt(I32), Real) = GCD(I32, Real) = I32
        AssertGcd(Optional(I32), Real, I32);

    [Test]
    public void OptI32_GCD_Bool_ReturnsNull() =>
        // GCD(Opt(I32), Bool) = GCD(I32, Bool) = null
        AssertGcd(Optional(I32), Bool, null);

    [Test]
    public void OptI32_GCD_Any_ReturnsOptI32() =>
        // opt(T) <: any, so GCD(Opt(I32), Any) = Opt(I32)
        AssertGcd(Optional(I32), Any, Optional(I32));

    [Test]
    public void OptBool_GCD_Bool_ReturnsBool() =>
        // GCD(Opt(Bool), Bool) = GCD(Bool, Bool) = Bool
        AssertGcd(Optional(Bool), Bool, Bool);

    [Test]
    public void OptChar_GCD_Char_ReturnsChar() =>
        // GCD(Opt(Char), Char) = GCD(Char, Char) = Char
        AssertGcd(Optional(Char), Char, Char);

    [Test]
    public void OptReal_GCD_I32_ReturnsI32() =>
        // GCD(Opt(Real), I32) = GCD(Real, I32) = I32
        AssertGcd(Optional(Real), I32, I32);

    [Test]
    public void OptReal_GCD_U8_ReturnsU8() =>
        // GCD(Opt(Real), U8) = GCD(Real, U8) = U8
        AssertGcd(Optional(Real), U8, U8);

    [Test]
    public void OptU8_GCD_I32_ReturnsU8() =>
        // GCD(Opt(U8), I32) = GCD(U8, I32) = U8
        AssertGcd(Optional(U8), I32, U8);

    [Test]
    public void OptI64_GCD_U64_ReturnsU48() =>
        // GCD(Opt(I64), U64) = GCD(I64, U64) = U48
        AssertGcd(Optional(I64), U64, U48);

    [Test]
    public void OptBool_GCD_Char_ReturnsNull() =>
        // GCD(Opt(Bool), Char) = GCD(Bool, Char) = null
        AssertGcd(Optional(Bool), Char, null);

    [Test]
    public void OptBool_GCD_Any_ReturnsOptBool() =>
        AssertGcd(Optional(Bool), Any, Optional(Bool));

    [Test]
    public void OptReal_GCD_Any_ReturnsOptReal() =>
        AssertGcd(Optional(Real), Any, Optional(Real));

    [Test]
    public void OptChar_GCD_Any_ReturnsOptChar() =>
        AssertGcd(Optional(Char), Any, Optional(Char));

    [Test]
    public void OptI32_GCD_ArrayI32_ReturnsNull() =>
        // GCD(Opt(I32), Array(I32)) = GCD(I32, Array(I32)) = null (different kinds)
        AssertGcd(Optional(I32), Array(I32), null);

    [Test]
    public void OptArrayI32_GCD_ArrayI32_ReturnsArrayI32() =>
        // GCD(Opt(Array(I32)), Array(I32)) = GCD(Array(I32), Array(I32)) = Array(I32)
        AssertGcd(Optional(Array(I32)), Array(I32), Array(I32));

    [Test]
    public void OptArrayI32_GCD_ArrayReal_ReturnsArrayI32() =>
        // GCD(Opt(Array(I32)), Array(Real)) = GCD(Array(I32), Array(Real)) = Array(I32)
        AssertGcd(Optional(Array(I32)), Array(Real), Array(I32));

    [Test]
    public void OptArrayI32_GCD_ArrayBool_ReturnsNull() =>
        // GCD(Array(I32), Array(Bool)) = null
        AssertGcd(Optional(Array(I32)), Array(Bool), null);

    [Test]
    public void OptStructA_GCD_StructA_ReturnsStructA() =>
        // GCD(Opt(Struct(a:I32)), Struct(a:I32)) = GCD(Struct(a:I32), Struct(a:I32)) = Struct(a:I32)
        AssertGcd(Optional(Struct("a", I32)), Struct("a", I32), Struct("a", I32));

    [Test]
    public void OptStructA_I32_GCD_StructA_Real_ReturnsStructA_I32() =>
        // GCD(Opt(Struct(a:I32)), Struct(a:Real)) = GCD(Struct(a:I32), Struct(a:Real))
        // Struct GCD: shared field a → GCD(I32, Real) = I32 → Struct(a:I32)
        AssertGcd(Optional(Struct("a", I32)), Struct("a", Real), Struct("a", I32));

    [Test]
    public void OptFun_GCD_SameFun_ReturnsFun() =>
        // GCD(Opt(Fun(I32,Real)), Fun(I32,Real)) = GCD(Fun(I32,Real), Fun(I32,Real)) = Fun(I32,Real)
        AssertGcd(Optional(Fun(I32, Real)), Fun(I32, Real), Fun(I32, Real));

    [Test]
    public void OptI32_GCD_Struct_ReturnsNull() =>
        // GCD(Opt(I32), Struct) = GCD(I32, Struct) = null
        AssertGcd(Optional(I32), Struct("a", I32), null);

    [Test]
    public void OptI32_GCD_Fun_ReturnsNull() =>
        // GCD(Opt(I32), Fun) = GCD(I32, Fun) = null
        AssertGcd(Optional(I32), Fun(I32, Real), null);

    [Test]
    public void OptArray_GCD_OptStruct_ReturnsNull() =>
        AssertGcd(Optional(Array(I32)), Optional(Struct("a", I32)), null);

    [Test]
    public void OptArray_GCD_OptFun_ReturnsNull() =>
        AssertGcd(Optional(Array(I32)), Optional(Fun(I32, I32)), null);

    [Test]
    public void OptStruct_GCD_OptFun_ReturnsNull() =>
        AssertGcd(Optional(Struct("a", I32)), Optional(Fun(I32, I32)), null);

    [Test]
    public void OptI32_GCD_OptArrayI32_ReturnsNull() =>
        // GCD(I32, Array(I32)) = null → Opt(null) = null
        AssertGcd(Optional(I32), Optional(Array(I32)), null);

    [Test]
    public void OptAny_GCD_Any_ReturnsAny() =>
        // opt(any) = any, so GCD(Any, Any) = Any
        AssertGcd(Optional(Any), Any, Any);

    [Test]
    public void OptAny_GCD_I32_ReturnsI32() =>
        // opt(any) = any, so GCD(Any, I32) = I32
        AssertGcd(Optional(Any), I32, I32);

    [Test]
    public void OptAny_GCD_OptI32_ReturnsOptI32() =>
        // GCD(Any, I32) = I32 → Opt(I32)
        AssertGcd(Optional(Any), Optional(I32), Optional(I32));

    [Test]
    public void OptAny_GCD_OptAny_ReturnsOptAny() =>
        // GCD(Any, Any) = Any → Opt(Any)
        AssertGcd(Optional(Any), Optional(Any), Optional(Any));

    [Test]
    public void OptAny_GCD_None_ReturnsNone() =>
        // GCD(Opt(Any), None): None fires first → GcdWithNone(Opt(Any)) → None
        AssertGcd(Optional(Any), None, None);

    [Test]
    public void OptI32_GCD_None_ReturnsNone() =>
        // None is subtype of any Optional
        AssertGcd(Optional(I32), None, None);

    [Test]
    public void OptBool_GCD_None_ReturnsNone() =>
        AssertGcd(Optional(Bool), None, None);

    [Test]
    public void OptArrayI32_GCD_None_ReturnsNone() =>
        AssertGcd(Optional(Array(I32)), None, None);

    [Test]
    public void NoneAndAllPrimitives_GCD_ReturnsNullOrNone() {
        foreach (var primitive in PrimitiveTypes)
        {
            // none <: any, so GCD(None, Any) = None; for others GCD(None, T) = null
            ITicNodeState expected = primitive.Equals(Any) ? None : null;
            AssertGcd(None, primitive, expected);
        }
    }

    [Test]
    public void NoneAndAllOptionalPrimitives_GCD_ReturnsNone() {
        foreach (var primitive in PrimitiveTypes)
            AssertGcd(None, Optional(primitive), None);
    }

    [Test]
    public void OptPrimitive_GCD_Any_ReturnsOptPrimitive() {
        foreach (var primitive in PrimitiveTypes)
        {
            // opt(T) <: any, so GCD(Opt(T), Any) = Opt(T); but opt(any)=any
            var expected = primitive.Equals(Any)
                ? Any
                : (ITicNodeState)Optional(primitive);
            AssertGcd(Optional(primitive), Any, expected);
        }
    }

    [Test]
    public void OptPrimitive_GCD_SamePrimitive_ReturnsPrimitive() {
        foreach (var primitive in PrimitiveTypes)
        {
            // GCD(Opt(T), T) = GCD(T, T) = T — for all T including Any
            AssertGcd(Optional(primitive), primitive, primitive);
        }
    }

    [Test]
    public void OptArrayLeft_GCD_ArrayRight_Bulk() {
        foreach (var types in PrimitiveTypesLca)
        {
            // GCD(Opt(Arr(L)), Arr(R)) = GCD(Arr(L), Arr(R)) = Arr(GCD(L, R))
            var gcdInner = types.Left.GetFirstCommonDescendantOrNull(types.Right);
            ITicNodeState expected = gcdInner == null ? null : (ITicNodeState)Array(gcdInner);
            AssertGcd(Optional(Array(types.Left)), Array(types.Right), expected);
        }
    }

    [Test]
    public void OptArrayLeft_GCD_OptArrayRight_Bulk() {
        foreach (var types in PrimitiveTypesLca)
        {
            // GCD(Opt(Arr(L)), Opt(Arr(R))) = Opt(GCD(Arr(L), Arr(R)))
            var gcdInner = types.Left.GetFirstCommonDescendantOrNull(types.Right);
            ITicNodeState expected = gcdInner == null ? null : (ITicNodeState)Optional(Array(gcdInner));
            AssertGcd(Optional(Array(types.Left)), Optional(Array(types.Right)), expected);
        }
    }
}
