using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests
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

        public void SingleLineCommentsOnMultipleConstantEquatations(string expr) =>
            expr.AssertReturns(("y", 1.0), ("z", true));

        [TestCase("y = /*here is a comment  z = true")]
        [TestCase("y = 1.0 here is a comment*/\r z = true")]
        [TestCase("y = 1.0\r z#here is a comment*/  = true")]
        [TestCase("y = 1.0\r z = true/*here is a comment")]
        [TestCase("y = 1.0\r z = #true here is a comment*/")]
        [TestCase("y = 1.0\r z = #here is a comment*/ true")]
        [TestCase("y = 1.0\r z = #here is a comment/* true")]

        [TestCase("y =#here is a comment y 1.0\r z = true")]
        [TestCase("y = 2.0\r z = #here is a comment/ true")]
        public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();

    }
}