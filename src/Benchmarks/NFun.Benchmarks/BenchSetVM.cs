namespace Nfun.Benchmarks;
using static BenchSets;

/// <summary>
/// VM benchmark set — same expressions as V1 Simple, but only those
/// that the VM currently supports (no lambdas, no toText).
/// </summary>
static class BenchSetVM {
    public static NfunBenchSet VMSet() => new("vm", new[] { SimpleVM(), MediumVM() });

    static BenchSubSet SimpleVM() => new("SimpleVM", 64, new BenchScript[]
    {
        // All simple arithmetic expressions from V1
        Pure("y = 2 * x + 1"),
        Pure("y = (a - b) / 2.0"),
        Pure("y = a // 3 + a % 3"),
        Pure("y = a ** 2.0"),
        Pure("y = x > 0 and x < 100"),
        Pure("y = a == b or c != d"),
        Pure("y = not flag"),
        Pure("y = if(x > 0) x else -x"),
        Pure("y = max(a, b)"),
        // typed multi-I/O
        Pure("x1:int\r x2:int\r o1 = x1 * x1 + x1 * 2\r o2 = if(x2 > 0 and o1 != 0) x1 else o1"),
        Pure("a:int\r b:int\r c:real\r o1 = a + b\r o2:real = a * c\r o3 = if(a > b) c else -c"),
        // Updatable
        IntX("x:int\r y = x + 1"),
        IntX("x:int\r y = x * 2 - 3"),
        IntX("x:int\r y = x > 0 and x < 100"),
        IntX("x:int\r y = if(x > 0) x else -x"),
        IntX("x:int\r y = max(x, 0)"),
        IntX("x:int\r y = x % 7 + x / 3"),
        IntX("x:int\r y:real = x * 3.14 + 1.0"),
    });

    /// <summary>
    /// Medium VM: user functions, struct operations. No lambdas (rule it*2).
    /// </summary>
    static BenchSubSet MediumVM() => new("MediumVM", 8, new BenchScript[]
    {
        // User functions
        Pure("abs(x) = if(x >= 0) x else -x\r y = abs(-42) + abs(7)"),
        // Struct output
        Pure("person = {name = 'Alice'; age = 30; active = true}\r y = {greeting = person.name; doubleAge = person.age * 2; flag = person.active}"),
        // Struct as function argument
        Pure("getInfo(p) = {summary = p.name; score = p.age * 2}\r input = {name = 'Bob'; age = 25; active = true}\r y = getInfo(input).score"),
        // Multi I/O
        Pure("a:int\r b:int\r c:int\r d:real\r e:bool\r o1 = a + b + c\r o2:real = (a + b) * d\r o3 = if(e) a else b\r o4 = max(a, max(b, c))"),
        // Updatable with user function
        IntX("x:int\r abs(v) = if(v >= 0) v else -v\r y = abs(x) + abs(x - 10)"),
        // Recursive factorial
        Pure("fact(n):int = if(n<=1) 1 else fact(n-1)*n\r y = fact(7)"),
    });
}
