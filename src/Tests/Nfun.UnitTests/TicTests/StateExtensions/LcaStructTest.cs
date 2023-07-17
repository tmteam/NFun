namespace NFun.UnitTests.TicTests.StateExtensions;

using Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class LcaStructTest {
    [Test]
    public void EmptyStructLcaToItself() =>
        AssertLca(StateStruct.Empty(), StateStruct.Empty(), StateStruct.Empty());

    [Test]
    public void ConcreteStructWithAdditionalField() {
        var str = Struct(
            ("prim", I32),
            ("arr", Array(Array(Char))),
            ("fun", Fun(Bool, I16)),
            ("str", Struct(
                ("prim", I32),
                ("arr", Array(Array(Char))),
                ("fun", Fun(Bool, I16))))
        );
        var other = str.With("additional", TicNode.CreateInvisibleNode(Char));
        AssertLca(str, other, str);
    }

    [Test]
    public void ItemsWithDifferentTypesDissapears() {
        var strA = Struct(
            ("prim", I32),
            ("arr", Array(Array(Char))),
            ("fun", Fun(Bool, I16)),
            ("str",
                Struct(
                    ("prim", U32),
                    ("arr", Array(Array(Char))),
                    ("fun", Fun(new[] { Char }, I16))
                )),
            ("sameField", I32)
        );

        var strB = Struct(
            ("sameField", I32),
            ("prim", U32),
            ("arr", Array(Array(Bool))),
            ("fun", Fun(Bool, I32)),
            ("str",
                Struct(
                    ("prim", U32),
                    ("arr", Array(Array(Char))),
                    ("fun", Fun(Char, I16))
                ))
        );

        AssertLca(strA, strB,
            Struct(
                ("sameField", I32),
                ("str", Struct(
                    ("prim", U32),
                    ("arr", Array(Array(Char))),
                    ("fun", Fun(Char, I16))
                ))
            ));
    }

    [Test]
    public void ItemsWithDifferentFieldsDisappears() {
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

        AssertLca(strA, strB, EmptyStruct());
    }

    [Test]
    public void Constrains1() {
        var strA = Struct("a", Constrains(I16, Real));
        var strB = Struct("a", Constrains(U8, U64));
        // RES = if(...) strA else strB
        // so RES has to fit both of these structures
        // Lets see all the types for strA.a: I16, I32, I64, Real
        // Lets see all the types for strA.b: U8, U16, U32, U64
        // So there is no intersection, and the most conrete RES that can fit both is empty struct
        AssertLca(strA, strB, StateStruct.Empty());
    }

    [Test]
    public void Constrains2() {
        var strA = Struct("a", Constrains(I16, Real));
        var strB = Struct("a", Constrains(U8, I64));
        // RES = if(...) strA else strB
        // so RES has to fit both of these structures
        // Lets see all the types for strA.a: I16, I32, I64, Real
        // Lets see all the types for strA.b: U8, U16, U32, I16, I32, I64
        // so result is [I16..I64]
        AssertLca(strA, strB, Struct("a", Constrains(I16, I64)));
    }

    [Test]
    public void Constrains3() {
        var strA = Struct("a", Constrains(Array(I16), Any));
        var strB = Struct("a", Constrains(Array(U16), null));
        AssertLca(strA, strB, Struct("a", Constrains(Array(I24), Any)));
    }

    [Test]
    public void Constrains4() {
        var strA = Struct("a", Array(Constrains(I16)));
        var strB = Struct("a", Array(Constrains(U16)));
        AssertLca(strA, strB, Struct("a", Array(Constrains(I24))));
    }

    [Test]
    public void Constrains5() {
        var strA = Struct(
            ("a", Array(Constrains(I16))),
            ("b", I16));
        var strB = Struct(
            ("a", Array(Constrains(U16))),
            ("c", Any)
        );
        AssertLca(strA, strB, Struct("a", Array(Constrains(I24))));
    }

    [Test]
    public void Constrains6() {
        var strA = Struct(
            ("a", Array(I64)),
            ("b", Any));
        var strB = Struct(
            ("a", Array(Constrains(U16))),
            ("c", Any)
        );
        AssertLca(strA, strB, Struct("a", Array(I64)));
    }

    [Test]
    public void Comparable1() {
        var strA = Struct("a", Constrains(isComparable: true));
        var strB = Struct("a", Constrains(isComparable: true));
        AssertLca(strA, strB, Struct("a", Constrains(isComparable: true)));
    }

    [Test]
    public void Comparable2() {
        // if some is comparable, and other is not - it means that result constrains has to be comparable
        var strA = Struct("a", Constrains(isComparable: true));
        var strB = Struct("a", EmptyConstrains);
        AssertLca(strA, strB, Struct("a", Constrains(isComparable: true)));
    }

    [Test]
    public void Comparable3() {
        var strA = Struct("a", Constrains(I16, Any, isComparable: true));
        var strB = Struct("a", Constrains(U8, Any));
        AssertLca(strA, strB, Struct("a", Constrains(I16, Real, true)));
    }
}
