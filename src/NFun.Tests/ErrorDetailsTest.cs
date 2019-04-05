using System;
using NFun;
using NFun.ParseErrors;
using NFun.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ErrorDetailsTest
    {
        [TestCase("y = x ","***"," z")]
        [TestCase("y = x ","!"," z")]
        [TestCase("y = x ","!"," z")]
        [TestCase("y = x + ","123z","")]
        [TestCase("y = ","add(x, y","")]
        [TestCase("y = ","somenotdefinedfunction(x, y)","")]
        public void ErrorPosition(string beforeError, string errorBody, string afterError)
        {
            try
            {
                FunBuilder.With(beforeError + errorBody + afterError)
                    .Build();
                Assert.Fail("Exception was not raised");
            }
            catch (FunParseException e)
            {
                int start = beforeError.Length;
                int end = start + errorBody.Length;
                Assert.AreEqual(start, e.Start);
                Assert.AreEqual(end, e.End);
            }
        }
    }
}