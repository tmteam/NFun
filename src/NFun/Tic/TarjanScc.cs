using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

/// <summary>
/// Tarjan's strongly-connected components algorithm (Tarjan '72) on the TIC constraint graph.
///
/// Edges followed (matching <see cref="NodeToposort.Visit"/> traversal):
///   1. <see cref="StateRefTo"/> target — reference resolution edge.
///   2. <see cref="ICompositeState"/> members — constructor-guarded edge.
///   3. <see cref="TicNode.Ancestors"/> — subtype-bound edge.
///
/// Per Pottier–Rémy '92, μ-types are properties of the graph; SCCs of size &gt; 1 (or singleton
/// with self-loop) are candidate μ-types. SCCs of size 1 without self-loop are acyclic.
///
/// Per Cardelli–Mitchell '89 §3, an SCC is contractive iff at least one back-edge in any cycle
/// within it crosses a type constructor (i.e. is a composite-member edge). Use
/// <see cref="IsContractive"/> to certify.
///
/// O(V+E). Allocates per-node visit state — used only when a cyclic SCC is detected (via
/// <see cref="ContainsCyclicSCC"/> precheck) to keep the non-recursive hot path zero-overhead.
/// </summary>
public static class TarjanScc {

    /// <summary>
    /// Compute strongly-connected components of the graph induced by the
    /// given root nodes. Returns SCCs in REVERSE TOPOLOGICAL order of the
    /// condensation (i.e. each SCC appears before any SCC depending on it,
    /// suitable for post-order processing).
    /// </summary>
    public static List<List<TicNode>> ComputeSccs(IEnumerable<TicNode> roots) {
        var ctx = new Context();
        foreach (var root in roots)
            if (!ctx.indices.ContainsKey(root))
                StrongConnect(root, ctx);
        return ctx.result;
    }

    /// <summary>
    /// Returns true iff the SCC has size &gt; 1 OR is a singleton with a
    /// self-loop (i.e. the single node is reachable from itself by one or
    /// more edges). Acyclic singletons return false.
    /// </summary>
    public static bool IsCyclicScc(IReadOnlyList<TicNode> scc) {
        if (scc.Count > 1) return true;
        // Singleton self-loop check.
        var n = scc[0];
        foreach (var s in Successors(n))
            if (s == n) return true;
        return false;
    }

    /// <summary>
    /// Cardelli–Mitchell '89 §3 contractivity test: an SCC is contractive
    /// iff at least one edge between two SCC members traverses a type
    /// constructor (composite-member edge: <see cref="ICompositeState"/>
    /// member access OR <see cref="StateOptional"/>/<see cref="StateArray"/>
    /// element). Reference and Ancestor edges are NOT constructor-guarded.
    ///
    /// A cyclic SCC that is not contractive corresponds to a non-contractive
    /// μ-equation (e.g. <c>μX. X</c>) which has no canonical solution — it
    /// must be rejected per Banach fixed-point precondition.
    /// </summary>
    public static bool IsContractive(IReadOnlyList<TicNode> scc) {
        if (scc.Count == 1 && !IsCyclicScc(scc)) return true; // trivially
        var sccSet = new HashSet<TicNode>(scc);
        foreach (var n in scc) {
            if (n.State is ICompositeState composite) {
                for (int i = 0; i < composite.MemberCount; i++) {
                    var member = composite.GetMember(i);
                    if (sccSet.Contains(member))
                        return true; // composite-member edge into SCC
                }
            }
        }
        return false;
    }

    /// <summary>
    /// All graph successors of <paramref name="n"/> for SCC purposes.
    /// Order matches NodeToposort.Visit branches.
    /// </summary>
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
