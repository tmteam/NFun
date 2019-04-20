
using System.Linq;
using NFun;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class AttributesTest
    {
        [TestCase("|>private\r y = x", "y",new []{"private"})]
        [TestCase("|>foo\r z = x", "z",new []{"foo"})]
        [TestCase("|>z\r z = x", "z",new []{"z"})]
        [TestCase("|>foo\r|>bar\r z = x", "z",new []{"foo","bar"})]
        [TestCase("some = 1\r|>foo\r|>bar\r z = x", "z",new []{"foo","bar"})]
        [TestCase("some = 1\r|>foo\r|>bar\r|>aaa\r z = x", "z",new []{"foo","bar","aaa"})]
        [TestCase("|>start\rsome = 1\r|>foo\r|>bar\r|>aaa\r z = x", "some",new []{"start"})]
        [TestCase("|>start\rsome = 1\r z = x", "z",new string[0])]
        [TestCase("|>start\r 1*x", "out",new []{"start"})]
        [TestCase("1*x", "out",new string[0])]
        public void ValuelessAttributeOnVariables(
            string expression
            , string variable,
            string[] attribute)
        {
            var runtime =FunBuilder.BuildDefault(expression);
            var varInfo = runtime.Outputs.SingleOrDefault(o => o.Name == variable);
            Assert.IsNotNull(varInfo);

            CollectionAssert.AreEquivalent(attribute, varInfo.Attributes.Select(v=>v.Name));
            Assert.IsTrue(varInfo.Attributes.All(a=>a.Value==null));
        }
    }
}