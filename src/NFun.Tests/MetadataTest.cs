using System.Linq;
using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class MetadataTest
    {
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
        public void OutputVariable_IsStrictTypedMetadata(string expr, string varName, bool expectedIsStrictType)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var variableInfo = runtime.Outputs.First(i => i.Name == varName);
            Assert.AreEqual(expectedIsStrictType, variableInfo.IsStrictTyped);
        }
    }
}