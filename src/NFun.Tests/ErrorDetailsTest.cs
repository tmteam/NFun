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
        [TestCase("y = ","add(x, y,","")]
        [TestCase("y = ","add(x, y z)","")]
        [TestCase("y = ","add(x y z)","")]
        [TestCase("y = ","add(x y)","")]
        [TestCase("y = ","somenotdefinedfunction(x, y)","")]
        [TestCase("y = ","f(a","")]
        [TestCase("","(","")]
        [TestCase("","[1,2,3","")]
        [TestCase("","[1,2,3,","")]
        [TestCase("","[1,2,3 4]","")]
        [TestCase("","[,]","")]
        [TestCase("","[,2]","")]
        [TestCase("y(x,y)","qwe"," x+y\r j = y(1,2)")]
        [TestCase("j = y(1,2) \r y(x,y)","qwe"," x+y")]
        [TestCase("j = y(1,2) \r y(x,y)",":"," x+y")]
        [TestCase("j = y(1,2) \r y(x,y)"," ","")]
        [TestCase("j = y(1,2) \r y(x,y) ="," ","")]
        [TestCase("j = y(1,2) \r y(x,y) = ","*","")]
        [TestCase("j = 1+2 \r ","*","\r k = 2+3")]
        [TestCase("j = 1+2 \r ","(2+3) =","15")]
        [TestCase("x:int ","x+1","")]
        [TestCase("y+1 \r","x+1","")]
        [TestCase("","(y(x, l))"," =x+g(c)=12")]
        [TestCase("","(y(x, l))"," =")]
        [TestCase("y(","x*2",")= x")]
        [TestCase("y(","2",")= x")]
        [TestCase("y(","2",",x)= x")]
        [TestCase("y(x,","2",")= x")]
        [TestCase("y(x,","(z)",")= x+z")]
        [TestCase("y(","(x)",")= x+1")]
        [TestCase("y(","((x))",")= x+1")]
        [TestCase("y(","(x)",",z)= x+z")]
        [TestCase("y = ","","")]
        [TestCase("y = ","*","")]

        public void ErrorPosition(string beforeError, string errorBody, string afterError)
        {
            try
            {
                FunBuilder.With(beforeError + errorBody + afterError)
                    .Build();
                Assert.Fail("Exception was not raised");
            }
            catch (FunParseException e) when(e.Start!= -1)
            {
                Console.WriteLine($"Parse: [FU{e.Code}] {e.Message} [{e.Start},{e.End}]");
                int start = beforeError.Length;
                int end = start + errorBody.Length - 1;
                if(e.Start>e.End)
                    Assert.Fail("Start is greater than end");
                Assert.Multiple(() =>
                {
                    Assert.AreEqual(start, e.Start, "start index");
                    Assert.AreEqual(end, e.End, "end index");
                });
            }
        }
    }
}