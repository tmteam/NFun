using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcSingleT
    {
        [TestCase("(age == 13) and (name == 'vasa')",true)]
        [TestCase("(age != 13) or (name != 'vasa')",false)]
        [TestCase("name.reverse()","asav")]
        [TestCase("'{name}{age}'.reverse()","31asav")]
        [TestCase("'{name}{age}'.reverse()=='31asav'",true)]
        [TestCase("'mama'=='{name}{age}'.reverse()",false)]
        [TestCase("'hello world'","hello world")]
        [TestCase("1",1.0)]
        [TestCase("ids.count{it>2}",2)]
        [TestCase("ids.filter{it>2}",new[]{101,102})]
        [TestCase("out:int[]=ids.filter{it>age}.map{it*it}",new[]{10201,10404})]
        [TestCase("ids.reverse().join(',')","102,101,2,1")]
        [TestCase("['Hello','world']",new[]{"Hello","world"})]
        [TestCase("ids.map{it.toText()}",new[]{"1","2","101","102"})]
        public void GeneralUserInputModelTest(string expr, object expected){
            var input = new UserInputModel(
                name:"vasa",
                age: 13,
                size: 13.5,  
                iq: 50,
                ids: new[]{1,2,101,102});
            //CALC
            var result = Funny.Calc(expr, input);
            Assert.AreEqual(expected, result);
            //Context+calc
            var context = Funny.ForCalc<UserInputModel>();
            var result2 = context.Calc(expr, input);
            Assert.AreEqual(expected, result2);
            var result3 = context.Calc(expr, input);
            Assert.AreEqual(expected, result3);
            //lambda
            var lambda = context.Build(expr);
            var result4 = lambda(input);
            Assert.AreEqual(expected, result4);
            var result5 = lambda(input);
            Assert.AreEqual(expected, result5);

        }
        
        [Test]
        public void ReturnsComplexIntArrayConstant()
        {
            var result = Funny.Calc(
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
        [TestCase("y = name")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Throws<FunInvalidUsageTODOException>(() => Funny.Calc(expr,new UserInputModel("vasa")));
        
        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Throws<FunInvalidUsageTODOException>(() => Funny.Calc(
                "@{name = name}"
                , new UserInputModel("vasa")));

        [TestCase("age>someUnknownvariable")]
        public void UseUnknownInput_throws(string expression) =>
            Assert.Throws<FunInvalidUsageTODOException>(() =>
                Funny.Calc(expression, new UserInputModel(age: 22)));
    }
}