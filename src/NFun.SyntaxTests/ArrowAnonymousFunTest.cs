using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    [TestFixture]
    public class ArrowAnonymousFunTest
    {
        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter(i -> i>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter((i) -> i>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11,20,1,2].filter((i:int) -> i>10)",new[]{11,20})]
        [TestCase( @"y = [11,20,1,2].filter(i:int -> i>10)",new[]{11,20})]
        [TestCase( @"y = map([1,2,3], i:int  ->i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1,2,3] . map(i:int->i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1.0,2.0,3.0] . map(i->i*i)",new[]{1.0,4.0,9.0})]
        [TestCase( @"y = [1.0,2.0,3.0] . fold((i,j)->i+j)",6.0)]
        [TestCase( @"y = fold([1.0,2.0,3.0],(i,j)->i+j)",6.0)]
        [TestCase( @"y = [1,2,3] . fold((i:int, j:int)->i+j)",6)]
        [TestCase( @"y = fold([1,2,3],(i:int, j:int)->i+j)",6)]
        [TestCase( "y = [1.0,2.0,3.0].any((i)-> i == 1.0)",true)]
        [TestCase( "y = [1.0,2.0,3.0].any((i)-> i == 0.0)",false)]
        [TestCase( "y = [1.0,2.0,3.0].all((i)-> i >0)",true)]
        [TestCase( "y = [1.0,2.0,3.0].all((i)-> i >1.0)",false)]
        [TestCase( "f(m:real[], p):bool = m.all((i)-> i>p) \r y = f([1.0,2.0,3.0],1.0)",false)]

        [TestCase("y = [-7,-2,0,1,2,3].filter({it>0})", new[] { 1.0, 2.0, 3.0 })]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).fold((i,j)-> i+j)", 6.0 )]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).filter(i->i>2)", new[]{3.0})]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).map(i->i*i).map(i:int->i*i)", new[]{1,16,81})]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).map(i->i*i).map(i->i*i)", new[]{1.0,16.0,81.0})]

        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).fold((a,b)-> a+b)", 6.0 )]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).filter(a->a>2)", new[]{3.0})]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).map(a->a*a).map(b->b*b)", new[]{1.0,16.0,81.0})]

        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).map(a->a*a).map(b:int->b*b)", new[]{1,16,81})]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).filter(a:int->a>2)", new[]{3})]
        [TestCase("y = [-1,-2,0,1,2,3].filter({it>0}).fold((a:int,b)-> a+b)", 6 )]
        [TestCase(@"car3(g) = g(2);   y = car3(x->x-1)   ", 1.0)]
        [TestCase(@"car4(g) = g(2);   y = car4{it}   ", 2.0)]
        [TestCase(@"call5(f, x) = f(x); y = call5(x->x+1,  1)", 2.0)]
        [TestCase(@"call6(f, x) = f(x); y = call6(x->x+1.0, 1.0)", 2.0)]
        [TestCase(@"call7(f, x) = f(x); y = call7(((x:real)->x+1.0), 1.0)", 2.0)]
        [TestCase(@"call8(f) = i->f(i); y = call8(x->x+1)(2)", 3.0)]
        [TestCase(@"call9(f) = i->f(i); y = (x->x+1).call9()(2)", 3.0)]
        [TestCase(@"mult(x)=y->z->x*y*z;    y = mult(2)(3)(4)", 24.0)]
        [TestCase(@"mult()= x->y->z->x* y*z; y = mult()(2)(3)(4)", 24.0)]
        [TestCase(@"y = (x->x+1)(3.0)", 4.0)]
        [TestCase(@"f = x->x+1; y = f(3.0)", 4.0)]
        [TestCase(@"f = a->b->a+b; y = f(3.0)(5.0)", 8.0)]
        public void AnonymousFunctions_ConstantEquation(string expr, object expected)
        {
            var runtime = expr.Build();
            CollectionAssert.IsEmpty(runtime.Inputs,"Unexpected inputs on constant equations");
            runtime.Calc().AssertResultHas("y", expected);
        }
        
        [TestCase( "y = [1.0,2.0,3.0].map((i)-> i*x1*x2)",3.0,4.0, new []{12.0,24.0,36.0})]
        [TestCase( "x1:int\rx2:int\ry = [1,2,3].map((i:int)-> i*x1*x2)",3,4, new []{12,24,36})]
        [TestCase( "y = [1.0,2.0,3.0].fold((i,j)-> i*x1 - j*x2)",2.0,3.0, -17.0)]
        [TestCase( "y = [1.0,2.0,3.0].fold((i,j)-> i*x1 - j*x2)",3.0,4.0, -27.0)]
        [TestCase( "y = [1.0,2.0,3.0].fold((i,j)-> i*x1 - j*x2)",0.0,0.0, 0.0)]
        public void AnonymousFunctions_TwoArgumentsEquation(string expr, object x1,object x2, object expected) => 
            expr.Calc(("x1", x1), ("x2", x2)).AssertReturns("y", expected);

        [TestCase( "y = [1.0,2.0,3.0].all((i)-> i >x)",1.0, false)]
        [TestCase( "y = [1.0,2.0,3.0].map((i)-> i*x)",3.0, new []{3.0,6.0,9.0})]
        [TestCase( "y = [1.0,2.0,3.0].all((i)-> i >x)",1.0, false)]
        [TestCase( "x:int\r y = [1,2,3].all((i:int)-> i >x)",1, false)]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i,j)-> x)",123.0, 123.0)]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i,j)->i+j+x)",2.0,10.0)]
        public void AnonymousFunctions_SingleArgumentEquation(string expr, object arg, object expected) => 
            expr.Calc(("x", arg)).AssertReturns("y", expected);

        [TestCase( "z = x*2\r y = [1.0,2.0,3.0].map((i)-> i*z)",2.0, new[]{4.0,8.0, 12.0}, 4.0)]
        [TestCase( "z = x*2\r y = [1.0,2.0,3.0].map((i)-> i*z)",1.0, new[]{2.0,4.0, 6.0}, 2.0)]
        public void AnonymousFunctions_SingleArgument_twoEquations(string expr, double arg, object yExpected, object zExpected) =>
            expr.Calc("x", arg).AssertReturns(("y", yExpected), ("z", zExpected));

        [TestCase("y = [1.0].fold(((i,j)->i+j)")]
        [TestCase("y = fold(((i,j),k)->i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i*2,j)->i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((2,j)->i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((j)->i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((j)->j)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i,j,k)->i+j+k)")]
        [TestCase( @"y = [1.0,2.0,3.0].fold((i)->i)")]
        [TestCase("[1.0,2.0].map((i,i)->i+1)")]
        [TestCase("[1.0,2.0].fold((i,i)->i+1)")]
        [TestCase( "x:bool\r y = [1,2,3].all((i)-> i>x)")]
        [TestCase( "f(m:real[], p):bool = m.all((i)-> i>zzz) \r y = f([1.0,2.0,3.0],1.0)")]
        [TestCase( "x:bool \r y = x and ([1.0,2.0,3.0].all((x)-> x >=1.0))")]
        [TestCase( "y = [-x,-x,-x].all((x)-> x < 0.0)")]
        [TestCase( "z = [-x,-x,-x] \r  y = z.all((z)-> z < 0.0)")]
        [TestCase( "y = [x,2.0,3.0].all((x)-> x >1.0)")]
        public void ObviouslyFailsOnParse(string expr) => expr.AssertObviousFailsOnParse();

    }
}