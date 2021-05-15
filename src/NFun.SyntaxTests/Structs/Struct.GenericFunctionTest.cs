using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.Structs
{
    public class StructGenericFunctionTest
    {
        [Test]
        public void CallGenericFunctionFieldAccess() =>
            ("f(x) = x.age;" +
             "x1= the{age = 12};" +
             "x2= the{age = true};" +
             "r = f(x1); b = f(x2);").AssertResultHas(("r", 12.0), ("b", true));

        [Test]
        [Ignore("Undefined behaviour")]
        public void CallToAllowedReqTypeDef() =>
             ("f(x) = x.age; y1 = f(user); y2 = f(user.child[0])").Calc();

        [Test]
        public void CallGenericFunctionFieldNegate() =>
            ("f(x) = -x.age;" +
             "x1= the{age = 12};" +
             "r = f(x1); ")
            .AssertResultHas("r",-12.0);

        [Test]
        public void CallGenericFunctionMultipleFieldOfArrayAccess_PipeForward() =>
            ("f(x) = x.a.concat(x.b);" +
             "x1= the{a = 'mama'; b = 'popo'};" +
             "t = f(x1);" +
             "iarr = f(the{a=[1,2,3]; b = [4,5,6]})")
            .AssertResultHas(("t", "mamapopo"),("iarr", new[]{1.0,2,3,4,5,6}));

        [Test]
        public void CallGenericFunctionMultipleFieldOfArrayAccess() =>
            ("f(x) = concat(x.a, x.b);" +
             "x1= the{a = 'mama'; b = 'popo'};" +
             "t = f(x1);" +
             "iarr = f(the{a=[1,2,3]; b = [4,5,6]})").AssertResultHas(("t", "mamapopo"),("iarr", new[]{1.0,2.0,3.0,4.0,5.0,6.0}));

        [Test]
        public void CallGenericFunctionMultipleFieldOfTextAccess() =>
            ("f(x) = concat(x.a, x.b);" +
             "t = f(the{a = 'mama'; b = 'popo'});").AssertResultHas("t", "mamapopo");

        [Test]
        public void CallGenericFunctionMultipleFieldOfRealArrayAccess() =>
            ("f(x) = concat(x.a, x.b);" +
             "x1= the{a = 'mama'; b = 'popo'};" +
             "iarr = f(the{a=[1,2,3]; b = [4,5,6]})").AssertResultHas("iarr", new[]{1.0,2.0,3.0,4.0,5.0,6.0});

        [Test]
        public void CallGenericFunctionMultipleFieldOfConcreteIntArrayAccess() =>
            ("f(x) = concat(x.a, x.b);" +
             "iarr:int[] = f(the{a=[1,2,3]; b = [4,5,6]})").AssertResultHas("iarr", new[]{1,2,3,4,5,6});

        [Test]
        public void CallGenericFunctionMultipleFieldOfConcreteRealsArrayAccess() =>
            ("f(x) = concat(x.a, x.b);" +
             "iarr:real[] = f( the{a=[1.0,2.0,3.0]; b = [4.0,5.0,6.0]} )").AssertResultHas("iarr", new[]{1.0,2,3,4,5,6});

        [Test]
        public void CallGenericFunctionSingleFieldOfConcreteRealsArrayAccess() =>
            ("f(x) = reverse(x.a);" +
             "y:real[] = f( the{a=[1.0,2.0]} )").AssertResultHas("y", new[]{2.0,1.0});

        [Test]
        public void CallGenericFunctionWithFieldIndexing() =>
            ("f(x) = x.item[1];" +
                       "x1= the{item = [1,2,3] };" +
                       "r = f(x1);").AssertResultHas("r", 2.0);

        [Test]
        public void CallGenericFunctionWithAdditionalFields() =>
            ("f(x) = x.size;" +
             "x1= the{age = 12; size = 24; name = 'vasa'};" +
             "r = f(x1);").AssertResultHas("r", 24.0);

        [Test]
        public void CallComplexFunction() =>
            ("f(x) = x[1].name;" +
             "a = [the{age = 42; name = 'vasa'}, the{age = 21; name = 'peta'}];" +
             "y = f(a);").AssertResultHas("y","peta");
    
        [Test]
        public void GenericStructFunctionReturn() =>
           ("f(x) = the{res = x}; "+
            "r = f(42).res;" +
            "txt = f('try').res").AssertResultHas(("r",42.0),("txt","try"));

        [Test]
        public void SingleGenericStructFunctionReturn() =>
            ("f(x) = the{res = x}; " +
             "r = f(42).res;").AssertResultHas("r", 42.0);

        [Test]
        public void SingleGenericStructFunction_WithConcrete_ReturnsCouncrete() =>
            ("f(x) = the{res = x}; " +
             "r = f(42.0).res;").AssertResultHas("r",42.0);

        [Test]
        public void ConstrainedGenericStructFunctionReturn() =>
            ("f(x) = the{twice = x+x; dec = x-1}; " +
                    "t = f(42).twice;" +
                    "d = f(123).dec").AssertResultHas(("t", 84.0),("d", 122.0));

        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void GenericFactorialReq_ReturnStruct(double x, double y) =>
            @"fact(n) = if(n<=1) the{res = 1} else the{res = fact(n-1).res*n}
                  y = fact(x).res".Calc("x",x).AssertReturns("y",y);

        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void GenericFactorialReq_ArgIsStruct(double x, double y) =>
            @"fact(n) = if(n.field<=1) 1 else fact(the{field=n.field-1})*n.field
                  y = fact(the{field=x})".Calc("x",x).AssertReturns("y",y);

        [TestCase("f(x) = x.a; y = f(the{nonExistField = 1})")]
        [TestCase("f(x) = x.a; y = f(the{})")]
        [TestCase("f(x) = x.a; y:bool = f(the{x = 1})")]
        [TestCase(@"fact(n) = if(n.field<=1) 1 else fact(the{field=n.field-1}) * n.field;
                  y = fact(the{a=x})")]
        [TestCase(@"f(n) = n.field;
                  y = fact(the{nonExistingField=x})")]
        [TestCase(@"fact(n) = if(n.field<=1) 1 else fact(the{field=n.field-1})*n.nonExistingField")]
        public void ObviousFails(string expr)=> expr.AssertObviousFailsOnParse();
    }
}