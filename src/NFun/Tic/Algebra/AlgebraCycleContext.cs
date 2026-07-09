namespace NFun.Tic.Algebra;

using System.Collections.Generic;
using SolvingStates;

/// <summary>
/// Explicit coinductive context of the binary algebra family (∨/∧/⊓ — Lca, Gcd, Unify/Merge
/// and the bridges between them: struct-field Unify → Merge → interval Lca, F-bound S-axis
/// joins, GcdBound field meets, Fit's invariant-field Unify cells).
///
/// This IS the Amadio–Cardelli assumption set of the coinductive arms: each cycle-capable
/// arm records the relation instance it is about to prove and, on re-entry of the same
/// instance below itself, discharges the subgoal coinductively (μ-rule — assume the goal).
/// Re-entry answers are per-operator: struct-∨ returns an operand (join of a μ-type with
/// itself), the S-axis join drops the bound (weakening is always sound for ∨), struct-∧
/// returns an operand.
///
/// The context is an explicit parameter — the algebra layer holds NO ambient state
/// (no ThreadStatic): it is allocated lazily by the first cycle-capable arm on the path and
/// threaded through the whole mutually-recursive family. Entries are stack-shaped: pushed
/// before the arm recurses, popped when it completes — the set always describes exactly the
/// relation instances open on the CURRENT recursion path (siblings never see each other's
/// hypotheses), which is the same discipline the removed ThreadStatic lists enforced.
///
/// Assumption sets are SEPARATE per operator: an ∨-hypothesis must not discharge an
/// ∧-subgoal (Lca(t,t) legitimately computes Gcd(t,t) on contravariant Fun args — a shared
/// set would short-circuit that meet to a wrong answer).
/// </summary>
internal sealed class AlgebraCycleContext {
    // Named-μ hypotheses of struct-∨: TypeName is the structural key because every Lca level
    // rebuilds struct snapshots — ref-equality cannot see the cycle.
    private List<string> _lcaStructNames;

    // Anonymous-pair hypotheses of struct-∨ (symmetric ref-identity).
    private List<(StateStruct, StateStruct)> _lcaStructPairs;

    // Named-μ hypotheses of struct-∧.
    private List<string> _gcdStructNames;

    // Hypotheses of the S-axis join (IntersectBoundsOrNull), symmetric ref-identity.
    private List<(StateStruct, StateStruct)> _boundJoinPairs;

    public bool LcaNameInProgress(string name) => NameInProgress(_lcaStructNames, name);
    public void EnterLcaName(string name) => (_lcaStructNames ??= new()).Add(name);
    public void ExitLcaName() => _lcaStructNames.RemoveAt(_lcaStructNames.Count - 1);

    public bool LcaPairInProgress(StateStruct a, StateStruct b) => PairInProgress(_lcaStructPairs, a, b);
    public void EnterLcaPair(StateStruct a, StateStruct b) => (_lcaStructPairs ??= new()).Add((a, b));
    public void ExitLcaPair() => _lcaStructPairs.RemoveAt(_lcaStructPairs.Count - 1);

    public bool GcdNameInProgress(string name) => NameInProgress(_gcdStructNames, name);
    public void EnterGcdName(string name) => (_gcdStructNames ??= new()).Add(name);
    public void ExitGcdName() => _gcdStructNames.RemoveAt(_gcdStructNames.Count - 1);

    public bool BoundJoinPairInProgress(StateStruct a, StateStruct b) => PairInProgress(_boundJoinPairs, a, b);
    public void EnterBoundJoinPair(StateStruct a, StateStruct b) => (_boundJoinPairs ??= new()).Add((a, b));
    public void ExitBoundJoinPair() => _boundJoinPairs.RemoveAt(_boundJoinPairs.Count - 1);

    private static bool NameInProgress(List<string> set, string name) {
        if (set == null) return false;
        for (int i = 0; i < set.Count; i++)
            if (set[i] == name)
                return true;
        return false;
    }

    private static bool PairInProgress(List<(StateStruct, StateStruct)> set, StateStruct a, StateStruct b) {
        if (set == null) return false;
        for (int i = 0; i < set.Count; i++)
            if (ReferenceEquals(set[i].Item1, a) && ReferenceEquals(set[i].Item2, b)
                || ReferenceEquals(set[i].Item1, b) && ReferenceEquals(set[i].Item2, a))
                return true;
        return false;
    }
}
