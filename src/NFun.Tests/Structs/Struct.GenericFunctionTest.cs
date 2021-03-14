using NFun;
using NFun.Exceptions;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.Structs
{
    public class StructGenericFunctionTest
    {
        [Test]
        public void CallGenericFunctionFieldAccess() =>
            FunBuilder
                .Build( "f(x) = x.age;" +
                        "x1= @{age = 12};" +
                        "x2= @{age = true};" +
                        "r = f(x1); b = f(x2);")
                .Calculate()
                .AssertHas(VarVal.New("r",12.0))
                .AssertHas(VarVal.New("b",true));
        [Test]
        public void CallGenericFunctionFieldNegate()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = -x.age;" +
                       "x1= @{age = 12};" +
                       "r = f(x1); ")
                .Calculate()
                .AssertHas(VarVal.New("r", -12.0));
        }
        [Test]
        public void CallGenericFunctionMultipleFieldOfArrayAccess_PipeForward()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = x.a.concat(x.b);" +
                       "x1= @{a = 'mama'; b = 'popo'};" +
                       "t = f(x1);" +
                       "iarr = f(@{a=[1,2,3]; b = [4,5,6]})")
                .Calculate()
                .AssertHas(VarVal.New("t", "mamapopo"))
                .AssertHas(VarVal.New("iarr", new[]{1.0,2,3,4,5,6}));

        }
        
        [Test]
        public void CallGenericFunctionMultipleFieldOfArrayAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = concat(x.a, x.b);" +
                       "x1= @{a = 'mama'; b = 'popo'};" +
                       "t = f(x1);" +
                       "iarr = f(@{a=[1,2,3]; b = [4,5,6]})")
                .Calculate()
                .AssertHas(VarVal.New("t", "mamapopo"))
                .AssertHas(VarVal.New("iarr", new[]{1.0,2.0,3.0,4.0,5.0,6.0}));

        }
        
        [Test]
        public void CallGenericFunctionMultipleFieldOfTextAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = concat(x.a, x.b);" +
                       "t = f(@{a = 'mama'; b = 'popo'});")
                .Calculate()
                .AssertHas(VarVal.New("t", "mamapopo"));

        }
        [Test]
        public void CallGenericFunctionMultipleFieldOfRealArrayAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = concat(x.a, x.b);" +
                       "x1= @{a = 'mama'; b = 'popo'};" +
                       "iarr = f(@{a=[1,2,3]; b = [4,5,6]})")
                .Calculate()
                .AssertHas(VarVal.New("iarr", new[]{1.0,2.0,3.0,4.0,5.0,6.0}));

        }

        [Test]
        public void CallGenericFunctionMultipleFieldOfConcreteIntArrayAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = concat(x.a, x.b);" +
                       "iarr:int[] = f(@{a=[1,2,3]; b = [4,5,6]})")
                .Calculate()
                .AssertHas(VarVal.New("iarr", new[]{1,2,3,4,5,6}));
        }
        
        [Test]
        public void CallGenericFunctionMultipleFieldOfConcreteRealsArrayAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = concat(x.a, x.b);" +
                       "iarr:real[] = f( @{a=[1.0,2.0,3.0]; b = [4.0,5.0,6.0]} )")
                .Calculate()
                .AssertHas(VarVal.New("iarr", new[]{1.0,2,3,4,5,6}));
        }
        [Test]
        public void CallGenericFunctionSingleFieldOfConcreteRealsArrayAccess()
        {
            TraceLog.IsEnabled = true;
            FunBuilder
                .Build("f(x) = reverse(x.a);" +
                       "y:real[] = f( @{a=[1.0,2.0]} )")
                .Calculate()
                .AssertHas(VarVal.New("y", new[]{2.0,1.0}));
        }

        [Test]
        public void CallGenericFunctionWithFieldIndexing() =>
            FunBuilder
                .Build("f(x) = x.item[1];" +
                       "x1= @{item = [1,2,3] };" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", 2.0));

        [Test]
        public void CallGenericFunctionWithAdditionalFields() =>
            FunBuilder
                .Build("f(x) = x.size;" +
                       "x1= @{age = 12; size = 24; name = 'vasa'};" +
                       "r = f(x1);")
                .Calculate()
                .AssertHas(VarVal.New("r", 24.0));

        
        [Test]
        public void CallComplexFunction() =>
            FunBuilder
                .Build( "f(x) = x[1].name;" +
                        "a = [@{age = 42; name = 'vasa'}, @{age = 21; name = 'peta'}];" +
                        "y = f(a);")
                .Calculate()
                .AssertHas(VarVal.New("y","peta"));

        
        
        [Test]
        public void GenericStructFunctionReturn() =>
            FunBuilder
                .Build( 
                    "f(x) = @{res = x}; "+
                    "r = f(42).res;" +
                    "txt = f('try').res")
                .Calculate()
                .AssertHas(VarVal.New("r",42.0))
                .AssertHas(VarVal.New("txt","try"));

        [Test]
        public void SingleGenericStructFunctionReturn() =>
            FunBuilder
                .Build(
                    "f(x) = @{res = x}; " +
                    "r = f(42).res;")
                .Calculate()
                .AssertHas(VarVal.New("r", 42.0));

        [Test]
        public void SingleGenericStructFunction_WithConcrete_ReturnsCouncrete()
        {
            TraceLog.IsEnabled = true;
            
            FunBuilder
                .Build(
                    "f(x) = @{res = x}; " +
                    "r = f(42.0).res;")
                .Calculate()
                .AssertHas(VarVal.New("r", 42.0));
        }

        [Test]
        public void ConstrainedGenericStructFunctionReturn() =>
            FunBuilder
                .Build(
                    "f(x) = @{twice = x+x; dec = x-1}; " +
                    "t = f(42).twice;" +
                    "d = f(123).dec")
                .Calculate()
                .AssertHas(VarVal.New("t", 84.0))
                .AssertHas(VarVal.New("d", 122.0));

        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void GenericFactorialReq_ReturnStruct(int x, double y)
        {
            string text =
                @"fact(n) = if(n<=1) @{res = 1} else @{res = fact(n-1).res*n}
                  y = fact(x).res";
            var runtime = FunBuilder.Build(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }
        [TestCase(1, 1)]
        [TestCase(3, 6)]
        [TestCase(6, 720)]
        public void GenericFactorialReq_ArgIsStruct(int x, double y)
        {
            string text =
                @"fact(n) = if(n.field<=1) 1 else fact(@{field=n.field-1})*n.field
                  y = fact(@{field=x})";
            var runtime = FunBuilder.Build(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }

        [TestCase("f(x) = x.a; y = f(@{nonExistField = 1})")]
        [TestCase("f(x) = x.a; y = f(@{})")]

        [TestCase(@"fact(n) = if(n.field<=1) 1 else fact(@{field=n.field-1}) * n.field;
                  y = fact(@{a=x})")]
        [TestCase(@"f(n) = n.field;
                  y = fact(@{nonExistingField=x})")]
        [TestCase(@"fact(n) = if(n.field<=1) 1 else fact(@{field=n.field-1})*n.nonExistingField")]
        public void ObviousFails(string expr) 
            => Assert.Throws<FunParseException>(()=>FunBuilder.Build(expr));
    }
}