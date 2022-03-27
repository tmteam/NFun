using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests {

[TestFixture]
public class ErrorDetailsTest {
    [TestCase("s = x ", "123abc", " z")]
    [TestCase("s = x ", "!", " z")]
    [TestCase("s = x ", "!", " z")]
    [TestCase("s = x + ", "123z", "")]
    [TestCase("", "(", "")]
    [TestCase("y(x,y)", "qwe", " x+y\r j = y(1,2)")]
    [TestCase("j = y(1,2) \r y(x,a):", "x", " +y")]
    [TestCase("j = y(1,2) \r y(x,b) ", "=", "")]
    [TestCase("j = y(1,2) \r y(x,c) ", "= ", "")]
    [TestCase("j = y(1,2) \r y(x,d) ", "=  ", "")]
    [TestCase("j = y(1,2) \r y(x,e) = ", "*", "")]
    [TestCase("j = 1+2*k ", "=", " 2+3")]
    [TestCase("j = 1+2 \r ", "(2+3) =", "15")]
    [TestCase("", "(2+3) =", "15")]
    [TestCase("x:int ", "x+1", "")]
    [TestCase("y+1 \r", "x+1", "")]
    [TestCase("", "(y(x, l))", " =x+g(c)=12")]
    [TestCase("", "(y(x, l))", " =")]
    [TestCase("", "(y(x, l))", " =x+l")]
    [TestCase("f(", "x*2", ")= x")]
    [TestCase("f(", "2", ")= x")]
    [TestCase("f(", "2", ",x)= x")]
    [TestCase("f(x,", "2", ")= x")]
    [TestCase("f(x,", "(z)", ")= x+z")]
    [TestCase("f(", "(x)", ")= x+1")]
    [TestCase("f(", "((x))", ")= x+1")]
    [TestCase("f(", "(x)", ",z)= x+z")]
    [TestCase("f = ", "", "")]
    [TestCase("f = ", "*", "")]
    [TestCase("y(x):", "lalala", " = y")]
    [TestCase("y(x):", "int[", "= y")]
    [TestCase("", "out", "+1")]
    [TestCase("", "out", "")]
    [TestCase("z = x+1 \r y = ", "y", " +1\rj = i+1")]
    [TestCase("z = x+1 \r y = ", "y", "\rj = i+1")]
    [TestCase("z(x) = x+1 \ry = ", "y", "\rj = z(i)")]
    [TestCase("if ", "1+2", " 1 else 2")]
    [TestCase("x:int[]; y = x[","true and false","]")]
    [TestCase("y(x) = x + ", "z", "")]
    [TestCase("y(x) = ", "z", " + x")]
    [TestCase("x:bool;y=sin(","x",")")]
    [TestCase("y(x:int):bool = if (true) true else ", "x", "")]
    [TestCase("y(x) = ", "z", " +x")]
    [TestCase("", "y(x,x)=", "x+1")]
    [TestCase("", "y(x,x,z)=", "x+1")]
    [TestCase("m =[1.0,6.0]",".foold","(rule(i,x)=i+1)")]
    [TestCase("[1.0,7.0].fold(rule(i,","i",")=i+1)")]
    [TestCase("[1.0,8.0].map(rule","(i,j)=i+j",")")]
    [TestCase("foo(x) = x +1\r y=", "foo", "*3")]
    [TestCase("\r y=", "foo", "*3 \r foo(x) = x +1")]
    [TestCase("foo(x) = x +1\r ", "foo", "*3 ")]
    [TestCase("y = if (x>0) 1 ", "if", "(x<0) -1 else 0")]
    [TestCase("y = 1 ", "z=", "2")]
    [TestCase("", "set", " x=1")]
    public void ErrorPosition(string beforeError, string errorBody, string afterError) =>
        AssertErrorPosition(beforeError, errorBody, afterError);

    [TestCase("y=", "'something \\' some postfix", "")]
    [TestCase("y=", "'\\' some postfix", "")]
    [TestCase("y='some text ", "\\", "")]
    [TestCase("y=", "'", "")]
    [TestCase("", "'", "")]
    public void QuotationNotClosed_ErrorPosition(string beforeError, string errorBody, string afterError) =>
        AssertErrorPosition(beforeError, errorBody, afterError);

    [TestCase("y='", "\\e", "lse' some postfix")]
    [TestCase("y='", "\\G", "' some postfix")]
    [TestCase("y='", "\\(", "' some postfix")]
    [TestCase("y='\\\\", "\\(", "hi' some postfix")]
    [TestCase("y='something", "\\ ", "else' some postfix")]
    [TestCase("y='something", "\\e", "lse' some postfix")]
    [TestCase("y='something \\\\", "\\e", "lse' some postfix")]
    public void QuotationBadEscape_ErrorPosition(string beforeError, string errorBody, string afterError) =>
        AssertErrorPosition(beforeError, errorBody, afterError);

    [TestCase("y = add(x, ", "y", "")]
    [TestCase("y = add(x, y", ",", "")]
    [TestCase("y = add(x", ", ,", "y)")]
    [TestCase("y = add(x, y", " ", "z)")]
    [TestCase("k = add(x", " ", "y z)")]
    [TestCase("k = add(x", " ", "y)")]
    [TestCase("k = ", "some_cycled_function", "(x, y)")]
    [TestCase("k = ", "some_not_defined_function", "(x1,x2 )")]
    [TestCase("k = f(", "a", "")]
    public void FunctionCall_ErrorPostion(string beforeError, string errorBody, string afterError) =>
        AssertErrorPosition(beforeError, errorBody, afterError);

    [TestCase("q=[1.0", " ", "2.0]")]
    [TestCase("q=[2,2,", "3", "")]
    [TestCase("q=[3,2,3", ",", "")]
    [TestCase("q=[4,2,3", " ", "")]
    [TestCase("q=[5,2,3", "  ", "")]
    [TestCase("m=[6,2,3", ",", "")]
    [TestCase("m=[7,2,3", ",,", "")]
    [TestCase("m=[8,2,3", ",]", "")]
    [TestCase("m=[9,2,3", " ", "4]")]
    [TestCase("m=[10,2,3,", "123anc", ",4]")]
    [TestCase("m=[11,2,3,", "123anc", "]")]
    [TestCase("m=[12,2,3,", "y = 12,", "4]")]
    [TestCase("m=[13,2,3", "   ", "4] @ [5,6]")]
    [TestCase("s=[14,2", ", ,", "3,4]")]
    [TestCase("s=[15,2", ",,", "3,4]")]
    [TestCase("s=[", ",", "]")]
    [TestCase("s=[", ",", "2]")]
    [TestCase("s=[", ",", ",2]")]
    [TestCase("s=", "[", "")]
    public void InitializeArray_ErrorPosition(string beforeError, string errorBody, string afterError) =>
        AssertErrorPosition(beforeError, errorBody, afterError);


    [TestCase("true and 1")]
    [TestCase("true and 1.0")]
    [TestCase("x = {m = 1.0}; out = true and x.m")]
    [TestCase("out = true and {m = 1.0}.m")]
    [TestCase("out = true and {m = 1.0}")]
    [TestCase("out = true and rule it>0")]
    [TestCase("out = true and if(true) 1 else 2")]
    [TestCase("out = true and (1,2,3)")]
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();
    
    private static void AssertErrorPosition(string beforeError, string errorBody, string afterError) {
        var value = beforeError + errorBody + afterError;
        Console.WriteLine(value);
        try
        {
            value.Build();
            Assert.Fail("Exception was not raised");
        }
        catch (FunnyParseException e) when (e.Start != -1)
        {
            Console.WriteLine($"Parse: [FU{e.Code}] {e.Message} [{e.Start},{e.End}]");
            Console.WriteLine($"Error: [{e.Start},{e.End}]: '{e.Interval.SubString(value)}'");

            int start = beforeError.Length;
            int end = start + errorBody.Length;

            if (e.Start > e.End)
                Assert.Fail($"[FU{e.Code}] Start is greater than end");
            Assert.Multiple(
                () => {
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