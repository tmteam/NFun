using System;
using System.Linq;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

/// <summary>
/// Smoke tests for the bytecode VM. Each test runs the same expression
/// through both tree-walker and VM, comparing results.
/// </summary>
[TestFixture]
public class VMSmokeTest {

    // ═══════════════════════════════════════════════════════════
    //  Helper: run expression in both tree-walker and VM, compare
    // ═══════════════════════════════════════════════════════════

    private static void AssertVmMatchesTreeWalker(string expr, string varName) {
        var tw = Funny.Hardcore.Build(expr);
        tw.Run();
        var twResult = tw[varName].Value;

        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        var vmResult = vm.GetOutput(varName);

        if (twResult is double twD && vmResult is double vmD)
            Assert.AreEqual(twD, vmD, 0.0001, $"VM={vmResult}, TW={twResult}");
        else
            Assert.AreEqual(twResult, vmResult, $"VM ({vmResult?.GetType()?.Name})={vmResult}, TW ({twResult?.GetType()?.Name})={twResult}");
    }

    private static void AssertVmMatchesTreeWalkerOpt(string expr, string varName) {
        var tw = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build(expr);
        tw.Run();
        var twResult = tw[varName].Value;

        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM(expr);
        vm.Run();
        var vmResult = vm.GetOutput(varName);

        if (twResult is double twD && vmResult is double vmD)
            Assert.AreEqual(twD, vmD, 0.0001);
        else
            Assert.AreEqual(twResult, vmResult);
    }

    // ═══════════════════════════════════════════════════════════
    //  1. Int Arithmetic (existing)
    // ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    //  2. Real Arithmetic (existing + edge cases)
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 2.5 + 1.5", "y", 4.0)]
    [TestCase("y = 10.0 / 3.0", "y", 10.0 / 3.0)]
    public void RealArithmetic(string expr, string varName, double expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        var result = vm.GetOutput(varName);
        Assert.IsNotNull(result, $"GetOutput returned null for {varName}");
        Assert.AreEqual(expected, (double)result, 0.0001, $"Type={result.GetType().Name}");
    }

    [Test]
    public void RealEdge_NegativeNumber() {
        var vm = Funny.Hardcore.BuildVM("y = -3.14");
        vm.Run();
        var result = vm.GetOutput("y");
        Assert.AreEqual(-3.14, (double)result, 0.0001);
    }

    [Test]
    public void RealEdge_LargeNumber() {
        var vm = Funny.Hardcore.BuildVM("y = 1000000.0 * 1000000.0");
        vm.Run();
        var result = vm.GetOutput("y");
        Assert.AreEqual(1e12, (double)result, 1.0);
    }

    [Test]
    public void RealEdge_VerySmall() {
        var vm = Funny.Hardcore.BuildVM("y = 0.001 * 0.001");
        vm.Run();
        var result = vm.GetOutput("y");
        Assert.AreEqual(0.000001, (double)result, 1e-9);
    }

    [Test]
    public void RealEdge_Remainder() {
        var vm = Funny.Hardcore.BuildVM("y = 10 % 3");
        vm.Run();
        Assert.AreEqual(1, vm.GetOutput("y"));
    }

    [Test]
    public void RealEdge_Power() {
        var vm = Funny.Hardcore.BuildVM("y = 2 ** 10");
        vm.Run();
        Assert.AreEqual(1024, vm.GetOutput("y"));
    }

    [Test]
    public void RealEdge_UnaryNegate() {
        var vm = Funny.Hardcore.BuildVM("y = -(5 + 3)");
        vm.Run();
        Assert.AreEqual(-8, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  3. Bool Logic (existing + xor)
    // ═══════════════════════════════════════════════════════════

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

    [TestCase("y = true xor false", "y", true)]
    [TestCase("y = true xor true", "y", false)]
    [TestCase("y = false xor false", "y", false)]
    public void BoolLogic_Xor(string expr, string varName, bool expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, vm.GetOutput(varName));
    }

    // ═══════════════════════════════════════════════════════════
    //  4. Comparisons (existing + more)
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 5 > 3", "y", true)]
    [TestCase("y = 5 < 3", "y", false)]
    [TestCase("y = 5 == 5", "y", true)]
    [TestCase("y = 5 != 3", "y", true)]
    public void Comparisons(string expr, string varName, bool expected) {
        var tw = Funny.Hardcore.Build(expr);
        tw.Run();
        var twResult = tw["y"].Value;

        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        var vmResult = vm.GetOutput(varName);
        Assert.AreEqual(expected, vmResult, $"VM returned {vmResult?.GetType()?.Name}:{vmResult}, tree-walker returned {twResult?.GetType()?.Name}:{twResult}");
    }

    [TestCase("y = 3 >= 3", "y", true)]
    [TestCase("y = 3 <= 3", "y", true)]
    [TestCase("y = 4 >= 5", "y", false)]
    [TestCase("y = 6 <= 5", "y", false)]
    public void Comparisons_GeLe(string expr, string varName, bool expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, vm.GetOutput(varName));
    }

    [Test]
    public void ComparisonChain() {
        var vm = Funny.Hardcore.BuildVM("y = 1 < 2 < 3");
        vm.Run();
        Assert.AreEqual(true, vm.GetOutput("y"));
    }

    [Test]
    public void ComparisonChain_False() {
        var vm = Funny.Hardcore.BuildVM("y = 1 < 2 < 2");
        vm.Run();
        Assert.AreEqual(false, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  5. If-Else (existing + multi-branch)
    // ═══════════════════════════════════════════════════════════

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
    public void IfElseIf_MultiBranch() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("y = if(x==1) 10\r if(x==2) 20\r else 30");
        vm.SetInput("x", 2);
        vm.Run();
        Assert.AreEqual(20, vm.GetOutput("y"));
    }

    [Test]
    public void IfElseIf_ElseBranch() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("y = if(x==1) 10\r if(x==2) 20\r else 30");
        vm.SetInput("x", 99);
        vm.Run();
        Assert.AreEqual(30, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  6. Input Variables (existing)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void InputVariable() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("y = x + 1");
        vm.SetInput("x", 41);
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  7. Multiple Equations (existing + dependent chain)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void MultipleEquations() {
        var vm = Funny.Hardcore.BuildVM("a = 10\r b = a + 5");
        vm.Run();
        Assert.AreEqual(10, vm.GetOutput("a"));
        Assert.AreEqual(15, vm.GetOutput("b"));
    }

    [Test]
    public void MultipleEquations_Chain3() {
        var vm = Funny.Hardcore.BuildVM("a = 10\r b = a * 2\r c = b + a");
        vm.Run();
        Assert.AreEqual(10, vm.GetOutput("a"));
        Assert.AreEqual(20, vm.GetOutput("b"));
        Assert.AreEqual(30, vm.GetOutput("c"));
    }

    [Test]
    public void MultipleEquations_FourOutputs() {
        var vm = Funny.Hardcore.BuildVM("a = 1\r b = 2\r c = a + b\r d = c * c");
        vm.Run();
        Assert.AreEqual(1, vm.GetOutput("a"));
        Assert.AreEqual(2, vm.GetOutput("b"));
        Assert.AreEqual(3, vm.GetOutput("c"));
        Assert.AreEqual(9, vm.GetOutput("d"));
    }

    // ═══════════════════════════════════════════════════════════
    //  8. Built-in Functions (existing + more)
    // ═══════════════════════════════════════════════════════════

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

    [Test]
    public void BuiltInFunction_Min() {
        var vm = Funny.Hardcore.BuildVM("y = min(3, 7)");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void BuiltInFunction_Sqrt() {
        var vm = Funny.Hardcore.BuildVM("y = sqrt(16.0)");
        vm.Run();
        Assert.AreEqual(4.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void BuiltInFunction_Round() {
        var vm = Funny.Hardcore.BuildVM("y = round(3.567, 2)");
        vm.Run();
        Assert.AreEqual(3.57, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void BuiltInFunction_Ceil() {
        var vm = Funny.Hardcore.BuildVM("y = ceil(7.03)");
        vm.Run();
        Assert.AreEqual(8.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void BuiltInFunction_Floor() {
        var vm = Funny.Hardcore.BuildVM("y = floor(7.99)");
        vm.Run();
        Assert.AreEqual(7.0, (double)vm.GetOutput("y"), 0.0001);
    }

    // ═══════════════════════════════════════════════════════════
    //  9. User Functions (existing + 3 args)
    // ═══════════════════════════════════════════════════════════

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

    [Test]
    public void UserFunction_ThreeArgs() {
        var vm = Funny.Hardcore.BuildVM("sumOf3(a,b,c) = a + b + c\r y = sumOf3(10, 20, 12)");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void UserFunction_CallingAnother() {
        var vm = Funny.Hardcore.BuildVM("double(x) = x * 2\r quadruple(x) = double(double(x))\r y = quadruple(10)");
        vm.Run();
        Assert.AreEqual(40, vm.GetOutput("y"));
    }

    [Test]
    public void UserFunction_WithIfElse() {
        var vm = Funny.Hardcore.BuildVM("myAbs(x) = if(x < 0) -x else x\r y = myAbs(-7)");
        vm.Run();
        Assert.AreEqual(7, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  10. Structs (existing + nested + multiple fields)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructLiteral() {
        var vm = Funny.Hardcore.BuildVM("s = {x = 1, y = 2}\r out = s.x + s.y");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("out"));
    }

    [Test]
    public void Struct_MultipleFields() {
        var vm = Funny.Hardcore.BuildVM("s = {a = 10, b = 20, c = 30}\r y = s.a + s.b + s.c");
        vm.Run();
        Assert.AreEqual(60, vm.GetOutput("y"));
    }

    [Test]
    public void Struct_Nested() {
        var vm = Funny.Hardcore.BuildVM("s = {inner = {value = 42}}\r y = s.inner.value");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void Struct_DeepNested() {
        var vm = Funny.Hardcore.BuildVM("s = {a = {b = {c = 99}}}\r y = s.a.b.c");
        vm.Run();
        Assert.AreEqual(99, vm.GetOutput("y"));
    }

    [Test]
    public void Struct_MixedFieldTypes() {
        var vm = Funny.Hardcore.BuildVM("s = {flag = true, count = 5}\r y = if(s.flag) s.count else 0");
        vm.Run();
        Assert.AreEqual(5, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  11. Arrays (existing + operations)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ArrayLiteral_Simple() {
        var vm = Funny.Hardcore.BuildVM("y = [1,2,3]");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void ArrayLiteral_PipedCount() {
        var vm = Funny.Hardcore.BuildVM("y = [1,2,3].count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Count_Empty() {
        AssertVmMatchesTreeWalker("y:int[] = []\r c = y.count()", "c");
    }

    [Test]
    public void Array_First() {
        AssertVmMatchesTreeWalker("y = first([10,20,30])", "y");
    }

    [Test]
    public void Array_Last() {
        AssertVmMatchesTreeWalker("y = last([10,20,30])", "y");
    }

    [Test]
    public void Array_Reverse() {
        var vm = Funny.Hardcore.BuildVM("y = reverse([1,2,3]).count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Concat() {
        var vm = Funny.Hardcore.BuildVM("y = concat([1,2],[3,4]).count()");
        vm.Run();
        Assert.AreEqual(4, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Append() {
        var vm = Funny.Hardcore.BuildVM("y = append([1,2], 3).count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_GetElement() {
        AssertVmMatchesTreeWalker("y = [10,20,30][1]", "y");
    }

    [Test]
    public void Array_Sum() {
        AssertVmMatchesTreeWalker("y = sum([1,2,3,4])", "y");
    }

    [Test]
    public void Array_Sort() {
        var vm = Funny.Hardcore.BuildVM("y = sort([3,1,2])");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Array_Repeat() {
        var vm = Funny.Hardcore.BuildVM("y = repeat(7, 3).count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Take() {
        var vm = Funny.Hardcore.BuildVM("y = take([1,2,3,4,5], 3).count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Skip() {
        var vm = Funny.Hardcore.BuildVM("y = skip([1,2,3,4,5], 2).count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_InOperator() {
        AssertVmMatchesTreeWalker("y = 2 in [1,2,3]", "y");
    }

    [Test]
    public void Array_InOperator_False() {
        AssertVmMatchesTreeWalker("y = 9 in [1,2,3]", "y");
    }

    [Test]
    public void Array_Flat() {
        var vm = Funny.Hardcore.BuildVM("y = flat([[1,2],[3,4]]).count()");
        vm.Run();
        Assert.AreEqual(4, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Set() {
        var vm = Funny.Hardcore.BuildVM("y = set([10,20,30], 1, 99)");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Array_Any_NonEmpty() {
        AssertVmMatchesTreeWalker("y = any([1,2,3])", "y");
    }

    [Test]
    public void Array_Unique() {
        var vm = Funny.Hardcore.BuildVM("y = unique([1,2,3],[2,3,4]).count()");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ── Array slicing ──

    [Test]
    public void Array_Slice() {
        var vm = Funny.Hardcore.BuildVM("y = [10,20,30,40][1:2]");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Array_SliceFromStart() {
        var vm = Funny.Hardcore.BuildVM("y = [10,20,30,40][:2]");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Array_SliceToEnd() {
        var vm = Funny.Hardcore.BuildVM("y = [10,20,30,40][2:]");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ── Range array init ──

    [Test]
    public void Array_RangeInit() {
        var vm = Funny.Hardcore.BuildVM("y = [1..5].count()");
        vm.Run();
        Assert.AreEqual(5, vm.GetOutput("y"));
    }

    [Test]
    public void Array_RangeInit_Descending() {
        var vm = Funny.Hardcore.BuildVM("y = [5..1].count()");
        vm.Run();
        Assert.AreEqual(5, vm.GetOutput("y"));
    }

    [Test]
    public void Array_RangeWithStep() {
        var vm = Funny.Hardcore.BuildVM("y = [1..7 step 2].count()");
        vm.Run();
        Assert.AreEqual(4, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  12. Lambdas (existing + complex)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Lambda_MapDouble() {
        var vm = Funny.Hardcore.BuildVM("y = [1,2,3].map(rule it * 2)");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Lambda_FilterGt1() {
        var vm = Funny.Hardcore.BuildVM("y = [1,2,3].filter(rule it > 1).count()");
        vm.Run();
        Assert.AreEqual(2, vm.GetOutput("y"));
    }

    [Test]
    public void Lambda_Fold_Sum() {
        AssertVmMatchesTreeWalker("y = [1,2,3,4].fold(rule it1 + it2)", "y");
    }

    [Test]
    public void Lambda_All_True() {
        AssertVmMatchesTreeWalker("y = [2,4,6].all(rule it > 0)", "y");
    }

    [Test]
    public void Lambda_All_False() {
        AssertVmMatchesTreeWalker("y = [2,-1,6].all(rule it > 0)", "y");
    }

    [Test]
    public void Lambda_Any_WithPredicate() {
        AssertVmMatchesTreeWalker("y = [1,2,3].any(rule it > 2)", "y");
    }

    [Test]
    public void Lambda_MapThenFilter() {
        var vm = Funny.Hardcore.BuildVM("y = [1,2,3,4].map(rule it * 2).filter(rule it > 4).count()");
        vm.Run();
        Assert.AreEqual(2, vm.GetOutput("y"));
    }

    [Test]
    public void Lambda_AnnotatedSyntax() {
        AssertVmMatchesTreeWalker("y = [1,2,3].fold(rule(a,b) = a + b)", "y");
    }

    [Test]
    public void Lambda_FoldWithSeed() {
        AssertVmMatchesTreeWalker("y = [1,2,3].fold(0, rule(a,b) = a + b)", "y");
    }

    [Test]
    public void Lambda_SortBy() {
        var vm = Funny.Hardcore.BuildVM("y = [{v=3},{v=1},{v=2}].sort(rule it.v)");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  13. Bitwise Operations
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 0xFF & 0x0F", "y", 0x0F)]
    [TestCase("y = 0xF0 | 0x0F", "y", 0xFF)]
    [TestCase("y = 0xFF ^ 0x0F", "y", 0xF0)]
    public void Bitwise_AndOrXor(string expr, string varName, int expected) {
        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();
        Assert.AreEqual(expected, vm.GetOutput(varName));
    }

    [Test]
    public void Bitwise_Not() {
        var vm = Funny.Hardcore.BuildVM("y:int64 = ~0");
        vm.Run();
        Assert.AreEqual(-1L, vm.GetOutput("y"));
    }

    [Test]
    public void Bitwise_ShiftLeft() {
        var vm = Funny.Hardcore.BuildVM("y = 1 << 3");
        vm.Run();
        Assert.AreEqual(8, vm.GetOutput("y"));
    }

    [Test]
    public void Bitwise_ShiftRight() {
        var vm = Funny.Hardcore.BuildVM("y = 16 >> 2");
        vm.Run();
        Assert.AreEqual(4, vm.GetOutput("y"));
    }

    [Test]
    public void Bitwise_ComplexMask() {
        var vm = Funny.Hardcore.BuildVM("y = (0b1100 & 0b1010) | 0b0001");
        vm.Run();
        Assert.AreEqual(0b1001, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  14. Type Conversions / Annotations
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TypeAnnotation_Byte() {
        var vm = Funny.Hardcore.BuildVM("y:byte = 42");
        vm.Run();
        Assert.AreEqual((byte)42, vm.GetOutput("y"));
    }

    [Test]
    public void TypeAnnotation_Int64() {
        var vm = Funny.Hardcore.BuildVM("y:int64 = 100");
        vm.Run();
        Assert.AreEqual(100L, vm.GetOutput("y"));
    }

    [Test]
    public void TypeAnnotation_Real() {
        var vm = Funny.Hardcore.BuildVM("y:real = 42");
        vm.Run();
        Assert.AreEqual(42.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void ImplicitWidening_ByteToInt() {
        var vm = Funny.Hardcore.BuildVM("a:byte = 10\r y:int = a + 1");
        vm.Run();
        Assert.AreEqual(11, vm.GetOutput("y"));
    }

    [Test]
    public void ImplicitWidening_IntToReal() {
        var vm = Funny.Hardcore.BuildVM("a = 3\r y = a / 2.0");
        vm.Run();
        Assert.AreEqual(1.5, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void TypeAnnotation_Uint32() {
        var vm = Funny.Hardcore.BuildVM("y:uint32 = 42");
        vm.Run();
        Assert.AreEqual(42u, vm.GetOutput("y"));
    }

    [Test]
    public void TypeAnnotation_Int16() {
        var vm = Funny.Hardcore.BuildVM("y:int16 = 42");
        vm.Run();
        Assert.AreEqual((short)42, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  15. Char Literals
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void CharLiteral() {
        var vm = Funny.Hardcore.BuildVM("y = /'a'");
        vm.Run();
        Assert.AreEqual('a', vm.GetOutput("y"));
    }

    [Test]
    public void CharLiteral_Digit() {
        var vm = Funny.Hardcore.BuildVM("y = /'0'");
        vm.Run();
        Assert.AreEqual('0', vm.GetOutput("y"));
    }

    [Test]
    public void CharLiteral_Equality() {
        AssertVmMatchesTreeWalker("y = /'a' == /'a'", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  16. Text / String Operations
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TextLiteral_SingleQuote() {
        var vm = Funny.Hardcore.BuildVM("y = 'hello'");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void TextLiteral_DoubleQuote() {
        var vm = Funny.Hardcore.BuildVM("y = \"hello\"");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_Count() {
        AssertVmMatchesTreeWalker("y = 'hello'.count()", "y");
    }

    [Test]
    public void Text_Reverse() {
        var vm = Funny.Hardcore.BuildVM("y = 'hello'.reverse()");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_Concat() {
        var vm = Funny.Hardcore.BuildVM("y = concat('hello', ' world')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_ToUpper() {
        var vm = Funny.Hardcore.BuildVM("y = toUpper('hello')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_ToLower() {
        var vm = Funny.Hardcore.BuildVM("y = toLower('HELLO')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_Trim() {
        var vm = Funny.Hardcore.BuildVM("y = trim('  hello  ')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_ToText() {
        var vm = Funny.Hardcore.BuildVM("y = toText(42)");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_IndexAccess() {
        // Text is char[]; element access returns char
        AssertVmMatchesTreeWalker("y = 'hello'[0]", "y");
    }

    [Test]
    public void Text_Comparison() {
        AssertVmMatchesTreeWalker("y = 'abc' < 'def'", "y");
    }

    [Test]
    public void Text_Equality() {
        AssertVmMatchesTreeWalker("y = 'hello' == 'hello'", "y");
    }

    [Test]
    public void Text_Split() {
        var vm = Funny.Hardcore.BuildVM("y = split('a,b,c', ',').count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    // ── Text interpolation / templates ──

    [Test]
    public void Text_Template_Simple() {
        var vm = Funny.Hardcore.BuildVM("y = '1 + 2 = {1+2}'");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_Template_Variable() {
        var vm = Funny.Hardcore.BuildVM("a = 42\r y = 'value is {a}'");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  17. Piped Function Calls
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Piped_Abs() {
        AssertVmMatchesTreeWalker("y = (-5).abs()", "y");
    }

    [Test]
    public void Piped_Max() {
        AssertVmMatchesTreeWalker("y = 3.max(7)", "y");
    }

    [Test]
    public void Piped_Chain() {
        var vm = Funny.Hardcore.BuildVM("y = [3,1,2].sort().reverse().count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Piped_ArrayFirst() {
        AssertVmMatchesTreeWalker("y = [10,20,30].first()", "y");
    }

    [Test]
    public void Piped_ArrayLast() {
        AssertVmMatchesTreeWalker("y = [10,20,30].last()", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  18. Complex Expressions (existing + more)
    // ═══════════════════════════════════════════════════════════

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

    [Test]
    public void Complex_MixedArithAndComparison() {
        var vm = Funny.Hardcore.BuildVM("y = (2 + 3) * 4 > 10");
        vm.Run();
        Assert.AreEqual(true, vm.GetOutput("y"));
    }

    [Test]
    public void Complex_Parentheses() {
        var vm = Funny.Hardcore.BuildVM("y = 2 * (3 + 4)");
        vm.Run();
        Assert.AreEqual(14, vm.GetOutput("y"));
    }

    [Test]
    public void Complex_MultipleOpsWithFunctions() {
        var vm = Funny.Hardcore.BuildVM("y = max(2 + 3, 4 * 2) - min(1, 0)");
        vm.Run();
        Assert.AreEqual(8, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  19. Try-Catch (existing + nested)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TryCatch_NoError() {
        var vm = Funny.Hardcore.BuildVM("y = try 42 catch 0");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void TryCatch_WithError() {
        var vm = Funny.Hardcore.BuildVM("y = try oops() catch 99");
        vm.Run();
        Assert.AreEqual(99, vm.GetOutput("y"));
    }

    [Test]
    public void TryCatch_ExprInTry() {
        var vm = Funny.Hardcore.BuildVM("y = try (2 + 3) catch 0");
        vm.Run();
        Assert.AreEqual(5, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  20. delayed() function (existing)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Delayed_TreeWalkerWorks() {
        var tw = Funny.Hardcore
            .WithFunction(NFun.VM.DelayedFunction.Instance)
            .Build("y = delayed(10, 42)");
        tw.Run();
        Assert.AreEqual(42, tw["y"].Value);
    }

    [Test]
    public void Delayed_VMWorks() {
        var vm = Funny.Hardcore
            .WithFunction(NFun.VM.DelayedFunction.Instance)
            .BuildVM("y = delayed(10, 42)");
        vm.Run();
        var result = vm.GetOutput("y");
        Assert.IsNotNull(result);
        Assert.AreEqual(42, Convert.ToInt32(result));
    }

    // ═══════════════════════════════════════════════════════════
    //  21. Optional (existing + safe access + safe indexing)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Optional_Coalesce() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("a:int? = none\r y = a ?? 42");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_ForceUnwrap() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("a:int? = 42\r y = a!");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_IsNone_True() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("a:int? = none\r y = a == none");
        vm.Run();
        Assert.AreEqual(true, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_CoalesceHasValue() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("a:int? = 10\r y = a ?? 42");
        vm.Run();
        Assert.AreEqual(10, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_CoalesceChain() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("a:int? = none\r b:int? = none\r y = a ?? b ?? 99");
        vm.Run();
        Assert.AreEqual(99, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_SafeFieldAccess_HasValue() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("s = if(true) {value = 42} else none\r y = s?.value ?? 0");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_SafeFieldAccess_IsNone() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("s = if(false) {value = 42} else none\r y = s?.value ?? 0");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_SafeArrayAccess_HasValue() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("arr = [10, 20, 30]\r y = arr?[1] ?? 0");
        vm.Run();
        Assert.AreEqual(20, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_SafeArrayAccess_OutOfBounds() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("arr = [10, 20, 30]\r y = arr?[99] ?? 0");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Optional_ImplicitLift() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("y:int? = 42\r z = y ?? 0");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("z"));
    }

    [Test]
    public void Optional_NoneDefault() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("y:int? = none\r z = y ?? -1");
        vm.Run();
        Assert.AreEqual(-1, vm.GetOutput("z"));
    }

    // ═══════════════════════════════════════════════════════════
    //  22. Default Values
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Default_Int() {
        var vm = Funny.Hardcore.BuildVM("y:int = default");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Default_Real() {
        var vm = Funny.Hardcore.BuildVM("y:real = default");
        vm.Run();
        Assert.AreEqual(0.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void Default_Bool() {
        var vm = Funny.Hardcore.BuildVM("y:bool = default");
        vm.Run();
        Assert.AreEqual(false, vm.GetOutput("y"));
    }

    [Test]
    public void Default_InIfElse() {
        var vm = Funny.Hardcore.BuildVM("y = if(false) 42 else default");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  23. Hex and Binary Literals
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void HexLiteral() {
        var vm = Funny.Hardcore.BuildVM("y = 0xFF");
        vm.Run();
        Assert.AreEqual(255, vm.GetOutput("y"));
    }

    [Test]
    public void BinaryLiteral() {
        var vm = Funny.Hardcore.BuildVM("y = 0b1010");
        vm.Run();
        Assert.AreEqual(10, vm.GetOutput("y"));
    }

    [Test]
    public void HexLiteral_Arithmetic() {
        var vm = Funny.Hardcore.BuildVM("y = 0xFF + 1");
        vm.Run();
        Assert.AreEqual(256, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  24. Numeric Separator
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void NumericSeparator() {
        var vm = Funny.Hardcore.BuildVM("y = 1_000 + 234");
        vm.Run();
        Assert.AreEqual(1234, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  25. Oops / Error Function
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Oops_InCatch() {
        var vm = Funny.Hardcore.BuildVM("y = try oops('bad') catch 0");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Oops_NotEvaluated_IfBranchNotTaken() {
        var vm = Funny.Hardcore.BuildVM("y = if(true) 42 else oops()");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  26. Convert Function
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Convert_IntToText() {
        var vm = Funny.Hardcore.BuildVM("y = toText(42)");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Convert_RealToText() {
        var vm = Funny.Hardcore.BuildVM("y = toText(3.14)");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  27. Math Functions
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Math_Sin() {
        var vm = Funny.Hardcore.BuildVM("y = sin(0.0)");
        vm.Run();
        Assert.AreEqual(0.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void Math_Cos() {
        var vm = Funny.Hardcore.BuildVM("y = cos(0.0)");
        vm.Run();
        Assert.AreEqual(1.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void Math_Log() {
        var vm = Funny.Hardcore.BuildVM("y = log(1.0)");
        vm.Run();
        Assert.AreEqual(0.0, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void Math_Exp() {
        var vm = Funny.Hardcore.BuildVM("y = exp(0.0)");
        vm.Run();
        Assert.AreEqual(1.0, (double)vm.GetOutput("y"), 0.0001);
    }

    // ═══════════════════════════════════════════════════════════
    //  28. Comparison of various types
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Compare_RealNumbers() {
        var vm = Funny.Hardcore.BuildVM("y = 3.14 > 2.71");
        vm.Run();
        Assert.AreEqual(true, vm.GetOutput("y"));
    }

    [Test]
    public void Compare_MixedIntReal() {
        var vm = Funny.Hardcore.BuildVM("y = 3 < 3.5");
        vm.Run();
        Assert.AreEqual(true, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  29. Generic User Functions
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void GenericUserFunc_Identity() {
        var vm = Funny.Hardcore.BuildVM("id(x) = x\r y:int = id(42)");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("y"));
    }

    [Test]
    public void GenericUserFunc_ThreeSum_Int() {
        var vm = Funny.Hardcore.BuildVM("threeSum(a,b,c) = a+b+c\r y:int = threeSum(1,2,3)");
        vm.Run();
        Assert.AreEqual(6, vm.GetOutput("y"));
    }

    [Test]
    public void GenericUserFunc_ThreeSum_Real() {
        var vm = Funny.Hardcore.BuildVM("threeSum(a,b,c) = a+b+c\r y:real = threeSum(1.0, 2.0, 3.0)");
        vm.Run();
        Assert.AreEqual(6.0, (double)vm.GetOutput("y"), 0.0001);
    }

    // ═══════════════════════════════════════════════════════════
    //  30. Variable Reuse across equations
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void VariableReuse_SameVarInMultipleEquations() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("a = x + 1\r b = x * 2\r c = a + b");
        vm.SetInput("x", 5);
        vm.Run();
        Assert.AreEqual(6, vm.GetOutput("a"));
        Assert.AreEqual(10, vm.GetOutput("b"));
        Assert.AreEqual(16, vm.GetOutput("c"));
    }

    // ═══════════════════════════════════════════════════════════
    //  31. Boolean expressions in if-else
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void BoolExpr_AndOr_InIfElse() {
        var vm = Funny.Hardcore
            .WithApriori("a", FunnyType.Bool)
            .WithApriori("b", FunnyType.Bool)
            .BuildVM("y = if(a and b) 1\r if(a or b) 2\r else 3");
        vm.SetInput("a", true);
        vm.SetInput("b", false);
        vm.Run();
        Assert.AreEqual(2, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  32. Numeric literals: various types
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Literal_Zero() {
        var vm = Funny.Hardcore.BuildVM("y = 0");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Literal_MaxByte() {
        var vm = Funny.Hardcore.BuildVM("y:byte = 255");
        vm.Run();
        Assert.AreEqual((byte)255, vm.GetOutput("y"));
    }

    [Test]
    public void Literal_NegativeInt() {
        var vm = Funny.Hardcore.BuildVM("y = -100");
        vm.Run();
        Assert.AreEqual(-100, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  33. Real division
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void RealDivision() {
        var vm = Funny.Hardcore.BuildVM("y = 7 / 2.0");
        vm.Run();
        Assert.AreEqual(3.5, (double)vm.GetOutput("y"), 0.0001);
    }

    [Test]
    public void IntDivision_Truncates() {
        var vm = Funny.Hardcore.BuildVM("y = 7 // 2");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  34. Array of different types
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_OfBools() {
        var vm = Funny.Hardcore.BuildVM("y = [true, false, true].count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    [Test]
    public void Array_OfReals() {
        var vm = Funny.Hardcore.BuildVM("y = [1.5, 2.5, 3.5].count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  35. Range function (separate from array [..] syntax)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Range_Function() {
        AssertVmMatchesTreeWalker("y = range(1, 5).count()", "y");
    }

    [Test]
    public void Range_FunctionWithStep() {
        AssertVmMatchesTreeWalker("y = range(1, 10, 3).count()", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  36. Find / Intersect / Except
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_Find() {
        AssertVmMatchesTreeWalker("y = find([10,20,30], 20)", "y");
    }

    [Test]
    public void Array_Find_NotFound() {
        AssertVmMatchesTreeWalker("y = find([10,20,30], 99)", "y");
    }

    [Test]
    public void Array_Intersect() {
        var vm = Funny.Hardcore.BuildVM("y = intersect([1,2,3],[2,3,4]).count()");
        vm.Run();
        Assert.AreEqual(2, vm.GetOutput("y"));
    }

    [Test]
    public void Array_Except() {
        var vm = Funny.Hardcore.BuildVM("y = except([1,2,3],[2,3,4]).count()");
        vm.Run();
        Assert.AreEqual(1, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  37. Chunk
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_Chunk() {
        var vm = Funny.Hardcore.BuildVM("y = chunk([1,2,3,4,5], 2).count()");
        vm.Run();
        Assert.AreEqual(3, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  38. Array Median / Min / Max (array overloads)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_MaxOverload() {
        AssertVmMatchesTreeWalker("y = max([1,5,3])", "y");
    }

    [Test]
    public void Array_MinOverload() {
        AssertVmMatchesTreeWalker("y = min([9,5,3])", "y");
    }

    [Test]
    public void Array_Median() {
        AssertVmMatchesTreeWalker("y = median([1,3,2])", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  39. Complex real-world-like expressions
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Complex_StructWithIf() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("s = {v = if(x > 0) x else 0}\r y = s.v");
        vm.SetInput("x", 5);
        vm.Run();
        Assert.AreEqual(5, vm.GetOutput("y"));
    }

    [Test]
    public void Complex_StructWithIf_Negative() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("s = {v = if(x > 0) x else 0}\r y = s.v");
        vm.SetInput("x", -3);
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Complex_ArrayMapPlusSum() {
        AssertVmMatchesTreeWalker("y = [1,2,3].map(rule it * it).sum()", "y");
    }

    [Test]
    public void Complex_FunctionWithArrayArg() {
        AssertVmMatchesTreeWalker("total(arr) = arr.sum()\r y = total([10,20,30])", "y");
    }

    [Test]
    public void Complex_NestedFunctionCalls() {
        var vm = Funny.Hardcore.BuildVM("y = max(min(10, 20), min(5, 15))");
        vm.Run();
        Assert.AreEqual(10, vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  40. Join function
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Text_Join() {
        var vm = Funny.Hardcore.BuildVM("y = join([1,2,3], ',')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  41. Count with predicate
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_CountWithPredicate() {
        AssertVmMatchesTreeWalker("y = [1,2,3,4,5].count(rule it > 3)", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  42. Sum with transform
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_SumWithTransform() {
        AssertVmMatchesTreeWalker("y = [1,2,3].sum(rule it * it)", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  43. Unite
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_Unite() {
        var vm = Funny.Hardcore.BuildVM("y = unite([1,2,3],[2,3,4]).count()");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  44. Avg
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_Avg() {
        var vm = Funny.Hardcore.BuildVM("y = avg([2.0, 4.0, 6.0])");
        vm.Run();
        Assert.AreEqual(4.0, (double)vm.GetOutput("y"), 0.0001);
    }

    // ═══════════════════════════════════════════════════════════
    //  45. SortDescending
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Array_SortDescending() {
        var vm = Funny.Hardcore.BuildVM("y = sortDescending([1,3,2])");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  46. Text functions: trimStart, trimEnd
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Text_TrimStart() {
        var vm = Funny.Hardcore.BuildVM("y = trimStart('   hello')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void Text_TrimEnd() {
        var vm = Funny.Hardcore.BuildVM("y = trimEnd('hello   ')");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    // ═══════════════════════════════════════════════════════════
    //  47. IP Address literal
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void IpLiteral() {
        var vm = Funny.Hardcore.BuildVM("y = 127.0.0.1");
        vm.Run();
        Assert.IsNotNull(vm.GetOutput("y"));
    }

    [Test]
    public void IpLiteral_Equality() {
        AssertVmMatchesTreeWalker("y = 127.0.0.1 == 127.0.0.1", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  48. Captured variables in lambdas
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Lambda_CapturedVariable() {
        AssertVmMatchesTreeWalker("factor = 10\r y = [1,2,3].map(rule it * factor).sum()", "y");
    }

    // ═══════════════════════════════════════════════════════════
    //  49. Complex optional patterns
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Optional_IfElseNone() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("y = if(true) 42 else none\r z = y ?? 0");
        vm.Run();
        Assert.AreEqual(42, vm.GetOutput("z"));
    }

    [Test]
    public void Optional_IfElseNone_FalseBranch() {
        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM("y = if(false) 42 else none\r z = y ?? 0");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("z"));
    }

    // ═══════════════════════════════════════════════════════════
    //  50. Re-running VM with different inputs
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void ReRun_DifferentInputs() {
        var vm = Funny.Hardcore
            .WithApriori("x", FunnyType.Int32)
            .BuildVM("y = x * x");

        vm.SetInput("x", 3);
        vm.Run();
        Assert.AreEqual(9, vm.GetOutput("y"));

        vm.SetInput("x", 7);
        vm.Run();
        Assert.AreEqual(49, vm.GetOutput("y"));
    }
}
