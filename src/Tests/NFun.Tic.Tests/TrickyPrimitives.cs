using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests {

class TrickyPrimitives {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void DeInitialize() => TraceLog.IsEnabled = false;


    [Test(Description = "y = isNan(1) ")]
    public void SimpleConcreteFunctionWithConstant() {
        //node |    1     0
        //expr |y = isNan(1) 
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetCall(new[] { StatePrimitive.Real, StatePrimitive.Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);
        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Bool, "y");
    }

    [Test(Description = "y = isNan(x) ")]
    public void SimpleConcreteFunctionWithVariable() {
        //node |    1     0
        //expr |y = isNan(x) 
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetCall(new[] { StatePrimitive.Real, StatePrimitive.Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);
        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Real, "x");
        result.AssertNamed(StatePrimitive.Bool, "y");
    }

    [Test(Description = "x:int; y = isNan(x) ")]
    public void SimpleConcreteFunctionWithVariableOfConcreteType() {
        //node |           1     0
        //expr |x:int; y = isNan(x) 
        var graph = new GraphBuilder();
        graph.SetVarType("x", StatePrimitive.I32);
        graph.SetVar("x", 0);
        graph.SetCall(new[] { StatePrimitive.Real, StatePrimitive.Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);
        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "x");
        result.AssertNamed(StatePrimitive.Bool, "y");
    }

    [Test(Description = "y = isNan(1i)")]
    public void SimpleConcreteFunctionWithConstLimit() {
        //node |    1     0       
        //expr |y = isNan(1i);
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetCall(new[] { StatePrimitive.Real, StatePrimitive.Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
    }

    [Test(Description = "y = isNan(x); z = ~x")]
    public void SimpleConcreteFunctionWithVariableThatLimitisAfterwards() {
        //node |    1     0       3        2
        //expr |y = isNan(x); z = isMaxInt(x) 
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetCall(new[] { StatePrimitive.Real, StatePrimitive.Bool }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        graph.SetVar("x", 2);
        graph.SetCall(new[] { StatePrimitive.I32, StatePrimitive.Bool }, new[] { 2, 3 });
        graph.SetDef("z", 3);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "x");
        result.AssertNamed(StatePrimitive.Bool, "y", "z");
    }

    [Test(Description = "y = x ")]
    public void OutputEqualsInput_simpleGeneric() {
        //node |1   0
        //expr |y = x 
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();

        var generic = result.AssertAndGetSingleGeneric(null, null, false);
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test(Description = "y = x; | y2 = x2")]
    public void TwoSimpleGenerics() {
        //node |     0  |       1
        //expr s|y = x; | y2 = x2

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        graph.SetVar("x2", 1);
        graph.SetDef("y2", 1);

        var result = graph.Solve();

        Assert.AreEqual(2, result.GenericsCount);

        var generics = result.GenericNodes.ToArray();

        generics[0].AssertGenericType(null, null, false);
        generics[1].AssertGenericType(null, null, false);

        var yRes = result.GetVariableNode("y").GetNonReference();
        var y2Res = result.GetVariableNode("y2").GetNonReference();
        CollectionAssert.AreEquivalent(generics, new[] { y2Res, yRes });

        var xRes = result.GetVariableNode("x").GetNonReference();
        var x2Res = result.GetVariableNode("x2").GetNonReference();
        CollectionAssert.AreEquivalent(generics, new[] { x2Res, xRes });
    }

    [Test]
    [Ignore("UB r type")]
    public void LimitCall_ComplexEquations_TypesSolved() {
        //     0 2 1      3 5  4      6 8 7
        // r = x + y; i = y << 2; x = 3 / 2
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("y", 1);
        graph.SetArith(0, 1, 2);
        graph.SetDef("r", 2);

        graph.SetVar("y", 3);
        graph.SetIntConst(4, StatePrimitive.U8);
        graph.SetBitShift(3, 4, 5);
        graph.SetDef("i", 5);

        graph.SetIntConst(6, StatePrimitive.U8);
        graph.SetIntConst(7, StatePrimitive.U8);
        graph.SetCall(StatePrimitive.Real, 6, 7, 8);
        graph.SetDef("x", 8);

        var result = graph.Solve();
        result.AssertNamed(StatePrimitive.Real, "x", "r");
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U24, StatePrimitive.I96);

        result.AssertAreGenerics(generic, "y", "i");
    }

    [Test]
    public void SummfoldcByBitShift_AllTypesAreInt() {
        //  0 2 1  4 3
        //( x + y )<<3

        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("y", 1);
        graph.SetArith(0, 1, 2);

        graph.SetIntConst(3, StatePrimitive.U8);

        graph.SetBitShift(2, 3, 4);
        graph.SetDef("out", 4);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U24, StatePrimitive.I96);

        result.AssertAreGenerics(generic, "x", "y", "out");
    }

    [Test]
    public void ConcreteTypeOfArithmetical_ConstantsAreConcrete() {
        //0 4 1 3 2  
        //x<<(1 + 2)

        var graph = new GraphBuilder();

        graph.SetVar("x", 0);

        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetIntConst(2, StatePrimitive.U8);
        graph.SetArith(1, 2, 3);
        graph.SetBitShift(0, 3, 4);
        graph.SetDef("out", 4);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U24, StatePrimitive.I96);

        result.AssertAreGenerics(generic, "x", "out");
    }


    [Test]
    public void TypeSpecified_PutHighterType_EquationSOlved() {
        //         1    0  
        //a:real;  a = 1:int32
        var graph = new GraphBuilder();
        graph.SetVarType("a", StatePrimitive.Real);
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetDef("a", 0);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Real, "a");
    }

    [Test]
    public void TypeLimitSet_ThanChangedToLower_LowerLimitAccepted() {
        //    0            1
        //a = 1:int;  a = 1.0:int64
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetDef("a", 0);
        graph.SetConst(1, StatePrimitive.I64);
        graph.SetDef("a", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I64, "a");
    }

    [Test]
    public void TypeLimitSet_ThanChangedToHigher_LowerLimitAccepted() {
        //1   0          3   2
        //a = 1:int64;  a = 1.0:int32

        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I64);
        graph.SetDef("a", 0);
        graph.SetConst(1, StatePrimitive.I32);
        graph.SetDef("a", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I64, "a");
    }


    [Test]
    public void EqualtyOnGenerics() {
        //     0  2  1     
        //y = 1.0 == x 

        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.Real);
        graph.SetVar("x", 1);
        var generic = graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Bool, "y");
        result.AssertNamed(StatePrimitive.Real, "x");
        Assert.AreEqual(StatePrimitive.Real, generic.GetNonReference());
    }

    [Test]
    public void EqualtyOnGenericsReversed() {
        //    0  2  1     
        //y = x == 1.0 

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, StatePrimitive.Real);
        var generic = graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Bool, "y");
        result.AssertNamed(StatePrimitive.Real, "x");
        Assert.AreEqual(StatePrimitive.Real, generic.GetNonReference());
    }

    [Test(Description = "y:int = 1.0")]
    public void Downcast_Throws() {
        //node |         0
        //expr |y:int = 1.0 
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.Real);
        graph.SetVarType("y", StatePrimitive.I32);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetDef("y", 0);
                graph.Solve();
            });
    }

    [Test]
    public void NegativeIntArgumentInUintFunction_throws() {
        //myFunction(a:u16):u16 = ...

        //node |       1        0
        //expr |y = myFunction(-1) 
        var graph = new GraphBuilder();
        graph.SetGenericConst(0, StatePrimitive.I16, StatePrimitive.Real, StatePrimitive.Real);

        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetCall(new ITicNodeState[] { StatePrimitive.U16, StatePrimitive.U16 }, new[] { 0, 1 });
                graph.SetDef("y", 1);
                graph.Solve();
            });
    }

    [TestCase(PrimitiveTypeName.Real)]
    [TestCase(PrimitiveTypeName.I32)]
    [TestCase(PrimitiveTypeName.I64)]
    public void PreferredIntegerInRealFunction(PrimitiveTypeName preferredType) {
        TraceLog.IsEnabled = true;
        //" 'a' has to prefer Real type, as it used as real argument in cos function"
        //node |    0         2  1 
        //expr |a = 0;   b = cos(a) 

        var graph = new GraphBuilder();
        graph.SetGenericConst(0, StatePrimitive.U8, StatePrimitive.Real, new StatePrimitive(preferredType));
        graph.SetDef("a", 0);
        graph.SetVar("a", 1);
        graph.SetCall(StatePrimitive.Real, 1, 2);
        graph.SetDef("b", 2);
        graph.Solve();

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Real, "b");
        result.AssertNamed(StatePrimitive.Real, "a");
    }
}

}