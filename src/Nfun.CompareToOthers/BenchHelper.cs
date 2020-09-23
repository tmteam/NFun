using System;
using System.Diagnostics;

namespace Nfun.CompareToOthers
{
    public static class BenchHelper
    {
        public static TimeSpan Measure(Action action, int iterations)
        {
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
                action();
            sw.Stop();
            return sw.Elapsed;
        }
    }
}