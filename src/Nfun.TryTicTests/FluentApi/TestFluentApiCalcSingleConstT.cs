using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.ModuleTests.FluentApi
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
                "@{id = age; items = [1,2,3,4].map{'{it}'}; price = 21*2}");
            var expected = new ContractOutputModel
            {
                Id = 13,
                Items = new[] {"1", "2", "3", "4"},
                Price = 42
            };
            Assert.IsTrue(TestHelper.AreSame(expected, result));
        }

        [Test]
        public void ArrayTransforms()
        {
            var result = Funny.Calc<int>("[1,2,3,4].count{it>2}");
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ReturnsIntArray()
        {
            var result = Funny.Calc<int[]>("[1..4].filter{it>age}.map{it**2}");
            Assert.AreEqual(new[] {4, 9, 16}, result);
        }

        [Test]
        public void ReturnsText()
        {
            var result = Funny.Calc<string>("[1..4].reverse().join(',')");
            Assert.AreEqual("4321", result);
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

        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Catch(() => Funny.Calc<UserInputModel>(
                "@{name = 'alaska'}"));

        [Test]
        public void UseUnknownInput_throws() =>
            Assert.Catch(() =>
                Funny.Calc<bool>("age>someUnknownvariable"));
    }
}