using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class BuiltInFunctionsArgumentNamesTest {

    // ── Math: single-arg ────────────────────────────────────────────────────
    [TestCase("y = sqrt(x = 4.0)", 2.0)]
    [TestCase("y = sin(x = 0.0)", 0.0)]
    [TestCase("y = cos(x = 0.0)", 1.0)]
    [TestCase("y = tan(x = 0.0)", 0.0)]
    [TestCase("y = atan(x = 0.0)", 0.0)]
    [TestCase("y = asin(x = 0.0)", 0.0)]
    [TestCase("y = acos(x = 1.0)", 0.0)]
    [TestCase("y = exp(x = 0.0)", 1.0)]
    [TestCase("y = log(x = 1.0)", 0.0)]
    [TestCase("y = log10(x = 1.0)", 0.0)]
    // ── Math: two-arg ───────────────────────────────────────────────────────
    [TestCase("y = atan2(x = 1.0, y = 0.0)", 0.0)]                  // reversed
    [TestCase("y = log(newBase = 10.0, value = 100.0)", 2.0)]       // reversed
    [TestCase("y = round(digits = 1, value = 3.14)", 3.1)]
    // ── Array: single-arg ───────────────────────────────────────────────────
    [TestCase("y = avg(arr = [2.0, 4.0])", 3.0)]
    public void NamedArgs_ReturnsReal(string expr, double expected) =>
        expr.AssertReturns("y", expected);

    // ── Math: single-arg ────────────────────────────────────────────────────
    [TestCase("y = abs(x = -5)", 5)]
    // ── Comparison: min/max ─────────────────────────────────────────────────
    [TestCase("y = max(b = 1, a = 5)", 5)]                          // all named
    [TestCase("y = max(a = 1, b = 5)", 5)]                          // reversed
    [TestCase("y = min(b = 10, a = 3)", 3)]                         // all named
    [TestCase("y = min(a = 10, b = 3)", 3)]                         // reversed
    // ── Comparison: max/min of array (single-arg) ───────────────────────────
    [TestCase("y = max(arr = [1,5,3])", 5)]
    [TestCase("y = min(arr = [1,5,3])", 1)]
    // ── Array: single-arg ───────────────────────────────────────────────────
    [TestCase("y = first(arr = [10,20,30])", 10)]
    [TestCase("y = last(arr = [10,20,30])", 30)]
    [TestCase("y = count(arr = [10,20,30])", 3)]
    [TestCase("y = median(arr = [3,1,2])", 2)]
    [TestCase("y = sum(arr = [1,2,3])", 6)]
    // ── Array: two-arg ──────────────────────────────────────────────────────
    [TestCase("y = count(predicate = rule(it:int)=it>1, arr = [1,2,3])", 2)]
    [TestCase("y = fold(f = rule(a:int,b:int)=a+b, arr = [1,2,3])", 6)]
    [TestCase("y = find(element = 2, arr = [1,2,3])", 1)]
    // ── Array: three-arg ────────────────────────────────────────────────────
    [TestCase("y = fold(f = rule(a:int,b:int)=a+b, seed = 10, arr = [1,2,3])", 16)]
    // ── Sum with mapper ─────────────────────────────────────────────────────
    [TestCase("y = sum(f = rule(it:int)=it*2, arr = [1,2,3])", 12)]
    // ── Mixed positional + named for built-in ───────────────────────────────
    [TestCase("y = max(5, b = 1)", 5)]
    // ── Pipe-forward with built-in named args ───────────────────────────────
    [TestCase("y = 5.max(b = 1)", 5)]
    public void NamedArgs_ReturnsInt(string expr, int expected) =>
        expr.AssertReturns("y", expected);

    // ── Array: any/all predicates ───────────────────────────────────────────
    [TestCase("y = any(arr = [1,2])", true)]
    [TestCase("y = any(predicate = rule(it:int)=it>2, arr = [1,2,3])", true)]
    [TestCase("y = all(predicate = rule(it:int)=it>0, arr = [1,2,3])", true)]
    public void NamedArgs_ReturnsBool(string expr, bool expected) =>
        expr.AssertReturns("y", expected);

    // ── Text functions ──────────────────────────────────────────────────────
    [TestCase("y = toText(value = 42)", "42")]
    [TestCase("y = trim(str = '  hi  ')", "hi")]
    [TestCase("y = trimStart(str = '  hi')", "hi")]
    [TestCase("y = trimEnd(str = 'hi  ')", "hi")]
    [TestCase("y = toUpper(str = 'hi')", "HI")]
    [TestCase("y = toLower(str = 'HI')", "hi")]
    [TestCase("y = join(separator = '-', arr = ['a','b'])", "a-b")]
    public void NamedArgs_ReturnsText(string expr, string expected) =>
        expr.AssertReturns("y", expected);

    // ── Text functions / mixed positional + named / pipe-forward ────────────
    [TestCase("y = split(separator = '-', str = 'a-b')", new[] { "a", "b" })]
    [TestCase("y = split('a-b', separator = '-')", new[] { "a", "b" })]      // first positional
    [TestCase("y = 'a-b'.split(separator = '-')", new[] { "a", "b" })]       // pipe-forward
    public void NamedArgs_ReturnsTextArray(string expr, string[] expected) =>
        expr.AssertReturns("y", expected);

    // ── Array: single-arg ───────────────────────────────────────────────────
    [TestCase("y = sort(arr = [3,1,2])", new[] { 1, 2, 3 })]
    [TestCase("y = sortDescending(arr = [3,1,2])", new[] { 3, 2, 1 })]
    [TestCase("y = flat(arr = [[1,2],[3]])", new[] { 1, 2, 3 })]
    [TestCase("y = reverse(arr = [1,2,3])", new[] { 3, 2, 1 })]
    // ── Array: two-arg ──────────────────────────────────────────────────────
    [TestCase("y = map(f = rule(it:int)=it*2, arr = [1,2,3])", new[] { 2, 4, 6 })]
    [TestCase("y = filter(predicate = rule(it:int)=it>1, arr = [1,2,3])", new[] { 2, 3 })]
    [TestCase("y = sort(selector = rule(it:int)= -it, arr = [1,2,3])", new[] { 3, 2, 1 })]
    [TestCase("y = sortDescending(selector = rule(it:int)= -it, arr = [3,2,1])", new[] { 1, 2, 3 })]
    [TestCase("y = take(count = 2, arr = [1,2,3])", new[] { 1, 2 })]
    [TestCase("y = skip(count = 1, arr = [1,2,3])", new[] { 2, 3 })]
    [TestCase("y = append(element = 4, arr = [1,2,3])", new[] { 1, 2, 3, 4 })]
    [TestCase("y = repeat(count = 3, element = 7)", new[] { 7, 7, 7 })]
    [TestCase("y = concat(b = [3,4], a = [1,2])", new[] { 1, 2, 3, 4 })]
    [TestCase("y = unite(b = [2,3], a = [1,2])", new[] { 1, 2, 3 })]
    [TestCase("y = intersect(b = [2,3], a = [1,2])", new[] { 2 })]
    [TestCase("y = except(b = [2], a = [1,2,3])", new[] { 1, 3 })]
    [TestCase("y = unique(b = [2,3], a = [1,2])", new[] { 1, 3 })]
    // ── Array: three-arg ────────────────────────────────────────────────────
    [TestCase("y = set(value = 9, index = 1, arr = [1,2,3])", new[] { 1, 9, 3 })]
    [TestCase("y = slice(to = 2, from = 0, arr = [1,2,3,4,5])", new[] { 1, 2, 3 })]
    // ── Array: four-arg ─────────────────────────────────────────────────────
    [TestCase("y = slice(step = 2, to = 4, from = 0, arr = [1,2,3,4,5])", new[] { 1, 3, 5 })]
    // ── Range functions ─────────────────────────────────────────────────────
    [TestCase("y = range(to = 3, from = 1)", new[] { 1, 2, 3 })]
    [TestCase("y = range(step = 2, to = 5, from = 1)", new[] { 1, 3, 5 })]
    // ── Mixed positional + named / pipe-forward ─────────────────────────────
    [TestCase("y = take([1,2,3], count = 2)", new[] { 1, 2 })]
    [TestCase("y = [1,2,3].take(count = 2)", new[] { 1, 2 })]
    public void NamedArgs_ReturnsIntArray(string expr, int[] expected) =>
        expr.AssertReturns("y", expected);

    // chunk returns a jagged array — not expressible as an attribute constant.
    [Test]
    public void Chunk_Named() =>
        "y = chunk(size = 2, arr = [1,2,3,4])".AssertReturns("y", new[] { new[] { 1, 2 }, new[] { 3, 4 } });

    // ── Error cases ─────────────────────────────────────────────────────────
    [TestCase("y = max(z = 1, a = 5)")]  // unknown named arg
    [TestCase("y = max(a = 1, a = 5)")]  // duplicate named arg
    [TestCase("y = max(5, a = 1)")]      // named arg overlaps positional
    [TestCase("y = 1 + (b = 2)")]        // named args on operator
    public void NamedArgs_InvalidUsage_Throws(string expr) =>
        Assert.Throws<FunnyParseException>(() => expr.Build());
}
