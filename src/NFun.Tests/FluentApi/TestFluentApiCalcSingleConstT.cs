using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcSingleConstT
    {
        [Test]
        public void Smoke()
        {
            var result = Funny.Calc<bool>("(13 == 13) and ('vasa' == 'vasa')");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void IoComplexTypeTransforms()
        {
            var result = Funny.Calc<ContractOutputModel>(
                "@{id = 13; items = [1,2,3,4].map{'{it}'}; price = 21*2}");
            var expected = new ContractOutputModel
            {
                Id = 13,
                Items = new[] {"1", "2", "3", "4"},
                Price = 42
            };
            Assert.IsTrue(TestTools.AreSame(expected, result));
        }

        [Test]
        public void ArrayTransforms()
        {
            var result = Funny.Calc<int>("[1,2,3,4].count{it>2}");
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ReturnsRealArray()
        {
            var result = Funny.Calc<double[]>("[1..4].filter{it>1}.map{it**2}");
            Assert.AreEqual(new[] {4, 9, 16}, result);
        }

        [Test]
        public void ReturnsText()
        {
            var result = Funny.Calc<string>("[1..4].reverse().join(',')");
            Assert.AreEqual("4,3,2,1", result);
        }

        [Test]
        public void ReturnsConstantText()
        {
            var result = Funny.Calc<string>("'Hello world'");
            Assert.AreEqual("Hello world", result);
        }

        [Test]
        public void ReturnsConstantArrayOfTexts()
        {
            var result = Funny.Calc<string[]>("['Hello','world']");
            Assert.AreEqual(new[] {"Hello", "world"}, result);
        }

        [Test]
        public void ReturnsArrayOfTexts()
        {
            var result = Funny.Calc<string[]>("[1..4].map{it.toText()}");
            Assert.AreEqual(new[] {"1", "2", "3", "4"}, result);
        }

        [Test]
        public void ReturnsComplexIntArrayConstant()
        {
            var result = Funny.Calc<int[][][]>(
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
            => Assert.Catch(() => Funny.Calc<UserInputModel>(expr));

        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Catch(() => Funny.Calc<UserInputModel>(
                "@{name = 'alaska'}"));

        [TestCase("[1..4].filter{it>age}.map{it**2}")]
        [TestCase("age>someUnknownvariable")]
        public void UseUnknownInput_throws(string expression) =>
            Assert.Catch(() => Funny.Calc<object>(expression));
        [TestCase("[1..4].filter{it>age}.map{it**2}")]
        [TestCase("age>someUnknownvariable")]
        public void UseUnknownInputWithWrongIntOutputType_throws(string expression) =>
            Assert.Catch(() => Funny.Calc<int>(expression));
    }
}