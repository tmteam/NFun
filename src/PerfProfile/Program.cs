using System;
using System.Diagnostics;
using NFun;

const int iterations = 300_000;

void Bench(string name, string expr) {
    for (int i = 0; i < 2000; i++)
        Funny.Hardcore.Build(expr);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
        Funny.Hardcore.Build(expr);
    sw.Stop();
    Console.WriteLine($"{name,-30} {sw.Elapsed.TotalMilliseconds * 1000 / iterations:F2} μs");
}

Console.WriteLine($"{"Expression",-30} {"Build μs"}");
Console.WriteLine(new string('-', 42));
Bench("x:int; y = x + 1",          "x:int\r y = x + 1");
Bench("y = 1 + 2",                 "y = 1 + 2");
Bench("y = if(true) 1 else 2",     "y = if(true) 1 else 2");
Bench("y = [1,2,3].count()",       "y = [1,2,3].count()");
Bench("y = {a=1, b=2}.a",          "y = {a=1, b=2}.a");
Bench("f(x) = x+1; y = f(5)",     "f(x) = x+1\r y = f(5)");
