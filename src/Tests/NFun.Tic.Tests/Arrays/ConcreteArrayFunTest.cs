using System;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Arrays;

using static StatePrimitive;

public class ConcreteArrayFunTest {
    [Test(Description = "y = x.NoNans()")]
    public void ConcreteCall() {
        //        1  0
        //y = NoNans(x)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetCall(new ITicNodeState[] { StateArray.Of(Real), Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Real), "x");
        result.AssertNamed(Bool, "y");
    }

    [Test(Description = "x:int[]; y = x.NoNans()")]
    public void ConcreteCall_WithUpCast() {
        //                 1  0
        //x:int[]; y = NoNans(x)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetCall(new ITicNodeState[] { StateArray.Of(Real), Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(Bool, "y");
    }


    [Test(Description = "y = [1i,-1i].NoNans()")]
    public void ConcreteCall_WithGenericArray() {
        //        3   2 0  1
        //y = NoNans( [ 1, -1])
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, I16);
        graph.SetStrictArrayInit(2, 0, 1);

        graph.SetCall(new ITicNodeState[] { StateArray.Of(Real), Bool }, new[] { 2, 3 });
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNode(StateArray.Of(Real), 2);
        result.AssertNode(Real, 0, 1);
        result.AssertNamed(Bool, "y");
    }

    [Test(Description = "reverse( 'hello')")]
    public void SetArrayConst() {
        //        1       0
        //y = reverse( 'hello')
        var graph = new GraphBuilder();
        graph.SetArrayConst(0, Char);
        var t = graph.InitializeVarNode();
        graph.SetCall(new[] { StateArray.Of(t), StateArray.Of(t) }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNode(StateArray.Of(Char), 0);
        result.AssertNamed(StateArray.Of(Char), "y");
    }

    [Test]
    public void CompareTwoDifferentArrays_Solved() {
        //    1 0    3   2
        //y = [1.0] == 'abc'
        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetStrictArrayInit(1, 0);
        graph.SetArrayConst(2, Char);
        graph.SetEquality(1, 2, 3);
        graph.SetDef("y", 3);

        var res = graph.Solve();
        res.AssertNoGenerics();
        res.AssertNamed(Bool, "y");
    }

    [Test]
    public void CompareTwoDifferentArrays_Solved2() {
        //    1 0    3    2
        //y = [1.0] == emptyArrayOfAny
        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetStrictArrayInit(1, 0);
        graph.SetArrayConst(2, Any);
        var generic = graph.SetEquality(1, 2, 3);
        graph.SetDef("y", 3);

        var res = graph.Solve();
        res.AssertNoGenerics();
        res.AssertNamed(Bool, "y");
        //Assert.AreEqual(Array.Of(Primitive.Any), generic.Element);
    }

    [Test]
    public void CompareConcreteAndGenericEmptyArray() {
        TraceLog.IsEnabled = true;
        //    1 0   3  2
        //y = [1.0] == []
        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetStrictArrayInit(1, 0);
        var arrayType = graph.SetStrictArrayInit(2);
        var eqGeneric = graph.SetEquality(1, 2, 3);
        graph.SetDef("y", 3);

        var res = graph.Solve();

        Console.WriteLine(eqGeneric.GetNonReference());

        res.AssertNoGenerics();
        res.AssertNamed(Bool, "y");
        Assert.AreEqual(Real, arrayType.GetNonReference());
    }

    [Test]
    public void CompareConcreteAndGenericEmptyArray2() {
        //         0      2  1
        //y = arrayOfReal == []
        var graph = new GraphBuilder();
        graph.SetArrayConst(0, Real);
        var arrayType = graph.SetStrictArrayInit(1);
        var eqGeneric = graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var res = graph.Solve();

        Console.WriteLine(eqGeneric.GetNonReference());

        res.AssertNoGenerics();
        res.AssertNamed(Bool, "y");
        Assert.AreEqual(Real, arrayType.GetNonReference());
    }

    [Test]
    public void Count() {
        //     1      0
        //y = count('abc')
        var graph = new GraphBuilder();
        graph.SetArrayConst(0, Char);

        graph.SetCall(new ITicNodeState[] { StateArray.Of(Any), I32 }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var res = graph.Solve();
        res.AssertNoGenerics();
        res.AssertNamed(I32, "y");
    }

    [Test]
    public void ImpossibleArgType_Throws() {
        //                 1  0
        //x:Any[]; y = NoNans(x)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(Any));
        graph.SetVar("x", 0);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetCall(
                    new ITicNodeState[] { StateArray.Of(Real), Bool },
                    new[] { 0, 1 });
                graph.SetDef("y", 1);
                graph.Solve();
                Assert.Fail();
            });
    }
}
