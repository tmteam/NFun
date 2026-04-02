namespace Nfun.Benchmarks;

using static BenchSets;

/// <summary>
/// LCA type coercion + optional types (none, ??, ?., !).
/// Measures performance of optional type inference features.
/// </summary>
static class BenchSetV2
{
    // =========================================================================
    // V2: "Full language" benchmark = V1 standard + optional/LCA additions.
    // Measures realistic workload where most scripts are plain, with some
    // optional/struct-LCA sprinkled in.
    // =========================================================================

    public static NfunBenchSet V2() => new("v2", new[]
        { SimpleV2(), MediumV2(), ComplexV2() },
        OptionalTypes: NFun.OptionalTypesSupport.ExperimentalEnabled);

    static BenchSubSet SimpleV2() => new("Simple", 64, new BenchScript[]
    {
        // ---- V1 standard scripts (plain language) ----
        Pure("y = 2 * x + 1"),
        Pure("y = (a - b) / 2.0"),
        Pure("y = a // 3 + a % 3"),
        Pure("y = a ** 2.0"),
        Pure("y = x > 0 and x < 100"),
        Pure("y = a == b or c != d"),
        Pure("y = not flag"),
        Pure("y = a xor b"),
        Pure("y = if(x > 0) x else -x"),
        Pure("y = max(a, b)"),
        Pure("y = 42.toText()"),
        Pure("y = 'hello world'"),
        Pure("x1:int\r x2:int\r o1 = x1 * x1 + x1 * 2\r o2 = if(x2 > 0 and o1 != 0) x1 else o1"),
        Pure("a:int\r b:int\r c:real\r o1 = a + b\r o2:real = a * c\r o3 = if(a > b) c else -c"),
        IntX("x:int\r y = x + 1"),
        IntX("x:int\r y = x * 2 - 3"),
        IntX("x:int\r y = x > 0 and x < 100"),
        IntX("x:int\r y = if(x > 0) x else -x"),
        IntX("x:int\r y = max(x, 0)"),
        IntX("x:int\r y = x.toText()"),
        IntX("x:int\r y = x % 7 + x / 3"),
        IntX("x:int\r y:real = x * 3.14 + 1.0"),
        // ---- Optional / LCA additions (comparable to V1 simple) ----
        Pure("a:int? = 42\r b:int? = none\r y = (a ?? 0) + (b ?? 10)"),
        Pure("x:int? = 42\r y = x ?? 0\r z = y * 2 + 1"),
        Pure("a:int? = none\r b:int? = 5\r c = a ?? b ?? 0\r y = c * 3 + 1"),
        Pure("x:real? = 1.5\r y:real = x ?? 0.0\r z = y ** 2.0"),
        Pure("a:int? = 42\r b = a!\r y = if(b > 0) b * 2 else -b"),
        IntX("x:int\r o:int? = if(x > 0) x else none\r y = (o ?? 0) * 2 + 1"),
    });

    static BenchSubSet MediumV2() => new("Medium", 8, new BenchScript[]
    {
        // ---- V1 standard scripts ----
        Pure("y = [1,2,3,4,5].map(rule it * 2)"),
        Pure("y = [1,2,3,4,5,6,7,8,9,10].filter(rule it > 5).count()"),
        Pure("y = [1,2,3,4,5].fold(rule it1 + it2)"),
        Pure("abs(x) = if(x >= 0) x else -x\r y = abs(-42) + abs(7)"),
        Pure("multi(a,b) =\r   if(a.count()!=b.count()) []\r   else [0..a.count()-1].map(rule a[it]*b[it])\r a = [1,2,3]\r b = [4,5,6]\r expected = [4,10,18]\r passed = a.multi(b)==expected"),
        Pure("a = 'hello'\r b = a.reverse()\r y = a.concat(' ').concat(b)"),
        Pure("person = {name = 'Alice'; age = 30; active = true}\r y = {greeting = person.name; doubleAge = person.age * 2; flag = person.active}"),
        Pure("getInfo(p) = {summary = p.name; score = p.age * 2}\r input = {name = 'Bob'; age = 25; active = true}\r y = getInfo(input).score"),
        Pure("a:int\r b:int\r c:int\r d:real\r e:bool\r o1 = a + b + c\r o2:real = (a + b) * d\r o3 = if(e) a else b\r o4 = max(a, max(b, c))\r o5 = [a, b, c].map(rule it * 2)"),
        IntX("x:int\r abs(v) = if(v >= 0) v else -v\r y = abs(x) + abs(x - 10)"),
        IntX("x:int\r y = [1,2,3,4,5].map(rule it * x)"),
        IntX("x:int\r y = [1,2,3,4,5].filter(rule it > x).count()"),
        IntX("x:int\r y = [1,2,3,4,5].fold(rule it1 + it2) + x"),
        // ---- Optional / struct-LCA additions (comparable to V1 medium) ----
        Pure("p = if(true) {name='Alice', age=30} else none\r name = p?.name ?? 'unknown'\r age:int = p?.age ?? 0\r y = name.concat(' ').concat(age.toText())"),
        Pure("arr:int?[] = [1, none, 3, none, 5]\r cleaned = arr.map(rule it ?? 0)\r total = cleaned.fold(rule it1 + it2)\r y = total"),
        Pure("x = if(true) {age=42, name='a', size=100} else {age=42}\r doubled = x.age * 2\r label = x.age.toText()\r y = doubled"),
        IntX("x:int\r s = if(x > 0) {v = x, label = 'pos'} else none\r sv:int = s?.v ?? -1\r y = sv * 2 + 1"),
    });

    static BenchSubSet ComplexV2() => new("Complex", 1, new BenchScript[]
    {
        // ---- V1 standard scripts ----
        Pure("twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)\r swap(arr, i, j) = arr.twiceSet(i,j,arr[j], arr[i])\r swapIfNotSorted(c, i) = if(c[i]<c[i+1]) c else c.swap(i, i+1)\r onelineSort(input) = [0..input.count()-2].fold(input, swapIfNotSorted)\r bubbleSort(input) = [0..input.count()-1].fold(input, rule onelineSort(it1))\r i:int[]  = [1,4,3,2,5].bubbleSort()\r r:real[] = [1,4,3,2,5].bubbleSort()"),
        Pure("fact(n):int = if(n<=1) 1 else fact(n-1)*n\r fibrec(n, iter, p1, p2) = if(n > iter) fibrec(n, iter+1, p1+p2, p1) else p1+p2\r fib(n) = if(n<3) 1 else fibrec(n-1, 2, 1, 1)\r y1 = fact(7)\r y2 = fib(10)"),
        Pure("fact(n:int) = if(n<=1) {res = 1} else {res = fact(n-1).res * n}\r transform(p) = {name = p.name; score = p.age * 2; eligible = p.age >= 18 and p.active}\r people = [{name='Alice'; age=5; active=true}, {name='Bob'; age=3; active=true}]\r results = people.map(rule transform(it))\r totalScore = results.map(rule it.score).fold(rule it1 + it2)\r factResult = fact(7).res"),
        Pure("ins:int[] = [1,5,3,5,6,1,2,100,0,3,2,10]\r sorted = ins.sort()\r evens = ins.filter(rule it % 2 == 0)\r total = ins.fold(rule it1 + it2)\r texts = ins.map(rule it.toText())\r hasZero = 0 in ins"),
        IntX("x:int\r fact(n):int = if(n<=1) 1 else fact(n-1)*n\r y = fact(max(1, x % 10))"),
        IntX("x:int\r myMax(a,b) = if(a > b) a else b\r myMin(a,b) = if(a < b) a else b\r clamp(v, lo, hi) = myMax(lo, myMin(v, hi))\r y:int[] = [1,2,3,4,5,6,7,8,9,10].map(rule clamp(it + x, 3, 7))"),
        // ---- Optional / struct-LCA additions (comparable to V1 complex) ----
        Pure("getScore(s) = s?.score ?? 0\r items = [if(true) {score=10, name='a'} else none, if(false) {score=20, name='b'} else none, if(true) {score=30, name='c'} else none]\r scores = items.map(getScore)\r total = scores.fold(rule it1 + it2)\r y = total"),
        Pure("p = if(true) {a=1, b=2, c=3} else none\r q = if(true) {a=10, b=20, c=30} else none\r pa = p?.a ?? 0\r pb = p?.b ?? 0\r pc = p?.c ?? 0\r qa = q?.a ?? 0\r qb = q?.b ?? 0\r qc = q?.c ?? 0\r sum = pa + pb + pc + qa + qb + qc\r y = if(sum > 0) (pa + qa) * (pb + qb) else -1"),
    });
}
