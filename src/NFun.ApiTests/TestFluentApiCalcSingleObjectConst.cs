using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestFluentApiCalcSingleObjectConst
    {
        [TestCase("(13 == 13) and ('vasa' == 'vasa')",true)]
        [TestCase("[1,2,3,4].count(fun it>2)",2)]
        [TestCase("[1..4].filter(fun it>2).map(fun it**2)",new[] {9.0, 16.0})]
        [TestCase("[1..4].reverse().join(',')","4,3,2,1")]
        [TestCase("'Hello world'","Hello world")]
        [TestCase("['Hello','world']",new[] {"Hello", "world"})]
        [TestCase("[1..4].map(fun it.toText())",new[] {"1", "2", "3", "4"})]
        public void GeneralCalcTest(string expr, object expected) => 
            Assert.AreEqual(expected, Funny.Calc(expr));

        [Test]
        public void ReturnsComplexIntArrayConstant()
        {
            var result = Funny.Calc(
                "[[[1,2],[]],[[3,4]],[[]]]");
            Assert.IsInstanceOf<object[]>(result);
            Assert.AreEqual(new[]
            {
                new[] {new[] {1, 2}, Array.Empty<int>()},
                new[] {new[] {3, 4}},
                new[] { Array.Empty<int>() }
            }, result);
        }
        
        
        [Test]
        public void OutputTypeIsStruct_returnsFunnyStruct()
        {
            var str = Funny.Calc(
                "{name = 'alaska'}");
            Assert.IsInstanceOf<IReadOnlyDictionary<string,object>>(str);
            var rs = str as IReadOnlyDictionary<string,object>;
            Assert.AreEqual(1, rs.Count);    
            Assert.AreEqual("alaska", rs["name"]);
        }

        [TestCase("")]
        [TestCase("x:int = 2")]
        [TestCase("a = 12; b = 32; x = a*b")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Throws<FunParseException>(() => Funny.Calc(expr));
        

        [TestCase("{id = age; items = [1,2,3,4].map(fun '{it}'); price = 21*2}")]
        [TestCase("[1..4].filter(fun it>age).map(fun it**2)")]
        [TestCase("age>someUnknownvariable")]
        [TestCase("x:int;")]

        public void UseUnknownInput_throws(string expression) =>
            Assert.Throws<FunParseException>(() => Funny.Calc(expression));
    }
}