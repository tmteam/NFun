using System;
using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests; 

class BasicArithmetics {
    [Test(Description = "x = 3 / 2")]
    public void SimpleDivideComputation() {
        //x = 3 / 2
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetCall(StatePrimitive.Real, 0, 1, 2);
        graph.SetDef("x", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Real, "x");
    }

    [Test(Description = "y = 1 + 2 * x")]
    public void SolvingGenericWithSingleVar() {
        //node |    0 4 1 3 2
        //expr |y = 1 + 2 * x;

        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetVar("x", 2);
        graph.SetArith(1, 2, 3);
        graph.SetArith(0, 3, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test(Description = "x:int; y = 1 + x")]
    public void ConcreteVarType() {
        //node |           0 2 1 
        //expr |x:int; y = 1 + x;

        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetVarType("x", StatePrimitive.I32);
        graph.SetVar("x", 1);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "y");
    }

    [Test(Description = "x:int; y = x + 1")]
    public void ConcreteVarType2() {
        //node |           0 2 1 
        //expr |x:int; y = x + 1;

        var graph = new GraphBuilder();
        graph.SetVarType("x", StatePrimitive.I32);
        graph.SetVar("x", 0);
        graph.SetIntConst(1, StatePrimitive.U8);

        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "y");
    }

    [TestCase]
    public void IncrementI64() {
        Console.WriteLine("y = x + 1i");

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, StatePrimitive.I64);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I64, "x", "y");
    }

    [TestCase]
    public void IncrementU64WithStrictInputType() {
        Console.WriteLine("x:uint64; y = x + 1");

        var graph = new GraphBuilder();
        graph.SetVarType("x", StatePrimitive.U64);
        graph.SetVar("x", 0);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.U64, "x", "y");
    }

    [TestCase]
    public void IncrementU32WithStrictOutputType() {
        Console.WriteLine("y:u32 = x + 1");

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetArith(0, 1, 2);
        graph.SetVarType("y", StatePrimitive.U32);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.U32, "x", "y");
    }

    [TestCase]
    public void GenericIncrement() {
        Console.WriteLine("y = x + 1");

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        var genericNode = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(genericNode, "x", "y");
    }


    [Test]
    public void StrictOnEquationArithmetics() {
        Console.WriteLine("x= 10i;   a = x*y + 10-x");

        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetDef("x", 0);

        graph.SetVar("x", 1);
        graph.SetVar("y", 2);
        graph.SetArith(1, 2, 3);
        graph.SetIntConst(4, StatePrimitive.U8);
        graph.SetArith(3, 4, 5);
        graph.SetVar("x", 6);
        graph.SetArith(5, 6, 7);
        graph.SetDef("a", 7);


        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "x", "y", "a");
    }

    [Test]
    public void GenericOneEquatopmArithmetics() {
        Console.WriteLine("x= 10;   a = x*y + 10-x");

        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetDef("x", 0);

        graph.SetVar("x", 1);
        graph.SetVar("y", 2);
        graph.SetArith(1, 2, 3);
        graph.SetIntConst(4, StatePrimitive.U8);
        graph.SetArith(3, 4, 5);
        graph.SetVar("x", 6);
        graph.SetArith(5, 6, 7);
        graph.SetDef("a", 7);

        var result = graph.Solve();

        var genericNode = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(genericNode, "x", "y", "a");
    }


    [Test]
    public void GenericTwoEquationsArithmetic() {
        Console.WriteLine("a = x*y + 10-x; b = r*x + 10-r");

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetVar("y", 1);
        graph.SetArith(0, 1, 2);
        graph.SetIntConst(3, StatePrimitive.U8);
        graph.SetArith(2, 3, 4);
        graph.SetVar("x", 5);
        graph.SetArith(4, 5, 6);
        graph.SetDef("a", 6);

        graph.SetVar("r", 7);
        graph.SetVar("x", 8);
        graph.SetArith(7, 8, 9);
        graph.SetIntConst(10, StatePrimitive.U8);
        graph.SetArith(9, 10, 11);
        graph.SetVar("r", 12);
        graph.SetArith(11, 12, 13);
        graph.SetDef("b", 13);

        var result = graph.Solve();

        var genericNode = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(genericNode, "x", "y", "a", "b");
    }

    [Test]
    public void InputRepeats_simpleGeneric() {
        //node |3   0 2 1 
        //expr |y = x + x 

        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetVar("x", 1);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();

        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test]
    public void UpcastArgTypeThatIsAfter_EquationSolved() {
        //     0 2 1       3 
        // y = a / b;  a = 1i

        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetCall(StatePrimitive.Real, 0, 1, 2);
        graph.SetDef("y", 2);
        graph.SetConst(3, StatePrimitive.I32);
        graph.SetDef("a", 3);

        var result = graph.Solve();

        Assert.AreEqual(0, result.GenericsStates.Count());

        //undefined behaviour. a can be either i32 either real
        //result.AssertNamed(StatePrimitive.I32, "a");

        result.AssertNamed(StatePrimitive.Real, "b");
        result.AssertNamed(StatePrimitive.Real, "y");
    }

    [Test]
    [Ignore("UB a type")]
    public void UpcastArgTypeThatIsBefore_EquationSolved() {
        //        0       1 3 2
        // // a = 1i; y = a / b;   

        var graph = new GraphBuilder();

        graph.SetConst(0, StatePrimitive.I32);
        graph.SetDef("a", 0);


        graph.SetVar("a", 1);
        graph.SetVar("b", 2);
        graph.SetCall(StatePrimitive.Real, 1, 2, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        //Assert.AreEqual(0,result.GenericsCount);
        result.AssertNamed(StatePrimitive.I32, "a");
        //Assert.AreEqual(ConcreteType.Real, result["b"));
        result.AssertNamed(StatePrimitive.Real, "y");
    }

    [Test]
    //[Ignore("Preferred Type")]
    public void UpcastArgType_ArithmOp_EquationSolved() {
        //        0        1 3 2       4
        // // a = 1.0; y = a + b;  b = 1i

        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.Real);
        graph.SetDef("a", 0);

        graph.SetVar("a", 1);
        graph.SetVar("b", 2);
        graph.SetArith(1, 2, 3);
        graph.SetDef("y", 3);


        graph.SetConst(4, StatePrimitive.I32);
        graph.SetDef("b", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Real, "a");
        result.AssertNamed(StatePrimitive.Real, "y");
        // Undefined beh. Is b i32 or real?
        //  result.AssertNamed(StatePrimitive.I32, "b");
    }

    [Test]
    public void TwoTypesAreLong_ItsSumIsLong() {
        //    0       1       2 4 3
        //a = 1l; b = 1l; x = a + b

        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I64);
        graph.SetDef("a", 0);

        graph.SetConst(1, StatePrimitive.I64);
        graph.SetDef("b", 1);

        graph.SetVar("a", 2);
        graph.SetDef("b", 3);
        graph.SetArith(2, 3, 4);
        graph.SetDef("x", 4);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I64, "x", "a", "b");
    }

    [Test]
    public void MultipleAncestors_EquationSolved() {
        //      0         1        2  4  3
        //y1  = y0;  y2 = y1; y3 = y2 * 2i

        var graph = new GraphBuilder();
        graph.SetVar("y0", 0);
        graph.SetDef("y1", 0);

        graph.SetVar("y1", 1);
        graph.SetDef("y2", 1);

        graph.SetVar("y2", 2);
        graph.SetConst(3, StatePrimitive.I32);
        graph.SetArith(2, 3, 4);
        graph.SetDef("y3", 4);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "y0", "y1", "y2", "y3");
    }

    [Test]
    public void ReverseMultipleAncestors_EquationSolved() {
        //      0          1        2 4 3
        //y1  = y0;  y2 = y1; y3 = y0 * 2i
        var graph = new GraphBuilder();
        graph.SetVar("y0", 0);
        graph.SetDef("y1", 0);

        graph.SetVar("y1", 1);
        graph.SetDef("y2", 1);

        graph.SetVar("y0", 2);
        graph.SetConst(3, StatePrimitive.I32);
        graph.SetArith(2, 3, 4);
        graph.SetDef("y3", 4);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "y0", "y1", "y2", "y3");
    }


    [Test]
    public void CircularDependenciesWithEquation_SingleGenericFound() {
        TraceLog.IsEnabled = true;
        //    021      354   
        //a = b*c; b = c*a; 

        var graph = new GraphBuilder();
        graph.SetVar("b", 0);
        graph.SetVar("c", 1);
        graph.SetArith(0, 1, 2);
        graph.SetDef("a", 2);

        graph.SetVar("c", 3);
        graph.SetVar("a", 4);
        graph.SetArith(3, 4, 5);
        graph.SetDef("b", 5);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "a", "b", "c");
    }

    [Test]
    public void CircularDependenciesWithEquation_ReversedInputOrder_SingleGenericFound() {
        TraceLog.IsEnabled = true;
        //    021      354   
        //a = b*c; b = c*a; 

        var graph = new GraphBuilder();

        graph.SetArith(0, 1, 2);
        graph.SetVar("b", 0);
        graph.SetVar("c", 1);
        graph.SetDef("a", 2);

        graph.SetArith(3, 4, 5);
        graph.SetVar("c", 3);
        graph.SetVar("a", 4);
        graph.SetDef("b", 5);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "a", "b", "c");
    }
}