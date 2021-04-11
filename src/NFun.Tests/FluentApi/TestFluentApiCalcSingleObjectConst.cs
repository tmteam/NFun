using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcSingleObjectConst
    {
        [Test]
        public void Smoke()
        {
            var result = Funny.Calc("(13 == 13) and ('vasa' == 'vasa')");
            Assert.AreEqual(true, result);
        }
        
        [Test]
        public void ArrayTransforms()
        {
            var result = Funny.Calc("[1,2,3,4].count{it>2}");
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ReturnsIntArray()
        {
            var result = Funny.Calc("[1..4].filter{it>2}.map{it**2}");
            Assert.AreEqual(new[] {9.0, 16.0}, result);
        }

        [Test]
        public void ReturnsText()
        {
            var result = Funny.Calc("[1..4].reverse().join(',')");
            Assert.AreEqual("4,3,2,1", result);
        }

        [Test]
        public void ReturnsConstantText()
        {
            var result = Funny.Calc("'Hello world'");
            Assert.AreEqual("Hello world", result);
        }

        [Test]
        public void ReturnsConstantArrayOfTexts()
        {
            var result = Funny.Calc("['Hello','world']");
            Assert.AreEqual(new[] {"Hello", "world"}, result);
        }

        [Test]
        public void ReturnsArrayOfTexts()
        {
            var result = Funny.Calc("[1..4].map{it.toText()}");
            Assert.AreEqual(new[] {"1", "2", "3", "4"}, result);
        }

        [Test]
        public void ReturnsComplexIntArrayConstant()
        {
            var result = Funny.Calc(
                "[[[1,2],[]],[[3,4]],[[]]]");
            Assert.AreEqual(new[]
            {
                new[] {new[] {1, 2}, new int[0]},
                new[] {new[] {3, 4}},
                new[] {new int[0]}
            }, result);
        }
        [TestCase("")]
        [TestCase("x:int;")]
        [TestCase("x:int = 2")]
        [TestCase("a = 12; b = 32; x = a*b")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Catch(() => Funny.Calc(expr));
        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Catch(() => Funny.Calc(
                "@{name = 'alaska'}"));

        [TestCase("@{id = age; items = [1,2,3,4].map{'{it}'}; price = 21*2}")]
        [TestCase("[1..4].filter{it>age}.map{it**2}")]
        [TestCase("age>someUnknownvariable")]
        public void UseUnknownInput_throws(string expression) =>
            Assert.Catch(() => Funny.Calc(expression));
    }
}