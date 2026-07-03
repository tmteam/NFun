using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFun.Tic.Stages;

/// <summary>
/// Worklist-based Pull driver — closes debt #10 by re-firing Pull on nodes whose
/// outgoing edge-set changed during solving (P3b violation in streaming Pull).
/// Spec: <c>specs_tic/Advanced/WorklistPull.md</c> + <c>WorklistPull_ExecPlan.md</c>.
///
/// Phase 1: scaffold only. <see cref="IsActive"/> is false unless an active call
/// stack established it; enqueue hooks are no-ops in that case. Phase 3 will flip
/// the default via <c>GraphBuilder.UseWorklistPull</c>.
/// </summary>
internal static class WorklistPullDriver {
    [ThreadStatic] private static Queue<TicNode> _worklist;
    [ThreadStatic] private static bool _isActive;
    [ThreadStatic] private static int _enqueueMark;
    [ThreadStatic] private static HashSet<(TicNode, TicNode)> _discharged;

    /// <summary>True only inside <see cref="RunPull"/>. Hooks short-circuit when false.</summary>
    public static bool IsActive {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isActive;
    }

    /// <summary>
    /// Enqueue <paramref name="node"/> for re-Pull if the driver is active.
    /// Dedup via <see cref="TicNode.EnqueueMark"/> — no HashSet allocation in the hot
    /// path, and NOT <see cref="TicNode.VisitMark"/>: that field is owned by in-flight
    /// graph traversals (PullRec visited-set); clobbering it from here breaks traversal
    /// termination on μ-cycles (see EnqueueMark doc).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Enqueue(TicNode node) {
        if (!_isActive || node == null) return;
        if (node.EnqueueMark == _enqueueMark) return;
        node.EnqueueMark = _enqueueMark;
        _worklist.Enqueue(node);
    }

    /// <summary>
    /// Coinductive assumption set for μ-recursive subtyping (Amadio-Cardelli '93),
    /// lifted to run scope. A composite constraint <c>desc ≤ anc</c> whose edge was
    /// CONSUMED by its Pull decomposition (RemoveAncestor in the composite×composite
    /// Apply cells) is recorded here; re-emitting the same pair via AddAncestor is
    /// coinductively redundant — on μ-knots (<c>type t = {kids: t[]}</c>) each knot
    /// member's decomposition re-emits the other member's already-discharged edge,
    /// oscillating forever. The in-Invoke visited-pair guard in StagesExtension cannot
    /// see this cycle: the decomposition is spread across separate drains, not nested
    /// in one Invoke. Skipping re-emission restores edge-set monotonicity — the
    /// termination invariant of any worklist algorithm. Streaming Pull is unaffected
    /// (single pass never re-emits a consumed pair).
    /// </summary>
    public static void MarkDischarged(TicNode descendant, TicNode ancestor) {
        if (!_isActive) return;
        _discharged.Add((descendant, ancestor));
    }

    /// <summary>True iff this exact pair was already discharged during this run.</summary>
    public static bool IsDischarged(TicNode descendant, TicNode ancestor) =>
        _isActive && _discharged.Contains((descendant, ancestor));

    /// <summary>
    /// Drain the worklist until empty. Initial seed in toposort order matches
    /// streaming Pull's first pass; re-enqueues from <see cref="Enqueue"/> close P3b.
    /// </summary>
    /// <param name="toposortedNodes">Nodes in topological order.</param>
    /// <param name="twoPhase">When true (graph contains None), Phase 1 seeds None-state nodes
    /// only (to establish IsOptional flags), then Phase 2 seeds non-None. Same temporal
    /// ordering as streaming <see cref="SolvingFunctions.PullConstraintsTwoPhase"/>.</param>
    public static void RunPull(TicNode[] toposortedNodes, bool twoPhase) {
        if (_isActive)
            throw new InvalidOperationException("WorklistPullDriver re-entry");

        _worklist = new Queue<TicNode>(toposortedNodes.Length * 2);
        _discharged = new HashSet<(TicNode, TicNode)>();
        _isActive = true;

        int maxDrains = Math.Max(1024, toposortedNodes.Length * 16);
        int drained = 0;

        try {
            if (twoPhase) {
                _enqueueMark = SolvingFunctions.NextMark();
                foreach (var n in toposortedNodes)
                    if (!n.IsMemberOfAnything && n.State == SolvingStates.StatePrimitive.None)
                        Enqueue(n);
                Drain(ref drained, maxDrains);

                _enqueueMark = SolvingFunctions.NextMark();
                foreach (var n in toposortedNodes)
                    if (!n.IsMemberOfAnything && n.State != SolvingStates.StatePrimitive.None)
                        Enqueue(n);
                Drain(ref drained, maxDrains);
            } else {
                _enqueueMark = SolvingFunctions.NextMark();
                foreach (var n in toposortedNodes)
                    if (!n.IsMemberOfAnything)
                        Enqueue(n);
                Drain(ref drained, maxDrains);
            }
        }
        finally {
            _isActive = false;
            _worklist = null;
            _discharged = null;
        }
    }

    private static void Drain(ref int drained, int maxDrains) {
        int startCount = TicNode.DiagAllocCount;
        while (_worklist.Count > 0) {
            if (++drained > maxDrains) {
                int delta = TicNode.DiagAllocCount - startCount;
                throw new InvalidOperationException(
                    $"WorklistPullDriver failed to converge after {drained} drains. " +
                    $"Nodes allocated during drain: {delta}. Monotonicity violation suspected.");
            }
            var node = _worklist.Dequeue();
            node.EnqueueMark = -1; // allow re-enqueue if this node's edges change again
            SolvingFunctions.PullConstraintsForNode(node);
        }
    }
}
