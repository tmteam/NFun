using NFun.Exceptions;
using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcManyTT
    {
        [TestCase("id = age*2; items = ids.map(toText);  Price = 42.1")]
        [TestCase("ID = age*2; Items = iDs.map(toText);  price = 42.1")]
        public void MapContracts(string expr) =>
            CalcInDifferentWays(expr,
                input:   new UserInputModel("vasa", 13, ids:new []{1,2,3}), 
                expected:new ContractOutputModel {Id = 26, Items = new[] {"1", "2", "3"}, Price = 42.1});

        [Test]
        public void FullConstInitialization() =>
            CalcInDifferentWays("id = 42; items = ['vasa','kate']; price = 42.1", new UserInputModel(),
                new ContractOutputModel
                {
                    Id = 42,
                    Price = 42.1,
                    Items = new[] {"vasa", "kate"}
                }
            );

        
        [Test]
        public void OutputFieldIsConstCharArray() =>
            CalcInDifferentWays("chars = 'test'", new UserInputModel(), new ModelWithCharArray
            {
                Chars = new[]{'t','e','s','t'}
            });
        
        
        [Test]
        public void InputAndOutputFieldsAreCharArrays() =>
            CalcInDifferentWays("Chars = letters.reverse()", new ModelWithCharArray2
            {
                Letters = new[]{'t','e','s','t'}
            }, new ModelWithCharArray
            {
                Chars = new[]{'t','s','e','t'}
            });
        
        [Test]
        public void InputFieldIsCharArray() =>
            CalcInDifferentWays("items = [letters.reverse()]", new ModelWithCharArray2
            {
                Letters = new[]{'t','e','s','t'}
            }, new ContractOutputModel
            {
                Items = new[]{"tset"}
            });
        
        [Test]
        public void NofieldsInitialized_throws() 
            => Assert.Throws<FunInvalidUsageTODOException>(()=>  
                Funny.CalcMany<UserInputModel,ContractOutputModel>(
                    expression: "someField1 = age; somefield2 = 2", 
                    input: new UserInputModel()));
        
        [TestCase("13.1")]
        [TestCase("age")]
        [TestCase("ids")]
        public void AnonymousEquation_throws(string expr) 
            => Assert.Throws<FunInvalidUsageTODOException>(
                ()=> Funny.CalcMany<UserInputModel,ContractOutputModel>(expr, new UserInputModel()));
        
        [Test]
        public void UnknownInputIdUsed_throws() 
            => Assert.Throws<FunInvalidUsageTODOException>(
                ()=> Funny.CalcMany<UserInputModel,ContractOutputModel>(
                    "id = someInput*age", new UserInputModel()));
    
        [Test]
        public void SomeFieldInitialized_DefaultValuesInUninitalizedFields() {
            var result = Funny.CalcMany<UserInputModel,ContractOutputModel>(
                "id = 321; somenotExisted = age", new UserInputModel());
            Assert.AreEqual(321, result.Id);
            Assert.AreEqual(new ContractOutputModel().Price, result.Price);
            CollectionAssert.AreEqual(new ContractOutputModel().Items, result.Items);
        }
        [TestCase("Id = age*Age; ")]
        [TestCase("Id = 321; Price = ID;")]
        public void UseDifferentInputCase_throws(string expression) =>
            Assert.Throws<FunParseException>(
                () => Funny.CalcMany<UserInputModel,ContractOutputModel>(expression, new UserInputModel()));
        
        private void CalcInDifferentWays<TInput,TOutput>(string expr, TInput input, TOutput expected) 
            where TOutput : new()
        {
            var result1 = Funny.CalcMany<TInput, TOutput>(expr, input);
            var context = Funny.ForCalcMany<TInput, TOutput>();
            var result2 = context.Calc(expr, input);
            var result3 = context.Calc(expr, input);
            var lambda1 = context.Build(expr);
            var result4 = lambda1(input);
            var result5 = lambda1(input);
            var lambda2 = context.Build(expr);
            var result6 = lambda2(input);
            var result7 = lambda2(input);
            
            Assert.IsTrue(TestTools.AreSame(expected, result1));
            Assert.IsTrue(TestTools.AreSame(expected, result2));
            Assert.IsTrue(TestTools.AreSame(expected, result3));
            Assert.IsTrue(TestTools.AreSame(expected, result4));
            Assert.IsTrue(TestTools.AreSame(expected, result5));
            Assert.IsTrue(TestTools.AreSame(expected, result6));
            Assert.IsTrue(TestTools.AreSame(expected, result7));
        }
    }
}