using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.SyntaxTests.VM;

/// <summary>
/// Differential tests: run each expression through both tree-walker and VM,
/// assert that outputs match. Covers arithmetic, logic, comparisons, if-else,
/// user functions, arrays, structs, text, optional types, try-catch, etc.
/// </summary>
[TestFixture]
public class VMDifferentialTest {

    // ═══════════════════════════════════════════════════════════
    //  Helper: compare all output variables between TW and VM
    // ═══════════════════════════════════════════════════════════

    private static void AssertMatch(string expr) {
        var tw = Funny.Hardcore.Build(expr);
        tw.Run();

        var vm = Funny.Hardcore.BuildVM(expr);
        vm.Run();

        foreach (var v in tw.Variables) {
            if (!v.IsOutput) continue;
            var twVal = v.Value;
            var vmVal = vm.GetOutput(v.Name);
            AssertValuesEqual(twVal, vmVal, v.Name, expr);
        }
    }

    private static void AssertMatchOptional(string expr) {
        var tw = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build(expr);
        tw.Run();

        var vm = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .BuildVM(expr);
        vm.Run();

        foreach (var v in tw.Variables) {
            if (!v.IsOutput) continue;
            var twVal = v.Value;
            var vmVal = vm.GetOutput(v.Name);
            AssertValuesEqual(twVal, vmVal, v.Name, expr);
        }
    }

    private static void AssertValuesEqual(object twVal, object vmVal, string varName, string expr) {
        // Normalize none representations: TW returns null, VM returns FunnyNone
        bool twIsNone = twVal == null || twVal is FunnyNone;
        bool vmIsNone = vmVal == null || vmVal is FunnyNone;
        if (twIsNone && vmIsNone)
            return; // both are none — match
        if (twIsNone != vmIsNone)
            Assert.Fail($"None mismatch on '{varName}' for expr [{expr}]: " +
                $"TW={(twVal == null ? "null" : twVal)}, VM={(vmVal == null ? "null" : vmVal)}");

        if (twVal is double twD && vmVal is double vmD) {
            Assert.AreEqual(twD, vmD, 0.0001,
                $"Mismatch on '{varName}' for expr [{expr}]: TW={twD}, VM={vmD}");
            return;
        }
        if (twVal is IFunnyArray twArr && vmVal is IFunnyArray vmArr) {
            Assert.AreEqual(twArr.Count, vmArr.Count,
                $"Array length mismatch on '{varName}' for expr [{expr}]: TW.Count={twArr.Count}, VM.Count={vmArr.Count}");
            for (int i = 0; i < twArr.Count; i++) {
                var twElem = twArr.GetElementOrNull(i);
                var vmElem = vmArr.GetElementOrNull(i);
                AssertValuesEqual(twElem, vmElem, $"{varName}[{i}]", expr);
            }
            return;
        }
        // For numeric types that might differ (e.g., Int32 vs Int64), compare via Convert
        if (twVal != null && vmVal != null && IsNumeric(twVal) && IsNumeric(vmVal)) {
            var twNum = Convert.ToDouble(twVal);
            var vmNum = Convert.ToDouble(vmVal);
            Assert.AreEqual(twNum, vmNum, 0.0001,
                $"Numeric mismatch on '{varName}' for expr [{expr}]: " +
                $"TW({twVal.GetType().Name})={twVal}, VM({vmVal.GetType().Name})={vmVal}");
            return;
        }
        Assert.AreEqual(twVal, vmVal,
            $"Mismatch on '{varName}' for expr [{expr}]: " +
            $"TW({twVal?.GetType()?.Name})={twVal}, VM({vmVal?.GetType()?.Name})={vmVal}");
    }

    private static bool IsNumeric(object o) =>
        o is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    // ═══════════════════════════════════════════════════════════
    //  1. Integer Arithmetic
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 2 + 3")]
    [TestCase("y = 10 - 3")]
    [TestCase("y = 4 * 5")]
    [TestCase("y = 10 // 3")]
    [TestCase("y = 42")]
    [TestCase("y = 0")]
    [TestCase("y = -100")]
    [TestCase("y = 10 % 3")]
    [TestCase("y = 2 ** 10")]
    [TestCase("y = -(5 + 3)")]
    [TestCase("y = (2 + 3) * 4 - 1")]
    [TestCase("y = 2 * (3 + 4)")]
    [TestCase("y = 1_000 + 234")]
    public void IntArithmetic(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  2. Real Arithmetic
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 2.5 + 1.5")]
    [TestCase("y = 10.0 / 3.0")]
    [TestCase("y = -3.14")]
    [TestCase("y = 1000000.0 * 1000000.0")]
    [TestCase("y = 0.001 * 0.001")]
    [TestCase("y = 7 / 2.0")]
    [TestCase("y = 7 // 2")]
    [TestCase("y = 3.14 + 2.71")]
    [TestCase("y = 100.0 - 0.001")]
    public void RealArithmetic(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  3. Boolean Logic
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = true")]
    [TestCase("y = false")]
    [TestCase("y = true and false")]
    [TestCase("y = true or false")]
    [TestCase("y = not true")]
    [TestCase("y = true xor false")]
    [TestCase("y = true xor true")]
    [TestCase("y = false xor false")]
    [TestCase("y = not (true and false)")]
    [TestCase("y = (true or false) and (not false)")]
    public void BoolLogic(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  4. Comparisons
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 5 > 3")]
    [TestCase("y = 5 < 3")]
    [TestCase("y = 5 == 5")]
    [TestCase("y = 5 != 3")]
    [TestCase("y = 3 >= 3")]
    [TestCase("y = 3 <= 3")]
    [TestCase("y = 4 >= 5")]
    [TestCase("y = 6 <= 5")]
    [TestCase("y = 1 < 2 < 3")]
    [TestCase("y = 1 < 2 < 2")]
    [TestCase("y = 3.14 > 2.71")]
    [TestCase("y = 3 < 3.5")]
    [TestCase("y = 'abc' < 'def'")]
    [TestCase("y = 'hello' == 'hello'")]
    public void Comparisons(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  5. If-Else
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = if(true) 1 else 2")]
    [TestCase("y = if(false) 1 else 2")]
    [TestCase("y = if(true) if(false) 1 else 2 else 3")]
    [TestCase("y = if(false) 42 else default")]
    [TestCase("y = if(true) 42 else oops()")]
    public void IfElse(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  6. Multiple Equations
    // ═══════════════════════════════════════════════════════════

    [TestCase("a = 10\r b = a + 5")]
    [TestCase("a = 10\r b = a * 2\r c = b + a")]
    [TestCase("a = 1\r b = 2\r c = a + b\r d = c * c")]
    public void MultipleEquations(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  7. Built-in Functions
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = max(3, 7)")]
    [TestCase("y = min(3, 7)")]
    [TestCase("y = abs(-42)")]
    [TestCase("y = sqrt(16.0)")]
    [TestCase("y = round(3.567, 2)")]
    [TestCase("y = ceil(7.03)")]
    [TestCase("y = floor(7.99)")]
    [TestCase("y = sin(0.0)")]
    [TestCase("y = cos(0.0)")]
    [TestCase("y = log(1.0)")]
    [TestCase("y = exp(0.0)")]
    [TestCase("y = max(2 + 3, 4 * 2) - min(1, 0)")]
    [TestCase("y = max(min(10, 20), min(5, 15))")]
    public void BuiltInFunctions(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  8. User Functions
    // ═══════════════════════════════════════════════════════════

    [TestCase("f(x) = x * 2\r y = f(21)")]
    [TestCase("add(a,b) = a + b\r y = add(10, 32)")]
    [TestCase("sumOf3(a,b,c) = a + b + c\r y = sumOf3(10, 20, 12)")]
    [TestCase("double(x) = x * 2\r quadruple(x) = double(double(x))\r y = quadruple(10)")]
    [TestCase("myAbs(x) = if(x < 0) -x else x\r y = myAbs(-7)")]
    [TestCase("id(x) = x\r y:int = id(42)")]
    [TestCase("threeSum(a,b,c) = a+b+c\r y:int = threeSum(1,2,3)")]
    [TestCase("threeSum(a,b,c) = a+b+c\r y:real = threeSum(1.0, 2.0, 3.0)")]
    public void UserFunctions(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  9. Structs
    // ═══════════════════════════════════════════════════════════

    [TestCase("s = {x = 1, y = 2}\r out = s.x + s.y")]
    [TestCase("s = {a = 10, b = 20, c = 30}\r y = s.a + s.b + s.c")]
    [TestCase("s = {inner = {value = 42}}\r y = s.inner.value")]
    [TestCase("s = {a = {b = {c = 99}}}\r y = s.a.b.c")]
    [TestCase("s = {flag = true, count = 5}\r y = if(s.flag) s.count else 0")]
    public void Structs(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  10. Arrays — basic
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = [1,2,3].count()")]
    [TestCase("y = [10,20,30][1]")]
    [TestCase("y = first([10,20,30])")]
    [TestCase("y = last([10,20,30])")]
    [TestCase("y = reverse([1,2,3]).count()")]
    [TestCase("y = concat([1,2],[3,4]).count()")]
    [TestCase("y = append([1,2], 3).count()")]
    [TestCase("y = sum([1,2,3,4])")]
    [TestCase("y = sort([3,1,2]).count()")]
    [TestCase("y = repeat(7, 3).count()")]
    [TestCase("y = take([1,2,3,4,5], 3).count()")]
    [TestCase("y = skip([1,2,3,4,5], 2).count()")]
    [TestCase("y = 2 in [1,2,3]")]
    [TestCase("y = 9 in [1,2,3]")]
    [TestCase("y = flat([[1,2],[3,4]]).count()")]
    [TestCase("y = [true, false, true].count()")]
    [TestCase("y = [1.5, 2.5, 3.5].count()")]
    [TestCase("y = max([1,5,3])")]
    [TestCase("y = min([9,5,3])")]
    [TestCase("y = median([1,3,2])")]
    [TestCase("y = avg([2.0, 4.0, 6.0])")]
    [TestCase("y = find([10,20,30], 20)")]
    [TestCase("y = find([10,20,30], 99)")]
    [TestCase("y = intersect([1,2,3],[2,3,4]).count()")]
    [TestCase("y = except([1,2,3],[2,3,4]).count()")]
    [TestCase("y = chunk([1,2,3,4,5], 2).count()")]
    public void Arrays(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  11. Array slicing and range
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = [1..5].count()")]
    [TestCase("y = [5..1].count()")]
    [TestCase("y = [1..7 step 2].count()")]
    [TestCase("y = range(1, 5).count()")]
    [TestCase("y = range(1, 10, 3).count()")]
    public void ArrayRanges(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  12. Lambdas (map, filter, fold, all, any)
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = [1,2,3].map(rule it * 2).count()")]
    [TestCase("y = [1,2,3].filter(rule it > 1).count()")]
    [TestCase("y = [1,2,3,4].fold(rule it1 + it2)")]
    [TestCase("y = [2,4,6].all(rule it > 0)")]
    [TestCase("y = [2,-1,6].all(rule it > 0)")]
    [TestCase("y = [1,2,3].any(rule it > 2)")]
    [TestCase("y = [1,2,3,4].map(rule it * 2).filter(rule it > 4).count()")]
    [TestCase("y = [1,2,3].fold(rule(a,b) = a + b)")]
    [TestCase("y = [1,2,3].fold(0, rule(a,b) = a + b)")]
    [TestCase("y = [1,2,3,4,5].count(rule it > 3)")]
    [TestCase("y = [1,2,3].sum(rule it * it)")]
    [TestCase("y = [1,2,3].map(rule it * it).sum()")]
    [TestCase("factor = 10\r y = [1,2,3].map(rule it * factor).sum()")]
    public void Lambdas(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  13. Higher-order calls
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = (rule it * 2)(21)")]
    [TestCase("y = (rule it + 10)(32)")]
    public void HigherOrder(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  14. Bitwise Operations
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 0xFF & 0x0F")]
    [TestCase("y = 0xF0 | 0x0F")]
    [TestCase("y = 0xFF ^ 0x0F")]
    [TestCase("y = 1 << 3")]
    [TestCase("y = 16 >> 2")]
    [TestCase("y = (0b1100 & 0b1010) | 0b0001")]
    public void Bitwise(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  15. Type Annotations and Conversions
    // ═══════════════════════════════════════════════════════════

    [TestCase("y:byte = 42")]
    [TestCase("y:int64 = 100")]
    [TestCase("y:real = 42")]
    [TestCase("y:uint32 = 42")]
    [TestCase("y:int16 = 42")]
    [TestCase("y:byte = 255")]
    [TestCase("a:byte = 10\r y:int = a + 1")]
    [TestCase("a = 3\r y = a / 2.0")]
    public void TypeAnnotations(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  16. Default Values
    // ═══════════════════════════════════════════════════════════

    [TestCase("y:int = default")]
    [TestCase("y:real = default")]
    [TestCase("y:bool = default")]
    public void DefaultValues(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  17. Hex and Binary Literals
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 0xFF")]
    [TestCase("y = 0b1010")]
    [TestCase("y = 0xFF + 1")]
    public void HexBinaryLiterals(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  18. Char Literals
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = /'a'")]
    [TestCase("y = /'0'")]
    [TestCase("y = /'a' == /'a'")]
    public void CharLiterals(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  19. Text / String Operations
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 'hello'")]
    [TestCase("y = \"hello\"")]
    [TestCase("y = 'hello'.count()")]
    [TestCase("y = 'hello'[0]")]
    [TestCase("y = split('a,b,c', ',').count()")]
    [TestCase("y = toText(42)")]
    [TestCase("y = toText(3.14)")]
    [TestCase("y = join([1,2,3], ',')")]
    [TestCase("a = 42\r y = 'value is {a}'")]
    [TestCase("y = '1 + 2 = {1+2}'")]
    public void TextOperations(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  20. Piped Function Calls
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = (-5).abs()")]
    [TestCase("y = 3.max(7)")]
    [TestCase("y = [3,1,2].sort().reverse().count()")]
    [TestCase("y = [10,20,30].first()")]
    [TestCase("y = [10,20,30].last()")]
    public void PipedCalls(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  21. Try-Catch
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = try 42 catch 0")]
    [TestCase("y = try oops() catch 99")]
    [TestCase("y = try (2 + 3) catch 0")]
    [TestCase("y = try oops('bad') catch 0")]
    public void TryCatch(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  22. IP Address Literals
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 127.0.0.1 == 127.0.0.1")]
    public void IpLiterals(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  23. Optional Types (requires dialect)
    // ═══════════════════════════════════════════════════════════

    [TestCase("a:int? = none\r y = a ?? 42")]
    [TestCase("a:int? = 42\r y = a!")]
    [TestCase("a:int? = none\r y = a == none")]
    [TestCase("a:int? = 10\r y = a ?? 42")]
    [TestCase("a:int? = none\r b:int? = none\r y = a ?? b ?? 99")]
    [TestCase("y:int? = 42\r z = y ?? 0")]
    [TestCase("y:int? = none\r z = y ?? -1")]
    [TestCase("y = if(true) 42 else none\r z = y ?? 0")]
    [TestCase("y = if(false) 42 else none\r z = y ?? 0")]
    [TestCase("s = if(true) {value = 42} else none\r y = s?.value ?? 0")]
    [TestCase("s = if(false) {value = 42} else none\r y = s?.value ?? 0")]
    [TestCase("arr = [10, 20, 30]\r y = arr?[1] ?? 0")]
    [TestCase("arr = [10, 20, 30]\r y = arr?[99] ?? 0")]
    public void OptionalTypes(string expr) => AssertMatchOptional(expr);

    // ═══════════════════════════════════════════════════════════
    //  24. Complex / Real-world expressions
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = (2 + 3) * 4 > 10")]
    [TestCase("y = max(2 + 3, 4 * 2) - min(1, 0)")]
    [TestCase("y = if(true) 1 else if(false) 2 else 3")]
    [TestCase("y = [1,2,3,4,5].filter(rule it % 2 == 0).count()")]
    [TestCase("y = [1,2,3].map(rule it * 10).filter(rule it > 15).count()")]
    [TestCase("price = 100\r tax = 20\r y = price + price * tax / 100")]
    [TestCase("y = max(abs(-5), abs(3))")]
    [TestCase("y = [1,2,3,4,5].filter(rule it > 2).map(rule it * it).sum()")]
    public void Complex(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  25. Edge cases
    // ═══════════════════════════════════════════════════════════

    [TestCase("y = 0 * 999")]
    [TestCase("y = 1 * 1")]
    [TestCase("y = 0 + 0")]
    [TestCase("y = true == true")]
    [TestCase("y = false == false")]
    [TestCase("y = [1].count()")]
    [TestCase("y = [1].first()")]
    [TestCase("y = [1].last()")]
    [TestCase("y = repeat(0, 5).count()")]
    [TestCase("y = if(1 > 0) 'yes' else 'no'")]
    [TestCase("y = [1,2,3,4,5].map(rule it + 1).count()")]
    public void EdgeCases(string expr) => AssertMatch(expr);

    // ═══════════════════════════════════════════════════════════
    //  26. Known VM bugs (isolated, ignored)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void BoolNotEqual() => AssertMatch("y = true != false");
}
