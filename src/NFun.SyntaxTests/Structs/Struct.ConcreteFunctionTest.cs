using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.Structs
{
    public class StructConcreteFunctionsTest
    {
        [Test]
        public void CallConcreteFunctionFieldAccess() =>
            ("f(x):int = x.age;" +
             "x1= {age = 12};" +
             "r = f(x1); ").AssertResultHas("r", 12);

        [Test]
        public void CallConcreteFunctionFieldOfIntNegate() =>
            ("f(x):int = -x.age;" +
             "x1= {age = 12};" +
             "r = f(x1); ").AssertResultHas("r", -12);

        [Test]
        public void CallConcreteFunctionFieldOfBoolNot() =>
            ("f(x) = not x.hasPenis;" +
             "x1= {hasPenis = true};" +
             "r = f(x1); ").AssertResultHas("r", false);

        [Test]
        public void CallConcreteFunctionMultipleFieldOfI64Access() =>
            ("f(x):int64 = x.a + x.b;" +
             "x1= {a = 12; b = -1};" +
             "r = f(x1);").AssertResultHas("r", (long)11);

        [Test]
        public void CallConcreteFunctionMultipleFieldOfRealAccess() =>
            ("f(x):real = x.a + x.b;" +
             "x1= {a = 12; b = -1};" +
             "r = f(x1);").AssertResultHas("r", 11.0);

        [Test]
        public void CallConcreteFunctionMultipleFieldOfBoolAccess() =>
            ("f(x) = x.a and x.b;" +
             "x1= {a = true; b = true};" +
             "r = f(x1);").AssertResultHas("r", true);

        [Test]
        public void CallConcreteNestedFunctionSingleFieldOfTextAccess() =>
            ("f(x):text = x.a.m1.concat('popo');" +
             "x1 = {a = {m1='mama'}};" +
             "r = f(x1);").AssertResultHas("r", "mamapopo");

        [Test]
        public void CallConcreteFunctionMultipleFieldOfTextAccess() =>
            ("f(x):text = x.a.concat(x.b);" +
             "x1= {a = 'mama'; b = 'popo'};" +
             "r = f(x1);").AssertResultHas("r", "mamapopo");


        [Test]
        public void CallConcreteNestedFunctionMultipleFieldOfTextAccess() =>
            ("f(x):text = x.a.m1.concat(x.b.m2);" +
             "x1 = {a = {m1='mama'}; b = {m2='popo'}};" +
             "r = f(x1);").AssertResultHas("r", "mamapopo");

        [Test]
        public void CallConcreteFunctionManyFieldsAccess()
        {
            TraceLog.IsEnabled = true;
            ("f(x):int = x.age+ x.size;" +
             "x1= {age = 12; size = 24};" +
             "r = f(x1); ").AssertResultHas("r", 36);
        }

        [Test]
        public void CallConcreteFunctionWithAdditionalFields() =>
            ("f(x):int = x.size;" +
             "x1= {age = 12; size = 24; name = 'vasa'};" +
             "r = f(x1);").AssertResultHas("r", 24);


        [Test]
        public void ConcreteStructFunctionReturn() =>
            ("f(x:uint32) = {twice = x+x; dec = x-1}; " +
             "t = f(42).twice;" +
             "d = f(123).dec").AssertResultHas(("t", (uint)84), ("d", (uint)122));

        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void ConcreteFactorialReq_ReturnStruct(int x, int y) =>
            @"fact(n:int) = if(n<=1) {res = 1} else {res = fact(n-1).res*n }
                  y = fact(x).res".Calc("x", x).AssertReturns("y", y);

        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void ConcreteFactorialReq_ArgIsStruct(int x, int y) =>
            @"fact(n):int = if(n.field<=1) 1 else fact({field=n.field-1}) * n.field;
                  y = fact({field=x})".Calc("x", x).AssertReturns("y", y);

        [Test]
        public void SingleStructFunction_WithConcrete_ReturnsCouncrete() =>
            ("f(x:real) = {res = x}; " +
             "r = f(42.0).res;").AssertResultHas("r", 42.0);

        [Test]
        public void CallFunctionWithFieldIndexing() =>
            ("f(x):byte = x.item[1];" +
             "x1= {item = [1,2,3] };" +
             "r = f(x1);").AssertResultHas("r", (byte)2);

        [TestCase("f(x):int = x.a; y = f({x = true})")]
        [TestCase("f(x):int = x.a; y:bool = f({missing = 1})")]
        [TestCase("f(x):real = {res = x}")]
        [TestCase(@"fact(n):int = if(n.field<=1) 1 else fact({field=n.field-1}) * n.field;
                  y = fact({a=x})")]
        [TestCase(@"f(n):int = n.field;
                  y = fact({nonExistingField=x})")]
        public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();
    }
}