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
        [TestCase("repeat(a) = a.concat(a)\r y = [1,2,3].repeat()",new[]{1,2,3,1,2,3})]
        [TestCase("repeat(a) = a.concat(a)\r y = ['a','b'].repeat().repeat()",new[]{"a","b","a","b","a","b","a","b"})]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1,2,3].first((i)=>i>1)", 2)]
        [TestCase("first(a, f) = a.filter(f)[0] \r y = [1.0,2.0,3.0].first((i)=>i>1)", 2.0)]
        [TestCase("filtrepeat(a, f) = a.concat(a).filter(f) \r y = [1.0,2.0,3.0].filtrepeat((i)=>i>1)", new[]{2.0,3.0,2.0,3.0})]
        public void ConstantEquation(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(Var.New("y", expected));
        }
    }
}