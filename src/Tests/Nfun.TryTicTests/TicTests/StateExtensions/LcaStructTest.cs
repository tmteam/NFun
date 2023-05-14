namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static Tic.SolvingStates.StatePrimitive;

public class LcaStructTest {
    [Test]
    public void EmptyStructLcaToItself() =>
        AssertLca(StateStruct.Empty(), StateStruct.Empty(), StateStruct.Empty());

    [Test]
    public void ConcreteStructWithAdditionalField() {
        var str = StateStruct.Of(
            ("prim", I32),
            ("arr", StateArray.Of(StateArray.Of(Char))),
            ("fun", StateFun.Of(new[] { Bool }, I16)),
            ("str", StateStruct.Of(
                ("prim", I32),
                ("arr", StateArray.Of(StateArray.Of(Char))),
                ("fun", StateFun.Of(new[] { Bool }, I16))))
        );
        var other = str.With("additional", TicNode.CreateInvisibleNode(Char));
        AssertLca(str, other, str);
    }

    [Test]
    public void ItemsWithDifferentTypesDissapears() {
        var strA = StateStruct.Of(
            ("prim", I32),
            ("arr", StateArray.Of(StateArray.Of(Char))),
            ("fun", StateFun.Of(new[] { Bool }, I16)),
            ("str",
                StateStruct.Of(
                    ("prim", U32),
                    ("arr", StateArray.Of(StateArray.Of(Char))),
                    ("fun", StateFun.Of(new[] { Char }, I16))
                )),
            ("sameField", I32)
        );

        var strB = StateStruct.Of(
            ("sameField", I32),
            ("prim", U32),
            ("arr", StateArray.Of(StateArray.Of(Bool))),
            ("fun", StateFun.Of(new[] { Bool }, I32)),
            ("str",
                StateStruct.Of(
                    ("prim", U32),
                    ("arr", StateArray.Of(StateArray.Of(Char))),
                    ("fun", StateFun.Of(new[] { Char }, I16))
                ))
        );

        AssertLca(strA, strB,
            StateStruct.Of(
                ("sameField", I32),
                ("str", StateStruct.Of(
                    ("prim", U32),
                    ("arr", StateArray.Of(StateArray.Of(Char))),
                    ("fun", StateFun.Of(new[] { Char }, I16))
                ))
            ));
    }

    [Test]
    public void ItemsWithDifferentFieldsDisappears() {
        var strA = StateStruct.Of(
            ("a", I32),
            ("b", StateArray.Of(StateArray.Of(Char))),
            ("c", StateFun.Of(new[] { Bool }, I16))
        );

        var strB = StateStruct.Of(
            ("d", I32),
            ("e", StateArray.Of(StateArray.Of(Bool))),
            ("f", StateStruct.Of(
                ("prim", U32),
                ("fun", StateFun.Of(new[] { Char }, I16))
            ))
        );

        AssertLca(strA, strB, StateStruct.Empty());
    }

    [Test]
    public void Constrains1() {
        var strA = StateStruct.Of("a", ConstrainsState.Of(I16, Real));
        var strB = StateStruct.Of("a", ConstrainsState.Of(U8, U64));
        // RES = if(...) strA else strB
        // so RES has to fit both of these structures
        // Lets see all the types for strA.a: I16, I32, I64, Real
        // Lets see all the types for strA.b: U8, U16, U32, U64
        // So there is no intersection, and the most conrete RES that can fit both is empty struct
        AssertLca(strA, strB, StateStruct.Empty());
    }

    [Test]
    public void Constrains2() {
        var strA = StateStruct.Of("a", ConstrainsState.Of(I16, Real));
        var strB = StateStruct.Of("a", ConstrainsState.Of(U8, I64));
        // RES = if(...) strA else strB
        // so RES has to fit both of these structures
        // Lets see all the types for strA.a: I16, I32, I64, Real
        // Lets see all the types for strA.b: U8, U16, U32, I16, I32, I64
        // so result is [I16..I64]
        AssertLca(strA, strB, StateStruct.Of("a", ConstrainsState.Of(I16, I64)));
    }

    [Test]
    public void Constrains3() {
        var strA = StateStruct.Of("a", ConstrainsState.Of(StateArray.Of(I16), Any));
        var strB = StateStruct.Of("a", ConstrainsState.Of(StateArray.Of(U16), null));
        AssertLca(strA, strB, StateStruct.Of("a", ConstrainsState.Of(StateArray.Of(I24), Any)));
    }

    [Test]
    public void Constrains4() {
        var strA = StateStruct.Of("a", StateArray.Of(ConstrainsState.Of(I16)));
        var strB = StateStruct.Of("a", StateArray.Of(ConstrainsState.Of(U16)));
        AssertLca(strA, strB, StateStruct.Of("a", StateArray.Of(ConstrainsState.Of(I24))));
    }

    [Test]
    public void Constrains5() {
        var strA = StateStruct.Of(
            ("a", StateArray.Of(ConstrainsState.Of(I16))),
            ("b", I16));
        var strB = StateStruct.Of(
            ("a", StateArray.Of(ConstrainsState.Of(U16))),
            ("c", Any)
        );
        AssertLca(strA, strB, StateStruct.Of("a", StateArray.Of(ConstrainsState.Of(I24))));
    }

    [Test]
    public void Constrains6() {
        var strA = StateStruct.Of(
            ("a", StateArray.Of(I64)),
            ("b", Any));
        var strB = StateStruct.Of(
            ("a", StateArray.Of(ConstrainsState.Of(U16))),
            ("c", Any)
        );
        AssertLca(strA, strB, StateStruct.Of("a", StateArray.Of(I64)));
    }

    [Test]
    public void Comparable1() {
        var strA = StateStruct.Of("a", ConstrainsState.Of(isComparable: true));
        var strB = StateStruct.Of("a", ConstrainsState.Of(isComparable: true));
        AssertLca(strA, strB, StateStruct.Of("a", ConstrainsState.Of(isComparable: true)));
    }

    [Test]
    public void Comparable2() {
        // if some is comparable, and other is not - it means that result constrains has to be comparable
        var strA = StateStruct.Of("a", ConstrainsState.Of(isComparable: true));
        var strB = StateStruct.Of("a", ConstrainsState.Empty);
        AssertLca(strA, strB, StateStruct.Of("a", ConstrainsState.Of(isComparable: true)));
    }

    [Test]
    public void Comparable3() {
        var strA = StateStruct.Of("a", ConstrainsState.Of(I16, Any, isComparable: true));
        var strB = StateStruct.Of("a", ConstrainsState.Of(U8, Any));
        AssertLca(strA, strB, StateStruct.Of("a", ConstrainsState.Of(I16, Real, true)));
    }
}
