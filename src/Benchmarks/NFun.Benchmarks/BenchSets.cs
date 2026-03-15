using System;
using System.Collections.Generic;

namespace Nfun.Benchmarks;

record BenchScript(string Script, Dictionary<string, object> Inputs, string[] Outputs);
record BenchSubSet(string Name, int Importance, BenchScript[] Scripts);
record BenchSet(string Name, BenchSubSet[] Subsets);

/// <summary>
/// Benchmark script collections. Each version captures the NFun feature set at that point.
/// v1 = base language (no optional types, no char literals).
/// </summary>
static class BenchSets
{
    internal static BenchScript Pure(string script) =>
        new(script, new Dictionary<string, object>(), Array.Empty<string>());

    internal static BenchScript IntX(string script) =>
        new(script, new Dictionary<string, object> { ["x"] = 1 }, new[] { "y" });


}
