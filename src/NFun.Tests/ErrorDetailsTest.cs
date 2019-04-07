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
        [TestCase("s = x ","***"," z")]
        [TestCase("s = x ","!"," z")]
        [TestCase("s = x ","!"," z")]
        [TestCase("s = x + ","123z","")]
        [TestCase("y = ","add(x, y","")]
        [TestCase("y = ","add(x, y,","")]
        [TestCase("y = ","add(x, y z)","")]
        [TestCase("k = ","add(x y z)","")]
        [TestCase("k = ","add(x y)","")]
        [TestCase("k = ","some_cycled_function(x, y)","")]
        [TestCase("k = ","some_not_defined_function(x1,x2 )","")]
        [TestCase("k = ","f(a","")]
        [TestCase("","(","")]
        [TestCase("","[1,2,3","")]
        [TestCase("","[1,2,3,","")]
        [TestCase("","[1,2,3 4]","")]
        [TestCase("h= ","[1,2,3 4]"," @ [5,6]")]
        [TestCase("","[,]","")]
        [TestCase("","[,2]","")]
        [TestCase("y(x,y)","qwe"," x+y\r j = y(1,2)")]
        [TestCase("j = y(1,2) \r y(x,z)","qwe"," x+y")]
        [TestCase("j = y(1,2) \r y(x,z)",":"," x+y")]
        [TestCase("j = y(1,2) \r y(x,z)"," ","")]
        [TestCase("j = y(1,2) \r y(x,z) ","=","")]
        [TestCase("j = y(1,2) \r y(x,z) ","= ","")]
        [TestCase("j = y(1,2) \r y(x,z) ","=  ","")]
        [TestCase("j = y(1,2) \r y(x,z) = ","*","")]
        [TestCase("j = 1+2*k ","="," 2+3")]
        [TestCase("j = 1+2 \r ","(2+3) =","15")]
        [TestCase("","(2+3) =","15")]
        [TestCase("x:int ","x+1","")]
        [TestCase("y+1 \r","x+1","")]
        [TestCase("","(y(x, l))"," =x+g(c)=12")]
        [TestCase("","(y(x, l))"," =")]
        [TestCase("","(y(x, l))"," =x+l")]
        [TestCase("f(","x*2",")= x")]
        [TestCase("f(","2",")= x")]
        [TestCase("f(","2",",x)= x")]
        [TestCase("f(x,","2",")= x")]
        [TestCase("f(x,","(z)",")= x+z")]
        [TestCase("f(","(x)",")= x+1")]
        [TestCase("f(","((x))",")= x+1")]
        [TestCase("f(","(x)",",z)= x+z")]
        [TestCase("f = ","","")]
        [TestCase("f = ","*","")]
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
                int end = start + errorBody.Length;
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