namespace NFun.Tic;

/// <summary>
/// Centralized registry of <see cref="TicNode.VisitMark"/> constants used as
/// cycle guards across the TIC solver. Pottier-Rémy '05 §10.6 mark-based
/// cycle detection: on entry to an operation, save the node's previous mark
/// and set the operation's mark; on re-entry detection (mark already set),
/// return the coinductive answer; on exit, restore the previous mark.
///
/// <para><b>Why a single file</b> — every TIC operation that recurses on the
/// type graph needs a unique mark int. Scattering them across state files
/// makes collision detection impossible at a glance and risks silent
/// re-entry into the wrong guard. This file is the single source of truth;
/// the <c>MarkConstants_*</c> tests assert distinctness and reserved ranges.</para>
///
/// <para><b>Reserved ranges</b> (negative for sub-zero detection vs. positive
/// for legacy <see cref="RefCycleSearchAlgorithm"/>):
/// <list type="bullet">
///   <item><c>-77, -123, -1569</c> — SolvingFunctions intra-pass marks</item>
///   <item><c>-33753</c> — NodeToposort</item>
///   <item><c>-55001</c> — StateOptional cycle guard</item>
///   <item><c>-55000..-57000</c> — StateStruct</item>
///   <item><c>-56000</c> — shared "leaf" mark across StateArray/StateOptional/StateStruct
///       (disjoint contexts — only one of these state types owns a node at any moment)</item>
///   <item><c>-58500..-58700</c> — StateComposite</item>
///   <item><c>-59000..-59700</c> — StateCompositeConstraints algebra</item>
///   <item><c>6782341, 672901236</c> — RefCycleSearchAlgorithm (positive, legacy)</item>
/// </list></para>
/// </summary>
public static class TicVisitMarks {

    // ── Algorithm-level (SolvingFunctions, NodeToposort, RefCycleSearch) ──

    /// <summary>Marks output-type leaves during SolveUselessGenerics signature processing.</summary>
    public const int OutputType = -77;

    /// <summary>Tracks visited TypeVariable nodes during Finalize.</summary>
    public const int TypeVariableVisited = -123;

    /// <summary>CollectLeafConstraints recursion guard.</summary>
    public const int LeafCollect = -1569;

    /// <summary>NodeToposort: node is already queued in the topological list.</summary>
    public const int NodeInList = -33753;

    /// <summary>StateOptional IsSolved cycle guard
    /// (generic functions with if..else none create cyclic Optional).</summary>
    public const int StateOptionalIsSolvedCycle = -55001;

    /// <summary>RefCycleSearch enter mark (positive, legacy).</summary>
    public const int RefVisiting = 6782341;

    /// <summary>RefCycleSearch exit mark (positive, legacy).</summary>
    public const int RefVisited = 672901236;

    // ── StateStruct ────────────────────────────────────────────────────────

    /// <summary>StateStruct.IsSolved cycle guard (recursive named struct).</summary>
    public const int StructIsSolved = -55000;

    /// <summary>Shared "in-leaf-traversal" mark across StateArray/StateOptional/StateStruct.
    /// Disjoint contexts: only one of these owns a TicNode at any moment, so the same
    /// numeric mark is reused safely. Documented here to make the reuse explicit.</summary>
    public const int StateLeaf = -56000;

    /// <summary>StateStruct.PrintState cycle guard (recursive named struct printing).</summary>
    public const int StructPrint = -57000;

    // ── StateComposite ─────────────────────────────────────────────────────

    /// <summary>StateComposite.AllLeafTypes recursion guard.</summary>
    public const int CompositeLeaf = -58500;

    /// <summary>StateComposite.IsMutable cycle guard.</summary>
    public const int CompositeIsMutableCycle = -58600;

    /// <summary>StateComposite.IsSolved cycle guard.</summary>
    public const int CompositeIsSolvedCycle = -58700;

    // ── StateCompositeConstraints (CompCs) algebra ─────────────────────────
    // Specs/Tic/Algebra_CompositeConstraints.md §3.8.1. Range -59000..-59700.
    // Each operator marks its operand ElementNode(s) on entry to detect
    // recursive re-entry through the type graph.

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
