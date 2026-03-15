namespace Nfun.Benchmarks;

using static BenchSets;

/// <summary>
/// LCA type coercion + optional types (none, ??, ?., !).
/// Measures performance of optional type inference features.
/// </summary>
static class BenchSetsLcaOptional
{
    public static BenchSet LcaOptional() => new("lcaOptional", new[]
        { SimpleLcaOpt(), MediumLcaOpt(), ComplexLcaOpt() });

    static BenchSubSet SimpleLcaOpt() => new("Simple", 64, new BenchScript[]
    {
        // --- Optional annotations, none literal, implicit lift ---
        Pure("y:int? = 42"),
        Pure("y:int? = none"),
        Pure("y:real? = 1.5"),
        Pure("y:text? = 'hello'"),
        Pure("y:bool? = true"),
        // --- if-else LCA: T + none → T? ---
        Pure("y = if(true) 42 else none"),
        Pure("y = if(true) 1.5 else none"),
        Pure("y = if(true) 'hello' else none"),
        Pure("y = if(true) true else none"),
        // --- Coalesce ?? ---
        Pure("y = none ?? 42"),
        Pure("y = none ?? 'hello'"),
        Pure("x:int? = 42\r y = x ?? 0"),
        Pure("x:int? = none\r y = x ?? 0"),
        Pure("x:real? = 1.5\r y = x ?? 0.0"),
        // --- Force unwrap ---
        Pure("x:int? = 42\r y = x!"),
        // --- Chained coalesce ---
        Pure("y = none ?? none ?? 42"),
        Pure("a:int? = none\r b:int? = 5\r y = a ?? b ?? 0"),
        // --- Array of optionals / optional array ---
        Pure("y:int?[] = [1, none, 3]"),
        Pure("y:int[]? = [1, 2, 3]"),
        Pure("y:int[]? = none"),
        // --- Numeric LCA (no optionals, pure type widening) ---
        Pure("y = if(true) 1 else 1.5"),
        Pure("y = if(true) 0xFF else 42"),
        // --- Updatable ---
        IntX("x:int\r y:int? = x"),
        IntX("x:int\r y = if(x > 0) x else none"),
        IntX("x:int\r y:real = if(x > 0) x else 1.5"),
    });

    static BenchSubSet MediumLcaOpt() => new("Medium", 8, new BenchScript[]
    {
        // --- Struct LCA ---
        Pure("x = if(true) {age=42, name='a', size=100} else {age=42}\r y = x.age"),
        Pure("x = if(true) {age = 0x1} else {age = 42.0}\r y = x.age"),
        Pure("arr = [{age=42}, {age=42, size=15}, {age=1, size=2, name='vasa'}]\r y = arr[0].age"),
        // --- Safe field access ?. ---
        Pure("x = if(true) {name='Alice'} else none\r y = x?.name ?? 'default'"),
        Pure("x = if(true) {age=25} else none\r y:int = x?.age ?? 0"),
        // --- Chained ?. ---
        Pure("x = if(true) {a = {b = 42}} else none\r y = x?.a?.b"),
        Pure("x = if(true) {a=1, b=2} else none\r y:int = x?.a ?? x?.b ?? 0"),
        // --- Map + optional coalesce ---
        Pure("y = [1,none,3].map(rule it ?? 0)"),
        // --- Function LCA with struct arg inference ---
        Pure("f1 = rule it.age + it.size\r f2 = rule it.age\r f3 = if(true) f1 else f2\r y = f3({age=42, size=15})"),
        // --- Updatable ---
        IntX("x:int\r s = if(x > 0) {v = x} else none\r y:int = s?.v ?? -1"),
    });

    static BenchSubSet ComplexLcaOpt() => new("Complex", 1, new BenchScript[]
    {
        // --- Deep if-else chain with optionals ---
        Pure("y = if(true) 42 else if(false) none else if(true) 7 else none"),
        // --- Struct LCA 3-way: field types widen to Any ---
        Pure("x = if(true) {age=0x1} else if(false) {age='name'} else {age=42.0}\r y = x.age"),
        // --- Struct + optional pipeline: safe access, coalesce, arithmetic ---
        Pure("x = if(true) {a=1, b=2} else none\r a = x?.a ?? 0\r b = x?.b ?? 0\r y = a + b"),
        // --- Complex: array of optional structs + safe access + function ---
        Pure("getAge(p) = p?.age ?? 0\r people = [if(true) {age=30} else none, if(false) {age=20} else none]\r y = people.map(getAge)"),
    });
}
