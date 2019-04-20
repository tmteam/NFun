
using System.Linq;
using NFun;
using NFun.ParseErrors;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class AttributesTest
    {
        
        [TestCase("@private\r x:int\r    y = x", "x",new []{"private"})]
        [TestCase("@foo\r@bar\r x:int\r  z = x", "x",new []{"foo","bar"})]
        [TestCase("some = 1\r@foo\r@bar\r x:int\r z = x", "x",new []{"foo","bar"})]
        [TestCase("@start\r x:int\r 1*x", "x",new []{"start"})]
        [TestCase("@private\r y = x", "y",new []{"private"})]
        [TestCase("@foo\r z = x", "z",new []{"foo"})]
        [TestCase("@z\r z = x", "z",new []{"z"})]
        [TestCase("@foo\r@bar\r z = x", "z",new []{"foo","bar"})]
        [TestCase("some = 1\r@foo\r@bar\r z = x", "z",new []{"foo","bar"})]
        [TestCase("some = 1\r@foo\r@bar\r@aaa\r z = x", "z",new []{"foo","bar","aaa"})]
        [TestCase("@start\rsome = 1\r@foo\r@bar\r@aaa\r z = x", "some",new []{"start"})]
        [TestCase("@start\rsome = 1\r z = x", "z",new string[0])]
        [TestCase("@start\r 1*x", "out",new []{"start"})]
        [TestCase("1*x", "out",new string[0])]
        public void ValuelessAttributeOnVariables(
            string expression
            , string variable,
            string[] attribute)
        {
            var runtime =FunBuilder.BuildDefault(expression);
            var varInfo = runtime.Outputs.Union(runtime.Inputs).SingleOrDefault(o => o.Name == variable);
            Assert.IsNotNull(varInfo);

            CollectionAssert.AreEquivalent(attribute, varInfo.Attributes.Select(v=>v.Name));
            Assert.IsTrue(varInfo.Attributes.All(a=>a.Value==null));
        }
        [TestCase("@name('lalala')\r x:int\r    y = x", "x","name","lalala")]
        [TestCase("@foo\r@bar('bobobo')\r x:int\r  z = x", "x","bar","bobobo")]
        [TestCase("some = 1\r\r@foo('bar')\r \r x:int\r z = x", "x","foo","bar")]
        
        [TestCase("@name('lalala')\r \r    y = x", "y","name","lalala")]
        [TestCase("@foo\r@bar('bobobo')\r z = x", "z","bar","bobobo")]
        [TestCase("some = 1\r\r@foo('bar')\r \r y = x*3", "y","foo","bar")]

        
        [TestCase("@name(true)\r x:int\r    y = x", "x","name",true)]
        [TestCase("@foo\r@bar(123)\r x:int\r  z = x", "x","bar",123)]
        [TestCase("some = 1\r\r@foo(123.5)\r \r x:int\r z = x", "x","foo",123.5)]
        
        [TestCase("@name(false)\r \r    y = x", "y","name",false)]
        [TestCase("@foo\r@bar('')\r z = x", "z","bar","")]
        [TestCase("some = 1\r\r@foo(0)\r \r y = x*3", "y","foo",0)]

        public void AttributeWithValue_ValueIsCorrect(
            string expression
            , string variable,
            string attribute, object value)
        {
            var runtime =FunBuilder.BuildDefault(expression);
            var varInfo = runtime.Outputs.Union(runtime.Inputs).SingleOrDefault(o => o.Name == variable);
            Assert.IsNotNull(varInfo);

            var actual = varInfo.Attributes.SingleOrDefault(v => v.Name== attribute);
            Assert.IsNotNull(actual);
            Assert.AreEqual(value,actual.Value);
        }
     
        [TestCase("@attr() \r  y = if then 3 else 4")]
        [TestCase("@attr(vasa)\r   y = if then 3 else 4")]
        [TestCase("\r   y = if then 3 else 4 @attr(vasa)")]
        [TestCase("\r @attr(vasa)")]
        [TestCase("\r @attr( \r x = 1")]
        [TestCase("\r @attr(x = 1")]
        [TestCase("\r @attr(x = 1) y = 2")]
        [TestCase("\r @attr(y) y = 2")]
        [TestCase("\r y = 2 @attr(1) \r z = 5")]
        [TestCase("\r y = 2 @(4) \r z = 5")]
        [TestCase("\r y = 2 \r @(4.3.2) \r z = 5")]
        [TestCase("\r y = 2  \r@('3) \r z = 5")]
        [TestCase("\r y = 2  \r@(''3) \r z = 5")]
        [TestCase("\r y = 2  \r@('',3) \r z = 5")]
        [TestCase("\r y = 2  \r@(') \r z = 5")]

        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
    }
}