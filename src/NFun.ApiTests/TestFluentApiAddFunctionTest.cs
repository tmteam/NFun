using System;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestFluentApiAddFunctionTest
    {
     
        [Test]
        public void SingleVariableFunction()
        {
            var context = Funny
                .WithFunction("myHello", (int i) => $"Hello mr #{i}")
                .WithFunction("myInc", (int i) => i + 1)
                .ForCalc<ModelWithInt, string>();
            
            Func<ModelWithInt, string> lambda = context.Build("out = myHello(myInc(id))");

            var result =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result,"Hello mr #43");
            
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"Hello mr #2");
        }
        [Test]
        public void TwoVariablesFunction()
        {
            var context = Funny
                .WithFunction("totxt", (int i1,int i2) => $"{i1},{i2}")
                .ForCalc<ModelWithInt, string>();
            
            var lambda = context.Build("totxt(id,1)");

            var result1 =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result1,"42,1");
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"1,1");
        }
        [Test]
        public void ThreeVariablesFunction()
        {
            var context = Funny
                .WithFunction("totxt", 
                    (int i1,int i2,int i3) => $"{i1},{i2},{i3}")
                .ForCalc<ModelWithInt, string>();
            
            var lambda = context.Build("totxt(id,2,3)");

            var result1 =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result1,"42,2,3");
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"1,2,3");
        }
        [Test]
        public void FourVariablesFunction()
        {
            var context = Funny
                .WithFunction("totxt", 
                    (int i1,int i2,int i3,int i4) => $"{i1},{i2},{i3},{i4}")
                .ForCalc<ModelWithInt, string>();
            
            var lambda = context.Build("totxt(id,2,3,4)");

            var result1 =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result1,"42,2,3,4");
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"1,2,3,4");
        }
        [Test]
        public void FiveVariablesFunction()
        {
            var context = Funny
                .WithFunction("totxt", 
                    (int i1,int i2,int i3,int i4, int i5) => $"{i1},{i2},{i3},{i4},{i5}")
                .ForCalc<ModelWithInt, string>();
            
            var lambda = context.Build("totxt(id,2,3,4,5)");

            var result1 =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result1,"42,2,3,4,5");
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"1,2,3,4,5");
        }
        [Test]
        public void SixVariablesFunction()
        {
            var context = Funny
                .WithFunction("totxt", 
                    (int i1,int i2,int i3,int i4, int i5,int i6) => $"{i1},{i2},{i3},{i4},{i5},{i6}")
                .ForCalc<ModelWithInt, string>();
            
            var lambda = context.Build("totxt(id,2,3,4,5,6)");

            var result1 =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result1,"42,2,3,4,5,6");
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"1,2,3,4,5,6");
        }
        [Test]
        public void SevenVariablesFunction()
        {
            var context = Funny
                .WithFunction("totxt", 
                    (int i1,int i2,int i3,int i4, int i5,int i6,int i7) => $"{i1},{i2},{i3},{i4},{i5},{i6},{i7}")
                .ForCalc<ModelWithInt, string>();
            
            var lambda = context.Build("totxt(id,2,3,4,5,6,7)");

            var result1 =  lambda(new ModelWithInt{id = 42});
            Assert.AreEqual(result1,"42,2,3,4,5,6,7");
            var result2 =  lambda(new ModelWithInt{id = 1});
            Assert.AreEqual(result2,"1,2,3,4,5,6,7");
        }
        [Test]
        public void CompositeAccess()
        {
            var context = Funny
                .WithFunction("myHello", (int i) => $"Hello mr #{i}")
                .WithFunction("csumm", (ComplexModel m) => m.a.id+ m.b.id)
                .ForCalc<ModelWithInt, int>();
            
            var lambda = context.Build(
                @"csumm(
                            the{
                                a= the{id= 10}
                                b= the{id= 20}
                            })");

            var result =  lambda(new ModelWithInt{id = 42});
            
            Assert.AreEqual(result,30);
        }
    }
}