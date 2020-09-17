using NFun;
using NUnit.Framework;

namespace Nfun.ModuleTests.UnitTests
{
    [TestFixture]
    public class ValueParserTest
    {
        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("12", 12)]
        
        [TestCase("[12]", new []{12})]
        [TestCase("[12,1]", new []{12,1})]
        [TestCase("[12,[1,2,3]]", new object[]{12,new[]{1,2,3}})]
        public void ParseValue(string text, object expected)
        {
            var value =  ValueParser.ParseValue(text);
            Assert.AreEqual(
                expected:  expected, 
                actual:    value);
        }
    }
}