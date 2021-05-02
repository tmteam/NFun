using System;
using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class AddFunctionTest
    {
        class ModelWithInt{ public int id { get; set; }}

        class ComplexModel
        {
            public ModelWithInt a { get; set; }
            public ModelWithInt b { get; set; }
        } 
        [Test]
        public void Smoke()
        {
            var context = Funny
                .WithFunction("myHello", (int i) => $"Hello mr #{i}")
                .WithFunction("myInc", (int i) => i + 1)
                .CreateContextFor<ModelWithInt, string>();
            
            Func<ModelWithInt, string> lambda = context.Build("out = myHello(myInc(id))");

            var result =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result,"Hello mr #43");
            
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"Hello mr #2");
        }
        
        [Test]
        public void CompositeAccess()
        {
            var context = Funny
                .WithFunction("myHello", (int i) => $"Hello mr #{i}")
                .WithFunction("csumm", (ComplexModel m) => m.a.id+ m.b.id)
                .CreateContextFor<ModelWithInt, int>();
            
            Func<ModelWithInt, int> lambda = context.Build(
                @"csumm(
                            @the{
                                a= @{id= 10}
                                b= @{id= 20}
                            })");

            var result =  lambda(new ModelWithInt{id = 42});
            
            Assert.AreEqual(result,30);
        }
    }
}