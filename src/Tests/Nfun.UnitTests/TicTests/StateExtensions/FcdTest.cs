namespace NFun.UnitTests.TicTests.StateExtensions;

using NUnit.Framework;
using Tic;
using NFun.Tic.Algebra;
using Tic.SolvingStates;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class GcdTest {

    [Test]
    public void Primitive1() => AssertGcd(Real, Real, Real);

    [Test]
    public void Primitive2() => AssertGcd(U64, I64, U48);

    [Test]
    public void Primitive3() => AssertGcd(Char, I64, null);

    [Test]
    public void Struct1() {
        var strA = Struct(
            ("a", I32),
            ("b", Array(Array(Char))),
            ("c", Fun(Bool, I16))
        );

        var strB = Struct(
            ("d", I32),
            ("e", Array(Array(Bool))),
            ("f", Struct(
                ("prim", U32),
                ("fun", Fun(Char, I16))
            ))
        );

        AssertGcd(strA, strB,
            Struct(
                ("a", I32),
                ("b", Array(Array(Char))),
                ("c", Fun(Bool, I16)),
                ("d", I32),
                ("e", Array(Array(Bool))),
                ("f", Struct(
                    ("prim", U32),
                    ("fun", Fun(new[] { Char }, I16))
                ))));
    }

    [Test]
    public void Struct2() {
        var strA = Struct(
            ("a", I32),
            ("c", Fun(new[] { Bool }, I16))
        );

        var strB = Struct(
            ("a", U32),
            ("d", I32)
        );

        // GCD(I32, U32) = U24 (common subtype of both)
        AssertGcd(strA, strB,
            Struct(
                ("a", U24),
                ("c", Fun(new[] { Bool }, I16)),
                ("d", I32)));
    }

    [Test]
    public void Struct3() {
        var strA = Struct("a", I32);
        var strB = Struct("d", I32);

        AssertGcd(strA, strB,
            Struct(
                ("a", I32),
                ("d", I32)));
    }

    [Test]
    public void Struct_SharedFieldWithCompatibleTypes() {
        // GCD({a:I32, b:Bool}, {a:Real, c:I32})
        // Field a: GCD(I32, Real) = I32 (I32 is subtype of Real)
        // Result: {a:I32, b:Bool, c:I32}
        var strA = Struct(
            ("a", I32),
            ("b", Bool)
        );
        var strB = Struct(
            ("a", Real),
            ("c", I32)
        );
        AssertGcd(strA, strB,
            Struct(
                ("a", I32),
                ("b", Bool),
                ("c", I32)));
    }

    [Test]
    public void Struct_SharedFieldWithSameType() {
        // GCD({a:I32, b:Bool}, {a:I32, c:Real})
        // Field a: GCD(I32, I32) = I32
        // Result: {a:I32, b:Bool, c:Real}
        var strA = Struct(
            ("a", I32),
            ("b", Bool)
        );
        var strB = Struct(
            ("a", I32),
            ("c", Real)
        );
        AssertGcd(strA, strB,
            Struct(
                ("a", I32),
                ("b", Bool),
                ("c", Real)));
    }

    public static void AssertGcd(ITicNodeState a, ITicNodeState b, ITicNodeState expected) {
        var result1 = a.Gcd(b);
        var result2 = b.Gcd(a);

        var aRef = Ref(TicNode.CreateTypeVariableNode("a", a));
        var bRef = Ref(TicNode.CreateTypeVariableNode("b", b));

        var aRefRef = Ref(TicNode.CreateTypeVariableNode("aa", aRef));
        var bRefRef = Ref(TicNode.CreateTypeVariableNode("bb", bRef));

        var result3 = aRef.Gcd(bRef);
        var result4 = bRefRef.Gcd(aRefRef);

        Assert.AreEqual(expected, result1, $"1: {a.StateDescription} GCD {b.StateDescription} = {result1?.StateDescription}, but was expected {expected?.StateDescription}");
        Assert.AreEqual(expected, result2, $"1: {b.StateDescription} GCD {a.StateDescription} = {result2?.StateDescription}, but was expected {expected?.StateDescription}");
        Assert.AreEqual(expected, result3, $"1: {aRef.StateDescription} GCD {bRef.StateDescription} = {result3?.StateDescription}, but was expected {expected?.StateDescription}");
        Assert.AreEqual(expected, result4, $"1: {aRefRef.StateDescription} GCD {bRefRef.StateDescription} = {result4?.StateDescription}, but was expected {expected?.StateDescription}");
    }
}
