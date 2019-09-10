using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFun;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ArraysTest
    {
        [TestCase("y = [1..4]", new[]{1,2,3,4})]
        [TestCase("y = [4..1]", new[]{4,3,2,1})]
        [TestCase("y = [1..7..2]", new[]{1,3,5,7})]
        [TestCase("y = [7..1..2]", new[]{7,5,3,1})]
        [TestCase("y = [1..8..2]", new[]{1,3,5,7})]
        [TestCase("y = [1.0..3.0..0.5]", new[]{1.0,1.5,2.0,2.5,3.0})]
        [TestCase("y = [3.0..1.0..0.5]", new[]{3.0,2.5,2.0,1.5, 1.0})]
        [TestCase("y = [1..3..0.5]", new[]{1.0,1.5,2.0,2.5,3.0})]
        [TestCase("y = [1..1]", new[]{1})]
        [TestCase("y = [1,2,3,4]", new[]{1,2,3,4})]
        [TestCase("y = [1]", new[]{1})]
        [TestCase("y = ['foo','bar']", new []{"foo","bar"})]
        [TestCase("y = [0..10][0]", 0)]
        [TestCase("y = [0..10][10]", 10)]
        [TestCase("y = [0..10][2:5]", new[]{2,3,4,5})]
        [TestCase("y = [0..10][1:1]", new[]{1})]
        [TestCase("y = [0..10][1:2]", new[]{1,2})]
        [TestCase("y = [0..10][:5]", new[]{0,1,2,3,4,5})]
        [TestCase("y = [0..10][5:]", new[]{5,6,7,8,9,10})]
        [TestCase("y = ['a','b'][0]", "a")]
        [TestCase("y = ['a','b'][1]", "b")]
        [TestCase("y = [1,2,3][:]", new[]{1,2,3})]
        [TestCase("y = [0..10][1:7:2]", new[]{1,3,5,7})]
        [TestCase("y = [0..10][1:2:]", new[]{1,2})]
        [TestCase("y = [0..10][1::2]", new[]{1,3,5,7,9})]
        [TestCase("y = [0..10][5::]", new[]{5,6,7,8,9,10})]
        [TestCase("y = [0..10][:2:]", new[]{0,1,2})]
        [TestCase("y = [0..10][::4]", new[]{0,4,8})]
        [TestCase("y = [0..10][:4:3]", new[]{0,3})]
        [TestCase("y = [1.0,1.2,2.4]", new[]{1.0,1.2, 2.4})]
        [TestCase("y = [1.0]", new[]{1.0})]
        [TestCase("y = 1 in [1,2,3]", true)]    
        [TestCase("y = 0 in [1,2,3]", false)]    
        [TestCase("y = not 0 in [1,2,3]", true)]    
        [TestCase("y = not 1 in [1,2,3]", false)]    
        
        [TestCase("y = []", new object[0])]

        [TestCase("y = [1,2,3]", new[]{1,2,3})]
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
            FunBuilder.BuildDefault(expr).Calculate().AssertReturns(Var.New("y", expected));
        }
        [TestCase("a = 2.0 \r b=3.0 \r  y = [1.0,a,b] ", new[]{1.0,2.0,3.0})]
        [TestCase("a = 2.0 \r b=3.0 \r y = [a,b] ", new[]{2.0,3.0})]
        [TestCase("a = 2.0 \r b=3.0 \r y = [a+1,b+2] ", new[]{3.0,5.0})]
        [TestCase("a = 2.0 \r b=3.0 \r y = [a*0,b*0] ", new[]{0.0,0.0})]
        [TestCase("a = true  \ry = if (a) [1.0] else [2.0, 3.0] ", new[]{1.0})]
        [TestCase("a = false  \r y = if (a) [1.0] else [2.0, 3.0]", new[]{2.0,3.0})]
     
        public void ConstantCalculableArrayTest(string expr, object expected)
        {
            FunBuilder.BuildDefault(expr).Calculate().AssertHas(Var.New("y", expected));
        }
        
        
        [TestCase("if (true) [1.0] else [2.0, 3.0] ", new[]{1.0})]
        [TestCase("if (false) [1.0] else [2.0, 3.0]", new[]{2.0,3.0})]
        [TestCase ("y(x) = x \r[1]",new[]{1})]
        [TestCase ("y(x) = x \r[1..3]",new[]{1,2,3})]
        [TestCase ("y(x) = x # some comment \r[1]",new[]{1})]
        [TestCase ("y(x) = x # some comment \r[1..3]",new[]{1,2,3})]
        public void AnonymousConstantArrayTest(string expr, object expected)
        {
            FunBuilder.BuildDefault(expr).Calculate().AssertHas(Var.New("out", expected));
        }
        
        [Test]
        public void IntersectToDimArrayTest()
        {
            var expression = "y = [[1.0,2.0],[3.0,4.0],[5.0]] . intersect ([[3.0,4.0],[1.0],[5.0],[4.0]])";
            var expected = new[] {new [] {3.0, 4.0},new[]{5.0}};

            FunBuilder.BuildDefault(expression).Calculate().AssertReturns(Var.New("y", expected));
        }
        [Test]
        public void ExceptToDimArrayTest()
        {
            var expression = "y = [[1.0,2.0],[3.0,4.0]]. except([[3.0,4.0],[1.0],[4.0]])";
            var expected = new[] {new [] {1.0, 2.0}};

            FunBuilder.BuildDefault(expression).Calculate().AssertReturns(Var.New("y", expected));
        }
        
        [Test]
        public void TwoDimConstatantTest()
        {
            var expected = new int[3][];
            expected[0] = new[] {1, 2};
            expected[1] = new[] {3, 4};
            expected[2] = new[] {5};
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            var expression = " y= [[1,2],[3,4],[5]]";
            
            var runtime = FunBuilder.BuildDefault(expression);
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
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            var expression = " y= [[1,2],[3,4]].concat([[5]])";
            
            var runtime = FunBuilder.BuildDefault(expression);
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
            
            var runtime = FunBuilder.BuildDefault(expression);
            var res = runtime.Calculate(Var.New("x", x)).Get("y");
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
            
            var runtime = FunBuilder.BuildDefault(expression);
            var res = runtime.Calculate(Var.New("x", x)).Get("y");
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
possum   = x.filter(i:int ->i>0).reduce((i:int,j:int)-> i+j)
filtrat   = x.filter(i:int ->i> filt) # filt - входная переменная
";
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate(Var.New("x", new[]{5,6,7,8}),
                Var.New("filt", 2)
                );
        }
        private static void AssertMultiDimentionalEquals(Var res, int[][] expectedOutput)
        {
            for (int i = 0; i < expectedOutput.Length; i++)
            {
                var enumerable = (FunArray)res.Value;
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
        [TestCase("y = [1.0..4]")]
        [TestCase("y = ['1'..4]")]
        [TestCase("y = ['1'..'4']")]
        [TestCase("y = [1.0..4.0]")]
        [TestCase("y = [1..7..]")]
        [TestCase("y = [1....2]")]
        [TestCase("y = [..2..2]")]
        [TestCase("y = [1..4")]
        [TestCase("y = [1..")]
        [TestCase("y = [2,1] in [1,2,3]")]    
        [TestCase("y = [1,5,2] in [1,2,3]")] 
        [TestCase("y = x\r[2]")]
        public void ObviouslyFailsOnParse(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
        [TestCase("y = [1..2..-2]")]
        [TestCase("y = [1..2..0]")]
        [TestCase("y = [4..1..-2]")]
        [TestCase("y = [4..1..0]")]
        [TestCase("y = [4..1..-2.0]")]
        [TestCase("y = [1..4..-2.0]")]
        [TestCase("y = [1..4..0]")]
        [TestCase("y = [0..10][11]")]
        [TestCase("y = ['a', 'b'][2]")]
        public void ObviouslyFailsOnRuntime(string expr) =>
            Assert.Throws<FunRuntimeException>(
                ()=> FunBuilder.BuildDefault(expr).Calculate());
    }
}