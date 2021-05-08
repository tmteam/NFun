using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcManyT
    {
        [Test]
        public void FullInitialization()
        {
            var result = Funny.CalcMany<ContractOutputModel>("id = 42; items = ['vasa','kate']; price = 42.1");
            Assert.AreEqual(42, result.Id);
            Assert.AreEqual(42.1, result.Price);
            CollectionAssert.AreEqual(new[]{"vasa","kate"}, result.Items);
        }

        [Test]
        public void OutputFieldIsConstCharArray() =>
            Assert.IsTrue(TestTools.AreSame(new ModelWithCharArray
            {
                Chars = new[] {'t', 'e', 's', 't'}
            }, Funny.CalcMany<ModelWithCharArray>("Chars = 'test'")));
        
        
        [Test]
        public void NofieldsInitialized_throws() 
            => Assert.Throws<FunInvalidUsageTODOException>(()=>  Funny.CalcMany<ContractOutputModel>("someField1 = 13.1; somefield2 = 2"));

        [Test]
        public void AnonymousEquation_throws() 
            => Assert.Throws<FunInvalidUsageTODOException>(()=> Funny.CalcMany<ContractOutputModel>("13.1"));

        [Test]
        public void UnknownInputIdUsed_throws() 
            => Assert.Throws<FunInvalidUsageTODOException>(()=> Funny.CalcMany<ContractOutputModel>("id = someInput"));
        
        [Test]
        public void SomeFieldInitialized_DefaultValuesInUninitalizedFields() {
            var result = Funny.CalcMany<ContractOutputModel>("id = 321; somenotExisted = 32");
            Assert.AreEqual(321, result.Id);
            Assert.AreEqual(new ContractOutputModel().Price, result.Price);
            CollectionAssert.AreEqual(new ContractOutputModel().Items, result.Items);
        }
    }
}