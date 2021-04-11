using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcManyTT
    {
        [Test]
        public void MapContracts()
        {
            var result = Funny.CalcMany<UserInputModel, ContractOutputModel>(
                expression: "id = age*2; items = ids.map(toText);  Price = 42.1", 
                input: new UserInputModel("vasa", 13, ids:new []{1,2,3}));
            TestTools.AreSame(new ContractOutputModel {Id = 26, Items = new[] {"1", "2", "3"}, Price = 42.1}, result);
        }

        [Test]
        public void FullConstInitialization()
        {
            var result = Funny.CalcMany<UserInputModel,ContractOutputModel>(
                "id = 42; items = ['vasa','kate']; price = 42.1", new UserInputModel());
            Assert.AreEqual(42, result.Id);
            Assert.AreEqual(42.1, result.Price);
            CollectionAssert.AreEqual(new[]{"vasa","kate"}, result.Items);
        }
        [Test]
        public void NofieldsInitialized_throws() 
            => Assert.Throws<FunInvalidUsageTODOException>(()=>  
                Funny.CalcMany<UserInputModel,ContractOutputModel>("someField1 = age; somefield2 = 2", new UserInputModel()));
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
    }
}