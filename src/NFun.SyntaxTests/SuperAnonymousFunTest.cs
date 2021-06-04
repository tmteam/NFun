using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    [TestFixture]
    public class SuperAnonymousFunTest
    {
        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter (rule it>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter (rule it>10)",new[]{11.0,20.0})]
        [TestCase(@"y = [11.0,20.0,1.0,2.0].filter  (rule it>10)", new[] { 11.0, 20.0 })]

        [TestCase( @"y = [11.0,20.0,1.0,2.0].filter((rule it>10))",new[]{11.0,20.0})]
        [TestCase(@"y = filter([11.0,20.0,1.0,2.0], (rule it>10))", new[] { 11.0, 20.0 })]
        [TestCase(@"y = filter([11.0,20.0,1.0,2.0], rule it>10) ", new[] { 11.0, 20.0 })]

        [TestCase(@"y:int[] = [11,20,1,2].filter(rule it>10)", new[]{11,20})]
        [TestCase( @"y:int[] = map([1,2,3], (rule it*it))",new[]{1,4,9})]
        [TestCase( @"y:int[] = [1,2,3].map(rule it*it)",new[]{1,4,9})]
        [TestCase( @"y = [1.0,2.0,3.0] . map((rule it*it))",new[]{1.0,4.0,9.0})]
        [TestCase( @"y = [1.0,2.0,3.0] . fold(rule it1+it2)",6.0)]
        [TestCase(@"y = fold([1.0,2.0,3.0],(rule it1+it2))", 6.0)]

        [TestCase(@"y = [1,2,3].fold(rule it1+it2)", 6.0)]
        [TestCase( "y = [1.0,2.0,3.0].any (rule it==1.0)",true)]
        [TestCase( "y = [1.0,2.0,3.0].any (rule it == 0.0)",false)]
        [TestCase( "y = [1.0,2.0,3.0].all (rule it >0)",true)]
        [TestCase( "y = [1.0,2.0,3.0].all (rule it >1.0)",false)]
        [TestCase( "f(m:real[], p):bool = m.all(rule it>p) \r y = f([1.0,2.0,3.0],1.0)",false)]

        [TestCase("y = [-7,-2,0,1,2,3].filter (rule it>0)", new[] { 1.0, 2.0, 3.0 })]
        [TestCase("y = [-1,-2,0,1,2,3].filter (rule it>0) .fold(rule it1+it2)", 6.0 )]
        [TestCase("y = [-1,-2,0,1,2,3].filter (rule it>0).filter(rule it>2)", new[]{3.0})]
        [TestCase("y:int[] = [-1,-2,0,1,2,3].filter (rule it>0).map(rule it*it).map(rule it*it)", new[]{1,16,81})]
        [TestCase("y = [-1,-2,0,1,2,3].filter (rule it>0).map(rule it*it).map(rule it*it)", new[]{1.0,16.0,81.0})]

        [TestCase("y = [-1,-2,0,1,2,3].filter(rule it>0).fold(rule it1+it2)", 6.0 )]
        [TestCase("y = [-1,-2,0,1,2,3].filter(rule it>0).filter(rule it>2)", new[]{3.0})]
        [TestCase("y = [-1,-2,0,1,2,3].filter(rule it>0).map(rule it*it).map(rule it*it)", new[]{1.0,16.0,81.0})]

        [TestCase("y:int[] = [-1,-2,0,1,2,3].filter(rule it>0).map(rule it*it).map(rule it*it)", new[]{1,16,81})]
        [TestCase("y = [-1,-2,0,1,2,3].filter(rule it>0).filter(rule it>2)", new[]{3.0})]
        [TestCase("y:int = [-1,-2,0,1,2,3].filter(rule it>0).fold(rule it1+it2)", 6 )]
        [TestCase("y:real = [-1,-2,0,1,2,3].filter(rule it>0).fold(rule it1+it2)", 6.0)]

        [TestCase(@"y = [[1,2],[3,4],[5,6]].map(rule  it.map(rule it+1).sum())", new[]{5.0,9,13})]
        [TestCase(@"y = [[1,2],[3,4],[5,6]].fold(-10, rule it1+ it2.sum())", 11.0)]
        [TestCase(@"y = (rule it+1)(3.0)", 4.0)]
        [TestCase(@"f = (rule it+1); y = f(3.0)", 4.0)]
        [TestCase(@"f = ((rule it+1)); y = f(3.0)", 4.0)]
        [TestCase(@"y = ((rule it+1))(3.0)", 4.0)]
        [TestCase(@"y = (((rule it+1)))(3.0)", 4.0)]

        [TestCase(@"car3(g) = g(2); y = car3((rule it-1))   ", 1.0)]
        [TestCase(@"car4(g) = g(2); y =   car4(rule it)   ", 2.0)]
        [TestCase(@"car41(g) = g(2); y =   car41 (rule it)   ", 2.0)]
        [TestCase(@"car4(g) = g(2); y =   car4((rule it))   ", 2.0)]

        [TestCase(@"call5(f, x) = f(x); y = call5((rule it+1),  1)", 2.0)]
        [TestCase(@"call6(f, x) = f(x); y = call6((rule it+1.0), 1.0)", 2.0)]

        [TestCase(@"call8(f) = (rule f(it)); y = call8((rule it+1))(2)", 3.0)]
        [TestCase(@"call9(f) = (rule f(it)); y = ((rule it+1)).call9()(2)", 3.0)]

        [TestCase(@"call10(f,x) = (rule f(x,it)); y =  max.call10(3)(2)", 3.0)]
        [TestCase(@"call11() = rule it; y =  call11()(2)", 2.0)]
        [TestCase(@"call12 = (rule it); y =  call12(2)", 2.0)]
        [TestCase("ids:int[]=[1,2,3,4]; age:int = 1;  ;y:int[] = ids.filter(rule it>age).map(rule it+1)",new int[]{3,4,5})]
        public void AnonymousFunctions_ConstantEquation(string expr, object expected)
        {
            var runtime = expr.Build();
            CollectionAssert.IsEmpty(runtime.Inputs,"Unexpected inputs on constant equations");
            runtime.Calc().AssertResultHas("y", expected);
        }

        [TestCase( "y = [1.0,2.0,3.0].map(rule it*x1*x2)",3.0,4.0, new []{12.0,24.0,36.0})]
        [TestCase( "x1:int\rx2:int\ry = [1,2,3].map(rule it*x1*x2)",3,4, new []{12,24,36})]
        [TestCase( "y = [1.0,2.0,3.0].fold(rule it1*x1 - it2*x2)",2.0,3.0, -17.0)]
        [TestCase( "y = [1.0,2.0,3.0].fold(rule it1*x1 - it2*x2)",3.0,4.0, -27.0)]
        [TestCase( "y = [1.0,2.0,3.0].fold(rule it1*x1 - it2*x2)",0.0,0.0, 0.0)]
        public void AnonymousFunctions_TwoArgumentsEquation(string expr, object x1,object x2, object expected) =>
            expr.Calc(("x1", x1), ("x2", x2)).AssertReturns("y", expected);

        [TestCase( "y = [1.0,2.0,3.0].all (rule it >x)",1.0, false)]
        [TestCase( "y = [1.0,2.0,3.0].map (rule it*x)",3.0, new []{3.0,6.0,9.0})]
        [TestCase( "y = [1.0,2.0,3.0].all (rule it >x)",1.0, false)]
        [TestCase( "x:int\r y = [1,2,3].all (rule it >x)",1, false)]
        [TestCase( @"y = [1.0,2.0,3.0].fold(rule x)",123.0, 123.0)]
        [TestCase( @"y = [1.0,2.0,3.0].fold(rule it1+it2+x)",2.0,10.0)]
        [TestCase(@"y = [1.0,2.0,3.0].fold(rule it2+x)", 2.0, 5.0)]
        [TestCase(@"y = [1.0,2.0,3.0].fold(rule it1+x)", 2.0, 5.0)]
        [TestCase(@"y = [[1,2],[3,4],[5,6]].map(rule it.map(rule it+x).sum()).sum()", 1.0, 27.0)]

        public void AnonymousFunctions_SingleArgumentEquation(string expr, object arg, object expected) => 
            expr.Calc("x", arg).AssertReturns("y", expected);

        [TestCase( "z = x*2\r y = [1.0,2.0,3.0].map(rule it*z)",2.0, new[]{4.0,8.0, 12.0}, 4.0)]
        [TestCase( "z = x*2\r y = [1.0,2.0,3.0].map(rule it*z)",1.0, new[]{2.0,4.0, 6.0}, 2.0)]
        public void AnonymousFunctions_SingleArgument_twoEquations(string expr, double arg, object yExpected, object zExpected) => 
            expr.Calc("x", arg).AssertReturns(("y", yExpected), ("z", zExpected));

        [TestCase("y = [1.0].fold(rule (it1+it2)")]
        [TestCase("y = [1.0].fold(rule It1+it2))")]
        [TestCase("y = [1.0].map(rule It))")]
        [TestCase("y = [1.0].map(rule It1))")]
        [TestCase("y = it")]
        [TestCase("y = it2")]
        [TestCase("y = it1")]
        [TestCase("y = it3")]
        [TestCase("it = x")]
        [TestCase("it1 = x")]
        [TestCase("it2 = x")]
        [TestCase("it3 = x")]
        [TestCase("y = [1.0].fold (it1+it2))")]
        [TestCase("y = [1.0].fold(rule it+it2)")]
        [TestCase("y = [1.0].fold(rule it1+it)")]
        [TestCase("y = [1.0].fold(rule it)")]

        [TestCase("y = [1.0].fold(rule (it1+it2+it3)")]

        [TestCase("y = fold(rule (x) it1+it2)")]
        [TestCase("[1.0,2.0].map(rule it1*it1)")]
        [TestCase("[1.0,2.0].map(rule it1*it)")]
        [TestCase( "x:bool\r y = [1,2,3].all(rule ))")]

        [TestCase( "x:bool\r y = [1,2,3].all(rule i>x))")]
        [TestCase( "x:bool\r y = [1,2,3].all(i>x rule ))")]
        [TestCase( "f(m:real[], p):bool = m.all(rule it>zzz) \r y = f([1.0,2.0,3.0],1.0)")]
        [TestCase( "f(m:real[], p):bool = m.all((rule it>zzz}) \r y = f([1.0,2.0,3.0],1.0)")]
        [TestCase( "x:bool \r y = x and ([1.0,2.0,3.0].all({it >=1.0))")]
        [TestCase( "y = [-x,-x,-x].all(it < 0.0)")]
        [TestCase( "z = [-x,-x,-x] \r  y = z.all(z < 0.0)")]
        [TestCase( "y = [x,2.0,3.0].all((rule it >1.0)")]
        [TestCase("y:int[] = [-1,-2,0,1,2,3].filter (rule it>0).map(rule it1*it2).map(rule it1*it2)")]
        [TestCase("y = [-1,-2,0,1,2,3].filter (rule it>0).map(rule it1*it2).map(rule it1*it2)")]
        public void ObviouslyFailsOnParse(string expr) => expr.AssertObviousFailsOnParse();
    }
}