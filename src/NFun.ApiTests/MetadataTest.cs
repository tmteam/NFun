using System.Linq;
using NFun.Interpritation.Functions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class MetadataTest
    {
        [TestCase("myfun1(a):int = a; y = x*3 ",               "myfun1",true)]
        [TestCase("myfun2(a,b):real = a+b; y = myfun2(x,z)*3 ","myfun2",true)]
        [TestCase("myfun3(a):real[] = a; y = myfun3(a) ",      "myfun3",true)]
        [TestCase("g(a):int = a; y = x*3 ","g",true)]
        [TestCase("g(a):real = a; y = g(a)*3 ","g",true)]
        [TestCase("g(a,b):real[] = a.concat(b); y = g(x,z) ","g",true)]
        [TestCase("g(a:real) = a; y = g(a)*3 ","g",false)]
        [TestCase("g(a:int) = a; y = g(a)*3 ","g",false)]
        public void ConcreteUserFunction_IsReturnTypeStrictTypedMetadata(string expr, string functionName, bool expectedIsStrictType)
        {
            var funDefinition = expr
                .Build()
                .UserFunctions
                .OfType<ConcreteUserFunction>()
                .First(u => u.Name == functionName);
            Assert.AreEqual(expectedIsStrictType, funDefinition.IsReturnTypeStrictlyTyped);
        }

        [TestCase("y = x*3 ","x",false)]
        [TestCase("x:int; y = x*3 ","x",true)]
        [TestCase("a:int; y = a*b*c ","a",true)]
        [TestCase("a:int; y = a*b*c ","b",false)]
        public void InputVariable_IsStrictTypedMetadata(string expr, string varName, bool expectedIsStrictType)
        {
            var variableInfo = expr.Build().Inputs.First(i => i.Name == varName);
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
            var variableInfo = expr.Build().Outputs.First(i => i.Name == varName);
            Assert.AreEqual(expectedIsStrictType, variableInfo.IsStrictTyped);
        }
    }
}