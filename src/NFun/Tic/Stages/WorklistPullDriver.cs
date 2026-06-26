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

    /// <summary>True only inside <see cref="RunPull"/>. Hooks short-circuit when false.</summary>
    public static bool IsActive {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isActive;
    }

    /// <summary>
    /// Enqueue <paramref name="node"/> for re-Pull if the driver is active.
    /// Dedup via <see cref="TicNode.VisitMark"/> — no HashSet allocation in the hot path.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Enqueue(TicNode node) {
        if (!_isActive || node == null) return;
        if (node.VisitMark == _enqueueMark) return;
        node.VisitMark = _enqueueMark;
        _worklist.Enqueue(node);
    }

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
            node.VisitMark = -1;
            SolvingFunctions.PullConstraintsForNode(node);
        }
    }
}
