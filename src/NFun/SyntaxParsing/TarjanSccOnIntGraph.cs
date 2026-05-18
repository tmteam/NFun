using System;
using System.Collections.Generic;

namespace NFun.SyntaxParsing;

/// <summary>
/// Tarjan '72 strongly-connected components on an adjacency-list graph indexed
/// by int. Returns SCCs in BUILD ORDER for dependency graphs where edges go
/// from depender → dependency: sinks (leaf dependencies) appear first,
/// sources (top-level consumers) last. This matches the natural post-order
/// emission of Tarjan — no reversal needed. Singleton SCCs without a
/// self-loop are acyclic.
///
/// Used for function-dependency analysis: SCCs of size &gt; 1 (or size==1 with
/// a self-loop in the graph) represent mutual/self-recursion cycles.
/// </summary>
internal static class TarjanSccOnIntGraph {
    public static int[][] Compute(int[][] graph) {
        var n = graph.Length;
        if (n == 0) return Array.Empty<int[]>();

        var ctx = new Context(n);
        for (int v = 0; v < n; v++)
            if (ctx.Index[v] < 0)
                StrongConnect(v, graph, ctx);

        // Tarjan emits SCCs in post-order: dependencies (sinks) first.
        // For depender→dependency edges that's BUILD ORDER — keep as-is.
        return ctx.Result.ToArray();
    }

    private sealed class Context {
        public int Next;
        public readonly int[] Index;
        public readonly int[] Lowlink;
        public readonly bool[] OnStack;
        public readonly Stack<int> Stack;
        public readonly List<int[]> Result;

        public Context(int n) {
            Index = new int[n];
            Lowlink = new int[n];
            OnStack = new bool[n];
            Stack = new Stack<int>();
            Result = new List<int[]>();
            for (int i = 0; i < n; i++) Index[i] = -1;
        }
    }

    private static void StrongConnect(int v, int[][] graph, Context ctx) {
        ctx.Index[v] = ctx.Lowlink[v] = ctx.Next++;
        ctx.Stack.Push(v);
        ctx.OnStack[v] = true;

        foreach (var w in graph[v]) {
            if (ctx.Index[w] < 0) {
                StrongConnect(w, graph, ctx);
                if (ctx.Lowlink[w] < ctx.Lowlink[v])
                    ctx.Lowlink[v] = ctx.Lowlink[w];
            }
            else if (ctx.OnStack[w]) {
                if (ctx.Index[w] < ctx.Lowlink[v])
                    ctx.Lowlink[v] = ctx.Index[w];
            }
        }

        if (ctx.Lowlink[v] == ctx.Index[v]) {
            var component = new List<int>();
            int u;
            do {
                u = ctx.Stack.Pop();
                ctx.OnStack[u] = false;
                component.Add(u);
            } while (u != v);
            ctx.Result.Add(component.ToArray());
        }
    }
}
