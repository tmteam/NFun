using System.Linq;
using NFun;
using NUnit.Framework;

namespace Funny.Tests
{
    public class MetadataTest
    {
        [TestCase("myfun1(a):int = a; y = x*3 ",               "myfun1",true)]
        [TestCase("myfun2(a,b):real = a+b; y = myfun2(x,z)*3 ","myfun2",true)]
        [TestCase("myfun3(a):real[] = a; y = myfun3(a) ",      "myfun3",true)]
        [TestCase("myfun4(a) = a; y = myfun4(a)*3 ",           "myfun4",false)]
        [TestCase("g(a):int = a; y = x*3 ","g",true)]
        [TestCase("g(a):real = a; y = g(a)*3 ","g",true)]
        [TestCase("g(a,b):real[] = a.concat(b); y = g(x,z) ","g",true)]
        [TestCase("g(a) = a; y = g(a)*3 ","g",false)]
        [TestCase("g(a:int) = a; y = g(a)*3 ","g",false)]
        public void UserFunction_IsReturnTypeStrictTypedMetadata(string expr, string functionName, bool expectedIsStrictType)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var funDefenition = runtime.UserFunctions.First(u => u.Name == functionName);
            Assert.AreEqual(expectedIsStrictType, funDefenition.IsReturnTypeStrictlyTyped);
        }
        
        [TestCase("myfun1(a):int = a; y = x*3 ",                   "myfun1","a",false)]
        [TestCase("myfun2(a,b):real = a+b; y = myfun2(x,z)*3 ",    "myfun2","b",false)]
        [TestCase("myfun3(a:int) = a; y = x*3 ",                   "myfun3","a",true)]
        [TestCase("myfun4(a,b:int):real = a+b; y = myfun4(x,z)*3 ","myfun4","b",true)]
        [TestCase("myfun5(b:int) = b","myfun5","b",true)]
        public void UserFunction_ArgumentIsStrictTypedMetadata(
            string expr, 
            string functionName, 
            string argName, 
            bool expectedIsStrictType)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var funDefenition = runtime.UserFunctions.First(u => u.Name == functionName);
            var argDefenition = funDefenition.Variables.First(v => v.Name == argName);
            Assert.AreEqual(expectedIsStrictType, argDefenition.IsStrictTyped);
        }
        
        [TestCase("y = x*3 ","x",false)]
        [TestCase("x:int; y = x*3 ","x",true)]
        [TestCase("a:int; y = a*b*c ","a",true)]
        [TestCase("a:int; y = a*b*c ","b",false)]
        public void InputVariable_IsStrictTypedMetadata(string expr, string varName, bool expectedIsStrictType)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var variableInfo = runtime.Inputs.First(i => i.Name == varName);
            Assert.AreEqual(expectedIsStrictType, variableInfo.IsStrictTyped);
        }
        
        [TestCase("x*3 ","out",false)]
        [TestCase("x:int; y = x*3 ","y",false)]
        [TestCase("a:int; a*b*c ","out",false)]
        [TestCase("a:int; y = a*b*c ","y",false)]
        [TestCase("x:int; y:real = x*3 ","y",true)]
        [TestCase("a:int; y:int = a*b*c ","y",true)]
        [TestCase("y:int[] = a ","y",true)]
        public void OutputVariable_IsStrictTypedMetadata(string expr, string varName, bool expectedIsStrictType)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var variableInfo = runtime.Outputs.First(i => i.Name == varName);
            Assert.AreEqual(expectedIsStrictType, variableInfo.IsStrictTyped);
        }
    }
}