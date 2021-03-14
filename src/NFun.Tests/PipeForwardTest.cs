using NFun;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class PipeForwardTest
    {
        [TestCase("y = 1.abs()",    1.0)]
        [TestCase("y = -1.abs()",  -1.0)]
        [TestCase("y = 1.max(2)",   2.0)]
        [TestCase("y = -2.max(1)", -2.0)]

        [TestCase(@"rr(x:real):bool = x>10
                     y = [11.0,20.0,1.0,2.0].filter(rr)", new[] { 11.0, 20.0 })]
        [TestCase(@"ii(x:int):bool = x>10
                     y = [11,20,1,2].filter(ii)", new[] { 11, 20 })]
        [TestCase(@"ii(x:int):int = x*x
                     y = [1,2,3].map(ii)", new[] { 1, 4, 9 })]

        [TestCase(@"f(x:int):bool = x>10
                     m(x:int):int  = x*x
                     y = [11,20,1,2] 
                            .filter(f)
                            .map(m)", new[] { 121, 400 })]
        [TestCase(@"f(x:int):bool = x>10
                     m(x:int):int  = x*x
                     y = [11,20,1,2] 
                            .filter(f)
                            .map(m)
                            .max()", 400)]
        [TestCase( @"
                     y = [11.0,20.0,1.0,2.0] 
                            . filter{it>10.0}
                            . map{it*it}
                            . max()", 400.0)]
        [TestCase("y = [1.0,2,3].max()",3.0)]

        [TestCase("  f(x:int):int = x*x \r y = 4.f()", 16)]
        [TestCase(" f(x:int):int = x+1 \r y = 4.f().f().f()", 7)]
        [TestCase("  f(x:int):int = x*x \r y = -(4.f())", -16)]
        [TestCase(" f(x:int):int = x*x \r y = 4.f() == f(4)", true)]
        public void ConstantSingleVariableTest(string expr, object expected)
        {
            var runtime = FunBuilder.Build(expr);
            runtime.Calculate()
                .AssertReturns(VarVal.New("y", expected));
        }
        [TestCase("x1 = 2; x2 = 1; y = -x1.max(x2)", -2.0)]
        public void ConstantTest(string expr, object yExpected)
        {
            var runtime = FunBuilder.Build(expr);
            runtime.Calculate()
                .AssertHas(VarVal.New("y", yExpected));
        }
        [TestCase(@"f(x:int):int = x*x y = 4.f)")]
        [TestCase(@"f(x:int):int = x*x y = 4.f( == f(4)")]
        [TestCase(@"f(x:int):int = x*x y = 4.f(,) == f(4)")]
        [TestCase(@"f(x:int):int = x*x y = 4.f(1) == f(4)")]
        [TestCase(@"f(x:int):int = x*x y = 4.f(1,2) == f(4)")]
        [TestCase(@"f(x:int):int = x*x y = .f()")]
        [TestCase(@"f(x:int):int = x*x y = 4*.f()")]
        [TestCase(@"y = f.4( == f(4)")]
        [TestCase(@"y = 4.f")]
        [TestCase(@"y = f.4")]
        [TestCase(@"y = f|")]
        [TestCase(@"y = [1,2,3].max")]
        public void ObviousFails(string expr)
        {
            try
            {
                FunBuilder.Build(expr);
                Assert.Fail("No parse error");
            }
            catch (FunParseException) { }
        }
    }
}