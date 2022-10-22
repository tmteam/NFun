using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect; 

public class UserFunctionsTest {

    [TestCase("o = 1")]
    [TestCase("g(x:int):int = 1")]
    [TestCase("g() = 1")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0")]
    
    [TestCase("g(x:int):int = 1;  out1 = 100")]
    [TestCase("g() = 1;  out1 = 100")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = 1")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = 1")]
    
    [TestCase("g(x:int):int = 1;  out1 = g(42)")]
    [TestCase("g() = 1;  out1 = g()")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    
    [TestCase("f(x) = 1; g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    public void AllowUserFunctionsDoesNotThrow(string expr)
        => expr.BuildWithDialect(allowUserFunctions: AllowUserFunctions.AllowAll);

    [TestCase("g(x:int):int = 1")]
    [TestCase("g() = 1")]

    [TestCase("g(x:int):int = 1;  out1 = 100")]
    [TestCase("g() = 1;  out1 = 100")]
    
    [TestCase("g(x:int):int = 1;  out1 = g(42)")]
    [TestCase("f(x) = x; g() = 1;  out1 = g()")]
    public void DenyRecursiveUserFunctionsDoesNotThrow(string expr)
        => expr.BuildWithDialect(allowUserFunctions: AllowUserFunctions.DenyRecursive);

    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = 1")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = 1")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    [TestCase("f(x) = 1; g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    public void DenyRecursiveUserFunctionsThrows(string expr)
        => TestHelper.AssertObviousFailsOnParse(()=>expr.BuildWithDialect(allowUserFunctions: AllowUserFunctions.DenyRecursive));
    
    
    [TestCase("a = min(1,2); #g(x) = if(x>0) g(x-1) else 0")]
    [TestCase("b = 0")]
    [TestCase("c = [1,2,3].map(rule it*it)")]
    public void DenyUserFunctionsDoesNotThrows(string expr)
        => expr.BuildWithDialect(allowUserFunctions: AllowUserFunctions.DenyUserFunctions);

    [TestCase("g(x:int):int = 1")]
    [TestCase("g() = 1")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0")]
    
    [TestCase("g(x:int):int = 1;  out1 = 100")]
    [TestCase("g() = 1;  out1 = 100")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = 1")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = 1")]
    
    [TestCase("g(x:int):int = 1;  out1 = g(42)")]
    [TestCase("g() = 1;  out1 = g()")]
    [TestCase("g(x:int):int = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    [TestCase("g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    
    [TestCase("f(x) = 1; g(x) = if(x>0) g(x-1) else 0;  out1 = g(10)")]
    public void DenyUserFunctionsThrows(string expr)
        => TestHelper.AssertObviousFailsOnParse(()=>expr.BuildWithDialect(allowUserFunctions: AllowUserFunctions.DenyUserFunctions));
}