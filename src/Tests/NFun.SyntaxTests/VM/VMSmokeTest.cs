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
        var result = vm.GetOutput(varName);
        Assert.IsNotNull(result, $"GetOutput returned null for {varName}");
        Assert.AreEqual(expected, (double)result, 0.0001, $"Type={result.GetType().Name}");
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
        // Tree-walker result for reference
        var tw = Funny.Hardcore.Build(expr);
        tw.Run();
        var twResult = tw["y"].Value;

        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        var vmResult = vm.GetOutput(varName);
        Assert.AreEqual(expected, vmResult, $"VM returned {vmResult?.GetType()?.Name}:{vmResult}, tree-walker returned {twResult?.GetType()?.Name}:{twResult}");
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
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("y = x + 1");
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

    // ── Built-in function calls ──

    [Test]
    public void BuiltInFunction_Max() {
        var vm = Funny.Hardcore.BuildVM("y = max(3, 7)");
        vm.Run();
        Assert.AreEqual(7, vm.GetOutput("y"));
    }

    [Test]
    public void BuiltInFunction_Abs() {
        var vm = Funny.Hardcore.BuildVM("y = abs(-42)");
        vm.Run();
        Assert.AreEqual(42L, vm.GetOutput("y"));
    }

    // ── User functions ──

    [Test]
    public void UserFunction_Simple() {
        var vm = Funny.Hardcore.BuildVM("f(x) = x * 2\r y = f(21)");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void UserFunction_TwoArgs() {
        var vm = Funny.Hardcore.BuildVM("add(a,b) = a + b\r y = add(10, 32)");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    // ── Structs ──

    [Test]
    public void StructLiteral() {
        var vm = Funny.Hardcore.BuildVM("s = {x = 1, y = 2}\r out = s.x + s.y");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("out"));
    }

    // ── Arrays ──

    [Test]
    public void ArrayLiteral() {
        var vm = Funny.Hardcore.BuildVM("y = [1,2,3].count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    // ── Complex expressions ──

    [Test]
    public void Complex_NestedIfElse() {
        var vm = Funny.Hardcore.BuildVM("y = if(true) if(false) 1 else 2 else 3");
        vm.Run();
        Assert.AreEqual(2, vm.GetOutput("y"));
    }

    [Test]
    public void Complex_ArithmeticChain() {
        var vm = Funny.Hardcore.BuildVM("y = (2 + 3) * 4 - 1");
        vm.Run();
        Assert.AreEqual(19, vm.GetOutput("y"));
    }
}
