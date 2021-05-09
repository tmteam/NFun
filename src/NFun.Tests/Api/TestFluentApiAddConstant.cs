using System;
using NUnit.Framework;

namespace NFun.Tests.Api
{
    public class TestFluentApiAddConstant {
        [Test]
        public void Smoke()
        {
            var context = Funny
                .WithConstant("age", 100)
                .WithConstant("name", "vasa")
                .ForCalc<ModelWithInt, string>();
            
            Func<ModelWithInt, string> lambda = context.Build("'{name}\\'s id is {id} and age is {age}'");
            var result1 =  lambda(new ModelWithInt{id = 42});
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result1,"vasa's id is 42 and age is 100");
            Assert.AreEqual(result2,"vasa's id is 1 and age is 100");
        }
        
        [TestCase("id","id")]
        [TestCase("Id","id")]
        [TestCase("Id","Id")]
        [TestCase("id","Id")]
        public void InputNameOverridesConstant(string constantName, string varName)
        {
            var context = Funny
                .WithConstant(constantName, 100)
                .ForCalc<ModelWithInt, string>();
            
            Func<ModelWithInt, string> lambda = context.Build("'id= {"+varName+"}'");

            var result =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result,"id= 42");
            
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"id= 1");
        }
        
        
        [TestCase("id","id")]
        [TestCase("Id","id")]
        [TestCase("Id","Id")]
        [TestCase("id","Id")]
        public void OutputNameOverridesConstant(string constantName, string varName)
        {
            var context = Funny
                .WithConstant(constantName, 100)
                .ForCalcMany<UserInputModel, ContractOutputModel>();
            
            var lambda = context.Build($"{varName}= age");

            var result1 =  lambda(new UserInputModel(age: 42));
            Assert.AreEqual(result1.Id,42);
            var result2 =  lambda(new UserInputModel(age: 11));
            Assert.AreEqual(result2.Id,11);

        }
        
        
    }
}