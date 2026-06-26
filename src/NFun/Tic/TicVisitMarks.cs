namespace NFun.Tic;

/// <summary>
/// Central registry of <see cref="TicNode.VisitMark"/> constants. Mark-based cycle detection
/// (Pottier-Rémy '05 §10.6): set on entry, return coinductive answer on re-entry, restore on
/// exit. Single source of truth — <c>MarkConstants_*</c> tests assert distinctness and ranges.
/// Negative ranges are TIC operations; positive ones are legacy <see cref="RefCycleSearchAlgorithm"/>.
/// </summary>
public static class TicVisitMarks {

    /// <summary>Output-type leaves in SolveUselessGenerics.</summary>
    public const int OutputType = -77;

    /// <summary>Visited TypeVariable nodes in Finalize.</summary>
    public const int TypeVariableVisited = -123;

    /// <summary>CollectLeafConstraints recursion guard.</summary>
    public const int LeafCollect = -1569;

    /// <summary>NodeToposort: node is already queued in the topological list.</summary>
    public const int NodeInList = -33753;

    /// <summary>StateOptional.IsSolved cycle guard (e.g. generic fn with <c>if..else none</c>).</summary>
    public const int StateOptionalIsSolvedCycle = -55001;

    /// <summary>RefCycleSearch enter mark (positive, legacy).</summary>
    public const int RefVisiting = 6782341;

    /// <summary>RefCycleSearch exit mark (positive, legacy).</summary>
    public const int RefVisited = 672901236;

    /// <summary>StateStruct.IsSolved cycle guard (recursive named struct).</summary>
    public const int StructIsSolved = -55000;

    /// <summary>Shared leaf-traversal mark across StateArray/Optional/Struct (mutually disjoint contexts).</summary>
    public const int StateLeaf = -56000;

    /// <summary>StateStruct.PrintState cycle guard (recursive named struct printing).</summary>
    public const int StructPrint = -57000;

    /// <summary>StateComposite.AllLeafTypes recursion guard.</summary>
    public const int CompositeLeaf = -58500;

    /// <summary>StateComposite.IsMutable cycle guard.</summary>
    public const int CompositeIsMutableCycle = -58600;

    /// <summary>StateComposite.IsSolved cycle guard.</summary>
    public const int CompositeIsSolvedCycle = -58700;

    // ── StateCompositeConstraints algebra (Specs/Tic/Algebra_CompositeConstraints.md §3.8.1) ──

    /// <summary>LCA(CompCs, CompCs) — same-class join.</summary>
    public const int CompCsLcaSame = -59000;

    /// <summary>Unify(CompCs, CompCs) — same-class meet (shared with same-class GCD per §3.2).</summary>
    public const int CompCsUnify = -59100;

    /// <summary>Concretest(CompCs) — resolve to concrete StateCollection.</summary>
    public const int CompCsConcretest = -59200;

    /// <summary>Abstractest(CompCs) — drop floor, keep cap.</summary>
    public const int CompCsAbstractest = -59300;

    /// <summary>LCA(CompCs, StateCollection) — cross-class join.</summary>
    public const int CompCsXCollLca = -59400;

    /// <summary>GCD(CompCs, StateCollection) — cross-class meet.</summary>
    public const int CompCsXCollGcd = -59500;

    /// <summary>Unify(CompCs, StateCollection) — cross-class.</summary>
    public const int CompCsXCollUnify = -59550;

    /// <summary>LCA(CompCs, StateArray) — cross-class join with ee-mode StateArray.</summary>
    public const int CompCsXArrayLca = -59600;

    /// <summary>GCD(CompCs, StateArray) — cross-class meet with ee-mode StateArray.</summary>
    public const int CompCsXArrayGcd = -59650;

    /// <summary>Unify(CompCs, StateArray) — cross-class with ee-mode StateArray.</summary>
    public const int CompCsXArrayUnify = -59700;
}
