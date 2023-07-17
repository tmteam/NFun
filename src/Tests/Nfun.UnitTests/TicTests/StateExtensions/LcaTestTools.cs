namespace NFun.UnitTests.TicTests.StateExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using TestTools;
using Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

public record TypeMap(StatePrimitive Left, StatePrimitive Right, StatePrimitive Lca);

public static class LcaTestTools {
    public static readonly IList<TypeMap> PrimitiveTypesLca;

    public static readonly StatePrimitive[] PrimitiveTypes;

    static LcaTestTools() {
        var _primitiveTypeNames = new[] {
            PrimitiveTypeName.Any, PrimitiveTypeName.Char, PrimitiveTypeName.Bool, PrimitiveTypeName.Ip,
            PrimitiveTypeName.Real, PrimitiveTypeName.I96, PrimitiveTypeName.I64, PrimitiveTypeName.I48,
            PrimitiveTypeName.I32, PrimitiveTypeName.I24, PrimitiveTypeName.I16, PrimitiveTypeName.U64,
            PrimitiveTypeName.U32, PrimitiveTypeName.U16, PrimitiveTypeName.U8,
        };

        var primitiveTypesLca = new List<Tuple<PrimitiveTypeName, PrimitiveTypeName, PrimitiveTypeName>>();

        // a:(a!=x) LCA x = any
        singleAncestor(PrimitiveTypeName.Any);
        singleAncestor(PrimitiveTypeName.Ip);
        singleAncestor(PrimitiveTypeName.Char);
        singleAncestor(PrimitiveTypeName.Bool);

        var numbers = new[] {
            PrimitiveTypeName.Real, PrimitiveTypeName.I96, PrimitiveTypeName.I64, PrimitiveTypeName.I48,
            PrimitiveTypeName.I32, PrimitiveTypeName.I24, PrimitiveTypeName.I16, PrimitiveTypeName.U64,
            PrimitiveTypeName.U32, PrimitiveTypeName.U16, PrimitiveTypeName.U12, PrimitiveTypeName.U8,
        };

        foreach (var type in numbers)
            registrate(type, PrimitiveTypeName.Any, PrimitiveTypeName.Any);

        // number lca u8 = number
        foreach (var type in numbers.Where(it => it != PrimitiveTypeName.U8))
            registrate(type, PrimitiveTypeName.U8, type);

        foreach (var type in numbers.Where(it => it != PrimitiveTypeName.Real))
            registrate(type, PrimitiveTypeName.Real, PrimitiveTypeName.Real);

        var i96s = numbers.Where(it => it != PrimitiveTypeName.Real);
        foreach (var type in i96s)
            registrate(type, PrimitiveTypeName.I96, PrimitiveTypeName.I96);

        var i64s = i96s.Where(it => it != PrimitiveTypeName.I96 && it != PrimitiveTypeName.U64);
        foreach (var type in i64s)
            registrate(type, PrimitiveTypeName.I64, PrimitiveTypeName.I64);

        var i48s = i64s.Where(it => it != PrimitiveTypeName.I64 && it != PrimitiveTypeName.U64).ToList();
        foreach (var type in i48s)
            registrate(type, PrimitiveTypeName.I48, PrimitiveTypeName.I48);

        var i32s = i48s.Where(it => it != PrimitiveTypeName.U32 && it != PrimitiveTypeName.I48);
        foreach (var type in i32s)
            registrate(type, PrimitiveTypeName.I32, PrimitiveTypeName.I32);

        var i24s = i32s.Where(it => it != PrimitiveTypeName.I32 && it != PrimitiveTypeName.U32);
        foreach (var type in i24s)
            registrate(type, PrimitiveTypeName.I24, PrimitiveTypeName.I24);


        registrate(PrimitiveTypeName.I64, PrimitiveTypeName.U64, PrimitiveTypeName.I96);
        registrate(PrimitiveTypeName.I48, PrimitiveTypeName.U64, PrimitiveTypeName.I96);
        registrate(PrimitiveTypeName.I32, PrimitiveTypeName.U64, PrimitiveTypeName.I96);
        registrate(PrimitiveTypeName.I24, PrimitiveTypeName.U64, PrimitiveTypeName.I96);
        registrate(PrimitiveTypeName.I16, PrimitiveTypeName.U64, PrimitiveTypeName.I96);


        registrate(PrimitiveTypeName.I32, PrimitiveTypeName.U32, PrimitiveTypeName.I48);
        registrate(PrimitiveTypeName.I24, PrimitiveTypeName.U32, PrimitiveTypeName.I48);
        registrate(PrimitiveTypeName.I16, PrimitiveTypeName.U32, PrimitiveTypeName.I48);

        registrate(PrimitiveTypeName.I16, PrimitiveTypeName.U16, PrimitiveTypeName.I24);
        registrate(PrimitiveTypeName.I16, PrimitiveTypeName.U8, PrimitiveTypeName.I16);

        registrate(PrimitiveTypeName.U64, PrimitiveTypeName.U32, PrimitiveTypeName.U64);
        registrate(PrimitiveTypeName.U64, PrimitiveTypeName.U16, PrimitiveTypeName.U64);

        registrate(PrimitiveTypeName.U32, PrimitiveTypeName.U16, PrimitiveTypeName.U32);
        registrate(PrimitiveTypeName.U32, PrimitiveTypeName.U8, PrimitiveTypeName.U32);
        registrate(PrimitiveTypeName.U16, PrimitiveTypeName.U8, PrimitiveTypeName.U16);

        // a LCA b = b LCA a
        foreach (var tripple in primitiveTypesLca.ToArray())
        {
            primitiveTypesLca.Add(
                t(tripple.Item2, tripple.Item1,
                    tripple.Item3));
        }

        // a LCA a = a
        foreach (var type in _primitiveTypeNames)
            primitiveTypesLca.Add(
                t(type, type, type));


        var _primitiveTypesLcaNames = primitiveTypesLca.Distinct().ToList();
        PrimitiveTypesLca = _primitiveTypesLcaNames.Select(it =>
                new TypeMap(new StatePrimitive(it.Item1), new StatePrimitive(it.Item2), new StatePrimitive(it.Item3)))
            .ToList();
        PrimitiveTypes = _primitiveTypeNames.Select(it => new StatePrimitive(it)).ToArray();
        // Validate ourselves
        foreach (var a in _primitiveTypeNames)
        foreach (var b in _primitiveTypeNames)
        {
            var aToB = _primitiveTypesLcaNames.Where(it => it.Item1 == a && it.Item2 == b).ToList();
            Assert.AreEqual(1, aToB.Count,
                $"Wrong lca entities count for {a}:{b} ({aToB.Count} entities found: {aToB.Select(a => a.ToString()).ToStringSmart()})");
        }

        void singleAncestor(PrimitiveTypeName ancestor) {
            foreach (var type in _primitiveTypeNames.Where(it => it != ancestor))
                registrate(type, ancestor, PrimitiveTypeName.Any);
        }

        void registrate(PrimitiveTypeName a, PrimitiveTypeName b, PrimitiveTypeName lca) =>
            primitiveTypesLca.Add(t(a, b, lca));

        Tuple<PrimitiveTypeName, PrimitiveTypeName, PrimitiveTypeName> t(PrimitiveTypeName a, PrimitiveTypeName b,
            PrimitiveTypeName lca) => new(a, b, lca);
    }

    public static void AssertLca(ITicNodeState a, ITicNodeState b, ITicNodeState expected) {

        var result1 = a.Lca(b);
        var result2 = b.Lca(a);

        var aRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("a", a));
        var bRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("b", b));

        var aRefRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("aa", aRef));
        var bRefRef = SolvingStates.Ref(TicNode.CreateTypeVariableNode("bb", bRef));

        var result3 = aRef.Lca(bRef);
        var result4 = bRefRef.Lca(aRefRef);

        Assert.AreEqual(expected, result1, $"1: {a.StateDescription} LCA {b.StateDescription} = {result1.StateDescription}, but was expected {expected.StateDescription}");
        Assert.AreEqual(expected, result2, $"1: {b.StateDescription} LCA {a.StateDescription} = {result2.StateDescription}, but was expected {expected.StateDescription}");
        Assert.AreEqual(expected, result3, $"1: {aRef.StateDescription} LCA {bRef.StateDescription} = {result3.StateDescription}, but was expected {expected.StateDescription}");
        Assert.AreEqual(expected, result4, $"1: {aRefRef.StateDescription} LCA {bRefRef.StateDescription} = {result4.StateDescription}, but was expected {expected.StateDescription}");
    }
}
