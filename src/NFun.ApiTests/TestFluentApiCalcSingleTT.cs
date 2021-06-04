using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestFluentApiCalcSingleTT
    {
        [TestCase("(Age == 13) and (NAME == 'vasa')",true)]
        [TestCase("(age == 13) and (name == 'vasa')",true)]
        [TestCase("(age != 13) or (name != 'vasa')",false)]
        [TestCase("'{name}{age}'.reverse()=='31asav'",true)]
        [TestCase("'mama'=='{name}{age}'.reverse()",false)]
        public void ReturnsBoolean(string expr, bool expected) => 
            CalcInDifferentWays(expr,new UserInputModel("vasa", 13),expected);

        [Test]
        public void BuildLambdaTwoTimes()
        {
            var context1 = Funny.ForCalc<ModelWithInt, string>();
            var context2 = Funny.ForCalc<ModelWithInt, string>();
            
            Func<ModelWithInt, string> lambda1 = context1.Build("'{id}'");
            Func<ModelWithInt, string> lambda2 = context2.Build("'{id}'");

            var result1 =  lambda1(new ModelWithInt{id = 42});
            var result2 =  lambda2(new ModelWithInt{id = 1});
            Assert.AreEqual(result1,"42");
            Assert.AreEqual(result2,"1");
        }
        
        [Test]
        public void IoComplexTypeTransforms() =>
            CalcInDifferentWays(expr: "the{id = age; items = ids.map{'{it}'}; price = size*2}",
                input: new UserInputModel("vasa", 13, size: 21, iq: 12, 1, 2, 3, 4),
                expected: new ContractOutputModel
                {
                    Id = 13,
                    Items = new[] {"1", "2", "3", "4"},
                    Price = 21 * 2
                });

        [TestCase("ids.count{it>2}",2)]
        [TestCase("1",1)]

        public void ReturnsInt(string expr, int expected) 
            => CalcInDifferentWays(expr,new UserInputModel("vasa", 13, size: 21, iq: 12, 1, 2, 3, 4),expected);

        [TestCase("IDS.filter{it>aGe}.map{it*it}",new[] {10201,10404})]
        [TestCase("ids.filter{it>age}.map{it*it}",new[] {10201,10404})]
        [TestCase("ids.filter{it>2}",new[]{101,102})]
        [TestCase("out:int[]=ids.filter{it>age}.map{it*it}",new[]{10201,10404})]
        public void ReturnsIntArray(string expr, int[] expected) 
            => CalcInDifferentWays(expr,new UserInputModel("vasa", 2, size: 21, iq: 1, 1, 2, 101, 102),expected);
        
        [Test]
        public void InputFieldIsCharArray() =>
            CalcInDifferentWays("[letters.reverse()]", new ModelWithCharArray2
            {
                Letters = new[]{'t','e','s','t'}
            }, new []{"tset"});

        [TestCase("IDS.reverse().join(',')","4,3,2,1")]
        [TestCase("Ids.reverse().join(',')","4,3,2,1")]
        [TestCase("ids.reverse().join(',')","4,3,2,1")]
        [TestCase("'Hello world'","Hello world")]
        [TestCase("'{name}{age}'.reverse()","31asav")]
        [TestCase("'{Name}{Age}'.reverse()","31asav")]
        [TestCase("name.reverse()","asav")]

        public void ReturnsText(string expr, string expected) 
            => CalcInDifferentWays(expr, new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4),expected);

        [TestCase("ids.map{it.toText()}",new[] {"1", "2", "101", "102"})]
        [TestCase("['Hello','world']",new[]{"Hello","world"})]
        public void ReturnsArrayOfTexts(string expr, string[] expected) 
            => CalcInDifferentWays(
                expr:     expr, 
                input:    new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 101, 102),
                expected: expected);

        private static void CalcInDifferentWays<TInput,TOutput>(string expr, TInput input,TOutput expected)
        {
            var result1 = Funny.Calc<TInput, TOutput>(expr, input);
            var context = Funny.ForCalc<TInput, TOutput>();
            var result2 = context.Calc(expr, input);
            var result3 = context.Calc(expr, input);
            var lambda1 = context.Build(expr);
            var result4 = lambda1(input);
            var result5 = lambda1(input);
            var lambda2 = context.Build(expr);
            var result6 = lambda2(input);
            var result7 = lambda2(input);

            Assert.IsTrue(TestHelper.AreSame(expected, result1));
            Assert.IsTrue(TestHelper.AreSame(expected, result2));
            Assert.IsTrue(TestHelper.AreSame(expected, result3));
            Assert.IsTrue(TestHelper.AreSame(expected, result4));
            Assert.IsTrue(TestHelper.AreSame(expected, result5));
            Assert.IsTrue(TestHelper.AreSame(expected, result6));
            Assert.IsTrue(TestHelper.AreSame(expected, result7));
        }

        [Test]
        public void ReturnsComplexIntArrayConstant() =>
            CalcInDifferentWays(
                expr: "[[[1,2],[]],[[3,4]],[[]]]",
                input: new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4),
                expected: new[]
                {
                    new[] {new[] {1, 2}, new int[0]},
                    new[] {new[] {3, 4}},
                    new[] {new int[0]}
                }
            );

        [TestCase("")]
        [TestCase("x:int = 2")]
        [TestCase("a = 12; b = 32; x = a*b")]
        [TestCase("y = age")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Throws<FunParseException>(() => Funny.Calc<UserInputModel,int>(expr,new UserInputModel(age:42)));
        
        [TestCase("age*AGE")]
        public void UseDifferentInputCase_throws(string expression) =>
            Assert.Throws<FunParseException>(() => Funny.Calc<UserInputModel,int>(expression, new UserInputModel(age: 22)));
        
        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Throws<FunInvalidUsageException>(() => Funny.Calc<UserInputModel, ModelWithoutEmptyConstructor>(
                "the{name = name}"
                , new UserInputModel("vasa")));
        
        [TestCase("age>someUnknownvariable")]
        [TestCase("x:int;")]
        public void UseUnknownInput_throws(string expr) =>
            Assert.Throws<FunParseException>(() =>
                Funny.Calc<UserInputModel, bool>(expr, new UserInputModel(age: 22)));
    }
}