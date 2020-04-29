using NFun.Types;
using NUnit.Framework;

namespace Nfun.TryTicTests.SyntaxTests
{
    [TestFixture]
    public class CommentsTest
    {
        [TestCase("y = 1.0#comment\r z = true")]
        [TestCase("y = 1.0\r#comment\r z = true")]
        [TestCase("y = 1.0\r z = true#here is a comment")]
        [TestCase("y = 1.0\r z = true#here is a comment")]
        [TestCase(@"y = 1.0 
                    z = true#")]
        [TestCase(@"y = 1.0 #a
                    #b
                    #c
                    z = true#d")]
        [TestCase(@"y = 1.0 
                    z = true#someComment")]
        [TestCase(@"
y = 1.0
#comment1
#comment2
#comment3
#comment4
#comment5
z = true")]

        public void SingleLineCommentsOnMultipleConstantEquatations(string expr)
        {
            var runtime = TestTools.Build(expr);
            runtime.Calculate()
                .AssertReturns(
                    VarVal.New("y", 1.0),
                    VarVal.New("z", true));
        }
    }
}