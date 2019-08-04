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
        [TestCase("s = x ","123abc"," z")]
        [TestCase("s = x ","!"," z")]
        [TestCase("s = x ","!"," z")]
        [TestCase("s = x + ","123z","")]
        [TestCase("","(","")]
        [TestCase("y(x,y)","qwe"," x+y\r j = y(1,2)")]
        [TestCase("j = y(1,2) \r y(x,a):","x"," +y")]
        [TestCase("j = y(1,2) \r y(x,b) ","=","")]
        [TestCase("j = y(1,2) \r y(x,c) ","= ","")]
        [TestCase("j = y(1,2) \r y(x,d) ","=  ","")]
        [TestCase("j = y(1,2) \r y(x,e) = ","*","")]
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
        [TestCase("y(x):","lalala"," = y")]
        [TestCase("y(x):","int[","= y")]
        [TestCase("","out","+1")]
        [TestCase("","out","")]
        [TestCase("z = x+1 \r y = ","y"," +1\rj = i+1")]
        [TestCase("z = x+1 \r y = ","y","\rj = i+1")]
        [TestCase("z(x) = x+1 \ry = ","y","\rj = z(i)")]
        [TestCase("if ","1+2"," 1 else 2")]
        [TestCase("x:int[] \r y = x","[true and false]","")]
        [TestCase("if (true) 1 else ", "true","")]
        [TestCase("if (true) 1; if (false) ", "true"," else 2")]
        [TestCase("if (true) 1; if (false) 2 else ", "true","")]
        [TestCase("y(x) = x + ","z","")]
        [TestCase("y(x) = ","z"," + x")]
        [TestCase("x:bool\ry=","sin(x)","")]
        [TestCase("","y(x:int):bool = ","if (true) true else x")]
        [TestCase("y(x) = ","z"," +x")]
        [TestCase("","y(x,x)","=x+1")]
        [TestCase("","y(x,x,z)","=x+1")]
        [TestCase("[1.0,2.0].fold((i,","i",")->i+1)")]
        [TestCase("[1.0,2.0].map((i,","i",")->i+1)")]
        [TestCase("y = ","min","*3 ")]
        [TestCase("","max","*3 ")]
        [TestCase("foo(x) = x +1\r y=","foo","*3")]
        [TestCase("\r y=","foo","*3 \r foo(x) = x +1")]
        [TestCase("foo(x) = x +1\r ","foo","*3 ")]
        [TestCase("y = if (x>0) 1 ", "if", "(x<0) -1 else 0")]
        [TestCase("y = 1 ", "z=", "2")]
        [TestCase("", "set", " x=1")]

        public void ErrorPosition(string beforeError, string errorBody, string afterError)
        {
            AssertErrorPosition(beforeError, errorBody, afterError);
        }
        [TestCase("y='", "something \\' some postfix", "")]
        [TestCase("y='something", "\\ ", "else' some postfix")]
        [TestCase("y='something", "\\e", "lse' some postfix")]
        [TestCase("y='something \\\\", "\\e", "lse' some postfix")]
        [TestCase("y='", "\\e", "lse' some postfix")]
        [TestCase("y='", "\\' some postfix", "")]
        [TestCase("y='", "\\G", "' some postfix")]
        [TestCase("y='", "\\(", "' some postfix")]
        [TestCase("y='\\\\", "\\(", "hi' some postfix")]
        [TestCase("y='some text ","\\", "")]
        [TestCase("y=","'","")]
        public void Quotation_ErrorPosition(string beforeError, string errorBody, string afterError)
        {
            AssertErrorPosition(beforeError, errorBody, afterError);
        }
        [TestCase("y = add(x, ","y","")]
        [TestCase("y = add(x, y",",","")]
        [TestCase("y = add(x",", ,","y)")]
        [TestCase("y = add(x, y"," ","z)")]
        [TestCase("k = add(x"," ","y z)")]
        [TestCase("k = add(x"," ","y)")]
        [TestCase("k = ","some_cycled_function(x, y)","")]
        [TestCase("k = ","some_not_defined_function(x1,x2 )","")]
        [TestCase("k = f(","a","")]
        public void FunctionCall_ErrorPostion(string beforeError, string errorBody, string afterError)
        {
            AssertErrorPosition(beforeError, errorBody, afterError);
        }
        [TestCase("q=[1.0"," ","2.0]")]
        [TestCase("q=[1,2,","3","")]
        [TestCase("q=[1,2,3",",","")]
        [TestCase("q=[1,2,3"," ","")]
        [TestCase("q=[1,2,3","  ","")]
        [TestCase("m=[1,2,3",",","")]
        [TestCase("m=[1,2,3",",,","")]
        [TestCase("m=[1,2,3",",","]")]
        [TestCase("m=[1,2,3"," ","4]")]
        [TestCase("m=[1,2,3,","123anc",",4]")]
        [TestCase("m=[1,2,3,","123anc","]")]
        [TestCase("m=[1,2,3,","y = 12",",4]")]
        [TestCase("m=[1,2,3","   ","4] @ [5,6]")]
        [TestCase("s=[1,2",", ,","3,4]")]
        [TestCase("s=[1,2",",,","3,4]")]
        [TestCase("s=[",",","]")]
        [TestCase("s=[",",","2]")]
        [TestCase("s=[",",",",2]")]
        [TestCase("s=","[","")]
        [TestCase("['1',", "2","]")]

        [TestCase("['1',", "2",",'3','4']")]
        [TestCase("[ '0','1', ", "2","]")]
        public void InitializeArray_ErrorPosition(string beforeError, string errorBody, string afterError)
        {
            AssertErrorPosition(beforeError, errorBody, afterError);
        }
        
        private static void AssertErrorPosition(string beforeError, string errorBody, string afterError)
        {
            var value = beforeError + errorBody + afterError;
            Console.WriteLine(value);

            try
            {
                FunBuilder.With(value)
                    .Build();
                Assert.Fail("Exception was not raised");
            }
            catch (FunParseException e) when (e.Start != -1)
            {
                Console.WriteLine($"Parse: [FU{e.Code}] {e.Message} [{e.Start},{e.End}]");
                Console.WriteLine($"Error: [{e.Start},{e.End}]: '{e.Interval.SubString(value)}'");

                int start = beforeError.Length;
                int end = start + errorBody.Length;
                
                if (e.Start > e.End)
                    Assert.Fail($"[FU{e.Code}] Start is greater than end");
                Assert.Multiple(() =>
                {
                    Assert.AreEqual(
                        expected: start, 
                        actual: e.Start, message: $"[FU{e.Code}] Start index");
                    Assert.AreEqual(
                        expected: end, 
                        actual: e.End, message: $"[FU{e.Code}] End index");
                });
            }
        }
    }
}