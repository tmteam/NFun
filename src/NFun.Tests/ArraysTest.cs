using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFun;
using NFun.Exceptions;
using NFun.Runtime.Arrays;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ArraysTest
    {
        [TestCase("y = [1..4]", new[]{1.0,2,3,4})]
        [TestCase("y = [4..1]", new[]{4.0,3,2,1})]
        [TestCase("y:uint[] = [1..4]", new uint[] { 1, 2, 3, 4 })]
        [TestCase("y:uint[] = [4..1]", new uint[] { 4, 3, 2, 1 })]
      
        [TestCase("y:int[] = [1..1]", new[]{1})]
        [TestCase("y:int[] = [1,2,3,4]", new[]{1,2,3,4})]
        [TestCase("y = [0x1]", new[]{1})]
        [TestCase("y = ['foo','bar']", new []{"foo","bar"})]
        [TestCase("y:int = [0..10][0]", 0)]
        [TestCase("y:int = [0..10][10]", 10)]
        [TestCase("y = [0..10][0]",   0.0)]
        [TestCase("y = [0..10][10]", 10.0)]
        [TestCase("y:int[] = [0..10][2:5]", new[]{2,3,4,5})]
        [TestCase("y:int[] = [0..10][1:1]", new[]{1})]
        [TestCase("y:int[] = [0..10][1:2]", new[]{1,2})]
        [TestCase("y:int[] = [0..10][:5]", new[]{0,1,2,3,4,5})]
        [TestCase("y:int[] = [0..10][5:]", new[]{5,6,7,8,9,10})]
        [TestCase("y = ['a','b'][0]", "a")]
        [TestCase("y = ['a','b'][1]", "b")]
        [TestCase("y:real[] = [1,2,3][:]", new[]{1.0,2.0,3.0})]
        [TestCase("y:int[] = [0..10][1:7:2]", new[]{1,3,5,7})]
        [TestCase("y:int[] = [0..10][1:2:]", new[]{1,2})]
        [TestCase("y:int[] = [0..10][1::2]", new[]{1,3,5,7,9})]
        [TestCase("y:int[] = [0..10][5::]", new[]{5,6,7,8,9,10})]
        [TestCase("y:int[] = [0..10][:2:]", new[]{0,1,2})]
        [TestCase("y:int[] = [0..10][::4]", new[]{0,4,8})]
        [TestCase("y:int[] = [0..10][:4:3]", new[]{0,3})]
        [TestCase("y = [1.0,1.2,2.4]", new[]{1.0,1.2, 2.4})]
        [TestCase("y = [1.0]", new[]{1.0})]
        [TestCase("y = 1 in [1,2,3]", true)]    
        [TestCase("y = 0 in [1,2,3]", false)]    
        [TestCase("y = not 0 in [1,2,3]", true)]    
        [TestCase("y = not 1 in [1,2,3]", false)]    
        
        [TestCase("y = []", new object[0])]

        [TestCase("y:int[] = [1,2,3]", new[]{1,2,3})]
        [TestCase("y = ['a','b','c']", new[]{"a","b","c"})]
        [TestCase("y = [1.0]==[]", false)]
        [TestCase("y = [1.0]==[2.0]", false)]
        [TestCase("y = [1.0,2.0,3.0]==[1,2,3]", true)]
        [TestCase("y = [1.0]!=[2.0]", true)]
        [TestCase("y = []==[]", true)]
        [TestCase("y = []!=[]", false)]
        [TestCase("y = [1.0]==[1.0]", true)]
        [TestCase("y = [1.0]!=[1.0]", false)]
        [TestCase("y = [1.0,2.0]==[1.0,2.0]", true)]
        public void ConstantArrayOperatorsTest(string expr, object expected)
        {
            FunBuilder.Build(expr).Calculate().AssertReturns(VarVal.New("y", expected));
        }
        [Ignore("Step array initialization is disabled")]
        [TestCase("y:int[] = [1..7..2]", new[]{1,3,5,7})]
        [TestCase("y:int[] = [7..1..2]", new[]{7,5,3,1})]
        [TestCase("y:int[] = [1..8..2]", new[]{1,3,5,7})]
        [TestCase("y = [1.0..3.0..0.5]", new[]{1.0,1.5,2.0,2.5,3.0})]
        [TestCase("y = [3.0..1.0..0.5]", new[]{3.0,2.5,2.0,1.5, 1.0})]
        [TestCase("y = [1..3..0.5]", new[]{1.0,1.5,2.0,2.5,3.0})]
        public void ConstantStepArrayInitTest(string expr, object expected)
        {
            FunBuilder.Build(expr).Calculate().AssertReturns(VarVal.New("y", expected));
        }
        [TestCase("a = 2.0 \r b=3.0 \r  y = [1.0,a,b] ", new[]{1.0,2.0,3.0})]
        [TestCase("a = 2.0 \r b=3.0 \r y = [a,b] ", new[]{2.0,3.0})]
        [TestCase("a = 2.0 \r b=3.0 \r y = [a+1,b+2] ", new[]{3.0,5.0})]
        [TestCase("a = 2.0 \r b=3.0 \r y = [a*0,b*0] ", new[]{0.0,0.0})]
        [TestCase("a = true  \ry = if (a) [1.0] else [2.0, 3.0] ", new[]{1.0})]
        [TestCase("a = false  \r y = if (a) [1.0] else [2.0, 3.0]", new[]{2.0,3.0})]
        public void ConstantCalculableArrayTest(string expr, object expected)
        {
            FunBuilder.Build(expr).Calculate().AssertHas(VarVal.New("y", expected));
        }
        
        [TestCase("[1,'2',3.0,4,5.2, true, false, 7.2]",new object[]{1.0,"2",3.0,4.0,5.2,true,false,7.2})]
        [TestCase("if (true) [1.0] else [2.0, 3.0] ", new[]{1.0})]
        [TestCase("if (false) [1.0] else [2.0, 3.0]", new[]{2.0,3.0})]
        [TestCase ("y(x) = x \r[1]",new[]{1.0})]
        [TestCase ("y(x) = x \r[1..3]",new[]{1.0,2,3})]
        [TestCase ("y(x) = x # some comment \r[1]",new[]{1.0})]
        [TestCase ("y(x) = x # some comment \r[1..3]",new[]{1.0,2,3})]
        public void AnonymousConstantArrayTest(string expr, object expected) 
            => FunBuilder.Build(expr).Calculate().AssertOutEquals(expected);

        [Test]
        public void IntersectToDimArrayTest()
        {
            var expression = "y = [[1.0,2.0],[3.0,4.0],[5.0]]." +
                             " intersect ([[3.0,4.0],[1.0],[5.0],[4.0]])";
            var expected = new[] {new [] {3.0, 4.0},new[]{5.0}};

            FunBuilder.Build(expression).Calculate().AssertReturns(VarVal.New("y", expected));
        }
        

        [TestCase(3, "y= [1..x]", new[] {1.0, 2, 3})]
        [TestCase( (Int64)42, "x:int64;   y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]
        [TestCase( (Int32)42, "x:int32;   y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]
        [TestCase( (Int16)42, "x:int16;   y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]
        [TestCase((UInt64)42, "x:uint64;  y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]
        [TestCase((UInt32)42, "x:uint32;  y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]
        [TestCase((UInt16)42, "x:uint16;  y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]
        [TestCase((byte)  42, "x:byte;    y:real[]= [1,2,x]", new[] {1.0, 2.0, 42.0})]

        [TestCase( (Int64)42, "x:int64;   y:int64[]= [1,2,x]", new long[] {1,2,42})]
        [TestCase( (Int32)42, "x:int32;   y:int64[]= [1,2,x]", new long[] {1,2,42})]
        [TestCase( (Int16)42, "x:int16;   y:int64[]= [1,2,x]", new long[] {1,2,42})]
        [TestCase((UInt32)42, "x:uint32;  y:int64[]= [1,2,x]", new long[] {1,2,42})]
        [TestCase((UInt16)42, "x:uint16;  y:int64[]= [1,2,x]", new long[] {1,2,42})]
        [TestCase((byte)  42, "x:byte;    y:int64[]= [1,2,x]", new long[] {1,2,42})]
         
        [TestCase( (Int32)42, "x:int32;   y:int32[]= [1,2,x]", new int[] {1,2,42})]
        [TestCase( (Int16)42, "x:int16;   y:int32[]= [1,2,x]", new int[] {1,2,42})]
        [TestCase((UInt16)42, "x:uint16;  y:int32[]= [1,2,x]", new int[] {1,2,42})]
        [TestCase((byte)  42, "x:byte;    y:int32[]= [1,2,x]", new int[] {1,2,42})]
        
        [TestCase((Int16)42, "x:int16;    y:int16[]= [1,2,x]", new Int16[] {1,2,42})]
        [TestCase((byte)  42, "x:byte;    y:int16[]= [1,2,x]", new Int16[] {1,2,42})]
        
        [TestCase((UInt64)42, "x:uint64;  y:uint64[]= [1,2,x]", new UInt64[] {1,2,42})]
        [TestCase((UInt32)42, "x:uint32;  y:uint64[]= [1,2,x]", new UInt64[] {1,2,42})]
        [TestCase((UInt16)42, "x:uint16;  y:uint64[]= [1,2,x]", new UInt64[] {1,2,42})]
        [TestCase((byte)  42, "x:byte;    y:uint64[]= [1,2,x]", new UInt64[] {1,2,42})]
        
        [TestCase((UInt32)42, "x:uint32;  y:uint32[]= [1,2,x]", new UInt32[] {1,2,42})]
        [TestCase((UInt16)42, "x:uint16;  y:uint32[]= [1,2,x]", new UInt32[] {1,2,42})]
        [TestCase((byte)  42, "x:byte;    y:uint32[]= [1,2,x]", new UInt32[] {1,2,42})]
        
        [TestCase((UInt16)42, "x:uint16;  y:uint16[]= [1,2,x]", new UInt16[] {1,2,42})]
        [TestCase((byte)  42, "x:byte;    y:uint16[]= [1,2,x]", new UInt16[] {1,2,42})]
        

        [TestCase(3, "y= [x..7]", new[] {3.0, 4, 5, 6, 7})]
        [TestCase(3, "y:int[]= [x,2,3]", new[] {3, 2, 3})]
        [TestCase(3, "y= [1..5][x]", 4.0)]
        [TestCase(true, "y= (if(x) [1,2] else [])[0]", 1.0)]

        //[TestCase(2, "x:int; y= [1..6..x]", new[] {1, 3, 5})]
        //[TestCase(0.5, "y= [1.0..3.0..x]", new[] {1.0, 1.5, 2.0, 2.5, 3.0})]
        public void SingleInputEquation_CheckOutputValues(object val, string expr, object expected)
        {
            FunBuilder.Build(expr).Calculate(VarVal.New("x",val)).AssertHas(VarVal.New("y", expected));
        }
        [Test]
        public void ConstantTwinAnyArray_NoTypeSpecification()
        {
            var expr = "out = [0,[1]]";
            var result = FunBuilder.Build(expr).Calculate().Get("out");
            Assert.AreEqual(result.Type, VarType.ArrayOf(VarType.Anything));            
            var arr1 = (IEnumerable<object>) result.Value;
            Assert.AreEqual(2, arr1.Count());
            Assert.AreEqual(0.0, arr1.ElementAt(0));
            var arr2 = arr1.ElementAt(1) as IEnumerable<object>;
            Assert.AreEqual(1.0, arr2.ElementAt(0));
        }
        
        [Test]
        public void ConstantTwinAnyArray_WithTypeSpecification()
        {
            var expr = "out:anything[] = [0,[1]]";
            var result = FunBuilder.Build(expr).Calculate().Get("out");
            Assert.AreEqual(result.Type, VarType.ArrayOf(VarType.Anything));            
            var arr1 = (IEnumerable<object>) result.Value;
            Assert.AreEqual(2, arr1.Count());
            Assert.AreEqual(0.0, arr1.ElementAt(0));
            var arr2 = arr1.ElementAt(1) as IEnumerable<object>;
            Assert.AreEqual(1.0, arr2.ElementAt(0));
        }

        [TestCase("out:anything[] = [0,[1]]")]
        [TestCase("out:anything[] = [[1],0]")]
        [TestCase("out:anything[] = [[true],0]")]
        [TestCase("out:anything[] = [[1],true]")]
        [TestCase("out:anything[] = [true,[1]]")]
        [TestCase("out:anything[] = [1,'vasa']")]
        [TestCase("out:anything[] = ['vasa',1.5]")]
        [TestCase("out = [0,[1]]")]
        [TestCase("out = [[1],0]")]
        [TestCase("out = [[true],0]")]
        [TestCase("out = [[1],true]")]
        [TestCase("out = [true,[1]]")]
        [TestCase("out = [1,'vasa']")]
        [TestCase("out = ['vasa',1.5]")]
        public void ConstantTwinAnyArrayWithUpcast(string expr)
        {
            TraceLog.IsEnabled = true;
            var result = FunBuilder.Build(expr).Calculate().Get("out");
            Assert.AreEqual(result.Type, VarType.ArrayOf(VarType.Anything));            
        }
        [TestCase("out = [[0x1],[1.0]]")]
        [TestCase("out = [[1.0],[0x1]]")]
        [TestCase("out = [[0x1],[1.0],[0x1]]")]
        [TestCase("out = [[1.0],[0x1],[1.0]]")]

        public void ConstantTwinRealArrayWithUpcast_returnsArrayOfReal(string expr)
        {
            TraceLog.IsEnabled = true;
            var result = FunBuilder.Build(expr).Calculate().Get("out");
            Assert.AreEqual(VarType.ArrayOf(VarType.ArrayOf(VarType.Real)),result.Type);            
        }
        
        [Test(Description = "out:real[][] = [[0x1],[1.0]]")]
        public void ConstantTwinRealArrayWithUpcast_typeIsSpecified()
        {
            TraceLog.IsEnabled = true;
            var expr = "out:real[][] = [[0x1],[1.0]]";
            var result = FunBuilder.Build(expr).Calculate().Get("out");
            Assert.AreEqual(VarType.ArrayOf(VarType.ArrayOf(VarType.Real)),result.Type);            
        }
        
        [Test]
        public void ConstantTwinAnyArrayWithUpcast()
        {
            var expr = "out = [[0x1],[1.0],[true]]";
            var result = FunBuilder.Build(expr).Calculate().Get("out");
            Assert.AreEqual(VarType.ArrayOf(VarType.ArrayOf(VarType.Anything)),result.Type);            
        }
        
        [Test]
        public void ConstantTrippleAnyArrayWithUpcast()
        {
            var expr = "out:anything = [false,0,1,'vasa',[1,2,[10000,2]]]";
            var result = FunBuilder.Build(expr).Calculate().GetValueOf("out");

            var arr1 = (IEnumerable<object>) result;
            Assert.AreEqual(5, arr1.Count());
            Assert.AreEqual(false, arr1.ElementAt(0));
            Assert.AreEqual(0.0, arr1.ElementAt(1));
            Assert.AreEqual(1.0, arr1.ElementAt(2));
            Assert.AreEqual("vasa", (arr1.ElementAt(3) as IFunArray).ToText());
            var arr2 = arr1.ElementAt(4) as IEnumerable<object>;
            Assert.AreEqual(1.0, arr2.ElementAt(0));
            Assert.AreEqual(2.0, arr2.ElementAt(1));
            var arr3 = arr2.ElementAt(2) as IEnumerable<object>;
            Assert.AreEqual(10000.0, arr3.ElementAt(0));
            Assert.AreEqual(2.0,     arr3.ElementAt(1));

        }
        
        [TestCase("byte")]
        [TestCase("uint16")]
        [TestCase("uint32")]
        [TestCase("uint64")]
        [TestCase("int16")]
        [TestCase("int32")]
        [TestCase("int64")]
        [TestCase("real")]
        public void SingleInputEquation_twinArrayUpCast(string downType)
        {
            var expr = $"x:{downType};    y:real[][]= [[1],[2,3,x],[x]]";

            var res = FunBuilder
                .Build(expr)
                .Calculate(VarVal.New("x", 42.0))
                .GetValueOf("y");
           
            var expected = new[]
            {
                new [] {1.0},
                new [] {2.0,3.0, 42.0},
                new [] {42.0},
            };
            var i = 0;
            foreach (var item in (IEnumerable<object>) res)
            {
                var item2 =(item as IEnumerable<object>);
                var j = 0;
                foreach (var actual in item2)
                {
                    Assert.AreEqual(expected[i][j], actual);                    
                    Assert.AreEqual(expected[i][j].GetType(), actual.GetType(),$"Invalid type of element '{actual}'");
                    j++;
                }
                i++;
            }
        }

        [Test]
        public void ExceptToDimArrayTest()
        {
            var expression = "y = [[1.0,2.0],[3.0,4.0]]. except([[3.0,4.0],[1.0],[4.0]])";
            var expected = new[] {new [] {1.0, 2.0}};

            FunBuilder.Build(expression).Calculate().AssertReturns(VarVal.New("y", expected));
        }
        
        [Test]
        public void TwoDimConstatantTest()
        {
            var expected = new int[3][];
            expected[0] = new[] {1, 2};
            expected[1] = new[] {3, 4};
            expected[2] = new[] {5};
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.Real));
            var expression = " y= [[1.0,2.0],[3.0,4.0],[5.0]]";
            
            var runtime = FunBuilder.Build(expression);
            var res = runtime.Calculate().Get("y");
            Assert.AreEqual(expectedType, res.Type);
            AssertMultiDimentionalEquals(res, expected);
        }
        [Test]
        public void TwoDimConcatConstatantTest()
        {
            var expected = new int[3][];
            expected[0] = new[] {1, 2};
            expected[1] = new[] {3, 4};
            expected[2] = new[] {5};
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.Real));
            var expression = " y= [[1,2],[3,4]].concat([[5.0]])";
            
            var runtime = FunBuilder.Build(expression);
            var res = runtime.Calculate().Get("y");
            Assert.AreEqual(expectedType, res.Type);
            AssertMultiDimentionalEquals(res, expected);
        }
        [Test]
        public void SingleMultiDimVariable_OutputEqualsInput()
        {
            var x = new int[3][];
            x[0] = new[] {1, 2};
            x[1] = new[] {3, 4};
            x[2] = new[] {5};
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            var expectedOutput = x;
            var expression = "x:int[][]\r y= x";
            
            var runtime = FunBuilder.Build(expression);
            var res = runtime.Calculate(VarVal.New("x", x)).Get("y");
            Assert.AreEqual(expectedType, res.Type);
            AssertMultiDimentionalEquals(res, expectedOutput);
        }
        [Test]
        public void SingleMultiDimVariable_OutputEqualsTwoInputs()
        {
            var x = new int[3][];
            x[0] = new[] {1, 2};
            x[1] = new[] {3, 4};
            x[2] = new[] {5};
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            var expectedOutput = new int[6][];
            expectedOutput[0] = new[] {1, 2};
            expectedOutput[1] = new[] {3, 4};
            expectedOutput[2] = new[] {5};
            expectedOutput[3] = new[] {1, 2};
            expectedOutput[4] = new[] {3, 4};
            expectedOutput[5] = new[] {5};

            var expression = "x:int[][]\r y= x.concat(x)";
            
            var runtime = FunBuilder.Build(expression);
            var res = runtime.Calculate(VarVal.New("x", x)).Get("y");
            Assert.AreEqual(expectedType, res.Type);
            AssertMultiDimentionalEquals(res, expectedOutput);
        }
        [Test]
        public void ArraysIntegrationTest()
        {
            var expr = @"
x: int[]
filt: int
concat    = ([1,2,3,4].concat(x))
size      = concat.count()
possum   = x.filter{it>0}.fold{it1+it2}
filtrat   = x.filter{it> filt} # filt - input variable
";
            var runtime = FunBuilder.Build(expr);
            var res = runtime.Calculate(VarVal.New("x", new[]{5,6,7,8}),
                VarVal.New("filt", 2)
                );
        }
        private static void AssertMultiDimentionalEquals(VarVal res, int[][] expectedOutput)
        {
            for (int i = 0; i < expectedOutput.Length; i++)
            {
                var enumerable = (IFunArray)res.Value;
                var array = enumerable.GetElementOrNull(i);

                for (int j = 0; j < expectedOutput[i].Length; j++)
                {
                    var element = (array as IEnumerable).Cast<object>().ElementAt(j);
                    Assert.AreEqual(element, expectedOutput[i][j]);
                }
            }
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
        [TestCase("y = [..2..2]")]
        [TestCase("y = [1..4")]
        [TestCase("y = [1..")]
       // [TestCase("y = [2,1] in [1,2,3]")]
       // [TestCase("y = [1,5,2] in [1,2,3]")]
        [TestCase("y = x\r[2]")]
        [TestCase("y = [1..2..-2]")]
        [TestCase("y = [1..2..0]")]
        [TestCase("y = [4..1..-2]")]
        [TestCase("y = [4..1..0]")]
        [TestCase("y = [4..1..-2.0]")]
        [TestCase("y = [1..4..-2.0]")]
        [TestCase("y = [1..4..0]")]
        public void ObviouslyFailsOnParse(string expr)
        {
            TraceLog.IsEnabled = true;
            Assert.Throws<FunParseException>(
                () => FunBuilder.Build(expr));
        }


        [TestCase("y = [0..10][11]")]
        [TestCase("y = ['a', 'b'][2]")]
        public void ObviouslyFailsOnRuntime(string expr) =>
            Assert.Throws<FunRuntimeException>(
                ()=> FunBuilder.Build(expr).Calculate());
    }
}