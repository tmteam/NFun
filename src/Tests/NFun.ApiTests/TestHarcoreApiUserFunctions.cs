using System.Linq;
using NFun.Interpretation.Functions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests; 

public class TestHardcoreApiUserFunctions {
    [Test]
    public void NoUserFunctions() =>
        "out1 = (10.0*x).toText()".AssertRuntimes(runtime =>
        {
            Assert.IsEmpty(runtime.UserFunctions);
        });

    [TestCase("g(x:int):int = 1", "g", false, FunctionRecursionKind.NoRecursion)]
    [TestCase("g() = 1", "g",true, FunctionRecursionKind.NoRecursion)]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0", "g", false, FunctionRecursionKind.SelfRecursion)]
    [TestCase("g(x) = if(x>0) g(x-1) else 0", "g", true, FunctionRecursionKind.SelfRecursion)]
    
    [TestCase("g(x:int):int = 1;  out1 = 100", "g", false, FunctionRecursionKind.NoRecursion)]
    [TestCase("g() = 1;  out1 = 100", "g",true, FunctionRecursionKind.NoRecursion)]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = 1", "g", false, FunctionRecursionKind.SelfRecursion)]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = 1", "g", true, FunctionRecursionKind.SelfRecursion)]
    
    [TestCase("g(x:int):int = 1;  out1 = g(42)", "g", false, FunctionRecursionKind.NoRecursion)]
    [TestCase("g() = 1;  out1 = g()", "g",true, FunctionRecursionKind.NoRecursion)]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = g(10)", "g", false, FunctionRecursionKind.SelfRecursion)]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)", "g", true, FunctionRecursionKind.SelfRecursion)]
    
    [TestCase("inc(x) = x+1;  a:int = 42.inc(); b:real = 42.5.inc()", "inc",true, FunctionRecursionKind.NoRecursion)]
    [TestCase("g(x) = if(x>0) g(x-1)+1 else 0; a:int = 42.g(); b:real = 42.5.g()", "g", true, FunctionRecursionKind.SelfRecursion)]
    public void SingleUserFunction(string expr, string name, bool isGeneric, FunctionRecursionKind recursionKind) =>
        expr.AssertRuntimes(runtime =>
        {
            Assert.AreEqual(1, runtime.UserFunctions.Count);
            Assert.AreEqual(isGeneric, runtime.UserFunctions[0].IsGeneric);
            Assert.AreEqual(recursionKind, runtime.UserFunctions[0].RecursionKind);
            Assert.AreEqual(name, runtime.UserFunctions[0].Name);
        });
    
    [TestCase("g(x:int):int = 1;  out1 = g(42); f() = 0;", "g","f")]
    [TestCase("abcdef(x:int) = 1;  ghijklm(x) = 0;", "abcdef","ghijklm")]
    public void TwoUserFunctions(string expr, string name1, string name2) =>
        expr.AssertRuntimes(runtime =>
        {
            Assert.AreEqual(2,runtime.UserFunctions.Count);
            Assert.IsTrue(runtime.UserFunctions.Any(f=>f.Name==name1));
            Assert.IsTrue(runtime.UserFunctions.Any(f=>f.Name==name2));
        });
    
}