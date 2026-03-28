using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class BuiltInFunctionsArgumentNamesTest {

    // ── Math: single-arg ────────────────────────────────────────────────────

    [Test] public void Sqrt() => "y = sqrt(x = 4.0)".AssertReturns("y", 2.0);
    [Test] public void Sin() => "y = sin(x = 0.0)".AssertReturns("y", 0.0);
    [Test] public void Cos() => "y = cos(x = 0.0)".AssertReturns("y", 1.0);
    [Test] public void Tan() => "y = tan(x = 0.0)".AssertReturns("y", 0.0);
    [Test] public void Atan() => "y = atan(x = 0.0)".AssertReturns("y", 0.0);
    [Test] public void Asin() => "y = asin(x = 0.0)".AssertReturns("y", 0.0);
    [Test] public void Acos() => "y = acos(x = 1.0)".AssertReturns("y", 0.0);
    [Test] public void Exp() => "y = exp(x = 0.0)".AssertReturns("y", 1.0);
    [Test] public void Log1Arg() => "y = log(x = 1.0)".AssertReturns("y", 0.0);
    [Test] public void Log10() => "y = log10(x = 1.0)".AssertReturns("y", 0.0);
    [Test] public void Abs() => "y = abs(x = -5)".AssertReturns("y", 5);

    // ── Math: two-arg ───────────────────────────────────────────────────────

    [Test] public void Atan2_Reversed() =>
        "y = atan2(x = 1.0, y = 0.0)".AssertReturns("y", 0.0);

    [Test] public void Log2Arg_Reversed() =>
        "y = log(newBase = 10.0, value = 100.0)".AssertReturns("y", 2.0);

    [Test] public void Round_Named() =>
        "y = round(digits = 1, value = 3.14)".AssertReturns("y", 3.1);

    // ── Comparison: min/max ─────────────────────────────────────────────────

    [Test] public void Max_AllNamed() =>
        "y = max(b = 1, a = 5)".AssertReturns("y", 5);

    [Test] public void Max_Reversed() =>
        "y = max(a = 1, b = 5)".AssertReturns("y", 5);

    [Test] public void Min_AllNamed() =>
        "y = min(b = 10, a = 3)".AssertReturns("y", 3);

    [Test] public void Min_Reversed() =>
        "y = min(a = 10, b = 3)".AssertReturns("y", 3);

    // ── Comparison: max/min of array (single-arg) ───────────────────────────

    [Test] public void MaxArray() =>
        "y = max(arr = [1,5,3])".AssertReturns("y", 5);

    [Test] public void MinArray() =>
        "y = min(arr = [1,5,3])".AssertReturns("y", 1);

    // ── Text functions ──────────────────────────────────────────────────────

    [Test] public void ToText() =>
        "y = toText(value = 42)".AssertReturns("y", "42");

    [Test] public void Trim() =>
        "y = trim(str = '  hi  ')".AssertReturns("y", "hi");

    [Test] public void TrimStart() =>
        "y = trimStart(str = '  hi')".AssertReturns("y", "hi");

    [Test] public void TrimEnd() =>
        "y = trimEnd(str = 'hi  ')".AssertReturns("y", "hi");

    [Test] public void ToUpper() =>
        "y = toUpper(str = 'hi')".AssertReturns("y", "HI");

    [Test] public void ToLower() =>
        "y = toLower(str = 'HI')".AssertReturns("y", "hi");

    [Test] public void Split_Named() =>
        "y = split(separator = '-', str = 'a-b')".AssertReturns("y", new[] { "a", "b" });

    [Test] public void Join_Named() =>
        "y = join(separator = '-', arr = ['a','b'])".AssertReturns("y", "a-b");

    // ── Array: single-arg ───────────────────────────────────────────────────

    [Test] public void First() =>
        "y = first(arr = [10,20,30])".AssertReturns("y", 10);

    [Test] public void Last() =>
        "y = last(arr = [10,20,30])".AssertReturns("y", 30);

    [Test] public void Count1Arg() =>
        "y = count(arr = [10,20,30])".AssertReturns("y", 3);

    [Test] public void Sort1Arg() =>
        "y = sort(arr = [3,1,2])".AssertReturns("y", new[] { 1, 2, 3 });

    [Test] public void SortDescending1Arg() =>
        "y = sortDescending(arr = [3,1,2])".AssertReturns("y", new[] { 3, 2, 1 });

    [Test] public void Median() =>
        "y = median(arr = [3,1,2])".AssertReturns("y", 2);

    [Test] public void Sum1Arg() =>
        "y = sum(arr = [1,2,3])".AssertReturns("y", 6);

    [Test] public void Avg() =>
        "y = avg(arr = [2.0, 4.0])".AssertReturns("y", 3.0);

    [Test] public void Any1Arg() =>
        "y = any(arr = [1,2])".AssertReturns("y", true);

    [Test] public void Flat() =>
        "y = flat(arr = [[1,2],[3]])".AssertReturns("y", new[] { 1, 2, 3 });

    [Test] public void Reverse() =>
        "y = reverse(arr = [1,2,3])".AssertReturns("y", new[] { 3, 2, 1 });

    // ── Array: two-arg ──────────────────────────────────────────────────────

    [Test] public void Map_Named() =>
        "y = map(f = rule(it:int)=it*2, arr = [1,2,3])".AssertReturns("y", new[] { 2, 4, 6 });

    [Test] public void Filter_Named() =>
        "y = filter(predicate = rule(it:int)=it>1, arr = [1,2,3])".AssertReturns("y", new[] { 2, 3 });

    [Test] public void Any2Arg_Named() =>
        "y = any(predicate = rule(it:int)=it>2, arr = [1,2,3])".AssertReturns("y", true);

    [Test] public void All_Named() =>
        "y = all(predicate = rule(it:int)=it>0, arr = [1,2,3])".AssertReturns("y", true);

    [Test] public void Count2Arg_Named() =>
        "y = count(predicate = rule(it:int)=it>1, arr = [1,2,3])".AssertReturns("y", 2);

    [Test] public void Sort2Arg_Named() =>
        "y = sort(selector = rule(it:int)= -it, arr = [1,2,3])".AssertReturns("y", new[] { 3, 2, 1 });

    [Test] public void SortDescending2Arg_Named() =>
        "y = sortDescending(selector = rule(it:int)= -it, arr = [3,2,1])".AssertReturns("y", new[] { 1, 2, 3 });

    [Test] public void Fold2Arg_Named() =>
        "y = fold(f = rule(a:int,b:int)=a+b, arr = [1,2,3])".AssertReturns("y", 6);

    [Test] public void Find_Named() =>
        "y = find(element = 2, arr = [1,2,3])".AssertReturns("y", 1);

    [Test] public void Chunk_Named() =>
        "y = chunk(size = 2, arr = [1,2,3,4])".AssertReturns("y", new[] { new[] { 1, 2 }, new[] { 3, 4 } });

    [Test] public void Take_Named() =>
        "y = take(count = 2, arr = [1,2,3])".AssertReturns("y", new[] { 1, 2 });

    [Test] public void Skip_Named() =>
        "y = skip(count = 1, arr = [1,2,3])".AssertReturns("y", new[] { 2, 3 });

    [Test] public void Append_Named() =>
        "y = append(element = 4, arr = [1,2,3])".AssertReturns("y", new[] { 1, 2, 3, 4 });

    [Test] public void Repeat_Named() =>
        "y = repeat(count = 3, element = 7)".AssertReturns("y", new[] { 7, 7, 7 });

    [Test] public void Concat_Named() =>
        "y = concat(b = [3,4], a = [1,2])".AssertReturns("y", new[] { 1, 2, 3, 4 });

    [Test] public void Unite_Named() =>
        "y = unite(b = [2,3], a = [1,2])".AssertReturns("y", new[] { 1, 2, 3 });

    [Test] public void Intersect_Named() =>
        "y = intersect(b = [2,3], a = [1,2])".AssertReturns("y", new[] { 2 });

    [Test] public void Except_Named() =>
        "y = except(b = [2], a = [1,2,3])".AssertReturns("y", new[] { 1, 3 });

    [Test] public void Unique_Named() =>
        "y = unique(b = [2,3], a = [1,2])".AssertReturns("y", new[] { 1, 3 });

    // ── Array: three-arg ────────────────────────────────────────────────────

    [Test] public void Set_Named() =>
        "y = set(value = 9, index = 1, arr = [1,2,3])".AssertReturns("y", new[] { 1, 9, 3 });

    [Test] public void Slice3_Named() =>
        "y = slice(to = 2, from = 0, arr = [1,2,3,4,5])".AssertReturns("y", new[] { 1, 2, 3 });

    [Test] public void Fold3Arg_Named() =>
        "y = fold(f = rule(a:int,b:int)=a+b, seed = 10, arr = [1,2,3])".AssertReturns("y", 16);

    // ── Array: four-arg ─────────────────────────────────────────────────────

    [Test] public void Slice4_Named() =>
        "y = slice(step = 2, to = 4, from = 0, arr = [1,2,3,4,5])".AssertReturns("y", new[] { 1, 3, 5 });

    // ── Range functions ─────────────────────────────────────────────────────

    [Test] public void Range_Named() =>
        "y = range(to = 3, from = 1)".AssertReturns("y", new[] { 1, 2, 3 });

    [Test] public void RangeStep_Named() =>
        "y = range(step = 2, to = 5, from = 1)".AssertReturns("y", new[] { 1, 3, 5 });

    // ── Sum with mapper ─────────────────────────────────────────────────────

    [Test] public void Sum2Arg_Named() =>
        "y = sum(f = rule(it:int)=it*2, arr = [1,2,3])".AssertReturns("y", 12);

    // ── Mixed positional + named for built-in ───────────────────────────────

    [Test] public void Max_FirstPositional_SecondNamed() =>
        "y = max(5, b = 1)".AssertReturns("y", 5);

    [Test] public void Split_FirstPositional_SecondNamed() =>
        "y = split('a-b', separator = '-')".AssertReturns("y", new[] { "a", "b" });

    [Test] public void Take_FirstPositional_SecondNamed() =>
        "y = take([1,2,3], count = 2)".AssertReturns("y", new[] { 1, 2 });

    // ── Pipe-forward with built-in named args ───────────────────────────────

    [Test] public void PipeForward_Max_Named() =>
        "y = 5.max(b = 1)".AssertReturns("y", 5);

    [Test] public void PipeForward_Take_Named() =>
        "y = [1,2,3].take(count = 2)".AssertReturns("y", new[] { 1, 2 });

    [Test] public void PipeForward_Split_Named() =>
        "y = 'a-b'.split(separator = '-')".AssertReturns("y", new[] { "a", "b" });

    // ── Error cases ─────────────────────────────────────────────────────────

    [Test] public void Error_UnknownNamedArg_BuiltIn() =>
        Assert.Throws<FunnyParseException>(() =>
            "y = max(z = 1, a = 5)".Build());

    [Test] public void Error_DuplicateNamedArg_BuiltIn() =>
        Assert.Throws<FunnyParseException>(() =>
            "y = max(a = 1, a = 5)".Build());

    [Test] public void Error_OverlapsPositional_BuiltIn() =>
        Assert.Throws<FunnyParseException>(() =>
            "y = max(5, a = 1)".Build());

    [Test] public void Error_NamedArgsOnOperator() =>
        Assert.Throws<FunnyParseException>(() =>
            "y = 1 + (b = 2)".Build());
}
