namespace NFun.UnitTests.TicTests.StateExtensions;

using System.Collections.Generic;
using System.Linq;
using Tic;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

/// <summary>
/// Tests for algebraic invariants of Lca, Gcd, UnifyOrNull, FitsInto.
/// These must hold regardless of implementation details.
/// </summary>
public class AlgebraInvariantsTest {

    // All concrete types we want to check invariants against
    private static IEnumerable<ITicNodeState> AllConcreteTypes() {
        foreach (var p in PrimitiveTypes) yield return p;
        yield return None;
        yield return Array(I32);
        yield return Array(Real);
        yield return Array(Bool);
        yield return Array(Any);
        yield return Array(Array(I32));      // nested array
        yield return Struct("a", I32);
        yield return Struct(("a", I32), ("b", Bool));
        yield return Struct(("x", Real), ("y", Array(I32)));  // struct with array field
        yield return EmptyStruct();
        yield return Fun(I32, Real);
        yield return Fun(Bool, Bool);
        yield return Fun(I32, Bool, Real);  // multi-arg
        yield return Optional(I32);
        yield return Optional(Real);
        yield return Optional(Bool);
        yield return Optional(Array(I32));
    }

    private static IEnumerable<ITicNodeState> AllConstrainedTypes() {
        yield return EmptyConstraints;
        yield return Constrains(U8, Real);
        yield return Constrains(I16, I64);
        yield return Constrains(isComparable: true);
        yield return Constrains(U8, Real, isComparable: true);
        yield return Constrains(I32);  // desc only, no ancestor
    }

    private static IEnumerable<ITicNodeState> AllTypes()
        => AllConcreteTypes().Concat(AllConstrainedTypes());

    [Test]
    public void Lca_Symmetry() {
        foreach (var a in AllTypes())
        foreach (var b in AllTypes())
        {
            var ab = a.Lca(b);
            var ba = b.Lca(a);
            Assert.AreEqual(ab, ba,
                $"LCA symmetry violated: Lca({a}, {b})={ab} but Lca({b}, {a})={ba}");
        }
    }

    [Test]
    public void Lca_Idempotent() {
        foreach (var a in AllConcreteTypes())
        {
            var aa = a.Lca(a);
            Assert.AreEqual(a, aa,
                $"LCA idempotent violated: Lca({a}, {a})={aa}");
        }
    }

    [Test]
    public void Lca_AnyAbsorbs_ConcreteTypes() {
        // Any is top of ALL types: Lca(A, Any) = Any for all A
        // (none <: any, opt(T) <: any)
        foreach (var a in AllTypes())
        {
            var result = a.Lca(Any);
            Assert.AreEqual(Any, result,
                $"LCA Any absorb violated: Lca({a}, Any)={result}");
        }
    }

    [Test]
    public void Lca_OptAnyEqualsAny() {
        // opt(any) = any (collapses), so Lca(A, Opt(Any)) = Lca(A, Any) = Any
        var optAny = Optional(Any);
        foreach (var a in AllTypes())
        {
            var result = a.Lca(optAny);
            Assert.AreEqual(Any, result,
                $"LCA Opt(Any) absorb violated: Lca({a}, Opt(Any))={result}");
        }
    }

    [Test]
    public void Lca_EmptyConstrainsIsBottom() {
        // Empty constraints is bottom: Lca(A, ⊥) = A
        foreach (var a in AllConcreteTypes())
        {
            var result = a.Lca(EmptyConstraints);
            Assert.AreEqual(a, result,
                $"LCA bottom violated: Lca({a}, empty)={result}, expected {a}");
        }
    }

    [Test]
    public void Lca_Associativity_Primitives() {
        // Lca(Lca(A,B), C) = Lca(A, Lca(B,C))
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        foreach (var c in PrimitiveTypes)
        {
            var ab_c = a.Lca(b).Lca(c);
            var a_bc = a.Lca(b.Lca(c));
            Assert.AreEqual(ab_c, a_bc,
                $"LCA associativity violated for ({a},{b},{c}): ({a.Lca(b)})∨{c}={ab_c} but {a}∨({b.Lca(c)})={a_bc}");
        }
    }

    [Test]
    public void Lca_ResultIsAncestorOfBoth_Primitives() {
        // Lca(A,B) = C  ⟹  A ≤ C  ∧  B ≤ C
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            var c = a.Lca(b);
            if (c is StatePrimitive cp)
            {
                Assert.IsTrue(a.CanBePessimisticConvertedTo(cp),
                    $"Lca({a},{b})={c} but {a} not convertible to {c}");
                Assert.IsTrue(b.CanBePessimisticConvertedTo(cp),
                    $"Lca({a},{b})={c} but {b} not convertible to {c}");
            }
        }
    }

    [Test]
    public void Lca_MixedComposites_ReturnAny() {
        // Lca(Array, Struct) = Any, Lca(Fun, Array) = Any, etc.
        var array = Array(I32);
        var struc = Struct("a", I32);
        var fun = Fun(I32, Real);
        Assert.AreEqual(Any, array.Lca(struc));
        Assert.AreEqual(Any, array.Lca(fun));
        Assert.AreEqual(Any, struc.Lca(fun));
    }

    [Test]
    public void Lca_PrimitiveAndComposite_ReturnAny() {
        var composites = new ITicNodeState[] {
            Array(I32), Struct("a", I32), Fun(I32, Real)
        };
        foreach (var p in PrimitiveTypes)
        {
            if (p.Equals(Any)) continue;
            foreach (var c in composites)
            {
                Assert.AreEqual(Any, p.Lca(c),
                    $"Lca({p}, {c}) should be Any");
            }
        }
    }

    [Test]
    public void Gcd_Symmetry() {
        foreach (var a in AllConcreteTypes())
        foreach (var b in AllConcreteTypes())
        {
            var ab = a.Gcd(b);
            var ba = b.Gcd(a);
            Assert.AreEqual(ab, ba,
                $"GCD symmetry violated: Gcd({a}, {b})={ab} but Gcd({b}, {a})={ba}");
        }
    }

    [Test]
    public void Gcd_Idempotent() {
        foreach (var a in AllConcreteTypes())
        {
            var aa = a.Gcd(a);
            Assert.AreEqual(a, aa,
                $"GCD idempotent violated: Gcd({a}, {a})={aa}");
        }
    }

    [Test]
    public void Gcd_AnyIsIdentity_ConcreteTypes() {
        // Any is identity for GCD of ALL types: Gcd(A, Any) = A
        // (none <: any, opt(T) <: any)
        foreach (var a in AllConcreteTypes())
        {
            var result = a.Gcd(Any);
            Assert.AreEqual(a, result,
                $"GCD Any identity violated: Gcd({a}, Any)={result}, expected {a}");
        }
    }

    [Test]
    public void Gcd_OptAnyIsIdentity() {
        // opt(any) = any, so Gcd(A, Opt(Any)) = Gcd(A, Any) = A
        var optAny = Optional(Any);
        foreach (var a in AllConcreteTypes())
        {
            var result = a.Gcd(optAny);
            Assert.AreEqual(a, result,
                $"GCD Opt(Any) identity violated: Gcd({a}, Opt(Any))={result}, expected {a}");
        }
    }

    [Test]
    public void Gcd_ResultIsDescendantOfBoth_Primitives() {
        // Gcd(A,B) = C  ⟹  C ≤ A  ∧  C ≤ B
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            var c = a.Gcd(b);
            if (c is StatePrimitive cp)
            {
                Assert.IsTrue(cp.CanBePessimisticConvertedTo(a),
                    $"Gcd({a},{b})={c} but {c} not convertible to {a}");
                Assert.IsTrue(cp.CanBePessimisticConvertedTo(b),
                    $"Gcd({a},{b})={c} but {c} not convertible to {b}");
            }
        }
    }

    [Test]
    public void Gcd_MixedComposites_ReturnNull() {
        // Gcd(Array, Struct) = null
        var array = Array(I32);
        var struc = Struct("a", I32);
        var fun = Fun(I32, Real);
        Assert.IsNull(array.Gcd(struc));
        Assert.IsNull(array.Gcd(fun));
        Assert.IsNull(struc.Gcd(fun));
    }

    [Test]
    public void Unify_Symmetry() {
        foreach (var a in AllTypes())
        foreach (var b in AllTypes())
        {
            var ab = a.UnifyOrNull(b);
            var ba = b.UnifyOrNull(a);
            Assert.AreEqual(ab, ba,
                $"Unify symmetry violated: Unify({a}, {b})={ab} but Unify({b}, {a})={ba}");
        }
    }

    [Test]
    public void Unify_Idempotent() {
        foreach (var a in AllTypes())
        {
            var aa = a.UnifyOrNull(a);
            Assert.IsNotNull(aa, $"Unify({a}, {a}) should not be null");
            Assert.AreEqual(a, aa,
                $"Unify idempotent violated: Unify({a}, {a})={aa}");
        }
    }

    [Test]
    public void Unify_AnyIsCompatibleWithAll() {
        // Unify(A, Any) is never null for any type (none <: any, opt(T) <: any)
        foreach (var a in AllTypes())
        {
            var result = a.UnifyOrNull(Any);
            Assert.IsNotNull(result,
                $"Unify({a}, Any) should not be null — Any is top of all types");
        }
    }

    [Test]
    public void Unify_EmptyConstrainsAcceptsAll() {
        foreach (var a in AllConcreteTypes())
        {
            var result = a.UnifyOrNull(EmptyConstraints);
            Assert.IsNotNull(result,
                $"Unify({a}, empty) should not be null");
        }
    }

    [Test]
    public void Unify_SamePrimitiveReturnsSelf() {
        foreach (var p in PrimitiveTypes)
        {
            var result = p.UnifyOrNull(p);
            Assert.AreEqual(p, result);
        }
    }

    [Test]
    public void Unify_DifferentPrimitivesReturnNull() {
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            if (a.Equals(b) || a.Equals(Any) || b.Equals(Any)) continue;
            var result = a.UnifyOrNull(b);
            Assert.IsNull(result,
                $"Unify({a}, {b}) should be null for different non-Any primitives, got {result}");
        }
    }

    [Test]
    public void Unify_PrimitiveFitsInConstrains() {
        // Unify(I32, [U8..Real]) = I32 (I32 is in the interval)
        var result = I32.UnifyOrNull(Constrains(U8, Real));
        Assert.AreEqual(I32, result);
    }

    [Test]
    public void Unify_PrimitiveOutsideConstrains() {
        // Unify(Bool, [U8..Real]) = null (Bool not in numeric interval)
        var result = Bool.UnifyOrNull(Constrains(U8, Real));
        Assert.IsNull(result);
    }

    [Test]
    public void Unify_ConstrainsIntersection() {
        // Unify([U8..Real], [I16..I64]) = [I16..I64] or similar intersection
        var a = Constrains(U8, Real);
        var b = Constrains(I16, I64);
        var result = a.UnifyOrNull(b);
        Assert.IsNotNull(result, "intervals overlap, should unify");
    }

    [Test]
    public void Unify_DisjointConstrains() {
        // Unify([I16..Real], [U8..U64]): I16,I32,I64,Real vs U8,U16,U32,U64
        // These share no common type => null
        var a = Constrains(I16, Real);
        var b = Constrains(U8, U64);
        var result = a.UnifyOrNull(b);
        Assert.IsNull(result, "disjoint intervals should not unify");
    }

    [Test]
    public void Unify_ArrayRecursive() {
        // Unify(I32[], Real[]) = null (I32 ≠ Real)
        var a = Array(I32);
        var b = Array(Real);
        Assert.IsNull(a.UnifyOrNull(b));

        // Unify(I32[], I32[]) = I32[]
        Assert.AreEqual(a, a.UnifyOrNull(a));
    }

    [Test]
    public void Unify_StructSameFields() {
        var a = Struct(("x", I32), ("y", Bool));
        var b = Struct(("x", I32), ("y", Bool));
        var result = a.UnifyOrNull(b);
        Assert.IsNotNull(result);
    }

    [Test]
    public void Unify_StructDifferentFieldTypes() {
        // {x:I32} unify {x:Real} = null (I32 ≠ Real, unify is strict)
        var a = Struct("x", I32);
        var b = Struct("x", Real);
        Assert.IsNull(a.UnifyOrNull(b));
    }

    [Test]
    public void Lca_Gcd_Duality_ForPrimitives() {
        // GCD(A,B) ≤ A ≤ LCA(A,B)
        // GCD(A,B) ≤ LCA(A,B)
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            var lca = a.Lca(b);
            var gcd = a.Gcd(b);
            if (lca is StatePrimitive lcaP && gcd is StatePrimitive gcdP)
            {
                Assert.IsTrue(gcdP.CanBePessimisticConvertedTo(a),
                    $"GCD({a},{b})={gcd} should be ≤ {a}");
                Assert.IsTrue(a.CanBePessimisticConvertedTo(lcaP),
                    $"{a} should be ≤ LCA({a},{b})={lca}");
                Assert.IsTrue(gcdP.CanBePessimisticConvertedTo(lcaP),
                    $"GCD ≤ LCA violated: GCD({a},{b})={gcd} ≤ LCA({a},{b})={lca}");
            }
        }
    }

    [Test]
    public void Lca_FitsInto_Relationship_Primitives() {
        // A always fits into Lca(A,B)
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            var lca = a.Lca(b);
            Assert.IsTrue(a.FitsInto(lca),
                $"{a} should fit into Lca({a},{b})={lca}");
            Assert.IsTrue(b.FitsInto(lca),
                $"{b} should fit into Lca({a},{b})={lca}");
        }
    }

    [Test]
    public void Gcd_FitsInto_Relationship_Primitives() {
        // Gcd(A,B) always fits into A and B (if not null)
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            var gcd = a.Gcd(b);
            if (gcd != null)
            {
                Assert.IsTrue(gcd.FitsInto(a),
                    $"Gcd({a},{b})={gcd} should fit into {a}");
                Assert.IsTrue(gcd.FitsInto(b),
                    $"Gcd({a},{b})={gcd} should fit into {b}");
            }
        }
    }

    [Test]
    public void Lca_Struct_FieldIntersection() {
        var ab = Struct(("x", I32), ("y", Bool));
        var ac = Struct(("x", I32), ("z", Real));
        var result = ab.Lca(ac);
        Assert.IsInstanceOf<StateStruct>(result);
        var s = (StateStruct)result;
        Assert.IsNotNull(s.GetFieldOrNull("x"), "common field 'x' should be in LCA");
        Assert.IsNull(s.GetFieldOrNull("y"), "'y' only in left");
        Assert.IsNull(s.GetFieldOrNull("z"), "'z' only in right");
    }

    [Test]
    public void Lca_Struct_CovariantFields() {
        // Lca({a:I32}, {a:Real}) = {a:Real}
        var s1 = Struct("a", I32);
        var s2 = Struct("a", Real);
        var result = s1.Lca(s2);
        var s = (StateStruct)result;
        Assert.AreEqual(1, s.Fields.Count());
        Assert.AreEqual(Real, s.GetFieldOrNull("a").State);
    }

    [Test]
    public void Lca_Struct_IncompatibleFieldTypes_KeepsWithAny() {
        // Lca({a:I32}, {a:Bool}) = {a:Any}
        var s1 = Struct("a", I32);
        var s2 = Struct("a", Bool);
        var result = s1.Lca(s2);
        var s = (StateStruct)result;
        Assert.AreEqual(1, s.Fields.Count());
        Assert.AreEqual(Any, s.GetFieldOrNull("a").State);
    }

    [Test]
    public void Lca_Struct_EmptyStructAbsorbs() {
        // Lca({a:I32}, {}) = {}
        var s1 = Struct("a", I32);
        var s2 = EmptyStruct();
        var result = s1.Lca(s2);
        var s = (StateStruct)result;
        Assert.AreEqual(0, s.Fields.Count());
    }

    [Test]
    public void Lca_Struct_Symmetry() {
        var s1 = Struct(("a", I32), ("b", Real));
        var s2 = Struct(("b", I64), ("c", Bool));
        var r1 = s1.Lca(s2);
        var r2 = s2.Lca(s1);
        Assert.AreEqual(r1, r2, $"Struct LCA symmetry: {r1} vs {r2}");
    }

    [Test]
    public void Lca_Struct_NestedStruct() {
        // Lca({a:{x:I32}}, {a:{x:Real}}) = {a:{x:Real}}
        var s1 = Struct("a", Struct("x", I32));
        var s2 = Struct("a", Struct("x", Real));
        var result = s1.Lca(s2);
        var s = (StateStruct)result;
        Assert.AreEqual(1, s.Fields.Count());
        var inner = s.GetFieldOrNull("a").State as StateStruct;
        Assert.IsNotNull(inner, "inner should be struct");
        Assert.AreEqual(Real, inner.GetFieldOrNull("x").State);
    }

    [Test]
    public void Lca_Struct_ArrayField() {
        // Lca({a:I32[]}, {a:Real[]}) = {a:Real[]}  (covariant array in covariant field)
        var s1 = Struct("a", Array(I32));
        var s2 = Struct("a", Array(Real));
        var result = s1.Lca(s2);
        var s = (StateStruct)result;
        Assert.AreEqual(1, s.Fields.Count());
        var arr = s.GetFieldOrNull("a").State as StateArray;
        Assert.IsNotNull(arr, "field should be array");
        Assert.AreEqual(Real, arr.Element);
    }

    [Test]
    public void FitsInto_Reflexive() {
        foreach (var a in AllConcreteTypes())
            Assert.IsTrue(a.FitsInto(a), $"{a} should fit into itself");
    }

    [Test]
    public void FitsInto_AnyAcceptsAllConcrete() {
        // Any is top: all types fit into Any (including None and Optional)
        foreach (var a in AllConcreteTypes())
            Assert.IsTrue(a.FitsInto(Any), $"{a} should fit into Any");
    }

    [Test]
    public void FitsInto_OptAnyAcceptsAll() {
        // opt(any) = any, so all types fit into Opt(Any) too
        var optAny = Optional(Any);
        foreach (var a in AllConcreteTypes())
            Assert.IsTrue(a.FitsInto(optAny), $"{a} should fit into Opt(Any)");
    }

    [Test]
    public void FitsInto_Transitivity_Primitives() {
        // A ≤ B  ∧  B ≤ C  ⟹  A ≤ C
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        foreach (var c in PrimitiveTypes)
        {
            if (a.FitsInto(b) && b.FitsInto(c))
                Assert.IsTrue(a.FitsInto(c),
                    $"Transitivity: {a}≤{b} and {b}≤{c} but {a} not ≤ {c}");
        }
    }

    [Test]
    public void FitsInto_Array_Covariance() {
        // A ≤ B  ⟹  A[] ≤ B[]
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            if (a.FitsInto(b))
            {
                var arrA = (ITicNodeState)Array(a);
                var arrB = (ITicNodeState)Array(b);
                Assert.IsTrue(arrA.FitsInto(arrB),
                    $"{a}≤{b} but {a}[] not ≤ {b}[]");
            }
        }
    }

    [Test]
    public void FitsInto_Struct_WidthSubtyping() {
        var wide = Struct(("a", I32), ("b", Bool));
        var narrow = Struct("a", I32);
        Assert.IsTrue(wide.FitsInto(narrow),
            "wider struct should fit into narrower");
        Assert.IsFalse(narrow.FitsInto(wide),
            "narrower struct should NOT fit into wider");
    }

    [Test]
    public void FitsInto_Struct_EmptyAcceptsAll() {
        // Any struct fits into {}
        var s1 = Struct(("a", I32), ("b", Bool));
        Assert.IsTrue(s1.FitsInto(EmptyStruct()));
        Assert.IsTrue(EmptyStruct().FitsInto(EmptyStruct()));
    }

    [Test]
    public void FitsInto_EmptyConstrainsAcceptsAll() {
        // Any type fits into unconstrained
        foreach (var a in AllConcreteTypes())
            Assert.IsTrue(a.FitsInto(EmptyConstraints),
                $"{a} should fit into empty constraints");
    }

    [Test]
    public void FitsInto_ConstrainsInterval() {
        // I32 fits into [U8..Real] (I32 is in the interval)
        var interval = Constrains(U8, Real);
        Assert.IsTrue(I32.FitsInto(interval));
        Assert.IsTrue(Real.FitsInto(interval));
        Assert.IsTrue(U8.FitsInto(interval));
        // Bool does not fit into [U8..Real]
        Assert.IsFalse(Bool.FitsInto(interval));
    }

    // ================================================================
    // Associativity (for non-primitives)
    // ================================================================

    // Compact set for O(N³) tests — mix of primitives, arrays, structs, funs, optionals
    private static ITicNodeState[] AssocTypes => new ITicNodeState[] {
        I32, Real, Bool, Any, U8, None,
        Array(I32), Array(Real), Array(Any),
        Struct("a", I32), Struct("a", Real), Struct(("a", I32), ("b", Bool)), EmptyStruct(),
        Fun(I32, Real), Fun(Bool, Bool),
        Optional(I32), Optional(Real), Optional(Bool), Optional(Array(I32)),
    };

    private static ITicNodeState[] AssocConstrainedTypes => new ITicNodeState[] {
        EmptyConstraints,
        Constrains(U8, Real),
        Constrains(I16, I64),
        Constrains(isComparable: true),
    };

    private static ITicNodeState[] AssocAllTypes
        => AssocTypes.Concat(AssocConstrainedTypes).ToArray();

    [Test]
    public void Lca_Associativity_Composites() {
        // Lca(Lca(A,B), C) = Lca(A, Lca(B,C))
        foreach (var a in AssocTypes)
        foreach (var b in AssocTypes)
        foreach (var c in AssocTypes)
        {
            var ab_c = a.Lca(b).Lca(c);
            var a_bc = a.Lca(b.Lca(c));
            Assert.AreEqual(ab_c, a_bc,
                $"LCA assoc violated for ({a}, {b}, {c}): ({a.Lca(b)})∨{c}={ab_c} but {a}∨({b.Lca(c)})={a_bc}");
        }
    }

    [Test]
    public void Lca_Associativity_WithConstraints() {
        foreach (var a in AssocAllTypes)
        foreach (var b in AssocAllTypes)
        foreach (var c in AssocAllTypes)
        {
            var ab_c = a.Lca(b).Lca(c);
            var a_bc = a.Lca(b.Lca(c));
            Assert.AreEqual(ab_c, a_bc,
                $"LCA assoc violated for ({a}, {b}, {c}): ({a.Lca(b)})∨{c}={ab_c} but {a}∨({b.Lca(c)})={a_bc}");
        }
    }

    [Test]
    public void Gcd_Associativity_Composites() {
        // For types where Gcd is defined:
        // Gcd(Gcd(A,B), C) = Gcd(A, Gcd(B,C))  — when all intermediate results are non-null
        foreach (var a in AssocTypes)
        foreach (var b in AssocTypes)
        foreach (var c in AssocTypes)
        {
            var ab = a.Gcd(b);
            var bc = b.Gcd(c);
            if (ab == null || bc == null) continue;
            var ab_c = ab.Gcd(c);
            var a_bc = a.Gcd(bc);
            if (ab_c == null && a_bc == null) continue; // both null is fine
            Assert.AreEqual(ab_c, a_bc,
                $"GCD assoc violated for ({a}, {b}, {c})");
        }
    }

    [Test]
    public void FitsInto_Struct_DepthCovariance() {
        // {a:I32} fits {a:Real}  — because I32 ≤ Real (covariant fields)
        var narrow = Struct("a", I32);
        var wide = Struct("a", Real);
        Assert.IsTrue(narrow.FitsInto(wide),
            "{a:I32} should fit into {a:Real} (covariant fields)");
    }

    [Test]
    public void FitsInto_Struct_DepthCovariance_Incompatible() {
        // {a:Bool} does NOT fit {a:I32}  — Bool and I32 unrelated
        var a = Struct("a", Bool);
        var b = Struct("a", I32);
        Assert.IsFalse(a.FitsInto(b));
    }

    [Test]
    public void FitsInto_Fun_Contravariance() {
        // Fun(Real→I32) fits Fun(I32→Real)?
        // Return: I32 ≤ Real ✓ (covariant)
        // Arg: to_arg(I32) ≤ target_arg(Real) ✓ (contravariant — caller passes I32, func accepts Real)
        var f1 = (ITicNodeState)Fun(Real, I32);
        var f2 = (ITicNodeState)Fun(I32, Real);
        Assert.IsTrue(f1.FitsInto(f2),
            "Fun(Real→I32) should fit Fun(I32→Real) — both arg and ret widen I32→Real");

        // Fun(I32→I32) fits Fun(Real→Real)?
        // Return: I32 ≤ Real ✓
        // Arg: to_arg(Real) ≤ target_arg(I32)? NO — caller passes Real, func only accepts I32
        var f3 = (ITicNodeState)Fun(I32, I32);
        var f4 = (ITicNodeState)Fun(Real, Real);
        Assert.IsFalse(f3.FitsInto(f4),
            "Fun(I32→I32) should NOT fit Fun(Real→Real) — arg contravariance violated");

        // Fun(Real→I32) fits Fun(Real→Real)?
        // Return: I32 ≤ Real ✓ (covariant)
        // Arg: to_arg(Real) ≤ target_arg(Real) ✓
        var f5 = (ITicNodeState)Fun(Real, I32);
        var f6 = (ITicNodeState)Fun(Real, Real);
        Assert.IsTrue(f5.FitsInto(f6),
            "Fun(Real→I32) should fit Fun(Real→Real)");

        // Fun(I32→Real) fits Fun(Real→I32)?
        // Return: Real ≤ I32? NO — return type doesn't fit
        // Arg: to_arg(Real) ≤ target_arg(I32)? NO — arg doesn't fit either
        var f7 = (ITicNodeState)Fun(I32, Real);
        var f8 = (ITicNodeState)Fun(Real, I32);
        Assert.IsFalse(f7.FitsInto(f8),
            "Fun(I32→Real) should NOT fit Fun(Real→I32) — both arg and ret fail");
    }

    [Test]
    public void FitsInto_Transitivity_Composites() {
        // Struct transitivity: {a:U8, b:X} ≤ {a:I32} ≤ {a:Real}
        var s1 = (ITicNodeState)Struct(("a", U8), ("b", Bool));
        var s2 = (ITicNodeState)Struct("a", I32);
        var s3 = (ITicNodeState)Struct("a", Real);

        if (s1.FitsInto(s2) && s2.FitsInto(s3))
            Assert.IsTrue(s1.FitsInto(s3), "Struct transitivity violated");

        // Array transitivity: U8[] ≤ I32[] ≤ Real[]
        var a1 = (ITicNodeState)Array(U8);
        var a2 = (ITicNodeState)Array(I32);
        var a3 = (ITicNodeState)Array(Real);

        if (a1.FitsInto(a2) && a2.FitsInto(a3))
            Assert.IsTrue(a1.FitsInto(a3), "Array transitivity violated");
    }

    [Test]
    public void FitsInto_Antisymmetry_Primitives() {
        // A ≤ B ∧ B ≤ A  ⟹  A = B
        foreach (var a in PrimitiveTypes)
        foreach (var b in PrimitiveTypes)
        {
            if (a.FitsInto(b) && b.FitsInto(a))
                Assert.AreEqual(a, b,
                    $"Antisymmetry: {a}≤{b} and {b}≤{a} but {a}≠{b}");
        }
    }

    // ================================================================
    // Cross-operation invariants for composites
    // ================================================================

    [Test]
    public void Lca_FitsInto_Relationship_Composites() {
        // A always fits into Lca(A,B)
        foreach (var a in AssocTypes)
        foreach (var b in AssocTypes)
        {
            var lca = a.Lca(b);
            Assert.IsTrue(a.FitsInto(lca),
                $"{a} should fit into Lca({a},{b})={lca}");
            Assert.IsTrue(b.FitsInto(lca),
                $"{b} should fit into Lca({a},{b})={lca}");
        }
    }

    [Test]
    public void Gcd_FitsInto_Relationship_Composites() {
        // Gcd(A,B) fits into A and B (if not null)
        foreach (var a in AssocTypes)
        foreach (var b in AssocTypes)
        {
            var gcd = a.Gcd(b);
            if (gcd != null)
            {
                Assert.IsTrue(gcd.FitsInto(a),
                    $"Gcd({a},{b})={gcd} should fit into {a}");
                Assert.IsTrue(gcd.FitsInto(b),
                    $"Gcd({a},{b})={gcd} should fit into {b}");
            }
        }
    }
}
