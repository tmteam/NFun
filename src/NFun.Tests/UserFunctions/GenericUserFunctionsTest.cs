using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.UserFunctions
{
    [TestFixture]
    public class GenericUserFunctionsTest
    {
        [TestCase("fake(a) = a\r y = fake(1.0)",1.0)]
        [TestCase("fake(a) = a\r y = fake(fake(true))",true)]
        [TestCase("fake(a) = a\r y = 'test'.fake().fake().fake()","test")]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)",1.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,false)",2.0)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2.0,true)",1.0)]
        [TestCase(@"choise(a,b,c,d,con1,con2) = 
                          if(con1) 
                                if(con2) a 
                                else     b
                          else 
                                if(con2) c
                                else     d

                    y:int = choise(1,2,3,4,false,true)",3)]
        
        [TestCase(@"choise(a,b,c,d,con1,con2) = 
                          if(con1) 
                                if(con2) a[0] 
                                else     b
                          else 
                                if(con2) c
                                else     d[0]

                    y:int = choise([1],2,3,[4,5],false,false)", 4)]
        
        [TestCase("mkarr(a,b,c,d,takefirst) = if(takefirst) [a,b] else [c,d]\r y = mkarr(1,2,3,4,false)",new[]{3.0,4.0})]
        [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()",new[]{1.0,2.0,3.0,1.0,2.0,3.0})]
        [TestCase("repeat(a) = a.concat(a)\r y = ['a','b'].repeat().repeat()",new[]{"a","b","a","b","a","b","a","b"})]
        [TestCase("first(a) = a[0]\r y = [5,4,3].first()",5.0)]
        [TestCase("first(a) = a[0]\r y = [[5,4],[3,2],[1]].first()",new[]{5.0,4.0})]
        [TestCase("first(a) = a[0]\r y = [[5.0,4.0],[3.0,2.0],[1.0]].first().first()",5.0)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1,2,3].first((i)->i>1)", 2.0)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1.0,2.0,3.0].first((i)->i>1)", 2.0)]
        [TestCase("filtrepeat(a, f) = a.concat(a).filter(f) \r y = [1.0,2.0,3.0].filtrepeat((i)->i>1)", new[]{2.0,3.0,2.0,3.0})]
        [TestCase("concat(a, b,c) = a.concat(b).concat(c) \r y:int[] = concat([1,2],[3,4],[5,6])", new[]{1,2,3,4,5,6})]
        public void ConstantEquation(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(VarVal.New("y", expected));
        }

        [TestCase("repeat(a) = a.concat(a); " +
                  "a = [1.0].repeat().repeat();" +
                  "b = ['a'].repeat().repeat();",new double[]{1,1,1,1},new[]{"a", "a" , "a" , "a" })]
        
        [TestCase("sum(a,b,c) = a+b+c; " +
                  "a:real = sum(1,2,3);" +
                  "b:int  = sum(1,2,3);", 6.0, 6)]
        public void ConstantEquationWithTwoUsesOfGenerics(string expr, object expectedA, object expectedB)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate()
                .AssertHas(VarVal.New("a", expectedA))
                .AssertHas(VarVal.New("b", expectedB));
        }
        [Ignore("hi order functions")]
        [TestCase(@"choose(f1, f3,  selector, arg1, arg2) = if(selector) f1(arg1,arg2) else f2(arg1,arg2); choose(max, min, true, 1,2)", 2.0)]
        [TestCase(@"car2(f, x) = i->f(x, i); max.car2(1)(2)",2.0)]
        [TestCase(@"mult(x)=y->z->x*y*z;    mult(2)(3)(4)",24.0)]
        [TestCase(@"mult()= x->y->z->x* y*z; mult()(2)(3)(4)",24.0)]
        [TestCase(@"car2(g) = g(2,4); car2(max)    ", 4.0)]
        [TestCase(@"car2(g) = g(2,4); car2(min)    ", 2.0)]
        [TestCase(@"car1(g) = g(2);   car1(x->x-1)   ", 1.0)]
        [TestCase(@"car1(g) = g(2);   car1(x->x)   ", 2.0)]
        [TestCase(@"car1(g) = g(2); my(x)=x-1; car1(my)   ", 1.0)]
        [TestCase(@"car1(g) = g(2,3,4); my(a,b,c)=a+b+c; car1(my)   ", 9.0)]
        public void HiOrder(string expr, object expected) 
            => FunBuilder.BuildDefault(expr).Calculate().AssertOutEquals(expected);
    }
}