using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

/// <summary>
/// Smoke tests for <see cref="NFun.Tic.Stages.WorklistPullDriver"/>.
/// Confirms the driver drives Pull to the same fixed point as streaming
/// Pull on representative graph shapes (DAG, arithmetic generic, recursive).
/// Pinned at TIC level — failures here mean the worklist scheduler diverged
/// from streaming on a case that streaming handles correctly.
/// </summary>
public class WorklistPullSmokeTests {

    private static GraphBuilder NewBuilder(bool worklist) =>
        new() { UseWorklistPull = worklist };

    [Test(Description = "x = 3 / 2 — DAG, single drain expected")]
    public void TrivialDag_WorklistMatchesStreaming() {
        var g = NewBuilder(worklist: true);
        g.SetIntConst(0, U8);
        g.SetIntConst(1, U8);
        g.SetCall(Real, 0, 1, 2);
        g.SetDef("x", 2);
        var result = g.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "x");
    }

    [Test(Description = "y = 1 + 2 * x — open generic over arith")]
    public void OpenGeneric_WorklistMatchesStreaming() {
        var g = NewBuilder(worklist: true);
        g.SetIntConst(0, U8);
        g.SetIntConst(1, U8);
        g.SetVar("x", 2);
        g.SetArith(1, 2, 3);
        g.SetArith(0, 3, 4);
        g.SetDef("y", 4);
        var result = g.Solve();
        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test(Description = "x:int; y = 1 + x — concrete input, fixed Pull result")]
    public void ConcreteInput_WorklistMatchesStreaming() {
        var g = NewBuilder(worklist: true);
        g.SetIntConst(0, U8);
        g.SetVarType("x", I32);
        g.SetVar("x", 1);
        g.SetArith(0, 1, 2);
        g.SetDef("y", 2);
        var result = g.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }
}
