namespace NFun.UnitTests.TicTests.Lca;

using System.Collections.Generic;
using NUnit.Framework;
using Tic;
using Tic.SolvingStates;
using static LcaTestTools;
using static Tic.SolvingStates.StatePrimitive;

public class LcaStructTest {

    [Test]
    public void EmptyStructLcaToItself() =>
        AssertLca(
            StateStruct.Of(new KeyValuePair<string, ITicNodeState>[0], true),
            StateStruct.Of(new KeyValuePair<string, ITicNodeState>[0], true),
            StateStruct.Of(new KeyValuePair<string, ITicNodeState>[0], true));

    [Test]
    public void ConcreteStructWithAdditionalField() {
        var str = StateStruct.Of(
            new[] {
                new KeyValuePair<string, ITicNodeState>("prim", I32),
                new KeyValuePair<string, ITicNodeState>("arr", StateArray.Of(StateArray.Of(Char))),
                new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Bool }, I16)),
                new KeyValuePair<string, ITicNodeState>("str",
                    StateStruct.Of(
                        new[] {
                            new KeyValuePair<string, ITicNodeState>("prim", I32),
                            new KeyValuePair<string, ITicNodeState>("arr", StateArray.Of(StateArray.Of(Char))),
                            new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Bool }, I16)),
                        }, true))
            }, true);
        var other = str.With("additional", TicNode.CreateInvisibleNode(Char));
        AssertLca(str, other, str);
    }

    [Test]
    public void ItemsWithDifferentTypesDissapears() {
        var strA = StateStruct.Of(
            new[] {
                new KeyValuePair<string, ITicNodeState>("prim", I32),
                new KeyValuePair<string, ITicNodeState>("arr", StateArray.Of(StateArray.Of(Char))),
                new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Bool }, I16)),
                new KeyValuePair<string, ITicNodeState>("str",
                    StateStruct.Of(
                        new[] {
                            new KeyValuePair<string, ITicNodeState>("prim", I32),
                            new KeyValuePair<string, ITicNodeState>("arr", StateArray.Of(StateArray.Of(Char))),
                            new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Bool }, I16)),
                        }, true))
            }, true);

        var strB = StateStruct.Of(
            new[] {
                new KeyValuePair<string, ITicNodeState>("sameField", I32),
                new KeyValuePair<string, ITicNodeState>("prim", U32),
                new KeyValuePair<string, ITicNodeState>("arr", StateArray.Of(StateArray.Of(Bool))),
                new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Bool }, I32)),
                new KeyValuePair<string, ITicNodeState>("str",
                    StateStruct.Of(
                        new[] {
                            new KeyValuePair<string, ITicNodeState>("prim", U32),
                            new KeyValuePair<string, ITicNodeState>("sameField", StateArray.Of(StateArray.Of(Char))),
                            new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Char }, I16)),
                        }, true))
            }, true);

        AssertLca(strA, strB,
            StateStruct.Of(
                new[] {
                    new KeyValuePair<string, ITicNodeState>("sameField", I32), new KeyValuePair<string, ITicNodeState>(
                        "str",
                        StateStruct.Of(
                            new[] {
                                new KeyValuePair<string, ITicNodeState>("sameField",
                                    StateArray.Of(StateArray.Of(Char))),
                            }, true))
                }, true));
    }

    [Test]
    public void ItemsWithDifferentFieldsDissapears() {
        var strA = StateStruct.Of(
            new[] {
                new KeyValuePair<string, ITicNodeState>("a", I32),
                new KeyValuePair<string, ITicNodeState>("b", StateArray.Of(StateArray.Of(Char))),
                new KeyValuePair<string, ITicNodeState>("c", StateFun.Of(new[] { Bool }, I16)),
            }, true);

        var strB = StateStruct.Of(
            new[] {
                new KeyValuePair<string, ITicNodeState>("d", I32),
                new KeyValuePair<string, ITicNodeState>("e", StateArray.Of(StateArray.Of(Bool))),
                new KeyValuePair<string, ITicNodeState>("f",
                    StateStruct.Of(
                        new[] {
                            new KeyValuePair<string, ITicNodeState>("prim", U32),
                            new KeyValuePair<string, ITicNodeState>("fun", StateFun.Of(new[] { Char }, I16)),
                        }, true))
            }, true);

        AssertLca(strA, strB, StateStruct.Of(new KeyValuePair<string, ITicNodeState>[0], true));
    }
}
