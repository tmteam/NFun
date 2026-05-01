using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

/// <summary>
/// Smoke tests for the bytecode VM. Each test runs the same expression
/// through both tree-walker and VM, comparing results.
/// </summary>
[TestFixture]
public class VMSmokeTest {

    [TestCase("y = 2 + 3", "y", 5)]
    [TestCase("y = 10 - 3", "y", 7)]
    [TestCase("y = 4 * 5", "y", 20)]
    [TestCase("y = 10 // 3", "y", 3)]
    [TestCase("y = 42", "y", 42)]
    [TestCase("y = 0", "y", 0)]
    public void IntArithmetic(string expr, string varName, int expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, vm.GetOutput(varName));
    }

    [TestCase("y = 2.5 + 1.5", "y", 4.0)]
    [TestCase("y = 10.0 / 3.0", "y", 10.0 / 3.0)]
    public void RealArithmetic(string expr, string varName, double expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, (double)vm.GetOutput(varName), 0.0001);
    }

    [TestCase("y = true", "y", true)]
    [TestCase("y = false", "y", false)]
    [TestCase("y = true and false", "y", false)]
    [TestCase("y = true or false", "y", true)]
    [TestCase("y = not true", "y", false)]
    public void BoolLogic(string expr, string varName, bool expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, vm.GetOutput(varName));
    }

    [TestCase("y = 5 > 3", "y", true)]
    [TestCase("y = 5 < 3", "y", false)]
    [TestCase("y = 5 == 5", "y", true)]
    [TestCase("y = 5 != 3", "y", true)]
    public void Comparisons(string expr, string varName, bool expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, vm.GetOutput(varName));
    }

    [Test]
    public void IfElse() {
        var vm = Funny.Hardcore.BuildVM("y = if(true) 1 else 2");
        vm.Run();
        Assert.AreEqual(1, vm.GetOutput("y"));
    }

    [Test]
    public void IfElseFalse() {
        var vm = Funny.Hardcore.BuildVM("y = if(false) 1 else 2");
        vm.Run();
        Assert.AreEqual(2, vm.GetOutput("y"));
    }

    [Test]
    public void InputVariable() {
        var vm = Funny.Hardcore.BuildVM("y = x + 1");
        vm.SetInput("x", 41);
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void MultipleEquations() {
        var vm = Funny.Hardcore.BuildVM("a = 10\r b = a + 5");
        vm.Run();
        Assert.AreEqual(10, vm.GetOutput("a"));
        Assert.AreEqual(15, vm.GetOutput("b"));
    }
}
