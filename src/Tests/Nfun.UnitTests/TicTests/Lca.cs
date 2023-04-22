namespace NFun.UnitTests.TicTests;

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;

public class LcaTest {
    private readonly IList<Tuple<StatePrimitive, StatePrimitive, StatePrimitive>> _primitiveTypesLca;

    private readonly StatePrimitive[] _primitiveType;

    public LcaTest() {
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

        var i48s = i64s.Where(it => it != PrimitiveTypeName.I64 && it!= PrimitiveTypeName.U64).ToList();
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
        _primitiveTypesLca = _primitiveTypesLcaNames.Select(it =>
                new Tuple<StatePrimitive, StatePrimitive, StatePrimitive>(new StatePrimitive(it.Item1),
                    new StatePrimitive(it.Item2), new StatePrimitive(it.Item3)))
            .ToList();
        _primitiveType = _primitiveTypeNames.Select(it => new StatePrimitive(it)).ToArray();
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


    [Test]
    public void Primitives() {
        foreach (var primitiveTriple in _primitiveTypesLca)
            AssertLca(primitiveTriple.Item1, primitiveTriple.Item2, primitiveTriple.Item3);
    }


    [Test]
    public void PrimitiveAndBottom_ReturnsPrimitive() {
        foreach (var primitive in _primitiveType)
            AssertLca(new ConstrainsState(), primitive, primitive);
    }

    [Test]
    public void PrimitiveAndConstraint_ReturnsLcaOfTypeAndDesc() {
        foreach (var primitiveTriple in _primitiveTypesLca)
            AssertLca(new ConstrainsState(primitiveTriple.Item1), primitiveTriple.Item2, primitiveTriple.Item3);
    }


    [Test]
    public void ConstraintAndConstraint_ReturnsLcaOfDescAndDesc() {
        foreach (var primitiveTriple in _primitiveTypesLca)
            AssertLca(new ConstrainsState(primitiveTriple.Item1), new ConstrainsState(primitiveTriple.Item2),
                primitiveTriple.Item3);
    }

    [Test]
    public void ConstraintAndBottom_ReturnsfDesc() {
        foreach (var primitive in _primitiveType)
            AssertLca(new ConstrainsState(primitive), new ConstrainsState(), primitive);
    }

    [Test]
    public void BottomAndBottom_returnBottom() {
        AssertLca(new ConstrainsState(), new ConstrainsState(), new ConstrainsState());
    }

    [Test]
    public void PrimitiveAndArrayOfPrimitive_ReturnsAny() {
        foreach (var primitive in _primitiveType)
            AssertLca(StateArray.Of(new StatePrimitive(PrimitiveTypeName.Any)), primitive, StatePrimitive.Any);
    }

    [Test]
    public void PrimitiveAndArrayOfBottoms_ReturnsAny() {
        foreach (var primitive in _primitiveType)
            AssertLca(StateArray.Of(new ConstrainsState()), primitive, StatePrimitive.Any);
    }

    [Test]
    public void PrimitiveAndArrayOfComposite_ReturnsAny() {
        foreach (var primitive in _primitiveType)
            AssertLca(StateArray.Of(StateArray.Of(new ConstrainsState())), primitive, StatePrimitive.Any);
    }

    [Test]
    public void ArrayOfSamePrimitiveTypes_ReturnsArrayOfLca() {
        foreach (var primitiveTriple in _primitiveTypesLca)
            AssertLca(StateArray.Of(primitiveTriple.Item1), StateArray.Of(primitiveTriple.Item2),
                StateArray.Of(primitiveTriple.Item3));
    }

    private void AssertLca(ITicNodeState a, ITicNodeState b, ITicNodeState expected) {
        if(a.Equals(StatePrimitive.Real) && b.Equals(StatePrimitive.Ip))
            Console.WriteLine("dd");

        var result1 = Lca.Calculate(a, b);
        var result2 = Lca.Calculate(b, a);

        var aRef = new StateRefTo(TicNode.CreateTypeVariableNode("a", a));
        var bRef = new StateRefTo(TicNode.CreateTypeVariableNode("b", b));

        var aRefRef = new StateRefTo(TicNode.CreateTypeVariableNode("aa", aRef));
        var bRefRef = new StateRefTo(TicNode.CreateTypeVariableNode("bb", bRef));

        var result3 = Lca.Calculate(aRef, bRef);
        var result4 = Lca.Calculate(bRefRef, aRefRef);

        Assert.AreEqual(expected, result1, $"1: {a} LCA {b} = {result1}, but was expected {expected}");
        Assert.AreEqual(expected, result2, $"1: {b} LCA {a} = {result2}, but was expected {expected}");
        Assert.AreEqual(expected, result3, $"1: {aRef} LCA {bRef} = {result3}, but was expected {expected}");
        Assert.AreEqual(expected, result4, $"1: {aRefRef} LCA {bRefRef} = {result4}, but was expected {expected}");
    }
}
