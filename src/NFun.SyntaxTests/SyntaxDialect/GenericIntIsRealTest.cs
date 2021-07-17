using System;
using System.Linq;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect
{
    public class GenericIntIsRealTest
    {
        private ClassicDialectSettings _dialect = Dialects.ModifyClassic(integerPreferedType: IntegerPreferedType.Real);
            
        [TestCase("y = 2+3", 5.0)]
        [TestCase("y = -2+-4", -6.0)]
        public void ConstantAddition(string expression, object expected)
            => Calc(expression).AssertReturns("y",expected);
        
        [TestCase("y = x+3", 2.5, 5.5)]
        [TestCase("y = -2+x",1.0, -1.0)]
        [TestCase("y = x+-2",0.0, -2.0)]
        [TestCase("y = x+-2",1.5, -0.5)]
        public void VarAddition(string expression,object input,  object expected) 
            => Build(expression).Calc("x",input).AssertResultHas("y",expected);
        
        [TestCase("y = -(1+2)",-3.0)]
        [TestCase("y = -2*(-4+2)", 4.0)]
        public void ConstantExpression(string expression, object expected)
            => Calc(expression).AssertReturns("y",expected);
        [TestCase("y = 2*3", 6.0)]
        [TestCase("y = -2*-4", 8.0)]
        public void ConstantMultiply(string expression, object expected) 
            => Calc(expression).AssertReturns("y",expected);
        [TestCase("y = x*-2",0.0, 0.0)]
        [TestCase("y = -2*x",1.0, -2.0)]
        [TestCase("y = x*-2",1.5, -3.0)]
        [TestCase("y = x*3.0", 2.5, 7.5)]
        public void VarMultiply(string expression,object input,  object expected) 
            => Build(expression).Calc("x",input).AssertResultHas("y",expected);
        [TestCase("y = -1",-1.0)]
        [TestCase("y = -(-1)", 1.0)]
        [TestCase("y = -(-(-1))",-1.0)]
        public void ConstantNegate(string expression, object expected)
            => Calc(expression).AssertReturns("y",expected);
        [TestCase("y = 2-3", -1.0)]
        public void ConstantSubstraction(string expression, object expected)
            => Calc(expression).AssertReturns("y",expected);
        
        [TestCase("y = x-3", 2.5, -0.5)]
        [TestCase("y = -2-x",1.0, -3.0)]
        [TestCase("y = x-2",0.0, -2.0)]
        public void VarSubstract(string expression,object input,  object expected) 
            => Build(expression).Calc("x",input).AssertResultHas("y",expected);
        
        [TestCase("y = x.rema(3)", 2,2)]
        [TestCase("y = x.rema(4)", 5,1)]
        [TestCase("y = x.rema(-4)", 5,1)]
        [TestCase("y = x.rema(4)", -5,-1)]
        [TestCase("y = x.rema(-4)", -5,-1)]
        [TestCase("y = x.rema(4)", -5,-1)]
        [TestCase("y = -(-(-x))",2,-2)]
        [TestCase("y = abs(x-4)",1,3)]
        public void SingleRealVariableEquation(string expr, double arg, double expected) => 
            Build(expr).Calc("x",arg).AssertReturns("y", expected);

        
        [TestCase("y = x1+x2",2,3,5)]
        [TestCase("y = 2*x1*x2",3,6, 36)]
        [TestCase("y = x1*4/x2",2,2,4)]
        [TestCase("y = (x1+x2)/4",2,2,1)]
        public void TwoVariablesEquation(string expr, double arg1, double arg2, double expected) => 
            Build(expr).Calc(("x1",arg1),("x2",arg2)).AssertResultHas("y",expected);
        
        [Test]
        public void OverrideConstantWithOutputVariable_constantNotUsed()
        {
            var runtime = Funny.Hardcore
                .WithDialect(_dialect)
                .WithConstant("pi", Math.PI)
                .Build("pi = 3; y = pi");

            runtime.AssertInputsCount(0);
            runtime.Calc().AssertReturns(("y", 3.0),("pi",3.0));
        }
        
        [TestCase("[1,2,3,4].fold(fun it1+it2)", 10.0)]
        [TestCase("[1,2,3,4].fold(0,(fun it1+it2))", 10.0)]
        [TestCase("[1,2,3,4].fold(-10,(fun it1+it2))", 0.0)]
        [TestCase("median([1.0,10.5,6.0])", 6.0)]
        [TestCase("median([1,-10,0])", 0.0)]
        [TestCase("range(7,10)", new[] {7.0, 8.0, 9.0, 10})]

        public void ConstantEquationWithPredefinedFunction(string expr, object expected) => Calc(expr).AssertOut(expected);
        
        [TestCase("[0,1.0,1]==[0,0,1]", false)]
        [TestCase("[0,1,1]!=[0,0.0,1]", true)]
        public void ConstantEquality(string expr, object expected)
            => Calc(expr).AssertReturns("out",expected);
        
        [TestCase(1)]
        [TestCase(42)]
        public void ConstantNCountAccess(int n)
        {
            TraceLog.IsEnabled = true;
            Calc("str = {field = 1}; " +
             $"y = {string.Join("+", Enumerable.Range(0, n).Select(_ => "str.field"))}")
                .AssertResultHas("y", (double) n);
        }
        [Test]
        public void AccessToNestedFields() =>
            Calc("y = {age = 42; name = 'vasa'}.age")
                .AssertReturns("y",42.0);
        [Test]
        public void DoubleNegateFieldAccessWithBrackets() =>
            Calc("a = {b = 1}; y = -(-a.b)").AssertResultHas("y", 1.0);
        
        [Test]
        public void NegateFieldAccess() =>
            Calc("a = {b = 1}; y = -a.b").AssertResultHas("y", -1.0);
        
        [Test]
        public void ConstAccessDoubleNestedComposite() =>
            Calc("y = { a1 = {b2 = [1,2,3]}}.a1.b2[1]")
                .AssertReturns("y", 2.0);
        
        [Test]
        public void VarAccessNestedCreated() =>
            Build("a = {b = x; c=x}; " +
             "b = {d = a; e = a.c; f = 3}; " +
             "y = b.d.b + b.e + b.f")
            .Calc("x",42.0)
            .AssertResultHas("y", 87.0);
        
        [Test]
        public void ConstAccessNestedComposite() =>
            Calc("y = { b = [1,2,3]}.b[1]")
                .AssertReturns("y", 2.0);
        [Test]
        public void ConstantAccess3EquationNested2()
        {
            TraceLog.IsEnabled = true;
            Calc("a1 = {af1_24 = 24; af2_1=1}; " +
                  "b2 = {bf1 = a1; bf2_1 = a1.af2_1}; " +
                  "y = a1.af1_24 + b2.bf1.af2_1 + b2.bf2_1")
                .AssertResultHas("y", 26.0);
        }
        
        [Test]
        public void ConstantAccessManyNestedCreatedHellTest2()
        {
            TraceLog.IsEnabled = true;
            Calc("a1 = {af1_24 = 24; af2_1=1}; " +
                  "b2 = {bf2_1 = 1}; " +
                  "c3 = {cf1_1 = b2.bf2_1; cf2_24 = 24}; " +
                  "e4 = {ef1 = a1.af1_24; ef2 = b2.bf2_1; ef3 = a1;  ef4_24 = c3.cf2_24}; " +
                  "y = a1.af1_24 + 1 + c3.cf2_24 + e4.ef4_24").AssertResultHas("y", 73.0);
        }
        
        [Test]
        public void CallGenericFunctionMultipleFieldOfRealArrayAccess() =>
            Calc("f(x) = concat(x.a, x.b);" +
                  "x1= {a = 'mama'; b = 'popo'};" +
                  "iarr = f({a=[1,2,3]; b = [4,5,6]})").AssertResultHas("iarr", new[]{1.0,2.0,3.0,4.0,5.0,6.0});
        
        [Test]
        public void CallGenericFunctionMultipleFieldOfConcreteRealsArrayAccess() =>
            Calc("f(x) = concat(x.a, x.b);" +
             "iarr:real[] = f( {a=[1.0,2.0,3.0]; b = [4.0,5.0,6.0]} )").AssertResultHas("iarr", new[]{1.0,2,3,4,5,6});
        
        [TestCase(@"car0(g) = g(2,4); y = car0(max)    ", 4.0)]
        [TestCase(@"car2(g) = g(2,4); y = car2(min)    ", 2.0)]
        [TestCase(@"car1(g) = g(2); my(x)=x-1; y =  car1(my)   ", 1.0)]
        [TestCase(@"car1(g) = g(2,3,4); my(a,b,c)=a+b+c; y = car1(my)   ", 9.0)]
        [TestCase(@"choose(f1, f2,  selector, arg1, arg2) = if(selector) f1(arg1,arg2) else f2(arg1,arg2); 
                   y =  choose(max, min, true, 1,2)", 2.0)]
        [TestCase("first(a) = a[0]\r y = [5,4,3].first()",5.0)]
        [TestCase("mkarr(a,b,c,d,takefirst) = if(takefirst) [a,b] else [c,d]\r y = mkarr(1,2,3,4,false)",new[]{3.0,4.0})]
        [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()",new[]{1.0,2.0,3.0,1.0,2.0,3.0})]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)",1.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,false)",2.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2.0,true)",1.0)]
        public void CalculateWithUserFunction(string expr, object expected) => Calc(expr).AssertReturns("y",expected);

        
        
        [TestCase(@"y = [1..7]
                        .map(fun it+1)
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..8]
                        .map(fun [it,1].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..9]
                        .map(fun [1,it].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..10]
                        .map(fun [1..it].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..11]
                        .map(fun [1..it].sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..12]
                        .map(fun [1..it]
                                .map(fun 2600/it)
                                .sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..13]
                        .map(fun [1..10]
                                .map(fun 2600/it)
                                .sum())
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"y = [1..14]
                        .map(fun it/2)
                        .sum()", BaseFunnyType.Real)]
        [TestCase(@"fibrec(n, iter, p1,p2) =
                          if (n >iter) 
                                fibrec(n, iter+1, p1+p2, p1)
                          else 
                                p1+p2  
          fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   
          y = fib(1)", BaseFunnyType.Real)]
        [TestCase(
            @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(1)", BaseFunnyType.Real)]
        public void SingleEquation_Runtime_OutputTypeCalculatesCorrect(string expr, BaseFunnyType type)
        {
            var clrtype = FunnyTypeConverters.GetOutputConverter(FunnyType.PrimitiveOf(type)).ClrType;
            
            Calc(expr).AssertResultIs(clrtype);
        }
        
        [TestCase("[1,'2',3.0,4,5.2, true, false, 7.2]", new object[] {1.0, "2", 3.0, 4.0, 5.2, true, false, 7.2})]
        [TestCase("[1,'23',4.0,0x5, true]", new object[] {1.0, "23",  4.0, 5, true})]
        public void AnonymousConstantArrayTest(string expr, object expected)
            => Calc(expr).AssertOut(expected);

        [TestCase("y = 2", 2.0)]
        [TestCase("y = 0.2", 0.2)]
        [TestCase("y = 11.222  ", 11.222)]
        [TestCase("y = 1111  ", 1111.0)]
        [TestCase("y = 11_11  ", 1111.0)]
        [TestCase("y = 1.1_11  ", 1.111)]
        public void NumericConstantEqualsExpected(string expression, object expected) =>
            Calc(expression).AssertReturns("y", expected);
        [Test]
        public void VarAccessCreatedInverted() =>
            Build("a = {b = 55; c=x}; y = a.b + a.c").Calc("x",42.0).AssertResultHas("y",97.0);
        [TestCase("a=1; b=2; c=3;",                             new[]{"a","b","c"}, new object[]{1.0, 2.0, 3.0})]
        [TestCase("a = 1; b = 2; c = 3",                        new[]{"a","b","c"}, new object[]{1.0, 2.0, 3.0})]
        [TestCase("a=1; b = if (a==1) 'one' else 'foo'; c=45;", new[]{"a","b","c"}, new object[]{1.0, "one", 45.0})]
        [TestCase("a=1; b = if (a == 0) 0 else 1; c = 1",       new[]{"a","b","c"}, new object[]{1.0, 1.0, 1.0})]
        [TestCase("a=0; b = cos(a); c = sin(a)",                new[]{"a","b","c"}, new object[]{0.0, 1.0, 0.0})]
        [TestCase("a = [1,2,3,4].max(); b = [1,2,3,4].min()",   new[]{"a","b"},     new object[]{4.0, 1.0})]
        [TestCase("a =[0..10][1]; b=[0..5][2]; c=[0..5][3];",   new[]{"a","b","c"}, new object[]{1.0, 2.0, 3.0})]
        public void SomeConstantInExpression(string expr, string[] outputNames, object[] constantValues)
        {
            var calculateResult = Calc(expr);
            for (var item=0; item < outputNames.Length; item++)
            {
                var val = constantValues[item];
                var name = outputNames[item];
                calculateResult.AssertResultHas(name,val);
            }
        }
        
        [TestCase("o1 = 1\r o2=o1\r o3 = 0", 1, 1, 0)]
        [TestCase("o1 = 1\r o2 = o1+1\r o3=2*o1*o2",1, 2, 4)]
        [TestCase("o1 = 1\r o3 = 2.0 \r o2 = o3",1, 2.0, 2.0)]
        [TestCase("o3 = 2 \ro2 = o3*2 \ro1 = o2*2\r ",8, 4, 2)]
        public void ThreeDependentConstantEquations_CalculatesCorrect(string expr,  double o1, double o2, double o3) => 
            Calc(expr).AssertReturns(("o1", o1), ("o2", o2), ("o3", o3));
        
        [TestCase(@"y = [[1,2],[3,4],[5,6]].map(fun  it.map(fun it+1).sum())", new[]{5.0,9,13})]
        [TestCase(@"y = [[1,2],[3,4],[5,6]].fold(-10, fun it1+ it2.sum())", 11.0)]
        [TestCase(@"y = (fun it+1)(3.0)", 4.0)]
        [TestCase(@"f = (fun it+1); y = f(3.0)", 4.0)]
        [TestCase(@"f = ((fun it+1)); y = f(3.0)", 4.0)]
        [TestCase(@"y = ((fun it+1))(3.0)", 4.0)]
        [TestCase(@"y = (((fun it+1)))(3.0)", 4.0)]
        [TestCase("y = [-1,-2,0,1,2,3].filter((fun it>0)).map((fun(i)=i*i).map(fun(i)=i*i)", new[]{1.0,16.0,81.0})]
        [TestCase( @"y = map([1,2,3], fun(i:int)=i*i)",new[]{1,4,9})]
        [TestCase( @"y = map([1,2,3], fun(i:int):real  =i*i)",new[]{1.0,4,9})]
        [TestCase( @"y = map([1,2,3], fun(i:int):int64  =i*i)",new long[]{1,4,9})]
        [TestCase( @"y = [1,2,3] . map(fun(i:int)=i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1.0,2.0,3.0] . map(fun(i)=i*i)",new[]{1.0,4.0,9.0})]
        public void SuperAnonymousFunctions_ConstantEquation(string expr, object expected)
        {
            var runtime = expr.Build();
            runtime.AssertInputsCount(0,"Unexpected inputs on constant equations");
            runtime.Calc().AssertResultHas("y", expected);
        }
        
        [TestCase(@"car3(g) = g(2); y = car3((fun it-1))   ", 1.0)]
        [TestCase(@"car4(g) = g(2); y =   car4(fun it)   ", 2.0)]
        [TestCase(@"car41(g) = g(2); y =   car41 (fun it)   ", 2.0)]
        
        private FunnyRuntime Build(string expr)
            => Funny.Hardcore.WithDialect(_dialect).Build(expr);
        
        private CalculationResult Calc(string expr)
            => Build(expr).Calc();
    }
}