using System;
using System.Linq;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect
{
    public abstract class GenericIntConstantTestBase<T>
    {
        private ClassicDialectSettings _dialect;

        protected GenericIntConstantTestBase(IntegerPreferedType prefered)
        {
            _dialect = Dialects.ModifyClassic(integerPreferedType: prefered);
        }

        [TestCase("y = 2+3", 5)]
        [TestCase("y = -2+-4", -6)]
        [TestCase("y = -(1+2)", -3)]
        [TestCase("y = -2*(-4+2)", 4)]
        [TestCase("y = 2*3", 6)]
        [TestCase("y = -2*-4", 8)]
        [TestCase("y = -1", -1)]
        [TestCase("y = -(-1)", 1)]
        [TestCase("y = -(-(-1))", -1)]
        [TestCase("y = 2-3", -1)]
        [TestCase("y = -1", -1)]
        [TestCase("y = -(-1)", 1)]
        [TestCase("y = -(-(-1))", -1)]
        [TestCase("y = {age = 42; name = 'vasa'}.age", 42)]
        [TestCase("a = {b = 1}; y = -(-a.b)", 1)]
        [TestCase("a = {b = 1}; y = -a.b", -1)]
        [TestCase("y = { a1 = {b2 = [1,2,3]}}.a1.b2[1]", 2)]
        [TestCase("y = { b = [1,2,3]}.b[1]", 2)]
        [TestCase("a1 = {af1_24 = 24; af2_1=1}; " +
                  "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
                  "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1", 26)]
        [TestCase("a1 = {af1_24 = 24; af2_1=1}; " +
                  "b2 = {bf2_1 = 1}; " +
                  "c3 = {cf1_1 = b2.bf2_1; cf2_24 = 24}; " +
                  "e4 = {ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
                  "y = a1.af1_24 + 1 + c3.cf2_24 + e4.ef4_24", 73)]
        [TestCase("y = 2", 2)]
        [TestCase("y = 1111  ", 1111)]
        [TestCase("y = 11_11  ", 1111)]
        [TestCase(@"y = [[1,2],[3,4],[5,6]].fold(-10, fun it1+ it2.sum())", 11)]
        [TestCase(@"car3(g) = g(2); y = car3((fun it-1))   ", 1)]
        [TestCase(@"car4(g) = g(2); y =   car4(fun it)   ", 2)]
        [TestCase(@"car41(g) = g(2); y =   car41 (fun it)   ", 2)]
        [TestCase("y = [1,2,3,4].fold(fun it1+it2)", 10)]
        [TestCase("y = [1,2,3,4].fold(0,(fun it1+it2))", 10)]
        [TestCase("y = [1,2,3,4].fold(-10,(fun it1+it2))", 0)]
        [TestCase("y = median([1,-10,0])", 0)]
        [TestCase(@"choose(f1, f2,  selector, arg1, arg2) = if(selector) f1(arg1,arg2) else f2(arg1,arg2); 
                   y =  choose(max, min, true, 1,2)", 2)]
        [TestCase("first(a) = a[0]\r y = [5,4,3].first()", 5)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)", 1)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,false)", 2)]
        [TestCase(@"y = [[1,2],[3,4],[5,6]].fold(-10, fun it1+ it2.sum())", 11)]
        [TestCase(@"mult(x)= fun(y)=fun(z)=x*y*z;    y = mult(2)(3)(4)", 24)]
        public void ConstantCalcReturnsTargetType(string expression, int expected)
            => Calc(expression).AssertResultHas("y", Convert(expected));

        [TestCase("y = 0.2", 0.2)]
        [TestCase("y = 11.222  ", 11.222)]
        [TestCase("y = 1.1_11  ", 1.111)]
        [TestCase(@"f = (fun it+1); y = f(3.0)", 4.0)]
        [TestCase(@"f = ((fun it+1)); y = f(3.0)", 4.0)]
        [TestCase(@"y = ((fun it+1))(3.0)", 4.0)]
        [TestCase(@"y = (((fun it+1)))(3.0)", 4.0)]
        [TestCase("y=median([1.0,10.5,6.0])", 6.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2.0,true)", 1.0)]
        [TestCase(@"y = [1..3]
                        .map(fun [1..5]
                                .map(fun 10/it)
                                .sum())
                        .sum()", 68.5)]
        [TestCase(@"y = [1..3]
                        .map(fun it/2)
                        .sum()", 3.0)]
        public void NumericConstantReturnsAlwaysReal(string expression, double expected) =>
            Calc(expression).AssertResultHas("y", expected);

        [TestCase("y = -(-(-x))", 2, -2.0)]
        public void NumericEquationReturnsAlwaysReal(string expression, double arg, double expected) =>
            Build(expression).Calc("x", arg).AssertResultHas("y", expected);


        [TestCase("o1 = 1\r o2=o1\r o3 = 0", 1, 1, 0)]
        [TestCase("o1 = 1\r o2 = o1+1\r o3=2*o1*o2", 1, 2, 4)]
        [TestCase("o3 = 2 \ro2 = o3*2 \ro1 = o2*2\r ", 8, 4, 2)]
        public void ThreeDependentConstantEquations_CalculatesCorrect(string expr, int o1, int o2, int o3) =>
            Calc(expr).AssertReturns(("o1", Convert(o1)), ("o2", Convert(o2)), ("o3", Convert(o3)));

        [TestCase("f(x) = concat(x.a, x.b);" +
                  "x1= {a = 'mama'; b = 'popo'};" +
                  "y = f({a=[1,2,3]; b = [4,5,6]})", new[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase("mkarr(a,b,c,d,takefirst) = if(takefirst) [a,b] else [c,d]\r y = mkarr(1,2,3,4,false)",
            new[] { 3, 4 })]
        [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()", new[] { 1, 2, 3, 1, 2, 3 })]
        [TestCase("y = [-1,-2,0,1,2,3].filter(fun it>0).map(fun(i)=i*i).map(fun(i)=i*i)", new[] { 1, 16, 81 })]
        [TestCase("y = range(7,10)", new[] { 7, 8, 9, 10 })]
        [TestCase(@"y = [[1,2],[3,4],[5,6]].map(fun  it.map(fun it+1).sum())", new[] { 5, 9, 13 })]
        public void ConstantCalcReturnsArrayOfTargetType(string expression, int[] expected)
            => Calc(expression).AssertResultHas("y", expected.Select(Convert).ToArray());

        [TestCase("y = x+3", 2, 5)]
        [TestCase("y = -2+x", 1, -1)]
        [TestCase("y = x+-2", 0, -2)]
        [TestCase("y = x+-2", 1, -1)]
        [TestCase("y = x*-2", 0, 0)]
        [TestCase("y = -2*x", 1, -2)]
        [TestCase("y = x*-2", 1, -2)]
        [TestCase("y = x*3", 2, 6)]
        [TestCase("y = x-3", 2, -1)]
        [TestCase("y = -2-x", 1, -3)]
        [TestCase("y = x-2", 0, -2)]
        [TestCase("y = x.rema(3)", 2, 2)]
        [TestCase("y = x.rema(4)", 5, 1)]
        [TestCase("y = x.rema(-4)", 5, 1)]
        [TestCase("y = x.rema(4)", -5, -1)]
        [TestCase("y = x.rema(-4)", -5, -1)]
        [TestCase("y = x.rema(4)", -5, -1)]
        [TestCase("y = abs(x-4)", 1, 3)]
        [TestCase("a = {b = x; c=x}; " +
                  "b = {d = a; e = a.c; f = 3}; " +
                  "y = b.d.b + b.e + b.f", 42, 87)]
        public void ArgCalcOfTargetType(string expression, int arg, int expected)
            => Build(expression).Calc("x", Convert(arg)).AssertResultHas("y", Convert(expected));

        [TestCase("y = x1+x2+1", 2, 3, 6)]
        [TestCase("y = 2*x1*x2", 3, 6, 36)]
        public void TwoArgCalcOfTargetType(string expression, int x1, int x2, int expected) =>
            Build(expression).Calc(("x1", Convert(x1)), ("x2", Convert(x2))).AssertResultHas("y", Convert(expected));

        [TestCase("y = x1*4/x2", 2, 2, 4)]
        [TestCase("y = (x1+x2)/4", 2, 2, 1)]
        public void TwoArgCalcAlwaysReturnsReal(string expression, object x1, object x2, double expected) =>
            Build(expression).Calc(("x1", x1), ("x2", x2)).AssertResultHas("y", expected);


        [TestCase(@"y = map([1,2,3], fun(i:int):real  =i*i)", new[] { 1.0, 4, 9 })]
        [TestCase(@"y = map([1,2,3], fun(i:int):int64  =i*i)", new long[] { 1, 4, 9 })]
        [TestCase(@"y = [1,2,3] . map(fun(i:int)=i*i)", new[] { 1, 4, 9 })]
        [TestCase(@"y = [1.0,2.0,3.0] . map(fun(i)=i*i)", new[] { 1.0, 4.0, 9.0 })]
        public void SuperAnonymousFunctions_ConstantEquation(string expr, object expected)
        {
            var runtime = expr.Build();
            runtime.AssertInputsCount(0, "Unexpected inputs on constant equations");
            runtime.Calc().AssertResultHas("y", expected);
        }

        [Test]
        public void OverrideConstantWithOutputVariable_constantNotUsed()
        {
            var runtime = Funny.Hardcore
                .WithDialect(_dialect)
                .WithConstant("pi", Math.PI)
                .Build("pi = 3; y = pi");

            runtime.AssertInputsCount(0);
            runtime.Calc().AssertReturns(("y", Convert(3)), ("pi", Convert(3)));
        }

        [TestCase("[0,1.0,1]==[0,0,1]", false)]
        [TestCase("[0,1,1]!=[0,0.0,1]", true)]
        [TestCase("[0,0.0]==[0,0.0]", true)]
        public void AssertEquation(string expr, bool expected)
            => Calc(expr).AssertReturns("out", expected);


        [TestCase(1)]
        [TestCase(42)]
        public void ConstantNCountAccess(int n)
        {
            TraceLog.IsEnabled = true;
            Calc("str = {field = 1}; " +
                 $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
                .AssertResultHas("y", Convert(n));
        }

        [TestCase(@"y = [1..7]
                        .map(fun it+1)
                        .sum()")]
        [TestCase(@"y = [1..8]
                        .map(fun [it,1].sum())
                        .sum()")]
        [TestCase(@"y = [1..9]
                        .map(fun [1,it].sum())
                        .sum()")]
        [TestCase(@"y = [1..10]
                        .map(fun [1..it].sum())
                        .sum()")]
        [TestCase(@"y = [1..11]
                        .map(fun [1..it].sum())
                        .sum()")]
        [TestCase(@"fibrec(n, iter, p1,p2) =
                          if (n >iter) 
                                fibrec(n, iter+1, p1+p2, p1)
                          else 
                                p1+p2  
          fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   
          y = fib(1)+0")]
        [TestCase(
            @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)+0")]
        public void SingleEquation_Runtime_OutputTypeCalculatesCorrect(string expr) =>
            Calc(expr).AssertResultIs(typeof(T));

        protected abstract T Convert(int value);

        protected FunnyRuntime Build(string expr)
            => Funny.Hardcore.WithDialect(_dialect).Build(expr);

        protected CalculationResult Calc(string expr)
            => Build(expr).Calc();
    }
}