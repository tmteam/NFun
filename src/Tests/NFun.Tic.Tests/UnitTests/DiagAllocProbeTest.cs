using System;
using System.Collections.Generic;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

/// <summary>
/// Oracle for worklist Pull convergence work (debt #10). Measures TicNode
/// allocations during Solve on the getLast graph that fails under worklist Pull.
///
/// Baseline (2026-06-26): Streaming Solve allocates 10 nodes, Worklist Solve
/// allocates 1016 before timing out at 1025 drains — ~100x ratio. Apply cells
/// emit fresh innerNodes per drain when state propagates through the recursive
/// μX. struct{v, next:Opt(X)} cycle.
///
/// See specs_tic/Advanced/WorklistPull_ConvergenceAnalysis.md for the diagnosis.
/// Refactor steps land here as the alloc count drops toward streaming's baseline.
/// </summary>
public class DiagAllocProbeTest {

    [Test(Description = "Streaming Pull baseline — convergent at 10 alloc")]
    public void GetLast_Streaming_NodeAllocCount() {
        var before = TicNode.DiagAllocCount;
        var graph = new GraphBuilder { UseWorklistPull = false };
        BuildGetLastGraph(graph);
        var afterBuild = TicNode.DiagAllocCount;
        graph.Solve();
        var afterSolve = TicNode.DiagAllocCount;
        TestContext.WriteLine($"Streaming: Build={afterBuild-before}, Solve={afterSolve-afterBuild}");
        Assert.Less(afterSolve - afterBuild, 50,
            "Streaming Pull alloc count regressed past 50 (was 10). Apply cell churn?");
    }

    [Test(Description = "Worklist Pull oracle — must reach streaming's baseline before Phase 2")]
    public void GetLast_Worklist_NodeAllocCount() {
        var before = TicNode.DiagAllocCount;
        var graph = new GraphBuilder { UseWorklistPull = true };
        BuildGetLastGraph(graph);
        var afterBuild = TicNode.DiagAllocCount;
        try {
            graph.Solve();
            var afterSolve = TicNode.DiagAllocCount;
            TestContext.WriteLine($"Worklist OK: Build={afterBuild-before}, Solve={afterSolve-afterBuild}");
        } catch (Exception ex) {
            var afterFail = TicNode.DiagAllocCount;
            TestContext.WriteLine($"Worklist FAIL ({ex.Message.Split('\n')[0]}): " +
                                  $"Build={afterBuild-before}, Solve={afterFail-afterBuild}");
            // Don't assert green — Worklist is expected to fail until Phase 2 lands.
            // Numeric trend is the signal: each refactor step should drop the alloc count.
        }
    }

    private static void BuildGetLastGraph(GraphBuilder graph) {
        var fun = graph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        graph.SetVar("n", 12);
        graph.SetSafeFieldAccess(12, 11, "next");
        graph.SetCall(fun, 11, 10);
        graph.SetVar("n", 13);
        graph.SetCoalesce(10, 13, 9);

        var fields = new Dictionary<string, TicNode>();
        var vField = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        var nextField = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        fields["v"] = vField;
        fields["next"] = nextField;
        var concreteStruct = new StateStruct(fields, isFrozen: true);
        graph.GetOrCreateStructNode(100, concreteStruct);
        graph.SetCall(fun, 100, 101);
    }
}
