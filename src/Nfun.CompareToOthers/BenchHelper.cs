using System;
using System.Diagnostics;

namespace Nfun.CompareToOthers
{
    public static class BenchHelper
    {
        public static TimeSpan Measure(Action action, int iterations, out long totalAlloc)
        {
            GC.Collect(1);
            long allocated = GC.GetTotalAllocatedBytes();
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
                action();
            sw.Stop();
            GC.Collect(1);
            totalAlloc = GC.GetTotalAllocatedBytes()-allocated;
            return sw.Elapsed;
        }
    }
}