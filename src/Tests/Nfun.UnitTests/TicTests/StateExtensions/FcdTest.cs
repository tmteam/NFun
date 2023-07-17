namespace NFun.UnitTests.TicTests.StateExtensions;

using NUnit.Framework;
using Tic;
using Tic.SolvingStates;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class FcdTest {

    [Test]
    public void Primitive1() => AssertFcd(Real, Real, Real);

    [Test]
    public void Primitive2() => AssertFcd(U64, I64, U48);

    [Test]
    public void Primitive3() => AssertFcd(Char, I64, null);

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

        AssertFcd(strA, strB,
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

        AssertFcd(strA, strB, null);
    }

    [Test]
    public void Struct3() {
        var strA = Struct("a", I32);
        var strB = Struct("d", I32);

        AssertFcd(strA, strB,
            Struct(
                ("a", I32),
                ("d", I32)));
    }

    public static void AssertFcd(ITicNodeState a, ITicNodeState b, ITicNodeState expected) {
        var result1 = a.Fcd(b);
        var result2 = b.Fcd(a);

        var aRef = Ref(TicNode.CreateTypeVariableNode("a", a));
        var bRef = Ref(TicNode.CreateTypeVariableNode("b", b));

        var aRefRef = Ref(TicNode.CreateTypeVariableNode("aa", aRef));
        var bRefRef = Ref(TicNode.CreateTypeVariableNode("bb", bRef));

        var result3 = aRef.Fcd(bRef);
        var result4 = bRefRef.Fcd(aRefRef);

        Assert.AreEqual(expected, result1, $"1: {a.StateDescription} FCD {b.StateDescription} = {result1?.StateDescription}, but was expected {expected?.StateDescription}");
        Assert.AreEqual(expected, result2, $"1: {b.StateDescription} FCD {a.StateDescription} = {result2?.StateDescription}, but was expected {expected?.StateDescription}");
        Assert.AreEqual(expected, result3, $"1: {aRef.StateDescription} FCD {bRef.StateDescription} = {result3?.StateDescription}, but was expected {expected?.StateDescription}");
        Assert.AreEqual(expected, result4, $"1: {aRefRef.StateDescription} FCD {bRefRef.StateDescription} = {result4?.StateDescription}, but was expected {expected?.StateDescription}");
    }
}
