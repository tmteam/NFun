using System.Linq;
using NFun.Exceptions;
using NFun.Runtime;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestFluentApiCalcSingleObjectConst
    {
        [TestCase("(13 == 13) and ('vasa' == 'vasa')",true)]
        [TestCase("[1,2,3,4].count{it>2}",2)]
        [TestCase("[1..4].filter{it>2}.map{it**2}",new[] {9.0, 16.0})]
        [TestCase("[1..4].reverse().join(',')","4,3,2,1")]
        [TestCase("'Hello world'","Hello world")]
        [TestCase("['Hello','world']",new[] {"Hello", "world"})]
        [TestCase("[1..4].map{it.toText()}",new[] {"1", "2", "3", "4"})]
        public void GeneralCalcTest(string expr, object expected) => 
            Assert.AreEqual(expected, Funny.Calc(expr));

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
        
        
        [Test]
        public void OutputTypeIsStruct_returnsFunnyStruct()
        {
            var str = Funny.Calc(
                "the{name = 'alaska'}");
            Assert.IsInstanceOf<IReadonlyFunnyStruct>(str);
            var rs = (str as IReadonlyFunnyStruct);
            Assert.AreEqual(1, rs.Fields.Count());    
            Assert.AreEqual("alaska", rs.GetValue("name"));
        }

        [TestCase("")]
        [TestCase("x:int = 2")]
        [TestCase("a = 12; b = 32; x = a*b")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Throws<FunParseException>(() => Funny.Calc(expr));
        

        [TestCase("the{id = age; items = [1,2,3,4].map{'{it}'}; price = 21*2}")]
        [TestCase("[1..4].filter{it>age}.map{it**2}")]
        [TestCase("age>someUnknownvariable")]
        [TestCase("x:int;")]

        public void UseUnknownInput_throws(string expression) =>
            Assert.Throws<FunParseException>(() => Funny.Calc(expression));
    }
}