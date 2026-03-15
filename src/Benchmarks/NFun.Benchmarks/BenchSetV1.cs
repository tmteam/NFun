namespace Nfun.Benchmarks;
using static BenchSets;

static class BenchSetV1 {
    public static BenchSet V1() => new("v1", new[] { SimpleV1(), MediumV1(), ComplexV1() });

    static BenchSubSet SimpleV1() => new("Simple", 64, new BenchScript[]
    {
        // --- Primitive ops, no arrays/structs. Some with typed & multi I/O ---
        Pure("y = 2 * x + 1"),                       // arithmetic: mul, add, 1 input
        Pure("y = (a - b) / 2.0"),                   // 2 inputs, real division
        Pure("y = a // 3 + a % 3"),                  // int div, remainder
        Pure("y = a ** 2.0"),                         // power
        Pure("y = x > 0 and x < 100"),               // comparison + logical AND
        Pure("y = a == b or c != d"),                 // 4 inputs, equality + OR
        Pure("y = not flag"),                         // logical NOT
        Pure("y = a xor b"),                          // XOR
        Pure("y = if(x > 0) x else -x"),             // if-else, unary minus
        Pure("y = max(a, b)"),                        // built-in function, 2 inputs
        Pure("y = 42.toText()"),                      // type conversion
        Pure("y = 'hello world'"),                    // text literal
        // typed multi-I/O: 2 inputs, 2 outputs, inter-output dependency
        Pure("x1:int\r x2:int\r o1 = x1 * x1 + x1 * 2\r o2 = if(x2 > 0 and o1 != 0) x1 else o1"),
        // typed multi-I/O: 3 inputs (mixed types), 3 typed outputs
        Pure("a:int\r b:int\r c:real\r o1 = a + b\r o2:real = a * c\r o3 = if(a > b) c else -c"),
        // --- Updatable: x:int input → y output (tests converter overhead) ---
        IntX("x:int\r y = x + 1"),
        IntX("x:int\r y = x * 2 - 3"),
        IntX("x:int\r y = x > 0 and x < 100"),
        IntX("x:int\r y = if(x > 0) x else -x"),
        IntX("x:int\r y = max(x, 0)"),
        IntX("x:int\r y = x.toText()"),
        IntX("x:int\r y = x % 7 + x / 3"),
        IntX("x:int\r y:real = x * 3.14 + 1.0"),
    });

    static BenchSubSet MediumV1() => new("Medium", 8, new BenchScript[]
    {
        // --- Arrays, user functions, lambdas, structs (3 fields), multi I/O ---
        Pure("y = [1,2,3,4,5].map(rule it * 2)"),
        Pure("y = [1,2,3,4,5,6,7,8,9,10].filter(rule it > 5).count()"),
        Pure("y = [1,2,3,4,5].fold(rule it1 + it2)"),
        Pure("abs(x) = if(x >= 0) x else -x\r y = abs(-42) + abs(7)"),
        Pure("multi(a,b) =\r   if(a.count()!=b.count()) []\r   else [0..a.count()-1].map(rule a[it]*b[it])\r a = [1,2,3]\r b = [4,5,6]\r expected = [4,10,18]\r passed = a.multi(b)==expected"),
        Pure("a = 'hello'\r b = a.reverse()\r y = a.concat(' ').concat(b)"),
        // struct output (3 fields), field access, transform
        Pure("person = {name = 'Alice'; age = 30; active = true}\r y = {greeting = person.name; doubleAge = person.age * 2; flag = person.active}"),
        // struct as function argument
        Pure("getInfo(p) = {summary = p.name; score = p.age * 2}\r input = {name = 'Bob'; age = 25; active = true}\r y = getInfo(input).score"),
        // 5 inputs, 5 outputs
        Pure("a:int\r b:int\r c:int\r d:real\r e:bool\r o1 = a + b + c\r o2:real = (a + b) * d\r o3 = if(e) a else b\r o4 = max(a, max(b, c))\r o5 = [a, b, c].map(rule it * 2)"),
        // --- Updatable ---
        IntX("x:int\r abs(v) = if(v >= 0) v else -v\r y = abs(x) + abs(x - 10)"),
        IntX("x:int\r y = [1,2,3,4,5].map(rule it * x)"),
        IntX("x:int\r y = [1,2,3,4,5].filter(rule it > x).count()"),
        IntX("x:int\r y = [1,2,3,4,5].fold(rule it1 + it2) + x"),
    });

    static BenchSubSet ComplexV1() => new("Complex", 1, new BenchScript[]
    {
        // --- Recursion, complex structs, bubble sort, heavy computation ---
        // bubble sort: recursive user functions, generics, fold, set, higher-order
        Pure("twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)\r swap(arr, i, j) = arr.twiceSet(i,j,arr[j], arr[i])\r swapIfNotSorted(c, i) = if(c[i]<c[i+1]) c else c.swap(i, i+1)\r onelineSort(input) = [0..input.count()-2].fold(input, swapIfNotSorted)\r bubbleSort(input) = [0..input.count()-1].fold(input, rule onelineSort(it1))\r i:int[]  = [1,4,3,2,5].bubbleSort()\r r:real[] = [1,4,3,2,5].bubbleSort()"),
        // recursive factorial + tail-recursive fibonacci
        Pure("fact(n):int = if(n<=1) 1 else fact(n-1)*n\r fibrec(n, iter, p1, p2) = if(n > iter) fibrec(n, iter+1, p1+p2, p1) else p1+p2\r fib(n) = if(n<3) 1 else fibrec(n-1, 2, 1, 1)\r y1 = fact(7)\r y2 = fib(10)"),
        // recursive struct return + struct pipeline with array of structs
        Pure("fact(n:int) = if(n<=1) {res = 1} else {res = fact(n-1).res * n}\r transform(p) = {name = p.name; score = p.age * 2; eligible = p.age >= 18 and p.active}\r people = [{name='Alice'; age=5; active=true}, {name='Bob'; age=3; active=true}]\r results = people.map(rule transform(it))\r totalScore = results.map(rule it.score).fold(rule it1 + it2)\r factResult = fact(7).res"),
        // heavy mixed: sort, filter, map, fold, membership, text, multiple outputs
        Pure("ins:int[] = [1,5,3,5,6,1,2,100,0,3,2,10]\r sorted = ins.sort()\r evens = ins.filter(rule it % 2 == 0)\r total = ins.fold(rule it1 + it2)\r texts = ins.map(rule it.toText())\r hasZero = 0 in ins"),
        // --- Updatable ---
        IntX("x:int\r fact(n):int = if(n<=1) 1 else fact(n-1)*n\r y = fact(max(1, x % 10))"),
        IntX("x:int\r myMax(a,b) = if(a > b) a else b\r myMin(a,b) = if(a < b) a else b\r clamp(v, lo, hi) = myMax(lo, myMin(v, hi))\r y:int[] = [1,2,3,4,5,6,7,8,9,10].map(rule clamp(it + x, 3, 7))"),
    });
}
