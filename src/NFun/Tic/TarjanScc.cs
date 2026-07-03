using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

/// <summary>
/// Tarjan '72 SCCs on the TIC constraint graph. Edges followed (matching
/// <see cref="NodeToposort.Visit"/>): <see cref="StateRefTo"/> target, <see cref="ICompositeState"/>
/// members, and <see cref="TicNode.Ancestors"/>. Cyclic SCCs are candidate μ-types
/// (Pottier–Rémy '92); contractivity is certified by <see cref="IsContractive"/>
/// (Cardelli–Mitchell '89 §3). O(V+E), invoked only when a cyclic SCC is suspected.
/// </summary>
public static class TarjanScc {

    /// <summary>
    /// SCCs of the graph reachable from <paramref name="roots"/>, in reverse-topological order
    /// of the condensation.
    /// </summary>
    public static List<List<TicNode>> ComputeSccs(IEnumerable<TicNode> roots) {
        var ctx = new Context();
        foreach (var root in roots)
            if (!ctx.indices.ContainsKey(root))
                StrongConnect(root, ctx);
        return ctx.result;
    }

    /// <summary>True iff the SCC is cyclic — size &gt; 1 OR a singleton with a self-loop.</summary>
    public static bool IsCyclicScc(IReadOnlyList<TicNode> scc) {
        if (scc.Count > 1) return true;
        // Singleton: only cyclic if self-loop.
        var n = scc[0];
        foreach (var s in Successors(n))
            if (s == n) return true;
        return false;
    }

    /// <summary>
    /// Cardelli–Mitchell '89 §3 contractivity: at least one intra-SCC edge crosses a type
    /// constructor (composite-member edge). Ref and Ancestor edges are not constructor-guarded.
    /// Non-contractive cyclic SCCs (e.g. μX. X) have no canonical fixed point and must be rejected.
    /// </summary>
    public static bool IsContractive(IReadOnlyList<TicNode> scc) {
        if (scc.Count == 1 && !IsCyclicScc(scc)) return true; // acyclic singleton
        var sccSet = new HashSet<TicNode>(scc);
        foreach (var n in scc) {
            if (n.State is ICompositeState composite) {
                for (int i = 0; i < composite.MemberCount; i++) {
                    var member = composite.GetMember(i);
                    if (sccSet.Contains(member))
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>Graph successors of <paramref name="n"/> in <see cref="NodeToposort.Visit"/> order.</summary>
    private static IEnumerable<TicNode> Successors(TicNode n) {
        if (n.State is StateRefTo refTo)
            yield return refTo.Node;
        else if (n.State is ICompositeState composite) {
            for (int i = 0; i < composite.MemberCount; i++)
                yield return composite.GetMember(i);
        }
        for (int i = 0; i < n.Ancestors.Count; i++)
            yield return n.Ancestors[i];
    }

    private sealed class Context {
        public int nextIndex = 0;
        public readonly Stack<TicNode> stack = new();
        public readonly HashSet<TicNode> onStack = new();
        public readonly Dictionary<TicNode, int> indices = new();
        public readonly Dictionary<TicNode, int> lowlinks = new();
        public readonly List<List<TicNode>> result = new();
    }

    private static void StrongConnect(TicNode v, Context ctx) {
        ctx.indices[v] = ctx.nextIndex;
        ctx.lowlinks[v] = ctx.nextIndex;
        ctx.nextIndex++;
        ctx.stack.Push(v);
        ctx.onStack.Add(v);

        foreach (var w in Successors(v)) {
            if (w == null) continue;
            if (!ctx.indices.ContainsKey(w)) {
                StrongConnect(w, ctx);
                if (ctx.lowlinks[w] < ctx.lowlinks[v])
                    ctx.lowlinks[v] = ctx.lowlinks[w];
            }
            else if (ctx.onStack.Contains(w)) {
                if (ctx.indices[w] < ctx.lowlinks[v])
                    ctx.lowlinks[v] = ctx.indices[w];
            }
        }

        if (ctx.lowlinks[v] == ctx.indices[v]) {
            var scc = new List<TicNode>();
            TicNode w;
            do {
                w = ctx.stack.Pop();
                ctx.onStack.Remove(w);
                scc.Add(w);
            } while (w != v);
            ctx.result.Add(scc);
        }
    }
}
