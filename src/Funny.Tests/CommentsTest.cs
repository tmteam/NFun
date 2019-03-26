using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class CommentsTest
    {
        [TestCase("y =/*here is a comment*/ 1.0\r z = true")]
        [TestCase("y = 1.0/*here is a comment*/\r z = true")]
        [TestCase("y = 1.0\r/*here is a comment*/ z = true")]
        [TestCase("y = 1.0\r z = true/*here is a comment*/")]
        [TestCase("y = 1.0\r z = true/*here is a comment*/")]
        [TestCase("/*here is a comment*/y = 1.0\r z = true")]
        [TestCase("/*here is a/r multicomment*/y = 1.0\r z = true")]
        [TestCase("/*a*//*b*//*c*/y = 1.0\r z = true")]
        [TestCase("y = 1.0/*a*/\r/*b*//*c*/ z = true")]
        [TestCase(@"y = 1.0
                    /*a*/
                    /*b*//*c*/z = true")]
        [TestCase(@"y = 1.0 
                    z = true/*d*/")]
        public void MultilineCommentsOnMultipleConstantEquatations(string expr)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(
                    Var.New("y", 1.0),
                    Var.New("z", true));
        }
        [TestCase("y = 1.0//comment\r z = true")]
        [TestCase("y = 1.0\r//comment\r z = true")]
        [TestCase("y = 1.0\r z = true//here is a comment")]
        [TestCase("y = 1.0\r z = true//here is a comment")]
        [TestCase(@"y = 1.0 
                    z = true//")]
        [TestCase(@"y = 1.0 //a
                    //b
                    //c
                    z = true//d")]
        [TestCase(@"y = 1.0 
                    z = true//someComment")]
        public void SingleLineCommentsOnMultipleConstantEquatations(string expr)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(
                    Var.New("y", 1.0),
                    Var.New("z", true));
        }
        [TestCase("y =/*here is a comment  z = true")]
        [TestCase("y = 1.0 here is a comment*/\r z = true")]
        [TestCase("y = 1.0\r z//here is a comment*/  = true")]
        [TestCase("y = 1.0\r z = true/*here is a comment")]
        [TestCase("y = 1.0\r z = //true here is a comment*/")]
        [TestCase("y = 1.0\r z = //here is a comment/ true")]
        [TestCase("y = 1.0\r z = //here is a comment*/ true")]
        [TestCase("y = 1.0\r z = //here is a comment/* true")]

        [TestCase("y =//here is a comment y 1.0\r z = true")]

        [TestCase(@"y = 1.0
                    
                    /*a*/
                    /*b*/z=/*c/ = true")]
        
        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
    }
}