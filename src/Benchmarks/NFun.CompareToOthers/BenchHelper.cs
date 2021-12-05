using System;
using System.Diagnostics;

namespace NFun.CompareToOthers {

public static class BenchHelper {
    public static TimeSpan Measure(Action action, int iterations, out long totalAlloc) {
        GC.WaitForPendingFinalizers();
        GC.Collect(1, GCCollectionMode.Forced);
        var allocated = GC.GetTotalAllocatedBytes();
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < iterations; i++)
            action();
        sw.Stop();
        GC.Collect(1, GCCollectionMode.Forced);
        totalAlloc = GC.GetTotalAllocatedBytes() - allocated;
        return sw.Elapsed;
    }
}

}