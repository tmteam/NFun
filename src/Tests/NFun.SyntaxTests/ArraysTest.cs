using System;
using System.Linq;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;
namespace NFun.SyntaxTests;

[TestFixture]
public class ArraysTest {
    [TestCase("y = [1..4]", new[] { 1, 2, 3, 4 })]
    [TestCase("y = [4..1]", new[] { 4, 3, 2, 1 })]
    [TestCase("y:uint[] = [1..4]", new uint[] { 1, 2, 3, 4 })]
    [TestCase("y:uint[] = [4..1]", new uint[] { 4, 3, 2, 1 })]
    [TestCase("y:int[] = [1..1]", new[] { 1 })]
    [TestCase("y:int[] = [1,2,3,4]", new[] { 1, 2, 3, 4 })]
    [TestCase("y = [1.2 .. 3]", new[] { 1.2, 2.2 })]
    [TestCase("y = [1.2 .. 3.2]", new[] { 1.2, 2.2, 3.2 })]
    [TestCase("y = [3.2 .. 1.2]", new[] { 3.2, 2.2, 1.2 })]
    [TestCase("y = [0x1]", new[] { 1 })]
    [TestCase("y = ['foo','bar']", new[] { "foo", "bar" })]
    [TestCase("y:int = [0..10][0]", 0)]
    [TestCase("y:int = [0..10][10]", 10)]
    [TestCase("y = [0..10.0][0]", 0.0)]
    [TestCase("y = [0..10][0]", 0)]
    [TestCase("y = [0..10][10]", 10)]
    [TestCase("y:int[] = [0..10][2:5]", new[] { 2, 3, 4, 5 })]
    [TestCase("y:int[] = [0..10][1:1]", new[] { 1 })]
    [TestCase("y:int[] = [0..10][1:2]", new[] { 1, 2 })]
    [TestCase("y:int[] = [0..10][:5]", new[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("y:int[] = [0..10][5:]", new[] { 5, 6, 7, 8, 9, 10 })]
    [TestCase("y = ['a','b'][0]", "a")]
    [TestCase("y = ['a','b'][1]", "b")]
    [TestCase("y:real[] = [1,2,3][:]", new[] { 1.0, 2.0, 3.0 })]
    [TestCase("y:int[] = [0..10][1:7:2]", new[] { 1, 3, 5, 7 })]
    [TestCase("y:int[] = [0..10][1:2:]", new[] { 1, 2 })]
    [TestCase("y:int[] = [0..10][1::2]", new[] { 1, 3, 5, 7, 9 })]
    [TestCase("y:int[] = [0..10][5::]", new[] { 5, 6, 7, 8, 9, 10 })]
    [TestCase("y:int[] = [0..10][:2:]", new[] { 0, 1, 2 })]
    [TestCase("y:int[] = [0..10][::4]", new[] { 0, 4, 8 })]
    [TestCase("y:int[] = [0..10][:4:3]", new[] { 0, 3 })]
    [TestCase("y = [1.0,1.2,2.4]", new[] { 1.0, 1.2, 2.4 })]
    [TestCase("y = [1.0]", new[] { 1.0 })]
    [TestCase("y = 1 in [1,2,3]", true)]
    [TestCase("y = 0 in [1,2,3]", false)]
    [TestCase("y = not 0 in [1,2,3]", true)]
    [TestCase("y = not 1 in [1,2,3]", false)]
    [TestCase("y = []", new object[0])]
    [TestCase("y:int[] = [1,2,3]", new[] { 1, 2, 3 })]
    [TestCase("y = ['a','b','c']", new[] { "a", "b", "c" })]
    [TestCase("y = [1.0]==[]", false)]
    [TestCase("y = [1.0]==[2.0]", false)]
    [TestCase("y = [1.0,2.0,3.0]==[1,2,3]", true)]
    [TestCase("y = [1.0]!=[2.0]", true)]
    [TestCase("y = []==[]", true)]
    [TestCase("y = []!=[]", false)]
    [TestCase("y = [1.0]==[1.0]", true)]
    [TestCase("y = [1.0]!=[1.0]", false)]
    [TestCase("y = [1.0,2.0]==[1.0,2.0]", true)]
    public void ConstantArrayOperatorsTest(string expr, object expected) => expr.AssertReturns("y", expected);

    [TestCase("y:int[] = [1..7 step 2]", new[] { 1, 3, 5, 7 })]
    [TestCase("y:int[] = [7..1 step 2]", new[] { 7, 5, 3, 1 })]
    [TestCase("y:int[] = [1..8 step 2]", new[] { 1, 3, 5, 7 })]
    [TestCase("y = [1.0..3.0 step 0.5]", new[] { 1.0, 1.5, 2.0, 2.5, 3.0 })]
    [TestCase("y = [3.0..1.0 step 0.5]", new[] { 3.0, 2.5, 2.0, 1.5, 1.0 })]
    [TestCase("y = [1..3 step 0.5]", new[] { 1.0, 1.5, 2.0, 2.5, 3.0 })]
    public void ConstantStepArrayInitTest(string expr, object expected) => expr.AssertReturns("y", expected);

    [TestCase("a = 2.0 \r b=3.0 \r  y = [1.0,a,b] ", new[] { 1.0, 2.0, 3.0 })]
    [TestCase("a = 2.0 \r b=3.0 \r y = [a,b] ", new[] { 2.0, 3.0 })]
    [TestCase("a = 2.0 \r b=3.0 \r y = [a+1,b+2] ", new[] { 3.0, 5.0 })]
    [TestCase("a = 2.0 \r b=3.0 \r y = [a*0,b*0] ", new[] { 0.0, 0.0 })]
    [TestCase("a = true  \ry = if (a) [1.0] else [2.0, 3.0] ", new[] { 1.0 })]
    [TestCase("a = false  \r y = if (a) [1.0] else [2.0, 3.0]", new[] { 2.0, 3.0 })]
    public void ConstantCalculableArrayTest(string expr, object expected) => expr.AssertResultHas("y", expected);

    [TestCase("[1,'2',3.0,4,5.2, true, false, 7.2]", new object[] { 1, "2", 3.0, 4, 5.2, true, false, 7.2 })]
    [TestCase("[1,'23',4.0,0x5, true]", new object[] { 1, "23", 4.0, 5, true })]
    [TestCase("if (true) [1.0] else [2.0, 3.0] ", new[] { 1.0 })]
    [TestCase("if (false) [1.0] else [2.0, 3.0]", new[] { 2.0, 3.0 })]
    [TestCase("y(x) = x \r[1]", new[] { 1 })]
    [TestCase("y(x) = x \r[1..3]", new[] { 1, 2, 3 })]
    [TestCase("y(x) = x \r[1.0..3]", new[] { 1.0, 2.0, 3.0 })]
    [TestCase("y(x) = x # some comment \r[1]", new[] { 1 })]
    [TestCase("y(x) = x # some comment \r[1..3]", new[] { 1, 2, 3 })]
    public void AnonymousConstantArrayTest(string expr, object expected)
        => expr.AssertAnonymousOut(expected);

    [Test]
    public void CompositeArrayOfAnyTest() =>
        "[1,'23',[],['a','bc'],[[]], 4.0,0x5, false]".AssertAnonymousOut(
            new object[] {
                1, "23", Array.Empty<object>(), new[] { "a", "bc" }, new[] { Array.Empty<object>() }, 4.0, 5, false
            });

    [Test]
    public void IntersectToDimArrayTest() {
        var expression = "y = [[1.0,2.0],[3.0,4.0],[5.0]]. intersect ([[3.0,4.0],[1.0],[5.0],[4.0]])";
        var expected = new[] { new[] { 3.0, 4.0 }, new[] { 5.0 } };
        expression.AssertReturns("y", expected);
    }


    [TestCase(3, "y= [1..x]", new[] { 1, 2, 3 })]
    [TestCase(3, "y= [1.0..x]", new[] { 1.0, 2, 3 })]
    [TestCase((Int64)42, "x:int64;   y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((Int32)42, "x:int32;   y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((Int16)42, "x:int16;   y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((UInt64)42, "x:uint64;  y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((UInt32)42, "x:uint32;  y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((UInt16)42, "x:uint16;  y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((byte)42, "x:byte;    y:real[]= [1,2,x]", new[] { 1.0, 2.0, 42.0 })]
    [TestCase((Int64)42, "x:int64;   y:int64[]= [1,2,x]", new long[] { 1, 2, 42 })]
    [TestCase((Int32)42, "x:int32;   y:int64[]= [1,2,x]", new long[] { 1, 2, 42 })]
    [TestCase((Int16)42, "x:int16;   y:int64[]= [1,2,x]", new long[] { 1, 2, 42 })]
    [TestCase((UInt32)42, "x:uint32;  y:int64[]= [1,2,x]", new long[] { 1, 2, 42 })]
    [TestCase((UInt16)42, "x:uint16;  y:int64[]= [1,2,x]", new long[] { 1, 2, 42 })]
    [TestCase((byte)42, "x:byte;    y:int64[]= [1,2,x]", new long[] { 1, 2, 42 })]
    [TestCase((Int32)42, "x:int32;   y:int32[]= [1,2,x]", new int[] { 1, 2, 42 })]
    [TestCase((Int16)42, "x:int16;   y:int32[]= [1,2,x]", new int[] { 1, 2, 42 })]
    [TestCase((UInt16)42, "x:uint16;  y:int32[]= [1,2,x]", new int[] { 1, 2, 42 })]
    [TestCase((byte)42, "x:byte;    y:int32[]= [1,2,x]", new int[] { 1, 2, 42 })]
    [TestCase((Int16)42, "x:int16;    y:int16[]= [1,2,x]", new Int16[] { 1, 2, 42 })]
    [TestCase((byte)42, "x:byte;    y:int16[]= [1,2,x]", new Int16[] { 1, 2, 42 })]
    [TestCase((UInt64)42, "x:uint64;  y:uint64[]= [1,2,x]", new UInt64[] { 1, 2, 42 })]
    [TestCase((UInt32)42, "x:uint32;  y:uint64[]= [1,2,x]", new UInt64[] { 1, 2, 42 })]
    [TestCase((UInt16)42, "x:uint16;  y:uint64[]= [1,2,x]", new UInt64[] { 1, 2, 42 })]
    [TestCase((byte)42, "x:byte;    y:uint64[]= [1,2,x]", new UInt64[] { 1, 2, 42 })]
    [TestCase((UInt32)42, "x:uint32;  y:uint32[]= [1,2,x]", new UInt32[] { 1, 2, 42 })]
    [TestCase((UInt16)42, "x:uint16;  y:uint32[]= [1,2,x]", new UInt32[] { 1, 2, 42 })]
    [TestCase((byte)42, "x:byte;    y:uint32[]= [1,2,x]", new UInt32[] { 1, 2, 42 })]
    [TestCase((UInt16)42, "x:uint16;  y:uint16[]= [1,2,x]", new UInt16[] { 1, 2, 42 })]
    [TestCase((byte)42, "x:byte;    y:uint16[]= [1,2,x]", new UInt16[] { 1, 2, 42 })]
    [TestCase(3, "y= [x..7]", new[] { 3, 4, 5, 6, 7 })]
    [TestCase(3, "y:int[]= [x,2,3]", new[] { 3, 2, 3 })]
    [TestCase(3, "y= [1..5][x]", 4)]
    [TestCase(true, "y= (if(x) [1,2] else [])[0]", 1)]
    [TestCase(2, "x:int; y= [1..6 step x]", new[] { 1, 3, 5 })]
    [TestCase(0.5, "y= [1.0..3.0 step x]", new[] { 1.0, 1.5, 2.0, 2.5, 3.0 })]
    public void SingleInputEquation_CheckOutputValues(object val, string expr, object expected) =>
        expr.Calc("x", val).AssertResultHas("y", expected);

    [Test]
    public void ConstantTwinAnyArray_NoTypeSpecification() {
        var expr = "out = [0,[1]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<object[]>(result);
        AssertArraysDeepEquiualent(new object[] { 0.0, new[] { 1.0 } }, result);
    }

    [TestCase("y:real[] = [0x1]", new[] { 1.0 })]
    [TestCase("y:real = [0x1].avg()", 1.0)]
    [TestCase("y = [0x1,0x3].avg()", 2.0)]
    [TestCase("y = [1,3].avg()", 2.0)]
    public void ArrayWithElementConvertion(string expr, object expected) => expr.Calc().AssertResultHas("y", expected);

    [Test]
    public void ConstantTwinAnyArray_WithTypeSpecification() {
        var expr = "out:any[] = [0,[1]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<object[]>(result);
        AssertArraysDeepEquiualent(new object[] { 0.0, new[] { 1.0 } }, result);
    }

    [TestCase("out:any[] = [0,[1]]")]
    [TestCase("out:any[] = [[1],0]")]
    [TestCase("out:any[] = [[true],0]")]
    [TestCase("out:any[] = [[1],true]")]
    [TestCase("out:any[] = [true,[1]]")]
    [TestCase("out:any[] = [1,'vasa']")]
    [TestCase("out:any[] = ['vasa',1.5]")]
    [TestCase("out = [0,[1]]")]
    [TestCase("out = [[1],0]")]
    [TestCase("out = [[true],0]")]
    [TestCase("out = [[1],true]")]
    [TestCase("out = [true,[1]]")]
    [TestCase("out = [1,'vasa']")]
    [TestCase("out = ['vasa',1.5]")]
    public void ConstantTwinAnyArrayWithUpcast(string expr) {
        TraceLog.IsEnabled = true;
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<object[]>(result);
    }

    [TestCase("out = [[0x1],[1.0]]")]
    //[TestCase("out = [[1.0],[0x1]]")]
    [TestCase("out = [[0x1],[1.0],[0x1]]")]
    //[TestCase("out = [[1.0],[0x1],[1.0]]")]
    public void ConstantTwinRealArrayWithUpcast_returnsArrayOfReal(string expr) {
        using var _ = TraceLog.Scope;
        var runtime = Funny.Hardcore.Build(expr);
        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Real)), runtime["out"].Type);
    }

    [Test(Description = "out:real[][] = [[0x1],[1.0]]")]
    public void ConstantTwinRealArrayWithUpcast_typeIsSpecified() {
        TraceLog.IsEnabled = true;
        var expr = "out:real[][] = [[0x1],[1.0]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<double[][]>(result);
    }

    [Test]
    public void ConstantTwinAnyArrayWithUpcast2() {
        using var _ = TraceLog.Scope;
        var expr = "out = [[0x1],[1.0],[true]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<object[][]>(result);
    }

    [Test]
    public void ConstantTwinAnyArrayWithUpcast3() {
        using var _ = TraceLog.Scope;
        var expr = "out = [[],[true]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<bool[][]>(result);
    }

    [Test]
    public void ConstantTwinAnyArrayWithUpcast4() {
        using var _ = TraceLog.Scope;
        var expr = "out:bool[][] = [[],[true]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<bool[][]>(result);
    }

    [Test]
    public void ConstantTwinAnyArrayWithUpcast5() {
        using var _ = TraceLog.Scope;
        var expr = "out:any[][] = [[],[true]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<object[][]>(result);
    }

    [Test]
    public void ConstantTwinAnyArrayWithUpcast6() {
        using var _ = TraceLog.Scope;
        var expr = "out = [[],[]]";
        var result = expr.Calc().Get("out");
        Assert.IsInstanceOf<object[][]>(result);
    }

    [Test]
    public void ConstantTrippleAnyArrayWithUpcast() {
        var expr = "out:any = [false,0,1,'vasa',[1,2,[10000,2]]]";
        var result = expr.Calc().Get("out");

        var expected = new object[] { false, 0.0, 1.0, "vasa", new Object[] { 1.0, 2.0, new[] { 10000, 2.0 } } };
        AssertArraysDeepEquiualent(expected, result);
    }

    private void AssertArraysDeepEquiualent<T>(T[] expected, object actual) {
        Assert.IsInstanceOf<T[]>(actual);
        var resultArray = actual as T[];
        Assert.AreEqual(expected.Length, resultArray.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            var o = resultArray[i];
            var e = expected[i];
            if (e is T[] ee)
                AssertArraysDeepEquiualent(ee, o);
            else
                Assert.AreEqual(e, o);
        }
    }

    [TestCase("byte")]
    [TestCase("uint16")]
    [TestCase("uint32")]
    [TestCase("uint64")]
    [TestCase("int16")]
    [TestCase("int32")]
    [TestCase("int64")]
    [TestCase("real")]
    public void SingleInputEquation_twinArrayUpCast(string downType) {
        var expr = $"x:{downType};    y:real[][]= [[1],[2,3,x],[x]]";

        var res = expr.Calc("x", 42.0).Get("y");

        var expected = new[] { new[] { 1.0 }, new[] { 2.0, 3.0, 42.0 }, new[] { 42.0 } };
        AssertArraysDeepEquiualent(expected, res);
    }

    [Test]
    public void ExceptToDimArrayTest() =>
        "y = [[1.0,2.0],[3.0,4.0]]. except([[3.0,4.0],[1.0],[4.0]])"
            .AssertReturns("y", new[] { new[] { 1.0, 2.0 } });

    [Test]
    public void TwoDimConstatantTest() {
        var expected = new Int32[3][];
        expected[0] = new[] { 1, 2 };
        expected[1] = new[] { 3, 4 };
        expected[2] = new[] { 5 };

        var expectedType = typeof(double[][]);
        var expression = " y= [[1.0,2.0],[3.0,4.0],[5.0]]";

        var res = expression.Calc().Get("y");
        Assert.AreEqual(expectedType, res.GetType());
        AssertMultiDimentionalEquals(res as double[][], expected);
    }

    [Test]
    public void TwoDimConcatConstatantTest() {
        var expected = new int[3][];
        expected[0] = new[] { 1, 2 };
        expected[1] = new[] { 3, 4 };
        expected[2] = new[] { 5 };

        var expression = " y= [[1,2],[3,4]].concat([[5.0]])";

        var res = expression.Calc().Get("y");
        Assert.IsInstanceOf<double[][]>(res);
        AssertMultiDimentionalEquals(res as double[][], expected);
    }

    [Test]
    public void SingleMultiDimVariable_OutputEqualsInput() {
        var x = new int[3][];
        x[0] = new[] { 1, 2 };
        x[1] = new[] { 3, 4 };
        x[2] = new[] { 5 };

        var expectedOutput = x;
        var expression = "x:int[][]\r y= x";

        var res = expression.Calc("x", x).Get("y");
        Assert.IsInstanceOf<int[][]>(res);
        AssertMultiDimentionalEquals(res as int[][], expectedOutput);
    }

    [Test]
    public void SingleMultiDimVariable_OutputEqualsTwoInputs() {
        var x = new int[3][];
        x[0] = new[] { 1, 2 };
        x[1] = new[] { 3, 4 };
        x[2] = new[] { 5 };

        var expectedType = FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Int32));
        var expectedOutput = new int[6][];
        expectedOutput[0] = new[] { 1, 2 };
        expectedOutput[1] = new[] { 3, 4 };
        expectedOutput[2] = new[] { 5 };
        expectedOutput[3] = new[] { 1, 2 };
        expectedOutput[4] = new[] { 3, 4 };
        expectedOutput[5] = new[] { 5 };

        var expression = "x:int[][]\r y= x.concat(x)";

        var res = expression.Calc("x", x).Get("y");
        Assert.IsInstanceOf<int[][]>(res);
        AssertMultiDimentionalEquals(res as int[][], expectedOutput);
    }

    [Test]
    public void ArraysIntegrationTest() {
        var expr = @"
x: int[]
filt: int
concat    = ([1,2,3,4].concat(x))
size      = concat.count()
possum   = x.filter(rule it>0).fold(rule it1+it2)
filtrat   = x.filter(rule it> filt) # filt - input variable
";
        expr.Calc(("x", new[] { 5, 6, 7, 8 }), ("filt", 2));
    }

    private static void AssertMultiDimentionalEquals<T>(T[][] result, int[][] expectedOutput) {
        for (int i = 0; i < expectedOutput.Length; i++)
        for (int j = 0; j < expectedOutput[i].Length; j++)
            Assert.AreEqual(result[i][j], expectedOutput[i][j]);
    }

    [TestCase("y = [")]
    [TestCase("y = [,]")]
    [TestCase("y = [,1.0]")]
    [TestCase("y = [,,1.0]")]
    [TestCase("y = [1.0+")]
    [TestCase("y = [1.0+]")]
    [TestCase("y = [1.0]+]")]
    [TestCase("y = [1.0]+[")]
    [TestCase("y = [1.0]+")]
    [TestCase("y = [1.0]++[2.0]")]
    [TestCase("y = +[2.0]")]
    [TestCase("y = [2.0 3.0]")]
    [TestCase("y = [2.0,,3.0]")]
    [TestCase("y = ['1'..4]")]
    [TestCase("y = ['1'..'4']")]
    [TestCase("y = [1..7..]")]
    [TestCase("y = [1....2]")]
    [TestCase("y = [1..3..2]")]
    [TestCase("y = [1..3..2 step 1]")]
    [TestCase("y = [1 step 2]")]
    [TestCase("y = [..2..2]")]
    [TestCase("y = [1..4")]
    [TestCase("y = [1..")]
    [TestCase("y = x\r[2]")]
    [TestCase("y = [1..2..-2]")]
    [TestCase("y = [1..2..0]")]
    [TestCase("y = [4..1..-2]")]
    [TestCase("y = [4..1..0]")]
    [TestCase("y = [4..1..-2.0]")]
    [TestCase("y = [1..4..-2.0]")]
    [TestCase("y = [1..4..0]")]
    public void ObviouslyFailsOnParse(string expr)
        => expr.AssertObviousFailsOnParse();


    [TestCase("y = [0..10][11]")]
    [TestCase("y = [0..1 step 0]")]
    [TestCase("y = ['a', 'b'][2]")]
    [TestCase("y = [1,2,3,4,5][::0]")]
    public void ObviouslyFailsOnRuntime(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());
    // Bug: `if(false) [] else [1,2,3]` previously resolved to UInt8[] instead of Int32[]
    // when empty array was the first branch in if-else. Now preserves preferred type.
    [TestCase("y = if(false) [] else [1,2,3]")]
    [TestCase("y = if(true) [1,2,3] else []")]
    public void IfElse_EmptyArray_PreservesPreferredType(string expr) {
        var result = expr.Calc();
        Assert.AreEqual(new[] { 1, 2, 3 }, result.Get("y"));
    }

    [Test]
    public void IfElse_EmptyArrayFirst_TypeIsInt32Array() {
        var runtime = "y = if(false) [] else [1,2,3]".Build();
        var yVar = runtime.Variables.Single(v => v.Name == "y");
        Assert.AreEqual(BaseFunnyType.ArrayOf, yVar.Type.BaseType);
        Assert.AreEqual(BaseFunnyType.Int32, yVar.Type.ArrayTypeSpecification.FunnyType.BaseType);
    }

    // ═══════════════════════════════════════════════════════════════
    // Slice with reversed indices should throw consistent error
    // ═══════════════════════════════════════════════════════════════

    [TestCase("[1,2,3][1:0]")]
    [TestCase("[1,2,3][2:0]")]
    [TestCase("[1,2,3][2:1]")]
    [TestCase("[1,2,3][3:0]")]
    [TestCase("[1,2,3,4,5][4:1]")]
    public void SliceReversedIndices_ThrowsConsistentError(string expr) {
        var runtime = $"y = {expr}".Build();
        var ex = Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
        Assert.That(ex.Message, Does.Contain("Start cannot be more than end"));
    }

    [TestCase("[1,2,3][1:0:1]")]
    [TestCase("[1,2,3][2:0:1]")]
    [TestCase("[1,2,3][2:1:1]")]
    public void SliceWithStepReversedIndices_ThrowsConsistentError(string expr) {
        var runtime = $"y = {expr}".Build();
        var ex = Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
        Assert.That(ex.Message, Does.Contain("Start cannot be more than end"));
    }

    [TestCase("[1,2,3][0:0]", new[] { 1 })]
    [TestCase("[1,2,3][0:2]", new[] { 1, 2, 3 })]
    [TestCase("[1,2,3][1:2]", new[] { 2, 3 })]
    public void SliceValidIndices_StillWorks(string expr, int[] expected) {
        $"y = {expr}".AssertReturns("y", expected);
    }

    // ═══════════════════════════════════════════════════════════════
    // Text take/skip clamping beyond length
    // ═══════════════════════════════════════════════════════════════

    [TestCase("'hello'.take(6)", "hello")]
    [TestCase("'hello'.take(100)", "hello")]
    [TestCase("'hello'.take(5)", "hello")]
    [TestCase("'hello'.take(3)", "hel")]
    [TestCase("'hello'.take(0)", "")]
    [TestCase("'hello'.take(1)", "h")]
    public void TextTake_ClampsBeyondLength(string expr, string expected) {
        expr.AssertReturns(expected);
    }

    [TestCase("'hello'.skip(0)", "hello")]
    [TestCase("'hello'.skip(3)", "lo")]
    [TestCase("'hello'.skip(5)", "")]
    [TestCase("'hello'.skip(100)", "")]
    public void TextSkip_StillWorks(string expr, string expected) {
        expr.AssertReturns(expected);
    }

    [TestCase("[1,2,3].take(100)", new[] { 1, 2, 3 })]
    [TestCase("[1,2,3].take(3)", new[] { 1, 2, 3 })]
    [TestCase("[1,2,3].take(1)", new[] { 1 })]
    public void ArrayTake_StillClamps(string expr, int[] expected) {
        expr.AssertReturns(expected);
    }

    // ═══════════════════════════════════════════════════════════════
    // Slice with variable start
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SliceWithVariableStart_ParsedCorrectly() {
        // Slice is inclusive on both ends: arr[2:4] = [3,4,5] (indices 2,3,4)
        "arr = [1,2,3,4,5]\r i = 2\r y = arr[i:4]"
            .AssertResultHas("y", new[] { 3, 4, 5 });
    }

    // ═══════════════════════════════════════════════════════════════
    // Range without annotation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RangeWithoutAnnotation_Works() {
        "y:int[] = [1..5]".AssertReturns("y", new[]{1,2,3,4,5}); // works WITH annotation
        Assert.DoesNotThrow(() => "y = [1..5]".Calc()); // works WITHOUT
    }

    // ═══════════════════════════════════════════════════════════════
    // Text sort
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TextSort_DoesNotCrash() {
        Assert.DoesNotThrow(() => "y = 'cba'.sort()".Calc());
    }

    // Comparable typeclass enforcement: sort<T:Comparable> rejects composite
    // element types (struct/array/fun) at compile time. The check lives in
    // ConstraintsState.SimplifyOrNull (case ICompositeState) and now also
    // gates CS-merge paths — without that, the IsComparable flag silently
    // OR-merged with a composite Descendant and surfaced as a runtime cast
    // to IComparable failure during sort comparison. Lang-mode only: ee-mode
    // .Calc() goes through legacy StateArray which never had this hole.
    [Test]
    public void SortOnListOfStructs_RejectedAtCompileTime() {
        Assert.Throws<FunnyParseException>(() =>
            NFun.Funny.Hardcore.BuildLang("a = [{key=1,value=10}]\nout = a.sort()"));
    }

    [Test]
    public void SortOnListOfArrays_RejectedAtCompileTime() {
        Assert.Throws<FunnyParseException>(() =>
            NFun.Funny.Hardcore.BuildLang("a = [[1,2],[3,4]]\nout = a.sort()"));
    }

    // take/skip with negative count emit a clean domain error rather than
    // leaking the underlying .NET overflow / Array.Copy sourceIndex message.
    // Mirrors the existing `repeat(_, -1)` contract.
    [TestCase("out = take([1,2], -1)", "Take count cannot be negative")]
    [TestCase("out = skip([1,2,3], -1)", "Skip count cannot be negative")]
    public void TakeOrSkip_NegativeCount_DomainError(string expr, string expectedMessageFragment) {
        var ex = Assert.Throws<FunnyRuntimeException>(() => expr.Calc());
        StringAssert.Contains(expectedMessageFragment, ex.Message);
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug1 — Range expression to MaxValue crashes at runtime with
    //   "Index was outside the bounds of the array". Reproducible for
    //   every integer type at its max value, and the symmetric descending
    //   case to MinValue. Range generator likely off-by-one with overflow.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug1_RangeToMaxValue_Int32() {
        "out = [2147483646..2147483647]".Calc()
            .AssertResultHas("out", new[] { 2147483646, 2147483647 });
    }

    [Test]
    public void MR5Bug1_RangeDescToMinValue_Int32() {
        "out = [-2147483647..-2147483648]".Calc()
            .AssertResultHas("out", new[] { -2147483647, -2147483648 });
    }

    [Test]
    public void MR5Bug1_RangeToMaxValue_Int64() {
        "out = [9223372036854775806..9223372036854775807]".Calc()
            .AssertResultHas("out", new[] { 9223372036854775806L, 9223372036854775807L });
    }

    [Test]
    public void MR5Bug1_RangeToMaxValue_Byte() {
        "out:byte[] = [254..255]".Calc()
            .AssertResultHas("out", new byte[] { 254, 255 });
    }

    [Test]
    public void MR5Bug1_RangeToMaxValue_Int16() {
        "out:int16[] = [32766..32767]".Calc()
            .AssertResultHas("out", new short[] { 32766, 32767 });
    }
}
