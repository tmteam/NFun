using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcSingleTT
    {
        [Test]
        public void Smoke()
        {
            var input = new UserInputModel("vasa", 13);
            var result = Funny.Calc<UserInputModel, bool>("(age == 13) and (name == 'vasa')", input);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void IoComplexTypeTransforms()
        {
            var input = new UserInputModel("vasa", 13, size: 21, iq: 12, 1, 2, 3, 4);
            var result = Funny.Calc<UserInputModel, ContractOutputModel>(
                "@{id = age; items = ids.map{'{it}'}; price = size*2}", input);
            var expected = new ContractOutputModel
            {
                Id = input.Age,
                Items = new[] {"1", "2", "3", "4"},
                Price = input.Size * 2
            };
            Assert.IsTrue(TestTools.AreSame(expected, result));
        }

        [Test]
        public void ArrayTransforms()
        {
            var result = Funny.Calc<UserInputModel, int>(
                "ids.count{it>2}", new UserInputModel("vasa", 13, size: 21, iq: 12, 1, 2, 3, 4));
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ReturnsIntArray()
        {
            var result = Funny.Calc<UserInputModel, int[]>(
                "ids.filter{it>age}.map{it*it}", new UserInputModel("vasa", 1, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[] {4, 9, 16}, result);
        }

        [Test]
        public void ReturnsText()
        {
            var result = Funny.Calc<UserInputModel, string>(
                "ids.reverse().join(',')", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual("4,3,2,1", result);
        }

        [Test]
        public void ReturnsConstantText()
        {
            var result = Funny.Calc<UserInputModel, string>(
                "'Hello world'", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual("Hello world", result);
        }

        [Test]
        public void ReturnsConstantArrayOfTexts()
        {
            var result = Funny.Calc<UserInputModel, string[]>(
                "['Hello','world']", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[] {"Hello", "world"}, result);
        }

        [Test]
        public void ReturnsArrayOfTexts()
        {
            var result = Funny.Calc<UserInputModel, string[]>(
                "ids.map{it.toText()} ", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[] {"1", "2", "3", "4"}, result);
        }

        [Test]
        public void ReturnsComplexIntArrayConstant()
        {
            var result = Funny.Calc<UserInputModel, int[][][]>(
                "[[[1,2],[]],[[3,4]],[[]]]", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
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
        [TestCase("y = age")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Catch(() => Funny.Calc<UserInputModel,int>(expr,new UserInputModel(age:42)));
        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Catch(() => Funny.Calc<UserInputModel, ModelWithoutEmptyConstructor>(
                "@{name = name}"
                , new UserInputModel("vasa")));
        [Test]
        public void UseUnknownInput_throws() =>
            Assert.Catch(() =>
                Funny.Calc<UserInputModel, bool>("age>someUnknownvariable", new UserInputModel(age: 22)));
    }
}