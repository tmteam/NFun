using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ArraysTest
    {
        [TestCase("y = [1.0,1.2,2.4]", new[]{1.0,1.2, 2.4})]
        [TestCase("y = [1.0]", new[]{1.0})]
        [TestCase("y = []", new object[0])]
        [TestCase("y = [1.0,2.0]::[3.0,4.0]", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = ([1.0]::[2.0])::[3.0,4.0]", new []{1.0,2.0,3.0,4.0})]
        [TestCase("y = [1,2,3]", new[]{1,2,3})]
        [TestCase("y = ['a','b','c']", new[]{"a","b","c"})]
        [TestCase("y = [1.0]==[]", false)]
        [TestCase("y = [1.0]==[2.0]", false)]
        [TestCase("y = [1.0,2.0,3.0]==[1,2,3]", true)]
        [TestCase("y = [1.0]<>[2.0]", true)]
        [TestCase("y = []==[]", true)]
        [TestCase("y = []<>[]", false)]
        [TestCase("y = [1.0]==[1.0]", true)]
        [TestCase("y = [1.0]<>[1.0]", false)]
        [TestCase("y = [1.0,2.0]==[1.0,2.0]", true)]
        [TestCase("y = [1.0,2.0]==([1.0]::[2.0])", true)]
        
        public void ConstantArrayTest(string expr, object expected)
        {
            Interpreter.BuildOrThrow(expr).Calculate().AssertReturns(Var.New("y", expected));
        }
        [TestCase("y = [1.0,a,b] a = 2.0 \r b=3.0 \r ", new[]{1.0,2.0,3.0})]
        [TestCase("y = [a,b] a = 2.0 \r b=3.0 \r ", new[]{2.0,3.0})]
        [TestCase("y = [a+1,b+2] a = 2.0 \r b=3.0 \r ", new[]{3.0,5.0})]
        [TestCase("y = [a*0,b*0] a = 2.0 \r b=3.0 \r ", new[]{0.0,0.0})]
        [TestCase("y = if a then [1.0] else [2.0, 3.0] \r a = true  ", new[]{1.0})]
        [TestCase("y = if a then [1.0] else [2.0, 3.0] \r a = false  ", new[]{2.0,3.0})]
        public void ConstantCalculableArrayTest(string expr, object expected)
        {
            Interpreter.BuildOrThrow(expr).Calculate().AssertHas(Var.New("y", expected));
        }
        [Test]
        public void TwoDimConstatantTest()
        {
            var expected = new int[3][];
            expected[0] = new[] {1, 2};
            expected[1] = new[] {3, 4};
            expected[2] = new[] {5};
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.IntType));
            var expression = " y= [[1,2],[3,4],[5]]";
            
            var runtime = Interpreter.BuildOrThrow(expression);
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
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.IntType));
            var expression = " y= [[1,2],[3,4]]::[[5]]";
            
            var runtime = Interpreter.BuildOrThrow(expression);
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
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.IntType));
            var expectedOutput = x;
            var expression = "x:int[][]\r y= x";
            
            var runtime = Interpreter.BuildOrThrow(expression);
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
            
            var expectedType = VarType.ArrayOf(VarType.ArrayOf(VarType.IntType));
            var expectedOutput = new int[6][];
            expectedOutput[0] = new[] {1, 2};
            expectedOutput[1] = new[] {3, 4};
            expectedOutput[2] = new[] {5};
            expectedOutput[3] = new[] {1, 2};
            expectedOutput[4] = new[] {3, 4};
            expectedOutput[5] = new[] {5};

            var expression = "x:int[][]\r y= x::x";
            
            var runtime = Interpreter.BuildOrThrow(expression);
            var res = runtime.Calculate(Var.New("x", x)).Get("y");
            Assert.AreEqual(expectedType, res.Type);
            AssertMultiDimentionalEquals(res, expectedOutput);
        }

        private static void AssertMultiDimentionalEquals(Var res, int[][] expectedOutput)
        {
            for (int i = 0; i < expectedOutput.Length; i++)
            {
                var enumerable = (res.Value as IEnumerable);
                var array = enumerable.Cast<object>().ElementAt(i);

                for (int j = 0; j < expectedOutput[i].Length; j++)
                {
                    var element = (array as IEnumerable).Cast<object>().ElementAt(j);
                    Assert.AreEqual(element, expectedOutput[i][j]);
                }
            }
        }

        [TestCase("y = [")]
        [TestCase("y = [1.0::")]
        [TestCase("y = [1.0::]")]
        [TestCase("y = [1.0]::]")]
        [TestCase("y = [1.0]::[")]
        [TestCase("y = [1.0]::")]
        [TestCase("y = [1.0]::::[2.0]")]
        [TestCase("y = ::[2.0]")]
        [TestCase("y = [2.0 3.0]")]
        [TestCase("y = [2.0,,3.0]")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
    }
}