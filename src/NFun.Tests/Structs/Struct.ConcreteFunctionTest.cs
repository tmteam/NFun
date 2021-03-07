using NFun;
using NFun.Exceptions;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.Structs
{
    public class StructConcreteFunctionsTest
    {
        [Test]
        public void CallConcreteFunctionFieldAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):int = x.age;" +
                       "x1= @{age = 12};" +
                       "r = f(x1); ")
                .Calculate()
                .AssertHas(VarVal.New("r", 12));
        }
        [Test]
        public void CallConcreteFunctionFieldOfIntNegate()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):int = -x.age;" +
                       "x1= @{age = 12};" +
                       "r = f(x1); ")
                .Calculate()
                .AssertHas(VarVal.New("r", -12));
        }
        [Test]
        public void CallConcreteFunctionFieldOfBoolNot()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = not x.hasPenis;" +
                       "x1= @{hasPenis = true};" +
                       "r = f(x1); ")
                .Calculate()
                .AssertHas(VarVal.New("r", false));
        }
        [Test]
        public void CallConcreteFunctionMultipleFieldOfI64Access()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):int64 = x.a + x.b;" +
                       "x1= @{a = 12; b = -1};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", (long)11));
        }
        
        [Test]
        public void CallConcreteFunctionMultipleFieldOfRealAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):real = x.a + x.b;" +
                       "x1= @{a = 12; b = -1};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", 11.0));
        }
        
        [Test]
        public void CallConcreteFunctionMultipleFieldOfBoolAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = x.a and x.b;" +
                       "x1= @{a = true; b = true};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", true));
        }
        [Test]
        public void CallConcreteNestedFunctionSingleFieldOfTextAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):text = x.a.m1.concat('popo');" +
                       "x1 = @{a = @{m1='mama'}};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", "mamapopo"));
        }
        [Test]
        public void CallConcreteFunctionMultipleFieldOfTextAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):text = x.a.concat(x.b);" +
                       "x1= @{a = 'mama'; b = 'popo'};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", "mamapopo"));
        }
        
        
        [Test]
        public void CallConcreteNestedFunctionMultipleFieldOfTextAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):text = x.a.m1.concat(x.b.m2);" +
                       "x1 = @{a = @{m1='mama'}; b = @{m2='popo'}};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", "mamapopo"));
        }
        [Test]
        public void CallConcreteFunctionManyFieldsAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x):int = x.age+ x.size;" +
                       "x1= @{age = 12; size = 24};" +
                       "r = f(x1); ")
                .Calculate()
                .AssertHas(VarVal.New("r", 36));
        }

        [Test]
        public void CallConcreteFunctionWithAdditionalFields() =>
            FunBuilder
                .Build("f(x):int = x.size;" +
                       "x1= @{age = 12; size = 24; name = 'vasa'};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", 24));
        
        
        
        [Test]
        public void ConcreteStructFunctionReturn() =>
            FunBuilder
                .Build(
                    "f(x:uint32) = @{twice = x+x; dec = x-1}; " +
                    "t = f(42).twice;" +
                    "d = f(123).dec")
                .Calculate()
                .AssertHas(VarVal.New("t", (uint)84))
                .AssertHas(VarVal.New("d", (uint)122));
        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void ConcreteFactorialReq_ReturnStruct(int x, int y)
        {
            string text =
                @"fact(n:int) = if(n<=1) @{res = 1} else @{res = fact(n-1).res*n }
                  y = fact(x).res";
            var runtime = FunBuilder.Build(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }
        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void ConcreteFactorialReq_ArgIsStruct(int x, int y)
        {
            string text =
                @"fact(n):int = if(n.val<=1) 1 else fact(@{n=n.val-1}) * n.val;
                  y = fact(@{n=x})";
            var runtime = FunBuilder.Build(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }
        
        [Test]
        public void SingleStructFunction_WithConcrete_ReturnsCouncrete() =>
            FunBuilder
                .Build(
                    "f(x:real) = @{res = x}; " +
                    "r = f(42.0).res;")
                .Calculate()
                .AssertHas(VarVal.New("r", 42.0));
        [Test]
        public void CallFunctionWithFieldIndexing() =>
            FunBuilder
                .Build("f(x):byte = x.item[1];" +
                       "x1= @{item = [1,2,3] };" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", (byte)2));
        
        [TestCase("f(x):int = x.a; y = f(@{missing = 1})")]
        [TestCase("f(x):real = @{res = x}")]
        public void ObviousFails(string expr) 
            => Assert.Throws<FunParseException>(()=>FunBuilder.Build(expr));
    }
}