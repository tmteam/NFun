using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class GenericUserFunctionsTest
    {
        [TestCase("fake(a) = a\r y = fake(1.0)",1.0)]
        [TestCase("fake(a) = a\r y = fake(fake(true))",true)]
        [TestCase("fake(a) = a\r y = 'test'.fake().fake().fake()","test")]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)",1)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,false)",2)]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2.0,true)",1.0)]
        [TestCase(@"choise(a,b,c,d,con1,con2) = 
                          if(con1) 
                                if(con2) a 
                                else     b
                          else 
                                if(con2) c
                                else     d

                    y = choise(1,2,3,4,false,true)",3)]
        
        [TestCase(@"choise(a,b,c,d,con1,con2) = 
                          if(con1) 
                                if(con2) a[0] 
                                else     b
                          else 
                                if(con2) c
                                else     d[0]

                    y = choise([1],2,3,[4,5],false,false)",4)]
        
        [TestCase("mkarr(a,b,c,d,takefirst) = if(takefirst) [a,b] else [c,d]\r y = mkarr(1,2,3,4,false)",new[]{3,4})]
        [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()",new[]{1,2,3,1,2,3})]
        [TestCase("repeat(a) = a.concat(a)\r y = ['a','b'].repeat().repeat()",new[]{"a","b","a","b","a","b","a","b"})]
        [TestCase("first(a) = a[0]\r y = [5,4,3].first()",5)]
        [TestCase("first(a) = a[0]\r y = [[5,4],[3,2],[1]].first()",new[]{5,4})]
        [TestCase("first(a) = a[0]\r y = [[5.0,4.0],[3.0,2.0],[1.0]].first().first()",5.0)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1,2,3].first((i)->i>1)", 2)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1.0,2.0,3.0].first((i)->i>1)", 2.0)]
        [TestCase("filtrepeat(a, f) = a.concat(a).filter(f) \r y = [1.0,2.0,3.0].filtrepeat((i)->i>1)", new[]{2.0,3.0,2.0,3.0})]
        [TestCase("concat(a, b,c) = a.concat(b).concat(c) \r y = concat([1,2],[3,4],[5,6])", new[]{1,2,3,4,5,6})]
        [TestCase("getLast(x) = if(x.count()==1) x[0] else getLast(x[1:]);  y=[1,2,3].getLast()", 3)]
        [TestCase("getHalf(x) = x[round(x.count()/2) : ];  y=[1,2,3,4].getHalf()", new int[] { 3, 4 })]

        public void ConstantEquation(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(VarVal.New("y", expected));
        }
    }
}