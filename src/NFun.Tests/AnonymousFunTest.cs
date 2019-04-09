using NFun;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class AnonymousFunTest
    {
        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter(i => i>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter((i) => i>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11,20,1,2].filter((i:int) => i>10)",new[]{11,20})]
        [TestCase( @"y = [11,20,1,2].filter(i:int => i>10)",new[]{11,20})]
        [TestCase( @"y = map([1,2,3], i:int  =>i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1,2,3] . map(i:int=>i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1.0,2.0,3.0] . map(i=>i*i)",new[]{1.0,4.0,9.0})]
        [TestCase( @"y = [1.0,2.0,3.0] . fold((i,j)=>i+j)",6.0)]
        [TestCase( @"y = fold([1.0,2.0,3.0],(i,j)=>i+j)",6.0)]
        [TestCase( @"y = [1,2,3] . fold((i:int, j:int)=>i+j)",6)]
        [TestCase( @"y = fold([1,2,3],(i:int, j:int)=>i+j)",6)]
        [TestCase( "y = [1.0,2.0,3.0].any((i)=> i == 1.0)",true)]
        [TestCase( "y = [1.0,2.0,3.0].any((i)=> i == 0.0)",false)]
        [TestCase( "y = [1.0,2.0,3.0].all((i)=> i >0)",true)]
        [TestCase( "y = [1.0,2.0,3.0].all((i)=> i >1.0)",false)]
        [TestCase( "f(m:real[], p):bool = m.all((i)=> i>p) \r y = f([1.0,2.0,3.0],1.0)",false)]

        public void AnonymousFunctions_ConstantEquation(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            CollectionAssert.IsEmpty(runtime.Inputs,"Unexpected inputs on constant equations");
            runtime.Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        [TestCase( "y = [1.0,2.0,3.0].all((i)=> i >x)",1.0, false)]
        [TestCase( "z = x*2\r y = [1.0,2.0,3.0].all((i)=> i >z)",0.5, false)]
        [TestCase( "x:int\r y = [1,2,3].all((i)=> i >x)",1, false)]
        [TestCase( "y = [1.0,2.0,3.0].map((i)=> i*x1*x2)",3.0,4.0, new []{12.0,24.0,36.0})]
        [TestCase( "x1:int\rx2:int\ry = [1,2,3].map((i)=> i*x1*x2)",3,4, new []{12,24,36})]
        [TestCase( "y = [1.0,2.0,3.0].fold((i,j)=> i*x1 - j*x2)",2.0,3.0, 100.0)]
        public void AnonymousFunctions_TwoArgumentsEquation(string expr, double x1,double x2, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(
                    Var.New("x1", x1),
                    Var.New("x2", x2))
                .AssertReturns(0.00001, Var.New("y", expected));
        }

        [TestCase( "y = [1.0,2.0,3.0].all((i)=> i >x)",1.0, false)]
        [TestCase( "y = [1.0,2.0,3.0].map((i)=> i*x)",3.0, new []{3.0,6.0,9.0})]
        [TestCase( "y = [x,2.0,3.0].all((x)=> x >1.0)",1.0, false)]
        [TestCase( "x:bool \r y = x and ([1.0,2.0,3.0].all((x)=> x >=1.0))",1.0, true)]
        public void AnonymousFunctions_SingleArgumentEquation(string expr, double arg, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate(Var.New("x", arg))
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        [TestCase("y = [1.0].fold(((i,j)=>i+j)")]
        [TestCase("y = fold(((i,j),k)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i*2,j)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((2,j)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((j)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((j)=>j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i,j,k)=>i+j+k)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i,j)=>i+j+k)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i,j)=> k)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i)=>i)")]
        [TestCase("[1.0,2.0].map((i,i)=>i+1)")]
        [TestCase("[1.0,2.0].fold((i,i)=>i+1)")]
        [TestCase( "x:bool\r y = [1,2,3].all((i)=> i>x)")]
        [TestCase( "f(m:real[], p):bool = m.all((i)=> i>zzz) \r y = f([1.0,2.0,3.0],1.0)")]
        public void ObviouslyFailsOnParse(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
    }
}