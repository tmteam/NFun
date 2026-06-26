using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.Algebra;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.Tic.Stages;

namespace NFun.Tic;

public static class SolvingFunctions {
    #region Merges

    [ThreadStatic] private static HashSet<(ITicNodeState, ITicNodeState)> _mergeVisited;

    /// <summary>
    /// Compute the merge (greatest lower bound under the type lattice) of two node states,
    /// or return <c>null</c> when no consistent merge exists.
    ///
    /// <para><b>Contract for <see cref="StateRefTo"/>:</b> if <paramref name="stateA"/> is a
    /// <see cref="StateRefTo"/>, the function dereferences to <c>refA.Node.State</c>, recursively
    /// merges with <paramref name="stateB"/>, and on success <i>mutates</i> the target node's
    /// <c>State</c> to hold the merged result. The return value is the ORIGINAL
    /// <see cref="StateRefTo"/> pointer — NOT the merged type. Callers that need the actual
    /// type must call <c>GetNonReference()</c> on the returned reference. This preserves the
    /// pointer-identity discipline used by <see cref="MergeInplace"/>: nodes already aliased
    /// to a target keep their alias, while the underlying state updates in place.</para>
    ///
    /// <para>Cycle handling: <c>_mergeVisited</c> guards both <see cref="StateStruct"/> and
    /// <see cref="StateRefTo"/> recursion (Pottier-Rémy '05 graph cycles; Amadio-Cardelli '93
    /// coinductive bisimulation). A re-entered pair is treated as "already merged" — the merge
    /// is idempotent under cycle, returning the first state.</para>
    /// </summary>
    public static ITicNodeState GetMergedStateOrNull(ITicNodeState stateA, ITicNodeState stateB,
        TicNode resultOwnerNode = null) {
        // Cycle guard for true graph cycles (Pottier-Rémy '05): RefTo→root→RefTo… recursion would
        // overflow on cyclic types. Coinductive bisimulation: assume already-visited pair
        // "merges" to the first state (idempotent merge under cycle).
        if (ReferenceEquals(stateA, stateB)) return stateA;
        if (stateB is ConstraintsState c && c.NoConstrains)
            return stateA;

        // None fits into any Optional type (universal: regardless of opt's IsSolved state).
        // MUST be checked BEFORE the immutable-vs-immutable equality shortcut below —
        // both None and solved Optionals are immutable ITypeState, so the shortcut would
        // mistakenly return null (None.Equals(opt(T)) = false). Found via unit-test coverage:
        // previously this rule was reachable only for UNSOLVED opt (mutable side), creating
        // an asymmetric contract that quietly diverged from the algebraic lift `None ≤ opt(T)`.
        if (stateA is StatePrimitive { Name: PrimitiveTypeName.None } && stateB is StateOptional optB1)
            return optB1;
        if (stateB is StatePrimitive { Name: PrimitiveTypeName.None } && stateA is StateOptional optA1)
            return optA1;

        // Immutable-vs-immutable shortcut: equality on solved leaf types is cheap and final.
        // EXCLUDE StateStruct because (1) fully-concrete anonymous structs are now immutable
        // under IsMutable=>!IsSolved, and (2) the struct↔struct case below has its own nominal
        // short-circuit + MergeStructs path that handles row-poly union and cycle correctly.
        if (stateA is ITypeState typeA && !typeA.IsMutable && stateA is not StateStruct)
        {
            if (stateB is ITypeState typeB && !typeB.IsMutable && stateB is not StateStruct)
                return !typeB.Equals(typeA) ? null : typeA;

            if (stateB is ConstraintsState constrainsB)
                return !constrainsB.CanBeConvertedTo(typeA) ? null : typeA;
        }

        switch (stateA)
        {
            case StateArray arrayA when stateB is StateArray arrayB:
                return MergeCollectionsWithCycleGuard(stateA, stateB, arrayA.ElementNode, arrayB.ElementNode, arrayA);
            // Stage 0 hierarchy: any Array-branch StateCollection (List / Array /
            // FixedArray) fits into the legacy StateArray slot. The merge keeps
            // the StateCollection identity (narrower-Constructor wins —
            // `Specs/Tic/ConstructorLattice.md` §Cross-kind merge identity).
            case StateArray arrayA2 when stateB is StateCollection collArrB
                && (collArrB.Constructor == ConstructorKind.List
                 || collArrB.Constructor == ConstructorKind.Array
                 || collArrB.Constructor == ConstructorKind.FixedArray):
                return MergeCollectionsWithCycleGuard(stateA, stateB, arrayA2.ElementNode, collArrB.ElementNode, collArrB);
            case StateCollection collArrA when stateB is StateArray arrayB2
                && (collArrA.Constructor == ConstructorKind.List
                 || collArrA.Constructor == ConstructorKind.Array
                 || collArrA.Constructor == ConstructorKind.FixedArray):
                return MergeCollectionsWithCycleGuard(stateA, stateB, collArrA.ElementNode, arrayB2.ElementNode, collArrA);
            // Same-kind / cross-Array-branch collection merge. Same-kind returns
            // self. Cross-kind among the Array-branch (List ⊂ Array ⊂ FixedArray)
            // returns the narrower identity per the lattice rule
            // (`Specs/Tic/ConstructorLattice.md` §Cross-kind merge identity).
            // Set is on a separate branch — cross-kind with it is rejected.
            case StateCollection collA when stateB is StateCollection collB:
            {
                if (collA.Constructor == collB.Constructor)
                    return MergeCollectionsWithCycleGuard(stateA, stateB, collA.ElementNode, collB.ElementNode, collA);
                var narrower = NarrowerArrayBranchOrNull(collA, collB);
                if (narrower == null) return null;
                return MergeCollectionsWithCycleGuard(stateA, stateB, collA.ElementNode, collB.ElementNode, narrower);
            }
            case StateFun funA when stateB is StateFun funB:
            {
                if (funA.ArgsCount != funB.ArgsCount)
                    return null;

                for (int i = 0; i < funA.ArgsCount; i++)
                    MergeInplace(funA.ArgNodes[i], funB.ArgNodes[i]);
                MergeInplace(funA.RetNode, funB.RetNode);
                return funA;
            }
            case StateOptional optA when stateB is StateOptional optB:
                MergeInplace(optA.ElementNode, optB.ElementNode);
                return optA;
            // StateOptional × CS{IsOptional}: CS represents Optional<Desc>, structurally
            // equivalent to optA's Optional<Element>. Merge the inner types: optA.Element
            // (carried by optA.ElementNode) with csB's effective inner state. If csB has
            // no Descendant, the inner is "any T" — no constraint to push down. If csB has
            // a primitive/composite Descendant, AddDescendant it to optA's element node's
            // constraint. Bug hunt round 5 #27.
            case StateOptional optA2 when stateB is ConstraintsState csB && csB.IsOptional:
            {
                if (csB.HasDescendant && optA2.ElementNode.GetNonReference().State is ConstraintsState innerCs)
                    innerCs.AddDescendant(csB.Descendant);
                else if (csB.HasDescendant) {
                    var merged = GetMergedStateOrNull(optA2.ElementNode.GetNonReference().State, csB.Descendant);
                    if (merged == null) return null;
                    optA2.ElementNode.State = merged;
                }
                return optA2;
            }
            // StateMap deleted — Map flows as StateCollection(Map, pair-struct).
            // Merge of two SC(Map) goes through the standard SC same-kind merge
            // case (line ~90) which already MergeInplaces the element struct
            // nodes pointwise via MergeStructs.
            // Mut ≤ Frozen — asymmetric upcast (Liskov: read-only view is safe).
            // Same algebra as `LcaStructFields` (StateExtensions.Lca.cs:181):
            // mixed-mutability collapses to immutable Struct with covariant
            // per-field merge. Without this, `[{a={b=1}}].map(rule it.a.b)`
            // fires FU773 because the lambda's open-row frozen probe meets the
            // list literal's `mut{a:mut{b:..}}` at an inner field where
            // GetMergedStateOrNull (called recursively via MergeInplace) refused
            // to merge. Bug hunt round 2 #6. MergeStructs always returns a
            // non-frozen StateStruct, never StateMutableStruct, so the result
            // type is the safe Liskov upcast.
            case StateMutableStruct mutA when stateB is StateStruct frozenB and not StateMutableStruct:
                return MergeStructs(mutA, frozenB);
            case StateStruct frozenA and not StateMutableStruct when stateB is StateMutableStruct mutB:
                return MergeStructs(frozenA, mutB);
            case StateMutableStruct mutA when stateB is StateMutableStruct mutB:
                return MergeMutableStructs(mutA, mutB);
            case StateStruct strA when stateB is StateStruct strB:
            {
                // Nominal short-circuit: same TypeName ⇒ same type, return either side
                // without descending into MergeStructs (which would mutate field nodes via
                // MergeInplace and corrupt registry instances). Different names ⇒ null.
                if (StateStruct.NominalEquals(strA, strB) is bool nominal)
                    return nominal ? strA : null;
                // Cycle guard for recursive struct types (Amadio-Cardelli '93 bisimulation):
                // self-referential fields cause MergeStructs → MergeInplace → GetMergedStateOrNull
                // → MergeStructs infinite recursion. Re-entered pair = "already merged"
                // (idempotent under cycle). HashSet is reused per-thread via Clear (one alloc
                // for process lifetime) — not nulled+realloc'd per outer call.
                var visited = _mergeVisited ??= new HashSet<(ITicNodeState, ITicNodeState)>();
                var keyStruct = (stateA, stateB);
                if (!visited.Add(keyStruct))
                    return strA; // cycle — coinductive merge to self
                try {
                    return MergeStructs(strA, strB);
                } finally {
                    visited.Remove(keyStruct);
                }
            }
            case StateStruct strA2 when stateB is ConstraintsState constrainsB2:
            {
                if (constrainsB2.HasDescendant && constrainsB2.Descendant is StateStruct descStruct)
                {
                    var merged = MergeStructs(strA2, descStruct);
                    // Preserve IsOptional: opt(T) merged with U = opt(merge(T,U))
                    return merged != null && constrainsB2.IsOptional
                        ? StateOptional.Of(merged) : merged;
                }
                if (!constrainsB2.HasDescendant && !constrainsB2.IsComparable)
                    return constrainsB2.IsOptional ? StateOptional.Of(strA2) : strA2;
                return null;
            }
            // Bug hunt round 8 #46. Mirrors the StateStruct + CS case above for the
            // StateCollection composite. Without this, the switch falls through to
            // `case ConstraintsState:` below (line 186), swaps args, recurses as
            // (StateCollection, CS) — no case matches → null → MergeInplace throws
            // CannotMerge. Surfaces when struct-field LCA returns a concrete
            // StateCollection verbatim (UnifyOrNull short-circuit at
            // StateExtensions.Unify.cs:30-38) while the other branch's field stays
            // CS{Desc=StateCollection}; Push later joins them via MergeInplace.
            case StateCollection collA2 when stateB is ConstraintsState constrainsBColl:
            {
                if (constrainsBColl.HasDescendant && constrainsBColl.Descendant is StateCollection descColl)
                {
                    var merged = GetMergedStateOrNull(collA2, descColl);
                    return merged != null && constrainsBColl.IsOptional
                        ? StateOptional.Of(merged) : merged;
                }
                if (!constrainsBColl.HasDescendant && !constrainsBColl.IsComparable)
                    return constrainsBColl.IsOptional ? StateOptional.Of(collA2) : collA2;
                return null;
            }
            // Bug hunt round 8 #46 (StateFun extension). Mirrors the StateCollection
            // case above for StateFun, NARROWED to fire only when CS carries a
            // StateFun in its Descendant. The empty / Desc-less / Comparable CS
            // paths fall through to the swap+recurse below — eager merge there
            // regresses `list(rule …, rule …).map(…)` factory inference (multi-
            // deposit at signature-T0 needs Lca, not pointwise MergeInplace).
            //
            // Sibling fix lives at ConstraintsState.AddDescendant: live StateFun
            // descendants are stored verbatim (no Concretest widening), so the
            // CS.Descendant reaching this cell is the actual lambda's StateFun
            // — pointwise MergeInplace correctly fuses binder/body node identities
            // across the two lambdas joined by struct-field LCA.
            case StateFun funA2 when stateB is ConstraintsState constrainsBFun
                && constrainsBFun.HasDescendant && constrainsBFun.Descendant is StateFun descFun:
            {
                var merged = GetMergedStateOrNull(funA2, descFun);
                return merged != null && constrainsBFun.IsOptional
                    ? StateOptional.Of(merged) : merged;
            }
            case ConstraintsState constrainsA when stateB is ConstraintsState constrainsB:
                return constrainsB.MergeOrNull(constrainsA, resultOwnerNode);
            case ConstraintsState:
                return GetMergedStateOrNull(stateB, stateA);
            // CompCs + CompCs: Unify (interval narrows, IsOptional/IsClearable OR).
            // Used when two signature param nodes carrying CompCs constraints
            // collide via MergeInplace (e.g. xs has two ancestor edges feeding
            // CompCs from xs.clear() and xs.count()).
            case StateCompositeConstraints compCsA when stateB is StateCompositeConstraints compCsB:
                return compCsA.UnifyCompCs(compCsB);
            case StateRefTo refA:
            {
                // See method-level xmldoc: mutates refA.Node.State, returns the ORIGINAL
                // StateRefTo (pointer). Callers needing the resolved type call GetNonReference().
                // HashSet reused per-thread via Clear (see StateStruct branch above).
                var visited = _mergeVisited ??= new HashSet<(ITicNodeState, ITicNodeState)>();
                var key = (stateA, stateB);
                if (!visited.Add(key))
                    return stateA; // cycle — coinductive merge to self
                try {
                    var state = GetMergedStateOrNull(refA.Node.State, stateB);
                    if (state == null) return null;
                    refA.Node.State = state;
                    return stateA;
                } finally {
                    visited.Remove(key);
                }
            }
        }

        if (stateB is StateRefTo)
            return GetMergedStateOrNull(stateB, stateA);

        return null;
    }

    private static StateStruct MergeStructs(StateStruct strA, StateStruct strB) {
        var result = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in strA.Fields)
        {
            var bNode = strB.GetFieldOrNull(key);
            if (bNode != null)
            {
                // When merging None field with non-None field → wrap in Optional
                var aRef = value.GetNonReference();
                var bRef = bNode.GetNonReference();
                if (aRef.State == StatePrimitive.None && bRef.State is not StatePrimitive { Name: PrimitiveTypeName.None })
                {
                    if (bRef.State is StateOptional)
                        value.State = bRef.State; // None merges cleanly with Optional
                    else {
                        var innerNode = TicNode.CreateTypeVariableNode("e" + key + "'", bRef.State);
                        value.State = new StateOptional(innerNode);
                    }
                }
                else if (bRef.State == StatePrimitive.None && aRef.State is not StatePrimitive { Name: PrimitiveTypeName.None })
                {
                    if (aRef.State is not StateOptional) {
                        var innerNode = TicNode.CreateTypeVariableNode("e" + key + "'", aRef.State);
                        value.State = new StateOptional(innerNode);
                    }
                    // else: a is already Optional — keep it
                }
                else
                    MergeInplace(value, bNode);
            }
            else if (strB.IsOpen)
            {
                // strB is row-polymorphic ("at least these fields"); width-subtype lift —
                // missing field is supplied by strA. Result inherits the field. GH #128 Bug C:
                // multiple distinct field-access chains (`arr[0].v` and `arr[0].kids[0].v`)
                // emit open-row struct demands at different positions; their LCA with the
                // closed named-type literal must widen to include both.
            }
            else if (strA.IsFrozen || strB.IsFrozen)
                return null;
            result.Add(key, value);
        }

        foreach (var (key, value) in strB.Fields)
            result.TryAdd(key, value);

        return new StateStruct(result, isFrozen: false, isOpen: strA.IsOpen || strB.IsOpen) {
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(strA.IsOptionalSourced, strB.IsOptionalSourced),
            TypeName = StateStruct.MergedTypeName(strA.TypeName, strB.TypeName),
        };
    }

    /// <summary>
    /// Merge two MutableStructs. Fields are invariant — MergeInplace (which requires equality).
    /// </summary>
    private static StateMutableStruct MergeMutableStructs(StateMutableStruct strA, StateMutableStruct strB) {
        var result = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in strA.Fields)
        {
            var bNode = strB.GetFieldOrNull(key);
            if (bNode != null)
                MergeInplace(value, bNode); // invariant: must be same type
            else if (strA.IsFrozen || strB.IsFrozen)
                return null;
            result.Add(key, value);
        }

        foreach (var (key, value) in strB.Fields)
            result.TryAdd(key, value);

        return new StateMutableStruct(result, isFrozen: false, isOpen: strA.IsOpen || strB.IsOpen);
    }

    /// <summary>
    /// For a cross-kind StateCollection × StateCollection merge among the
    /// Array-branch (List ⊂ Array ⊂ FixedArray), return the narrower side.
    /// Returns null when either side is outside the Array-branch (Set is on a
    /// separate branch — cross-branch merges are rejected). Used by
    /// `GetMergedStateOrNull` to enforce the Stage 0 identity rule.
    /// </summary>
    private static StateCollection NarrowerArrayBranchOrNull(StateCollection a, StateCollection b) {
        int ra = ArrayBranchRank(a.Constructor);
        int rb = ArrayBranchRank(b.Constructor);
        if (ra < 0 || rb < 0) return null;
        return ra <= rb ? a : b;
    }

    private static int ArrayBranchRank(ConstructorKind k) => k switch {
        ConstructorKind.List => 0,
        ConstructorKind.Array => 1,
        ConstructorKind.FixedArray => 2,
        _ => -1,
    };

    /// <summary>
    /// Merge wrapper for single-arg collection states (Array × Array,
    /// Array × Collection(List), Collection × Collection same-kind). Adds a
    /// visited-pair guard to the shared `_mergeVisited` HashSet so recursive
    /// list types — e.g. `t = list&lt;t&gt;` once named-recursive collections
    /// are exposed — terminate coinductively (Amadio-Cardelli '93). Without
    /// this guard the element-merge from `MergeInplace` re-entered the same
    /// state pair and stack-overflowed.
    /// </summary>
    private static ITicNodeState MergeCollectionsWithCycleGuard(
        ITicNodeState stateA, ITicNodeState stateB,
        TicNode elemA, TicNode elemB, ITicNodeState result) {
        var visited = _mergeVisited ??= new HashSet<(ITicNodeState, ITicNodeState)>();
        var key = (stateA, stateB);
        if (!visited.Add(key))
            return result; // cycle — coinductive merge to self
        try {
            MergeInplace(elemA, elemB);
            return result;
        } finally {
            visited.Remove(key);
        }
    }

    /// <summary>
    /// Merge two nodes. Both of them become equiualent.
    ///
    /// In complex situation, 'secondary' node becomes reference to 'main' node, if it is possible
    /// </summary>
    public static void MergeInplace(TicNode main, TicNode secondary) {
        if (main == secondary)
            return;
        if (main.State is StateRefTo)
        {
            var nonreferenceMain = main.GetNonReference();
            var nonreferenceSecondary = secondary.GetNonReference();
            MergeInplace(nonreferenceMain, nonreferenceSecondary);
            return;
        }

        if (secondary.GetNonReference() == main)
            return;
        var res = GetMergedStateOrNull(main.State, secondary.State, main);
        // Implicit lift: T ≤ opt(T). When one side is composite T and the other is opt(T'),
        // the non-Optional side can be lifted. Merge the inner types.
        // Syntax nodes (concrete expressions) keep their type — the Optional side "wins".
        if (res == null
            && main.State is ICompositeState && !(main.State is StateOptional)
            && secondary.State is StateOptional optSec) {
            var innerMerge = GetMergedStateOrNull(main.State, optSec.Element);
            if (innerMerge != null) {
                // Non-optional merges into Optional's element. Optional node is the result.
                main.State = new StateRefTo(secondary);
                return;
            }
        }
        if (res == null
            && secondary.State is ICompositeState && !(secondary.State is StateOptional)
            && main.State is StateOptional optMain) {
            var innerMerge = GetMergedStateOrNull(optMain.Element, secondary.State);
            if (innerMerge != null) {
                secondary.State = new StateRefTo(main);
                return;
            }
        }
        if (res == null)
            throw TicErrors.CannotMerge(main, secondary);

        main.State = res;
        if (res is ITypeState t && t.IsSolved)
        {
            secondary.State = res;
            Stages.WorklistPullDriver.Enqueue(main);
            return;
        }

        foreach (var a in secondary.Ancestors)
            if (a != main) main.AddAncestor(a);
        secondary.ClearAncestors();

        secondary.State = new StateRefTo(main);
        // Worklist Pull hook (debt #10): merge re-fires Pull on the surviving node.
        Stages.WorklistPullDriver.Enqueue(main);
    }

    /// <summary>
    /// True iff <paramref name="str"/> has a field whose TIC node reaches
    /// <paramref name="target"/> via composite traversal without crossing
    /// an Optional or Array constructor. Used by Push width-propagation to
    /// detect would-be self-closing cycles before they are committed.
    /// </summary>
    internal static bool StructHasFieldReaching(StateStruct str, TicNode target) {
        foreach (var f in str.Fields)
            if (ReachesStructWithoutOptionalBreak(f.Value, target))
                return true;
        return false;
    }

    /// <summary>
    /// Find the unique declared named type whose declared field set is a
    /// superset of the inferred struct's fields. Returns the name if exactly
    /// one match exists, null otherwise (no match, or ambiguous).
    /// </summary>
    private static string FindUniqueMatchingNamedType(StateStruct str, Types.INamedTypeFieldRegistry registry) {
        string match = null;
        foreach (var kv in registry.All) {
            var declared = kv.Value;
            bool isSubset = true;
            foreach (var inferred in str.Fields) {
                bool found = false;
                foreach (var d in declared) {
                    if (string.Equals(d.name, inferred.Key, StringComparison.OrdinalIgnoreCase)) {
                        found = true; break;
                    }
                }
                if (!found) { isSubset = false; break; }
            }
            if (!isSubset) continue;
            if (match != null) return null; // ambiguous
            match = kv.Key;
        }
        return match;
    }

    /// <summary>
    /// Walks a node's full state graph (including through Optional/Array
    /// constructors — those break cycles but not opt-source provenance) to
    /// find any IsOptionalSourced struct. Used by ThrowIfRecursiveTypeDefinition
    /// to decide whether a detected cycle should be repaired or thrown.
    /// </summary>
    private static bool HasReachableOptSourcedStruct(TicNode start) {
        var mark = NextMark();
        return Walk(start, mark);

        static bool Walk(TicNode node, int mark) {
            var nonRef = node.GetNonReference();
            if (nonRef.VisitMark == mark) return false;
            var prev = nonRef.VisitMark;
            nonRef.VisitMark = mark;
            try {
                if (nonRef.State is StateStruct s && s.IsOptionalSourced) return true;
                switch (nonRef.State) {
                    case StateRefTo refTo: return Walk(refTo.Node, mark);
                    case ICompositeState comp:
                        for (int i = 0; i < comp.MemberCount; i++)
                            if (Walk(comp.GetMember(i), mark)) return true;
                        return false;
                    default: return false;
                }
            } finally {
                nonRef.VisitMark = prev;
            }
        }
    }

    /// <summary>
    /// True iff the struct subgraph rooted at <paramref name="str"/> is opt-sourced — either the
    /// struct itself carries the IsOptionalSourced marker (set by SetSafeFieldAccess, preserved
    /// through merges) OR any struct reachable via composite traversal does. Consulted when a
    /// cycle is detected through a struct whose own state lost the marker via width-propagation
    /// that copied a non-opt-sourced struct.
    /// </summary>
    internal static bool StructSubgraphIsOptSourced(StateStruct str) {
        if (str.IsOptionalSourced) return true;
        var mark = NextMark();
        foreach (var f in str.Fields)
            if (NodeSubgraphIsOptSourced(f.Value, mark))
                return true;
        return false;

        static bool NodeSubgraphIsOptSourced(TicNode node, int mark) {
            var nonRef = node.GetNonReference();
            if (nonRef.VisitMark == mark) return false;
            var prev = nonRef.VisitMark;
            nonRef.VisitMark = mark;
            try {
                if (nonRef.State is StateStruct s && s.IsOptionalSourced) return true;
                if (nonRef.State is ICompositeState comp) {
                    for (int i = 0; i < comp.MemberCount; i++)
                        if (NodeSubgraphIsOptSourced(comp.GetMember(i), mark)) return true;
                }
                if (nonRef.State is StateRefTo refTo)
                    return NodeSubgraphIsOptSourced(refTo.Node, mark);
                return false;
            } finally {
                nonRef.VisitMark = prev;
            }
        }
    }

    /// <summary>
    /// Merge all node states. First non ref state (or first state) called 'main'
    /// 'main' state takes all constrains and ancestors
    ///
    /// All other nodes refs to 'main'
    ///
    /// Returns 'main'
    /// </summary>
    public static TicNode MergeGroup(IEnumerable<TicNode> cycleRoute) {
        // Materialize cycle to array — cycles are small (2-5 nodes typically)
        var cycle = cycleRoute as TicNode[] ?? cycleRoute.ToArray();

        TicNode main = null;
        foreach (var c in cycle)
            if (c.State is not StateRefTo) { main = c; break; }
        main ??= cycle[0];

        foreach (var current in cycle)
        {
            if (current == main)
                continue;

            if (current.State is StateRefTo refState)
            {
                // Validate: ref target must be in the cycle
                bool found = false;
                foreach (var c in cycle)
                    if (c == refState.Node) { found = true; break; }
                if (!found) throw new InvalidOperationException();
            }
            else
            {
                main.State = GetMergedStateOrNull(main.State, current.State) ??
                             throw TicErrors.CannotMerge(main, current);
            }

            foreach (var a in current.Ancestors)
                if (a != main) main.AddAncestor(a);
            current.ClearAncestors();

            if (!current.IsSolved)
                current.State = new StateRefTo(main);
        }

        // Filter ancestors: remove cycle members, keep unique
        // Cycles are small → linear scan is faster than HashSet
        var keptAncestors = new List<TicNode>();
        foreach (var a in main.Ancestors) {
            bool isCycleMember = false;
            foreach (var c in cycle)
                if (a == c) { isCycleMember = true; break; }
            if (isCycleMember) continue;
            bool isDuplicate = false;
            foreach (var k in keptAncestors)
                if (a == k) { isDuplicate = true; break; }
            if (!isDuplicate)
                keptAncestors.Add(a);
        }

        main.ClearAncestors();
        foreach (var a in keptAncestors)
            main.AddAncestor(a);
        return main;
    }

    #endregion


    /// <summary>
    /// Two-phase Pull for graphs containing None nodes.
    /// Called only when hasNone=true (checked by caller).
    /// Phase 1: Pull None nodes first (sets IsOptional flags).
    /// Phase 2: Pull non-None (Concretest sees IsOptional).
    /// </summary>
    public static void PullConstraintsTwoPhase(TicNode[] toposortedNodes) {
        // Phase 1: Pull None nodes first — sets IsOptional flags.
        foreach (var node in toposortedNodes) {
            if (node.IsMemberOfAnything) continue;
            if (node.State == StatePrimitive.None)
                PullConstraintsRecursive(node);
        }

        // Phase 2: Pull non-None — Concretest sees IsOptional.
        foreach (var node in toposortedNodes) {
            if (node.IsMemberOfAnything) continue;
            if (node.State == StatePrimitive.None) continue;
            PullConstraintsRecursive(node);
        }
    }

    /// <summary>Pull constraints for a single node (streaming toposort+Pull fusion).</summary>
    public static void PullConstraintsForNode(TicNode node) => PullConstraintsRecursive(node);

    private static void PullConstraintsRecursive(TicNode node) {
        // VisitMark + NextMark (zero allocation). Each top-level call gets a
        // unique monotonically-increasing mark; "already visited" =
        // node.VisitMark == mark. Equivalent to HashSet.Add semantics but
        // without heap allocation or hash computation. Correctness for both
        // DAGs and cyclic graphs: in a cycle the second visit returns early
        // just as HashSet.Contains would. The old "mark-with-restore" pattern
        // (set on enter, RESTORE on exit) was the one that misbehaved on
        // shared subtrees — we keep the mark set permanently for this
        // traversal which is equivalent to HashSet semantics.
        PullRec(node, NextMark());
    }

    private static void PullRec(TicNode node, int mark) {
        if (node.VisitMark == mark) return;
        node.VisitMark = mark;
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
            PullConstrains(node, node.Ancestors[0]);
        else if (ancSize > 0)
            foreach (var ancestor in node.Ancestors.ToSnapshot())
                PullConstrains(node, ancestor);
        if (node.State is ICompositeState composite)
            for (int mi = 0; mi < composite.MemberCount; mi++)
                PullRec(composite.GetMember(mi), mark);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PullConstrains(TicNode descendant, TicNode ancestor) {
        if (descendant == ancestor) return;
        var res = PullConstraintsFunctions.Singleton.Invoke(ancestor, descendant);
        if (!res)
            throw TicErrors.IncompatibleTypes(ancestor, descendant);
    }

    /// <summary>
    /// Propagate Preferred resolution hints across all CS nodes in the graph.
    /// After Pull/Push, Preferred may be lost when constraints flow through composite
    /// snapshots (e.g., struct field access → array → element). This pass collects
    /// Preferred from CS nodes with concrete primitive descendants (from actual values,
    /// not function constraints), then applies to compatible CS nodes without Preferred.
    /// See TicPreferred.md for formal specification.
    /// </summary>
    public static void PropagatePreferred(TicNode[] toposortedNodes) {
        StatePrimitive commonPreferred = null;
        var collectMark = NextMark();
        foreach (var node in toposortedNodes)
            CollectPreferred(node, ref commonPreferred, collectMark);
        if (commonPreferred == null)
            return;
        var applyMark = NextMark();
        foreach (var node in toposortedNodes)
            ApplyPreferred(node, commonPreferred, applyMark);
    }

    private static void CollectPreferred(TicNode node, ref StatePrimitive preferred, int mark) {
        var nr = node.GetNonReference();
        if (nr.VisitMark == mark) return;
        if (nr.State is ICompositeState composite) {
            var prev = nr.VisitMark;
            nr.VisitMark = mark;
            for (int mi = 0; mi < composite.MemberCount; mi++)
                CollectPreferred(composite.GetMember(mi), ref preferred, mark);
            nr.VisitMark = prev;
        }
        if (nr.State is ConstraintsState cs && cs.Preferred != null)
            preferred ??= cs.Preferred;
    }

    private static void ApplyPreferred(TicNode node, StatePrimitive preferred, int mark) {
        var nr = node.GetNonReference();
        if (nr.VisitMark == mark) return;
        if (nr.State is ICompositeState composite) {
            var prev = nr.VisitMark;
            nr.VisitMark = mark;
            for (int mi = 0; mi < composite.MemberCount; mi++)
                ApplyPreferred(composite.GetMember(mi), preferred, mark);
            nr.VisitMark = prev;
        }
        if (nr.State is ConstraintsState cs
            && cs.HasDescendant && cs.Descendant is StatePrimitive descPrim
            && cs.CanBeConvertedTo(preferred)) {
            // Override an existing Preferred that came from a generic-constraint
            // default (e.g., Arithmetical → U24) when the broadcast Preferred matches the
            // descendant exactly OR is wider. PropagatePreferred's source-of-truth is
            // typically a literal int's I32 — that should dominate over a snapshot of the
            // constraint's narrowest bound. Conservative override condition: only when the
            // existing Preferred is reference-equal to Descendant (i.e., it was set to the
            // narrowest bound, suggesting auto-init rather than literal intent).
            if (cs.Preferred == null)
                cs.Preferred = preferred;
            else if (!cs.Preferred.Equals(preferred)
                  && ReferenceEquals(cs.Preferred, descPrim))
                cs.Preferred = preferred;
        }
    }

    public static void PushConstraints(TicNode[] toposortedNodes) {
        for (int i = toposortedNodes.Length - 1; i >= 0; i--)
        {
            var descendant = toposortedNodes[i];
            if (descendant.IsMemberOfAnything)
                continue;

            PushConstraintsRecursive(descendant);
        }
    }

    /// <summary>Public entry for SCC closure: re-run Push from a single node.</summary>
    public static void PushConstraintsForNode(TicNode node) => PushConstraintsRecursive(node);

    private static void PushConstraintsRecursive(TicNode node) => PushRec(node, NextMark());

    private static void PushRec(TicNode node, int mark) {
        // None is a literal value — constraints can't be pushed into it.
        if (node.State == StatePrimitive.None) return;
        if (node.VisitMark == mark) return;
        node.VisitMark = mark;
        if (node.State is ICompositeState composite)
            for (int mi = 0; mi < composite.MemberCount; mi++)
                PushRec(composite.GetMember(mi), mark);
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
            PushConstraints(node, node.Ancestors[0]);
        else if (ancSize > 0)
            foreach (var ancestor in node.Ancestors.ToSnapshot())
                PushConstraints(node, ancestor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushConstraints(TicNode descendant, TicNode ancestor) {
        if (descendant == ancestor)
            return;
        if (!PushConstraintsFunctions.Singleton.Invoke(ancestor, descendant))
            throw TicErrors.IncompatibleNodes(ancestor, descendant);
    }

    public static bool Destruction(TicNode[] toposorteNodes, bool hasOptionalTypes = true, Types.INamedTypeFieldRegistry namedTypeRegistry = null, bool isRecursion = true) {
        int notSolvedCount = 0;

        // VisitMark-based dedup: each top-level call gets a fresh mark via
        // NextMark(); within that call, a node is visited at most once. This
        // is equivalent to a per-traversal HashSet but with zero allocation
        // and int-compare lookup. Works uniformly for DAGs and μ-cyclic graphs.
        for (int i = toposorteNodes.Length - 1; i >= 0; i--) {
            var descendant = toposorteNodes[i];
            if (descendant.IsMemberOfAnything) continue;
            DestructionRec(descendant, namedTypeRegistry, NextMark(), isRecursion);
            if (!descendant.IsSolved) notSolvedCount++;
        }

        // After Destruction stamps TypeName on the cycle root struct (TryRepairOptSourcedCycle),
        // propagate that name to every structurally-compatible struct snapshot reachable in the
        // graph. TIC algebraic operations (Pull, Lca, Concretest) create independent struct
        // *instances* — the cycle-rescue stamp on one does not flow to snapshots taken earlier in
        // solving. The propagation closes that gap so FunnyType conversion sees the named identity
        // uniformly across operator T snapshots, variable states, and call-site generic arguments.
        // Only matters when there are named types AND potential μ-recursion.
        if (namedTypeRegistry != null && isRecursion)
            PropagateTypeNamesAcrossSnapshots(toposorteNodes, namedTypeRegistry);

        // Materialize remaining IsOptional ConstraintsState → StateOptional
        // after Destruction, before Finalization. Skip when no Optional types present.
        if (hasOptionalTypes)
            MaterializeOptionalFlags(toposorteNodes);

        // F-bound lifting. After all conventional resolution, any opt-sourced cycle struct that
        // DIDN'T get a TypeName stamp (ambiguous registry match, or no registry) becomes the
        // target: replace the inner element node's StateStruct with CS{StructBound=S} so the
        // function signature carries an F-bounded generic instead of a dangling unnamed μ-struct
        // that runtime can't represent. Existing TypeName stamps survive: F-bound and TypeName
        // coexist; runtime picks TypeName when present (nominal), falls back to F-bound (structural).
        //
        // A successful F-bound lift IS proof that the type is genuinely μ-polymorphic — the lift
        // surfaces a previously-hidden generic position. Even if all top-level nodes' IsSolved
        // flags are set, a lift means the function signature carries an F-bounded generic that
        // needs the GenericUserFunction path (not ConcreteUserFunction). Returning `false` here
        // forces SolveCore to call Finalize, which builds TicResultsWithGenerics whose
        // GenericsStates picks up the lifted CS via reachability.
        bool anyLifted = isRecursion && LiftMuTypes(toposorteNodes, namedTypeRegistry);

        return notSolvedCount == 0 && !anyLifted;
    }

    /// <summary>
    /// F-bound lifting. Walks the toposorted nodes looking for the canonical post-cycle-rescue shape:
    /// <code>StateOptional(elem) where elem.State is StateStruct{IsOptionalSourced=true, TypeName=null}</code>
    /// — i.e., a successfully cycle-repaired struct whose registry lookup did
    /// NOT yield a unique TypeName (ambiguous, or registry empty). For each
    /// such elem node, replace its state with
    /// <code>ConstraintsState{StructBound = struct}</code>.
    ///
    /// The original struct becomes Sμ, owned by the new CS. Back-edges inside Sμ already RefTo
    /// the elem node — that semantic now means "back to the bounded variable T". Outer Optional
    /// preserved; the inner element node now carries the bound.
    /// </summary>
    private static bool LiftMuTypes(TicNode[] toposorteNodes, Types.INamedTypeFieldRegistry namedTypeRegistry) {
        var visited = new HashSet<TicNode>();
        bool anyLifted = false;
        foreach (var node in toposorteNodes)
            anyLifted |= TryLiftNode(node, namedTypeRegistry, visited);

        // Degenerate-opt-cycle redirect via StateRefTo (Pottier-Rémy '05 §10.6 graph-as-witness;
        // Amadio-Cardelli '93 §4.2). Skip if no F-bound was lifted in pass 1 — the redirect exists
        // only to align degenerate cycles with sibling F-bounds.
        if (!anyLifted) return false;
        var fbounds = CollectFBoundHolders(toposorteNodes);
        if (fbounds.Count > 0)
        {
            foreach (var node in toposorteNodes)
                anyLifted |= TryRedirectDegenerateOptCycle(node, fbounds);
        }
        return anyLifted;
    }

    private static List<TicNode> CollectFBoundHolders(TicNode[] toposorteNodes) {
        var result = new List<TicNode>();
        HashSet<TicNode> visited = null;
        foreach (var n in toposorteNodes) {
            if (n == null) continue;
            // Top-level fast path first: if no node directly has StructBound,
            // skip the recursive composite walk (which allocates HashSet).
            var nr = n.GetNonReference();
            if (nr.State is ConstraintsState cs && cs.HasStructBound) {
                visited ??= new HashSet<TicNode>();
                CollectFBoundRec(n, result, visited);
            } else if (nr.State is ICompositeState) {
                visited ??= new HashSet<TicNode>();
                CollectFBoundRec(n, result, visited);
            }
        }
        return result;
    }

    private static void CollectFBoundRec(TicNode n, List<TicNode> result, HashSet<TicNode> visited) {
        var nr = n.GetNonReference();
        if (!visited.Add(nr)) return;
        if (nr.State is ConstraintsState cs && cs.HasStructBound)
            result.Add(nr);
        if (nr.State is ICompositeState comp) {
            for (int i = 0; i < comp.MemberCount; i++)
                CollectFBoundRec(comp.GetMember(i), result, visited);
        }
    }

    private static bool TryRedirectDegenerateOptCycle(TicNode node, List<TicNode> fbounds) {
        if (node == null) return false;
        var nr = node.GetNonReference();
        if (nr.State is not StateOptional outer) return false;
        var seen = new HashSet<TicNode> { nr };
        var probe = outer.ElementNode.GetNonReference();
        TicNode cycleHead = null;
        for (int depth = 0; depth < 128; depth++) {
            if (!seen.Add(probe)) {
                if (cycleHead == null) return false;
                if (!cycleHead.IsMutable) return false;
                if (cycleHead.State is not StateOptional) return false;
                if (fbounds.Count == 0) return false;
                var fbound = fbounds[0];
                if (fbound == cycleHead) return false;
                if (fbound.State is not ConstraintsState fbCs || !fbCs.HasStructBound) return false;
                cycleHead.State = new StateRefTo(fbound);
                TraceLog.WriteLine($"  TryRedirectDegenerateOptCycle: {cycleHead.Name} opt-only cycle head → RefTo({fbound.Name})");
                return true;
            }
            TicNode next;
            if (probe.State is StateOptional o) {
                next = o.ElementNode.GetNonReference();
                if (cycleHead == null) cycleHead = probe;
            }
            else if (probe.State is StateRefTo r) {
                next = r.Node.GetNonReference();
            }
            else if (probe.State is StateStruct) return false;
            else if (probe.State is ConstraintsState pCs && pCs.HasStructBound) return false;
            else return false;
            probe = next;
        }
        return false;
    }

    private static bool TryLiftNode(TicNode node, Types.INamedTypeFieldRegistry registry, HashSet<TicNode> visited) {
        var nr = node.GetNonReference();
        if (!visited.Add(nr)) return false;

        bool anyLifted = false;
        switch (nr.State) {
            case StateOptional opt:
                anyLifted |= TryLiftElement(opt.ElementNode, registry);
                anyLifted |= TryLiftNode(opt.ElementNode, registry, visited);
                break;
            case ICompositeState comp:
                for (int i = 0; i < comp.MemberCount; i++)
                    anyLifted |= TryLiftNode(comp.GetMember(i), registry, visited);
                break;
        }
        return anyLifted;
    }

    private static bool TryLiftElement(TicNode elemNode, Types.INamedTypeFieldRegistry registry) {
        var elem = elemNode.GetNonReference();
        if (elem.State is not StateStruct s) return false;
        if (s.TypeName != null) return false;          // nominally locked — never replace
        if (!s.IsOptionalSourced) return false;         // not from ?. — not a μ-recursive shape
        // C1 contractivity check — only lift true μ-cycles. Plain `?.field` access on a
        // non-recursive struct also has IsOptionalSourced=true but is NOT a μ-type; lifting it
        // would create a phantom F-bound generic where there should be a fully-solved
        // structural type. Walk fields looking for a RefTo back to elem.
        if (!StructHasSelfRef(s, elem)) return false;
        // Subset-match lift gate: only lift if fields match some registered named type.
        if (registry != null && !StructFieldsSubsetOfAnyRegistered(s, registry)) return false;

        // Build the lifted form: elem.State = CS{StructBound = s}.
        // s's back-edges already RefTo(elem); after the swap they semantically
        // refer to the bounded variable T (= this CS).
        //
        // Freeze the F-bound at lift. From this point forward s represents the recursive
        // function's parameter shape constraint — its field set is CLOSED (Cardelli-Mitchell '89
        // §3 contractive F-bound, Pierce TAPL §26.3). Width propagation must not extend it at
        // call sites; call args structurally Fit-check (subtype) against this bound, never widen
        // it. Freeze-on-generalization rule analogous to TAPL §22.6 let-generalization closing
        // a row variable into a ∀.
        s.IsFrozen = true;
        var cs = ConstraintsState.Empty;
        cs.StructBound = s;
        TraceLog.WriteLine($"  LiftMuTypes: {elem.Name} StateStruct→CS{{StructBound=Sμ-frozen}}");
        elem.State = cs;
        return true;
    }

    /// <summary>
    /// Meet of two F-bound struct shapes (upper-bound conjunction). Field union; shared fields
    /// recursively Gcd. Self-refs rewired to <paramref name="resultOwner"/>. Cycle-aware via
    /// bisimulation; contractivity preserved (wrappers never stripped, no new self-refs).
    /// </summary>
    public static StateStruct GcdBound(
        StateStruct a, StateStruct b, TicNode resultOwner, TicNode otherOwner) {
        var visited = new Dictionary<(StateStruct, StateStruct), StateStruct>();
        return GcdBoundInner(a, b, resultOwner, otherOwner, visited);
    }

    private static StateStruct GcdBoundInner(
        StateStruct a, StateStruct b, TicNode resultOwner, TicNode otherOwner,
        Dictionary<(StateStruct, StateStruct), StateStruct> visited) {
        if (ReferenceEquals(a, b)) return a;
        if (visited.TryGetValue((a, b), out var memo)) return memo;
        var mergedName = StateStruct.MergedTypeName(a.TypeName, b.TypeName);
        if (mergedName == null && a.TypeName != null && b.TypeName != null) return null;

        // F-bound width-rigidity (Cardelli-Mitchell '89 §3, TAPL §15.2):
        // meet(a-frozen, b) = a-frozen iff fields(a) ⊆ fields(b); never extend the bound.
        if (a.IsFrozen && !b.IsFrozen) return GcdFrozenAndOpen(a, b, resultOwner, otherOwner, visited);
        if (b.IsFrozen && !a.IsFrozen) return GcdFrozenAndOpen(b, a, resultOwner, otherOwner, visited);

        var result = new StateStruct(
            new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase),
            isFrozen: a.IsFrozen && b.IsFrozen,
            isOpen: a.IsOpen && b.IsOpen) {
            TypeName = mergedName,
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(a.IsOptionalSourced, b.IsOptionalSourced),
        };
        visited[(a, b)] = result;

        foreach (var (name, valA) in a.Fields) {
            var valB = b.GetFieldOrNull(name);
            if (valB == null) {
                result.AddField(name, RewireFieldNode(valA, otherOwner, resultOwner));
                continue;
            }
            var sA = valA.GetNonReference().State;
            var sB = valB.GetNonReference().State;
            var mergedFieldState = MergeFieldStateGcd(sA, sB, resultOwner, otherOwner, visited);
            if (mergedFieldState == null) return null;
            var mergedFieldNode = TicNode.CreateInvisibleNode(mergedFieldState);
            if (valA.IsOptionalElement || valB.IsOptionalElement)
                mergedFieldNode.IsOptionalElement = true;
            result.AddField(name, mergedFieldNode);
        }
        foreach (var (name, valB) in b.Fields) {
            if (a.GetFieldOrNull(name) != null) continue;
            result.AddField(name, RewireFieldNode(valB, otherOwner, resultOwner));
        }
        return result;
    }

    /// <summary>
    /// Meet of frozen F-bound × open candidate. Requires fields(candidate) ⊇ fields(frozen);
    /// candidate's extras are width-subtype slack. Cardelli-Mitchell '89 §3, TAPL §15.2.
    /// </summary>
    private static StateStruct GcdFrozenAndOpen(
        StateStruct frozen, StateStruct candidate, TicNode resultOwner, TicNode otherOwner,
        Dictionary<(StateStruct, StateStruct), StateStruct> visited) {
        var key = (frozen, candidate);
        if (visited.TryGetValue(key, out var memo)) return memo;
        var mergedName = StateStruct.MergedTypeName(frozen.TypeName, candidate.TypeName);
        if (mergedName == null && frozen.TypeName != null && candidate.TypeName != null) return null;

        // The result preserves the frozen bound's shape — same field set,
        // recursively-merged value states for shared fields. Frozen is preserved.
        var result = new StateStruct(
            new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase),
            isFrozen: true,
            isOpen: false) {
            TypeName = mergedName,
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(frozen.IsOptionalSourced, candidate.IsOptionalSourced),
        };
        visited[key] = result;

        foreach (var (name, valFrozen) in frozen.Fields) {
            var valCand = candidate.GetFieldOrNull(name);
            if (valCand == null) return null;
            var sF = valFrozen.GetNonReference().State;
            var sC = valCand.GetNonReference().State;
            var mergedFieldState = MergeFieldStateGcd(sF, sC, resultOwner, otherOwner, visited);
            if (mergedFieldState == null) return null;
            var mergedFieldNode = TicNode.CreateInvisibleNode(mergedFieldState);
            if (valFrozen.IsOptionalElement || valCand.IsOptionalElement)
                mergedFieldNode.IsOptionalElement = true;
            result.AddField(name, mergedFieldNode);
        }
        return result;
    }

    private static ITicNodeState MergeFieldStateGcd(
        ITicNodeState sA, ITicNodeState sB, TicNode resultOwner, TicNode otherOwner,
        Dictionary<(StateStruct, StateStruct), StateStruct> visited) {
        // Self-ref ≡ generic X-position (Pierce TAPL §20.2 fold/unfold; Cardelli-Mitchell '89 §3).
        // Both / one-side self-ref both collapse to RefTo(resultOwner) — instantiation preserves
        // the bound's general shape.
        bool aSelf = IsSelfRefBackToOwner(sA, otherOwner) || IsSelfRefBackToOwner(sA, resultOwner)
                   || IsBoundCarrierCs(sA);
        bool bSelf = IsSelfRefBackToOwner(sB, otherOwner) || IsSelfRefBackToOwner(sB, resultOwner)
                   || IsBoundCarrierCs(sB);
        if (aSelf && bSelf) return new StateRefTo(resultOwner);
        if (aSelf || bSelf) return new StateRefTo(resultOwner);

        if (sA is StateOptional optA && sB is StateOptional optB) {
            var inner = MergeFieldStateGcd(
                optA.ElementNode.GetNonReference().State,
                optB.ElementNode.GetNonReference().State,
                resultOwner, otherOwner, visited);
            if (inner == null) return null;
            var node = TicNode.CreateInvisibleNode(inner);
            node.IsOptionalElement = true;
            return new StateOptional(node);
        }
        if (sA is StateArray arrA && sB is StateArray arrB) {
            var inner = MergeFieldStateGcd(
                arrA.ElementNode.GetNonReference().State,
                arrB.ElementNode.GetNonReference().State,
                resultOwner, otherOwner, visited);
            if (inner is ITypeState innerType) return StateArray.Of(innerType);
            return null;
        }
        if (sA is StateStruct ssA && sB is StateStruct ssB)
            return GcdBoundInner(ssA, ssB, resultOwner, otherOwner, visited);
        if (sA is ITypeState tsA && sB is ITypeState tsB)
            return Algebra.StateExtensions.Gcd(tsA, tsB);
        if (sA.Equals(sB)) return sA;
        return null;
    }

    /// <summary>True iff state is a CS{StructBound!=null} — recursion-variable position post-LiftMuTypes.</summary>
    private static bool IsBoundCarrierCs(ITicNodeState state) {
        return state is ConstraintsState cs && cs.HasStructBound;
    }

    private static bool IsSelfRefBackToOwner(ITicNodeState state, TicNode owner) {
        if (owner == null) return false;
        var current = state;
        var safety = 0;
        while (current is StateRefTo r && safety++ < 64) {
            if (ReferenceEquals(r.Node, owner)) return true;
            current = r.Node.State;
        }
        return false;
    }

    private static TicNode RewireFieldNode(TicNode field, TicNode oldOwner, TicNode newOwner) {
        // Rewire any self-RefTo from oldOwner to newOwner.
        var nr = field.GetNonReference();
        var rewired = RewireState(nr.State, oldOwner, newOwner);
        if (ReferenceEquals(rewired, nr.State)) return field;
        return TicNode.CreateInvisibleNode(rewired);
    }

    private static ITicNodeState RewireState(ITicNodeState state, TicNode oldOwner, TicNode newOwner) {
        switch (state) {
            case StateRefTo r when ReferenceEquals(r.Node, oldOwner):
                return new StateRefTo(newOwner);
            case StateOptional opt: {
                var innerS = opt.ElementNode.GetNonReference().State;
                var rewired = RewireState(innerS, oldOwner, newOwner);
                if (ReferenceEquals(rewired, innerS)) return state;
                var node = TicNode.CreateInvisibleNode(rewired);
                node.IsOptionalElement = true;
                return new StateOptional(node);
            }
            default:
                return state;
        }
    }

    /// <summary>Rewire self-RefTos on <paramref name="s"/>'s field nodes from oldOwner to newOwner.</summary>
    internal static StateStruct RewireStructBoundOwnership(StateStruct s, TicNode oldOwner, TicNode newOwner) {
        if (oldOwner == null || newOwner == null || ReferenceEquals(oldOwner, newOwner)) return s;
        bool needsCopy = false;
        foreach (var (_, fieldNode) in s.Fields) {
            if (StateContainsSelfRef(fieldNode.GetNonReference().State, oldOwner)) {
                needsCopy = true; break;
            }
        }
        if (!needsCopy) return s;
        var newFields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, fieldNode) in s.Fields) {
            var nr = fieldNode.GetNonReference();
            var rewired = RewireState(nr.State, oldOwner, newOwner);
            newFields.Add(name, ReferenceEquals(rewired, nr.State) ? fieldNode : TicNode.CreateInvisibleNode(rewired));
        }
        return new StateStruct(newFields, isFrozen: s.IsFrozen, isOpen: s.IsOpen) {
            TypeName = s.TypeName,
            IsOptionalSourced = s.IsOptionalSourced,
        };
    }

    private static bool StateContainsSelfRef(ITicNodeState state, TicNode owner) {
        switch (state) {
            case StateRefTo r when ReferenceEquals(r.Node, owner): return true;
            case StateOptional opt: return StateContainsSelfRef(opt.ElementNode.GetNonReference().State, owner);
            case StateArray arr: return StateContainsSelfRef(arr.ElementNode.GetNonReference().State, owner);
            default: return false;
        }
    }

    /// <summary>
    /// True iff <paramref name="s"/>'s field-set EXACTLY matches some registered named type's
    /// declared field set. Conservative gate for lifting: accepting strict-subset matches would
    /// lift body-internal operator snapshots (partial accumulation of ?.field constraints) that
    /// are NOT principal μ-types.
    /// </summary>
    private static bool StructFieldsExactlyMatchRegistered(StateStruct s, Types.INamedTypeFieldRegistry registry) {
        foreach (var kv in registry.All) {
            var declared = kv.Value;
            if (declared.Length != s.FieldsCount) continue;
            bool allFound = true;
            foreach (var inferred in s.Fields) {
                bool found = false;
                foreach (var d in declared)
                    if (string.Equals(d.name, inferred.Key, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                if (!found) { allFound = false; break; }
            }
            if (allFound) return true;
        }
        return false;
    }

    private static bool StructFieldsSubsetOfAnyRegistered(StateStruct s, Types.INamedTypeFieldRegistry registry) {
        if (s.FieldsCount == 0) return false;
        foreach (var kv in registry.All) {
            var declared = kv.Value;
            if (declared.Length < s.FieldsCount) continue;
            bool allFound = true;
            foreach (var inferred in s.Fields) {
                bool found = false;
                foreach (var d in declared)
                    if (string.Equals(d.name, inferred.Key, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                if (!found) { allFound = false; break; }
            }
            if (allFound) return true;
        }
        return false;
    }



    /// <summary>True iff the struct's field graph contains a self-RefTo (μ-cycle).</summary>
    internal static bool StructIsRecursiveCycle(StateStruct s, TicNode owner) {
        return StructHasSelfRef(s, owner);
    }

    private static bool StructHasSelfRef(StateStruct s, TicNode owner) {
        var visited = new HashSet<TicNode>();
        foreach (var (_, fieldNode) in s.Fields)
            if (FieldReachesOwner(fieldNode, owner, s, visited))
                return true;
        return false;
    }

    /// <summary>
    /// CS-internal slot-promotion predicate: at least one closing field path exists AND every
    /// closing path crosses an Optional/Array constructor (C1 contractivity, Pierce TAPL §20.2).
    /// False if no closing path exists OR any closes non-contractively (must continue to throw).
    /// </summary>
    internal static bool StructDescendantClosesContractively(StateStruct s, TicNode owner) {
        bool foundClosingPath = false;
        foreach (var (_, fieldNode) in s.Fields) {
            var visited = new HashSet<TicNode>();
            var verdict = ClosingPathVerdict(fieldNode, owner, s, visited, crossedOptOrArr: false);
            switch (verdict) {
                case PathVerdict.NoClose:
                    continue;
                case PathVerdict.ClosesContractively:
                    foundClosingPath = true;
                    continue;
                case PathVerdict.ClosesNonContractively:
                    return false;
            }
        }
        return foundClosingPath;
    }

    private enum PathVerdict {
        NoClose,
        ClosesContractively,
        ClosesNonContractively
    }

    private static PathVerdict ClosingPathVerdict(
            TicNode node, TicNode owner, StateStruct ownerStruct,
            HashSet<TicNode> visited, bool crossedOptOrArr) {
        var nr = node.GetNonReference();
        if (ReferenceEquals(nr, owner) || ReferenceEquals(nr.State, ownerStruct))
            return crossedOptOrArr
                ? PathVerdict.ClosesContractively
                : PathVerdict.ClosesNonContractively;
        if (!visited.Add(nr)) return PathVerdict.NoClose;
        switch (nr.State) {
            case StateRefTo r:
                return ClosingPathVerdict(r.Node, owner, ownerStruct, visited, crossedOptOrArr);
            case StateOptional opt:
                return ClosingPathVerdict(opt.ElementNode, owner, ownerStruct, visited, crossedOptOrArr: true);
            case StateArray arr:
                return ClosingPathVerdict(arr.ElementNode, owner, ownerStruct, visited, crossedOptOrArr: true);
            case ConstraintsState cs when cs.HasStructBound:
                // F-bound is a contractive boundary (Pierce TAPL §20) — path ends.
                return PathVerdict.NoClose;
            default:
                return PathVerdict.NoClose;
        }
    }

    private static bool FieldReachesOwner(TicNode node, TicNode owner, StateStruct ownerStruct, HashSet<TicNode> visited) {
        var nr = node.GetNonReference();
        if (ReferenceEquals(nr, owner)) return true;
        if (!visited.Add(nr)) return false;
        // Cycle-rescue wraps the cycle struct by sharing the SAME instance on the inner node —
        // reference equality on the state IS the cycle closure.
        if (ReferenceEquals(nr.State, ownerStruct)) return true;
        switch (nr.State) {
            case StateRefTo r:
                return FieldReachesOwner(r.Node, owner, ownerStruct, visited);
            case StateOptional opt:
                return FieldReachesOwner(opt.ElementNode, owner, ownerStruct, visited);
            case StateArray arr:
                return FieldReachesOwner(arr.ElementNode, owner, ownerStruct, visited);
            default:
                return false;
        }
    }

    /// <summary>
    /// Stamp TypeName on unnamed struct snapshots whose field set is a subset of exactly one
    /// already-stamped name in the graph. The "exactly one" rule prevents over-tagging when
    /// multiple named types share a common field prefix.
    /// </summary>
    private static void PropagateTypeNamesAcrossSnapshots(TicNode[] nodes, Types.INamedTypeFieldRegistry registry) {
        // Pass 1 collects stamped names + detects any opt-sourced struct (without one we are
        // not inside a μ-type body — stamping transient field-access snapshots would over-tag).
        var presentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool anyOptSourced = false;
        var collectMark = NextMark();
        foreach (var node in nodes)
            CollectStampedTypeNames(node, presentNames, ref anyOptSourced, collectMark);
        if (!anyOptSourced) return;
        if (presentNames.Count == 0) return;

        var nameFieldIndex = BuildNameFieldIndex(presentNames, registry);
        if (nameFieldIndex.Count == 0) return;

        var stampMark = NextMark();
        foreach (var node in nodes)
            StampUnnamedStructsByPresentNames(node, nameFieldIndex, stampMark);
    }

    private static List<KeyValuePair<string, HashSet<string>>> BuildNameFieldIndex(
        HashSet<string> presentNames, Types.INamedTypeFieldRegistry registry) {
        var index = new List<KeyValuePair<string, HashSet<string>>>(presentNames.Count);
        foreach (var name in presentNames) {
            if (!registry.TryGetFields(name, out var declared)) continue;
            var fieldSet = new HashSet<string>(declared.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var d in declared) fieldSet.Add(d.name);
            index.Add(new KeyValuePair<string, HashSet<string>>(name, fieldSet));
        }
        return index;
    }

    private static void CollectStampedTypeNames(TicNode node, HashSet<string> names, ref bool anyOptSourced, int mark) {
        var n = node.GetNonReference();
        if (n.VisitMark == mark) return;
        n.VisitMark = mark;
        switch (n.State) {
            case StateStruct s:
                if (s.TypeName != null) names.Add(s.TypeName);
                if (s.IsOptionalSourced) anyOptSourced = true;
                for (int i = 0; i < s.MemberCount; i++)
                    CollectStampedTypeNames(s.GetMember(i), names, ref anyOptSourced, mark);
                break;
            case ConstraintsState cs when cs.HasDescendant && cs.Descendant is StateStruct descStr:
                if (descStr.TypeName != null) names.Add(descStr.TypeName);
                if (descStr.IsOptionalSourced) anyOptSourced = true;
                for (int i = 0; i < descStr.MemberCount; i++)
                    CollectStampedTypeNames(descStr.GetMember(i), names, ref anyOptSourced, mark);
                break;
            case ICompositeState comp:
                for (int i = 0; i < comp.MemberCount; i++)
                    CollectStampedTypeNames(comp.GetMember(i), names, ref anyOptSourced, mark);
                break;
        }
    }

    private static void StampUnnamedStructsByPresentNames(
        TicNode node, List<KeyValuePair<string, HashSet<string>>> nameFieldIndex, int mark) {
        var n = node.GetNonReference();
        if (n.VisitMark == mark) return;
        n.VisitMark = mark;
        switch (n.State) {
            case StateStruct s:
                StampByNameFieldIndex(s, nameFieldIndex);
                for (int i = 0; i < s.MemberCount; i++)
                    StampUnnamedStructsByPresentNames(s.GetMember(i), nameFieldIndex, mark);
                break;
            case ConstraintsState cs when cs.HasDescendant && cs.Descendant is StateStruct descStr:
                StampByNameFieldIndex(descStr, nameFieldIndex);
                for (int i = 0; i < descStr.MemberCount; i++)
                    StampUnnamedStructsByPresentNames(descStr.GetMember(i), nameFieldIndex, mark);
                break;
            case ICompositeState comp:
                for (int i = 0; i < comp.MemberCount; i++)
                    StampUnnamedStructsByPresentNames(comp.GetMember(i), nameFieldIndex, mark);
                break;
        }
    }

    private static void StampByNameFieldIndex(StateStruct s, List<KeyValuePair<string, HashSet<string>>> nameFieldIndex) {
        if (s.TypeName != null) return;
        if (s.FieldsCount == 0) return; // empty struct snapshot — skip, no shape to match
        string match = null;
        for (int i = 0; i < nameFieldIndex.Count; i++) {
            var entry = nameFieldIndex[i];
            // Inferred fields ⊆ declared fields (subset by name) — O(inferred) via HashSet.
            bool isSubset = true;
            foreach (var inferred in s.Fields) {
                if (!entry.Value.Contains(inferred.Key)) { isSubset = false; break; }
            }
            if (!isSubset) continue;
            if (match != null) return;
            match = entry.Key;
        }
        if (match != null) s.TypeName = match;
    }

    /// <summary>Shared monotonic VisitMark counter. Thread-safe.</summary>
    private static int _nextMark = 1000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextMark() => System.Threading.Interlocked.Increment(ref _nextMark);
    private static void MaterializeOptionalFlags(TicNode[] nodes) {
        bool hasOptional = false;
        foreach (var node in nodes) {
            var s = node.GetNonReference().State;
            if (s is ConstraintsState { IsOptional: true }) {
                hasOptional = true;
                break;
            }
            if (s is ICompositeState c)
                for (int mi = 0; mi < c.MemberCount; mi++)
                    if (c.GetMember(mi).GetNonReference().State is ConstraintsState { IsOptional: true }) {
                        hasOptional = true;
                        break;
                    }
            if (hasOptional) break;
        }
        if (!hasOptional) return;

        var mark = NextMark();
        foreach (var node in nodes)
            MaterializeOptionalNode(node, nodes, mark);
    }

    private static void MaterializeOptionalNode(TicNode node, TicNode[] allNodes, int mark) {
        var n = node.GetNonReference();
        if (n.VisitMark == mark) return;
        n.VisitMark = mark;

        if (n.State is ConstraintsState cs && cs.IsOptional) {
            if (!cs.HasDescendant && !cs.HasAncestor && !cs.IsComparable)
            {
                TraceLog.WriteLine($"  Materialize: {n.Name}: {cs} → None (pure optional, no constraints)");
                n.State = StatePrimitive.None;
            }
            else
            {
                TraceLog.WriteLine($"  Materialize: {n.Name}: {cs} → opt(inner)");
                var innerCs = ConstraintsState.Of(cs.Descendant, cs.Ancestor, cs.IsComparable);
                innerCs.Preferred = cs.Preferred;
                var innerNode = TicNode.CreateTypeVariableNode("e" + n.Name + "'", innerCs);
                innerNode.IsOptionalElement = true;
                n.State = new StateOptional(innerNode);

                // Concrete-value syntax nodes merged into the Optional during Destruction must
                // keep their non-Optional type — redirect their RefTo to the inner node.
                foreach (var other in allNodes) {
                    if (other.Type == TicNodeType.SyntaxNode
                        && other.State is StateRefTo refTo
                        && ReferenceEquals(refTo.Node, n)) {
                        other.State = new StateRefTo(innerNode);
                    }
                }
            }
        }
        if (n.State is ICompositeState composite) {
            for (int mi = 0; mi < composite.MemberCount; mi++)
                MaterializeOptionalNode(composite.GetMember(mi), allNodes, mark);
        }
    }

    private static void DestructionRec(TicNode node, Types.INamedTypeFieldRegistry namedTypeRegistry, int mark, bool isRecursion) {
        // Per-traversal HashSet semantics via mark — zero allocation.
        if (node.VisitMark == mark) return;
        node.VisitMark = mark;
        ThrowIfRecursiveTypeDefinition(node, namedTypeRegistry, isRecursion);
        if (node.State is ICompositeState composite) {
            if (composite.HasAnyReferenceMember) node.State = composite.GetNonReferenced();
            for (int mi = 0; mi < composite.MemberCount; mi++)
                DestructionRec(composite.GetMember(mi), namedTypeRegistry, mark, isRecursion);
            if (node.State is StateOptional)
                node.FlattenNestedOptional();
        }
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
            Destruction(node, node.Ancestors[0]);
        else if (ancSize > 0)
            foreach (var ancestor in node.Ancestors.ToSnapshot())
                Destruction(node, ancestor);
    }

    public static bool Destruction(TicNode descendantNode, TicNode ancestorNode) {
        var nonRefAncestor = ancestorNode.GetNonReference();
        var nonRefDescendant = descendantNode.GetNonReference();
        if (nonRefDescendant == nonRefAncestor)
            return true;
        return DestructionFunctions.Singleton.Invoke(nonRefAncestor, nonRefDescendant);
    }


    public static void BecomeReferenceFor(this TicNode referencedNode, TicNode original) {
        referencedNode = referencedNode.GetNonReference();
        original = original.GetNonReference();
        if (referencedNode.Type == TicNodeType.SyntaxNode)
            MergeInplace(original, referencedNode);
        else
            MergeInplace(referencedNode, original);
    }

    /// <summary>
    /// <paramref name="descNode"/> nullable — memoize on <see cref="TicNode.TransformElementInner"/>
    /// only when worklist driver active (debt #10 wave-2).
    /// </summary>
    public static StateArray TransformToArrayOrNull(TicNode descNode, object descNodeName, ConstraintsState descendant) {
        if (descendant.NoConstrains)
        {
            var node = GetOrCreateTransformElement(descNode, descNodeName, ConstraintsState.Empty);
            return new StateArray(node);
        }
        else if (descendant.HasDescendant && descendant.Descendant is StateArray arrayEDesc)
        {
            if (!arrayEDesc.IsSolved)
                return arrayEDesc;
            TicNode node;
            if (Stages.WorklistPullDriver.IsActive && descNode != null && descNode.TransformElementInner != null) {
                node = descNode.TransformElementInner;
            } else {
                var constrains = ConstraintsState.Empty;
                constrains.AddDescendant(arrayEDesc.Element);
                node = TicNode.CreateTypeVariableNode("e" + descNodeName + "'", constrains);
                if (Stages.WorklistPullDriver.IsActive && descNode != null)
                    descNode.TransformElementInner = node;
            }
            return new StateArray(node);
        }
        // Array-branch SC fits legacy StateArray slot — reuse descendant's element identity.
        else if (descendant.HasDescendant
                 && descendant.Descendant is StateCollection collDesc
                 && (collDesc.Constructor == ConstructorKind.List
                  || collDesc.Constructor == ConstructorKind.Array
                  || collDesc.Constructor == ConstructorKind.FixedArray))
        {
            if (!collDesc.IsSolved)
                return new StateArray(collDesc.ElementNode);
            TicNode node;
            if (Stages.WorklistPullDriver.IsActive && descNode != null && descNode.TransformElementInner != null) {
                node = descNode.TransformElementInner;
            } else {
                var constrains = ConstraintsState.Empty;
                constrains.AddDescendant(collDesc.Element);
                node = TicNode.CreateTypeVariableNode("e" + descNodeName + "'", constrains);
                if (Stages.WorklistPullDriver.IsActive && descNode != null)
                    descNode.TransformElementInner = node;
            }
            return new StateArray(node);
        }
        else
            return null;
    }

    /// <summary>Memoize on TransformElementInner only when worklist active.</summary>
    private static TicNode GetOrCreateTransformElement(TicNode descNode, object descNodeName, ConstraintsState cs) {
        if (Stages.WorklistPullDriver.IsActive && descNode != null && descNode.TransformElementInner != null)
            return descNode.TransformElementInner;
        var node = TicNode.CreateTypeVariableNode("e" + descNodeName + "'", cs);
        if (Stages.WorklistPullDriver.IsActive && descNode != null)
            descNode.TransformElementInner = node;
        return node;
    }

    /// <summary>
    /// Parameterised sibling of <see cref="TransformToArrayOrNull"/> over <see cref="ConstructorKind"/>.
    /// Map's pair-struct element built inline. Null when CS cannot become this kind.
    /// </summary>
    public static StateCollection TransformToCollectionOrNull(
        ConstructorKind kind, TicNode descNode, object descNodeName, ConstraintsState descendant) {
        // CS{IsClearable=true} alone is shape-compatible — reject early if target kind isn't Clearable.
        bool noShapeConstraints = !descendant.HasDescendant && !descendant.HasAncestor
            && !descendant.IsComparable && !descendant.HasStructBound;
        if (noShapeConstraints && descendant.IsClearable && !ConstructorLattice.IsClearable(kind))
            return null;
        if (descendant.NoConstrains || (noShapeConstraints && descendant.IsClearable))
        {
            return BuildFreshCollection(kind, descNode, descNodeName);
        }
        if (descendant.HasDescendant
            && descendant.Descendant is StateCollection existing
            && existing.Constructor == kind)
        {
            if (!existing.IsSolved) return existing;
            // Fresh SC: copy element type into fresh field node(s) — break shared identity.
            if (kind == ConstructorKind.Map
                && existing.ElementNode.GetNonReference().State is StateStruct ss
                && ss.GetFieldOrNull("key") is { } kExist
                && ss.GetFieldOrNull("value") is { } vExist)
            {
                // Map's (k, v) pair — allocate per call (not single-element memo).
                var kCs = ConstraintsState.Empty;
                if (kExist.State is ITypeState kTs) kCs.AddDescendant(kTs);
                var vCs = ConstraintsState.Empty;
                if (vExist.State is ITypeState vTs) vCs.AddDescendant(vTs);
                var kNode = TicNode.CreateTypeVariableNode("k" + descNodeName + "'", kCs);
                var vNode = TicNode.CreateTypeVariableNode("v" + descNodeName + "'", vCs);
                return StateCollection.OfMap(kNode, vNode);
            }
            var c = ConstraintsState.Empty;
            c.AddDescendant(existing.Element);
            var node = GetOrCreateTransformElement(descNode, descNodeName, c);
            return new StateCollection(kind, node);
        }
        // Accept subtype kinds in Array-branch (list ⊆ array ⊆ fixedArray).
        if (descendant.HasDescendant
            && descendant.Descendant is StateCollection existingSub
            && ConstructorLattice.IsSubtypeOf(existingSub.Constructor, kind))
        {
            if (!existingSub.IsSolved) return existingSub;
            var c = ConstraintsState.Empty;
            c.AddDescendant(existingSub.Element);
            var node = GetOrCreateTransformElement(descNode, descNodeName, c);
            return new StateCollection(kind, node);
        }
        // ee-mode T[] (StateArray) IS fixedArray<T> — fits any Array-branch kind.
        if (descendant.HasDescendant
            && descendant.Descendant is StateArray eeArr
            && (kind == ConstructorKind.List || kind == ConstructorKind.Array
                || kind == ConstructorKind.FixedArray))
        {
            // StateStruct elements may carry μ-cycles — reuse the existing ElementNode so
            // the cycle structure survives (a fresh wrapper would trigger Recursive type).
            if (eeArr.Element is StateStruct)
                return new StateCollection(kind, eeArr.ElementNode);
            var c = ConstraintsState.Empty;
            if (eeArr.Element is ITypeState elemTs) c.AddDescendant(elemTs);
            var node = GetOrCreateTransformElement(descNode, descNodeName, c);
            return new StateCollection(kind, node);
        }
        return null;
    }

    /// <summary>Build a fresh SC: Map gets a {key, value} pair-struct, single-arg kinds get a CS-empty element node.</summary>
    private static StateCollection BuildFreshCollection(ConstructorKind kind, TicNode descNode, object descNodeName) {
        if (kind == ConstructorKind.Map) {
            // Map's (k, v) pair — allocate per call.
            var k = TicNode.CreateTypeVariableNode("k" + descNodeName + "'", ConstraintsState.Empty);
            var v = TicNode.CreateTypeVariableNode("v" + descNodeName + "'", ConstraintsState.Empty);
            return StateCollection.OfMap(k, v);
        }
        var elem = GetOrCreateTransformElement(descNode, descNodeName, ConstraintsState.Empty);
        return new StateCollection(kind, elem);
    }

    /// <summary>
    /// <paramref name="descNode"/> nullable — when null OR worklist driver inactive,
    /// allocates fresh per call (streaming behavior). When non-null AND worklist active,
    /// reuses <see cref="TicNode.WrapOptionalInner"/> cache to prevent fresh-allocation churn
    /// on worklist re-entry (debt #10 wave-2).
    /// </summary>
    public static StateOptional TransformToOptionalOrNull(TicNode descNode, object descNodeName, ConstraintsState descendant) {
        if (descendant.NoConstrains)
        {
            var node = GetOrCreateWrapOptionalInner(descNode, descNodeName, ConstraintsState.Empty);
            return new StateOptional(node);
        }

        if (descendant.HasDescendant && descendant.Descendant == StatePrimitive.None)
        {
            var node = GetOrCreateWrapOptionalInner(descNode, descNodeName, ConstraintsState.Empty);
            return new StateOptional(node);
        }

        if (descendant.HasDescendant && descendant.Descendant is StateOptional optDesc)
        {
            if (!optDesc.IsSolved)
                return optDesc;
            TicNode node;
            if (Stages.WorklistPullDriver.IsActive && descNode != null && descNode.WrapOptionalInner != null) {
                node = descNode.WrapOptionalInner;
            } else {
                var constrains = ConstraintsState.Empty;
                constrains.AddDescendant(optDesc.Element);
                node = TicNode.CreateTypeVariableNode("e" + descNodeName + "'", constrains);
                if (Stages.WorklistPullDriver.IsActive && descNode != null)
                    descNode.WrapOptionalInner = node;
            }
            return new StateOptional(node);
        }

        // IsOptional + constraints: materialize now so inner constraints are preserved
        // (implicit lift T ≤ opt(T) would lose them). Pure IsOptional handled later.
        if (descendant.IsOptional && (descendant.HasDescendant || descendant.HasAncestor))
        {
            TicNode node;
            if (Stages.WorklistPullDriver.IsActive && descNode != null && descNode.WrapOptionalInner != null) {
                node = descNode.WrapOptionalInner;
            } else {
                var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
                // Preserve Preferred; fall back to a concrete primitive Descendant (mirrors SetDef).
                innerCs.Preferred = descendant.Preferred
                    ?? (descendant.HasDescendant && descendant.Descendant is StatePrimitive dp
                        ? dp : null);
                node = TicNode.CreateTypeVariableNode("e" + descNodeName + "'", innerCs);
                if (Stages.WorklistPullDriver.IsActive && descNode != null)
                    descNode.WrapOptionalInner = node;
            }
            return new StateOptional(node);
        }

        return null;
    }

    /// <summary>Memoize on <see cref="TicNode.WrapOptionalInner"/> only when worklist active.</summary>
    private static TicNode GetOrCreateWrapOptionalInner(TicNode descNode, object descNodeName, ConstraintsState cs) {
        if (Stages.WorklistPullDriver.IsActive && descNode != null && descNode.WrapOptionalInner != null)
            return descNode.WrapOptionalInner;
        var node = TicNode.CreateTypeVariableNode("e" + descNodeName + "'", cs);
        if (Stages.WorklistPullDriver.IsActive && descNode != null)
            descNode.WrapOptionalInner = node;
        return node;
    }

    public static StateFun TransformToFunOrNull(object descNodeName, ConstraintsState descendant, StateFun ancestor) {
        if (descendant.NoConstrains)
        {
            var argNodes = new TicNode[ancestor.ArgsCount];
            for (int i = 0; i < ancestor.ArgsCount; i++)
            {
                var argNode = TicNode.CreateTypeVariableNode("a'" + descNodeName + "'" + i, ConstraintsState.Empty);
                ancestor.ArgNodes[i].AddAncestor(argNode);
                argNodes[i] = argNode;
            }

            var retNode = TicNode.CreateTypeVariableNode("r'" + descNodeName, ConstraintsState.Empty);
            retNode.AddAncestor(ancestor.RetNode);

            return StateFun.Of(argNodes, retNode);
        }

        if (descendant.Descendant is StateFun funDesc && funDesc.ArgsCount == ancestor.ArgsCount)
        {
            var argNodes = new TicNode[ancestor.ArgsCount];
            for (int i = 0; i < ancestor.ArgsCount; i++)
            {
                var state = funDesc.ArgNodes[i].State;

                var argNode = TicNode.CreateTypeVariableNode("a'" + descNodeName + "'" + i,
                    ConstraintsState.Of(
                        anc: state as StatePrimitive,
                        isComparable: state is ConstraintsState {IsComparable:true}));
                ancestor.ArgNodes[i].AddAncestor(argNode);
                argNodes[i] = argNode;
            }

            var retNode = TicNode.CreateTypeVariableNode("r'" + descNodeName,
                    ConstraintsState.Of(desc : funDesc.ReturnType));
            retNode.AddAncestor(ancestor.RetNode);

            return StateFun.Of(argNodes, retNode);
        }

        return null;
    }

    /// <summary>
    /// Pull-stage field-identity unification: shared-name fields between two struct shapes must
    /// denote the same TicNode. Safe-merge predicate: snapshot's field is a solved primitive AND
    /// ancestor's field is an empty CS — MergeInplace takes the solved-result fast path, no
    /// Optional/generic invariant touched. Other shapes deferred to regular Pull/Push edges.
    /// </summary>
    public static void UnifyStructFieldIdentities(
        System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, TicNode>> snapshotFields,
        StateStruct ancStruct) {
        foreach (var (key, value) in snapshotFields)
        {
            var snapField = value.GetNonReference();
            var ancField = ancStruct.GetFieldOrNull(key)?.GetNonReference();
            if (ancField == null || ancField == snapField) continue;
            if (snapField.State is not StatePrimitive) continue;
            if (ancField.State is not ConstraintsState ancCs || !ancCs.NoConstrains) continue;
            if (GetMergedStateOrNull(snapField.State, ancField.State) != null)
                MergeInplace(snapField, ancField);
        }
    }

    public static StateStruct TransformToStructOrNull(ConstraintsState descendant, StateStruct ancStruct) {
        if (descendant.NoConstrains)
            return ancStruct;

        if (descendant.HasDescendant && descendant.Descendant is StateStruct structDesc)
        {
            if (structDesc.IsSolved)
                return structDesc;

            bool allFieldsAreSolved = true;

            var newFields = new Dictionary<string, TicNode>(structDesc.MembersCount, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in structDesc.Fields)
            {
                var nrField = value.GetNonReference();
                allFieldsAreSolved = allFieldsAreSolved && nrField.IsSolved;
                newFields.Add(key, nrField);
            }

            return structDesc is StateMutableStruct
                ? new StateMutableStruct(newFields, isFrozen: false, isOpen: structDesc.IsOpen)
                : new StateStruct(newFields, isFrozen: structDesc.IsFrozen, isOpen: structDesc.IsOpen) {
                    IsOptionalSourced = structDesc.IsOptionalSourced,
                    TypeName = structDesc.TypeName,
                };
        }

        return null;
    }

    /// <summary>
    /// Throws on non-contractive cycles: struct/fun → struct/fun (no Optional/Array break) and
    /// standalone arr → arr → self. Valid μ-types crossing Optional/Array from a struct field
    /// are accepted.
    /// </summary>
    private static void ThrowIfRecursiveTypeDefinition(TicNode node, Types.INamedTypeFieldRegistry namedTypeRegistry, bool isRecursion) {
        // optSourcedSeen[0] = any opt-sourced struct on the cycle path ⇒ repair (μX. opt(struct{…X…})).
        var optSourcedSeen = new bool[1];
        if (isRecursion && HasReachableOptSourcedStruct(node)) optSourcedSeen[0] = true;
        ThrowIfRecursiveReq(node.State, 1, fromStruct: false, optSourcedSeen);

        // Local fns capture namedTypeRegistry by closure (no globals).
        void ThrowIfRecursiveReq(ITicNodeState state, int mark, bool fromStruct, bool[] optSeen) {
            if (state is StateStruct s && s.IsOptionalSourced) optSeen[0] = true;
            switch (state)
            {
                case StateRefTo refTo:
                    ThrowIfNodeReq(refTo.Node, mark, fromStruct, optSeen);
                    break;
                case StateOptional:
                    break; // Optional always breaks recursion (none is the base case)
                case StateArray arr:
                    if (fromStruct)
                        break; // Valid: struct field → array → struct (named recursive type)
                    // Standalone array chain — check for arr(arr(...self...))
                    ThrowIfNodeReq(arr.ElementNode, mark, fromStruct: false, optSeen);
                    break;
                case StateCollection coll:
                    if (fromStruct)
                        break; // Valid: struct field → collection → struct (named recursive type)
                    ThrowIfNodeReq(coll.ElementNode, mark, fromStruct: false, optSeen);
                    break;
                case ICompositeState composite:
                    var isStruct = state is StateStruct;
                    for (int mi = 0; mi < composite.MemberCount; mi++)
                        ThrowIfNodeReq(composite.GetMember(mi), mark, fromStruct || isStruct, optSeen);
                    break;
                case ConstraintsState cs:
                    // F-bound is a contractive break. When a CS carries StructBound, the bound is
                    // by construction contractive (LiftMuTypes precondition C1: every back-edge
                    // crosses Optional/Array). Re-entering the bounded variable T means unfolding
                    // to a struct that ITSELF satisfies the contractive bound, so a back-edge
                    // closing through `RefTo(node-with-CS{StructBound})` is analogous to a
                    // StateOptional break — terminate the walk.
                    if (cs.HasStructBound)
                    {
                        if (cs.HasDescendant)
                            ThrowIfRecursiveReq(cs.Descendant, mark, fromStruct, optSeen);
                        if (cs.HasAncestor)
                            ThrowIfRecursiveReq(cs.Ancestor, mark, fromStruct, optSeen);
                        break; // do NOT recurse into S — it's the contractive boundary
                    }
                    if (cs.HasDescendant)
                        ThrowIfRecursiveReq(cs.Descendant, mark, fromStruct, optSeen);
                    if (cs.HasAncestor)
                        ThrowIfRecursiveReq(cs.Ancestor, mark, fromStruct, optSeen);
                    break;
            }
        }

        void ThrowIfNodeReq(TicNode node, int mark, bool fromStruct, bool[] optSeen) {
            if (node.State is StateStruct s && s.IsOptionalSourced) optSeen[0] = true;
            if (node.VisitMark == mark)
            {
                // Contractive closing edge: if we re-entered an Array node from inside a
                // struct field traversal (fromStruct=true), the closing back-edge of the
                // cycle crosses an Array constructor — algebraically equivalent to the
                // Optional break that the recursion case `case StateArray arr: if
                // (fromStruct) break` accepts as a valid μ-type. Symmetric handling: the
                // cycle-detection branch must accept the same shape, otherwise the
                // VisitMark trips BEFORE the StateArray case runs. Type
                // t = {v:int, kids:t[]} — `root.kids` traverses struct → kids field →
                // arr_node and back, crossing arr_node twice via the array constructor.
                if (fromStruct && node.GetNonReference().State is StateArray)
                    return;
                if (fromStruct && node.GetNonReference().State is StateCollection)
                    return;

                // If any struct on the cycle path is opt-sourced, OR if the cycle's struct itself
                // is opt-sourced somewhere in its full reachable subgraph, this is a contractive
                // iso-recursive type μX. opt(struct{…X…}) — repair by wrapping the back-edge in
                // StateOptional and continue. Declared non-Optional self-recursion
                // (`type t = {self:t}`) never sets IsOptionalSourced anywhere, so it still errors.
                bool isOptSourced = optSeen[0]
                    || (node.GetNonReference().State is StateStruct cs
                        && StructSubgraphIsOptSourced(cs));
                if (isOptSourced && TryRepairOptSourcedCycle(node))
                    return;

                // CS-internal slot promotion: cycle root is CS{Descendant=S} that closes
                // contractively — rebind Descendant → StructBound (Cardelli-Mitchell '89, TAPL §20).
                if (TryPromoteCSDescendantToStructBound(node))
                    return;

                var route = new HashSet<TicNode>();
                FindRecursionTypeRoute(node, route);
                throw TicErrors.RecursiveTypeDefinition(route.ToArray());
            }
            var prev = node.VisitMark;
            node.VisitMark = mark;
            ThrowIfRecursiveReq(node.State, mark, fromStruct, optSeen);
            node.VisitMark = prev;
        }

        // Wrap closing field values in StateOptional to restore the contractive break.
        // Triggered only for opt-sourced cycles (originated through `?.`).
        bool TryRepairOptSourcedCycle(TicNode cycleNode) {
            if (cycleNode.GetNonReference().State is not StateStruct cycleStruct)
                return false;
            // TypeName already set ⇒ previous repair stamped this struct and broke the cycle.
            if (cycleStruct.TypeName != null) return true;
            // Wrap EVERY closing edge — partial repair leaves remaining struct→struct cycles.
            bool anyRepaired = false;
            foreach (var f in cycleStruct.Fields) {
                var fieldNode = f.Value.GetNonReference();
                if (fieldNode != cycleNode && !ReachesStructWithoutOptionalBreak(fieldNode, cycleNode))
                    continue;
                if (fieldNode.State is StateOptional) {
                    anyRepaired = true;
                    continue;
                }
                // Same-name named struct = previous-pass break via named-type identity (valid).
                // Any other non-mutable state is a non-Optional concrete closure — must error.
                if (!fieldNode.IsMutable) {
                    if (fieldNode.State is StateStruct s && s.TypeName != null
                        && (cycleStruct.TypeName == null
                            || s.TypeName.Equals(cycleStruct.TypeName, StringComparison.OrdinalIgnoreCase))) {
                        anyRepaired = true;
                        continue;
                    }
                    return false;
                }
                var inner = TicNode.CreateTypeVariableNode(
                    "e" + fieldNode.Name + "'", fieldNode.State);
                inner.IsOptionalElement = true;
                fieldNode.State = new StateOptional(inner);
                anyRepaired = true;
            }
            if (!anyRepaired) return false;
            // TypeName LAST — setting it earlier flips IsMutable=false and blocks the wrap
            // assignments above (struct→opt fails the already-solved assertion).
            if (cycleStruct.TypeName == null && namedTypeRegistry != null) {
                var match = FindUniqueMatchingNamedType(cycleStruct, namedTypeRegistry);
                if (match != null) cycleStruct.TypeName = match;
            }
            return true;
        }

        // Rebind CS{Descendant=S} → CS{StructBound=S} when every closing path on S is
        // contractive — information-preserving F-bound refinement.
        bool TryPromoteCSDescendantToStructBound(TicNode node) {
            var nr = node.GetNonReference();
            if (nr.State is not ConstraintsState cs) return false;
            if (cs.HasStructBound) return false;
            if (!cs.HasDescendant) return false;
            if (cs.Descendant is not StateStruct s) return false;
            if (!StructDescendantClosesContractively(s, nr)) return false;
            cs.StructBound = s;
            cs.ClearDescendant();
            TraceLog.WriteLine($"  PromoteCSDescendantToStructBound: {nr.Name} cs.Descendant→cs.StructBound");
            return true;
        }

        static bool FindRecursionTypeRoute(TicNode node, ISet<TicNode> nodes) {
            if (!nodes.Add(node))
                return true;

            if (node.State is StateRefTo r)
                return FindRecursionTypeRoute(r.Node, nodes);

            if (node.State is ICompositeState composite)
            {
                for (int mi = 0; mi < composite.MemberCount; mi++)
                {
                    if (FindRecursionTypeRoute(composite.GetMember(mi), nodes))
                        return true;
                }
            }

            nodes.Remove(node);
            return false;
        }
    }


    /// <summary>
    /// True iff DFS from <paramref name="start"/> can reach <paramref name="target"/> through
    /// composite members WITHOUT crossing an Optional or Array constructor (non-contractive closure).
    /// </summary>
    internal static bool ReachesStructWithoutOptionalBreak(TicNode start, TicNode target) {
        var mark = NextMark();
        return CheckReachesNode(start, target, mark);

        static bool CheckReachesNode(TicNode node, TicNode target, int mark) {
            var nonRef = node.GetNonReference();
            if (nonRef == target) return true;
            if (nonRef.VisitMark == mark) return false;
            var prev = nonRef.VisitMark;
            nonRef.VisitMark = mark;
            try {
                return CheckReachesState(nonRef.State, target, mark);
            } finally {
                nonRef.VisitMark = prev;
            }
        }

        static bool CheckReachesState(ITicNodeState state, TicNode target, int mark) {
            switch (state) {
                case StateRefTo refTo:
                    return CheckReachesNode(refTo.Node, target, mark);
                case StateOptional:
                case StateArray:
                    return false; // contractive break
                case ICompositeState composite:
                    for (int i = 0; i < composite.MemberCount; i++)
                        if (CheckReachesNode(composite.GetMember(i), target, mark))
                            return true;
                    return false;
                case ConstraintsState cs:
                    if (cs.HasDescendant && CheckReachesState(cs.Descendant, target, mark)) return true;
                    if (cs.HasAncestor && CheckReachesState(cs.Ancestor, target, mark)) return true;
                    return false;
                default:
                    return false;
            }
        }
    }

    #region Finalize

    public static ITicResults Finalize(
        TicNode[] toposortedNodes,
        IReadOnlyList<TicNode> outputNodes,
        IReadOnlyList<TicNode> inputNodes,
        TicNode[] syntaxNodes,
        Dictionary<string, TicNode> namedNodes,
        bool ignorePreferred,
        Types.INamedTypeFieldRegistry namedTypeRegistry = null,
        bool isRecursion = true) {
        var typeVariables = new List<TicNode>();

        int genericNodesCount = 0;
        for (int i = toposortedNodes.Length - 1; i >= 0; i--)
        {
            var node = toposortedNodes[i];
            FinalizeRecursive(node, namedTypeRegistry, NextMark(), isRecursion);

            CollectLeafTypeVariables(node, typeVariables, TicVisitMarks.TypeVariableVisited);

            if (!node.IsSolved)
                genericNodesCount++;
        }

        if (genericNodesCount == 0)
            return new TicResultsWithoutGenerics(namedNodes, syntaxNodes);

        SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes, ignorePreferred);

        // After resolving useless generics, check if any real generics remain.
        // Recursion boundary placeholders (ConstraintsState.Empty inside named structs)
        // may have been left unresolved — resolve them now.
        // But only promote to concrete if NO ConstraintsState remains anywhere.
        {
            bool anyConstraints = false;
            // Check all sources of generics: typeVariables, namedNodes, syntaxNodes
            foreach (var tv in typeVariables)
                if (tv.State is ConstraintsState) { anyConstraints = true; break; }
            if (!anyConstraints)
                foreach (var kv in namedNodes)
                    if (kv.Value?.State is ConstraintsState) { anyConstraints = true; break; }
            if (!anyConstraints)
                foreach (var sn in syntaxNodes)
                    if (sn?.State is ConstraintsState) { anyConstraints = true; break; }
            if (!anyConstraints)
                return new TicResultsWithoutGenerics(namedNodes, syntaxNodes);
        }

        return new TicResultsWithGenerics(typeVariables, namedNodes, syntaxNodes);
    }

    private static void FinalizeRecursive(TicNode node, Types.INamedTypeFieldRegistry namedTypeRegistry, int mark, bool isRecursion) {
        if (node.VisitMark == mark)
            return;
        var prevMark = node.VisitMark;
        node.VisitMark = mark;

        if (node.State is StateRefTo refTo)
        {
            ThrowIfRecursiveTypeDefinition(refTo.Node, namedTypeRegistry, isRecursion);
            var originalOne = refTo.Node.GetNonReference();

            if (originalOne.State is ITypeState)
                node.State = originalOne.State;
            else if (originalOne != refTo.Node)
                node.State = new StateRefTo(originalOne);
        }

        if (node.State is ICompositeState composite)
        {
            ThrowIfRecursiveTypeDefinition(node, namedTypeRegistry, isRecursion);

            if (composite.HasAnyReferenceMember)
            {
                node.State = composite.GetNonReferenced();
                composite = (ICompositeState)node.State;
            }

            for (int mi = 0; mi < composite.MemberCount; mi++)
                FinalizeRecursive(composite.GetMember(mi), namedTypeRegistry, mark, isRecursion);

            node.FlattenNestedOptional();
        }
        node.VisitMark = prevMark;
    }

    private static void SolveUselessGenerics(
        IEnumerable<TicNode> toposortedNodes,
        IReadOnlyList<TicNode> outputNodes,
        IEnumerable<TicNode> inputNodes,
        bool ignorePreferred) {

        const int outputTypeMark = TicVisitMarks.OutputType;
        foreach (var outputNode in outputNodes)
            foreach (var outputType in outputNode.GetAllOutputTypes())
                outputType.VisitMark = outputTypeMark;

        foreach (var inputNode in inputNodes)
        {
            if (inputNode.State is StateFun stateFun)
            {
                var leafs = new List<TicNode>();
                stateFun.CollectNotSolvedContravariantLeafs(leafs);
                foreach (var leafNode in leafs)
                {
                    if (leafNode.VisitMark == outputTypeMark)
                        continue;
                    leafNode.VisitMark = outputTypeMark;
                    leafNode.State = ((ConstraintsState)leafNode.State).SolveContravariant();
                }
            }
            else
            {
                // Non-StateFun input — mark as signature only when it has actual constraints,
                // is composite, or has a typeclass-constraint ancestor (CompCs). Unconstrained
                // inputs without such an edge resolve normally to Any.
                var nr = inputNode.GetNonReference();
                bool isLeaf = nr.State is ICompositeState
                    || (nr.State is ConstraintsState ics && (ics.HasDescendant || ics.HasAncestor));
                if (!isLeaf)
                {
                    // CompCs ancestor adoption: input becomes the CompCs so call-site Pull
                    // can flow the literal's element type through to the body's return.
                    // CS.IsClearable is folded in (Push CompCs→CS propagation), surviving
                    // adoption from a single ancestor.
                    var carriedIsClearable = nr.State is ConstraintsState carryCs && carryCs.IsClearable;
                    for (int ai = 0; ai < nr.Ancestors.Count; ai++)
                    {
                        var ancNr = nr.Ancestors[ai].GetNonReference();
                        if (ancNr.State is SolvingStates.StateCompositeConstraints compCs)
                        {
                            if (carriedIsClearable && !compCs.IsClearable) {
                                var lifted = compCs.WithIsClearable(true);
                                nr.State = lifted ?? (ITicNodeState)compCs;
                            } else {
                                nr.State = compCs;
                            }
                            isLeaf = true;
                            break;
                        }
                    }
                }
                if (isLeaf)
                    MarkSignatureLeaves(inputNode, outputTypeMark);
            }
        }

        // Body-internal generics resolve via SolveCovariant (signature nodes skipped).
        foreach (var node in toposortedNodes)
        {
            if (node.VisitMark == outputTypeMark)
                continue;
            if (node.State is not ConstraintsState c)
                continue;
            node.State = c.SolveCovariant();
        }
    }

    private static void MarkSignatureLeaves(TicNode node, int mark) {
        var nr = node.GetNonReference();
        if (nr.VisitMark == mark) return;
        nr.VisitMark = mark;
        if (nr.State is ICompositeState composite)
            for (int mi = 0; mi < composite.MemberCount; mi++)
                MarkSignatureLeaves(composite.GetMember(mi), mark);
        // CompCs ElementNode is the typeclass's T — mark it so SolveCovariant doesn't collapse to Any.
        else if (nr.State is SolvingStates.StateCompositeConstraints compCs)
            MarkSignatureLeaves(compCs.ElementNode, mark);
    }

    #endregion


    private static void CollectNotSolvedContravariantLeafs(this StateFun fun, List<TicNode> result) {
        foreach (var argNode in fun.ArgNodes)
            CollectLeafConstraints(argNode, result);
    }

    private const int LeafCollectMark = TicVisitMarks.LeafCollect;

    private static void CollectLeafConstraints(TicNode node, List<TicNode> result) {
        var state = node.State;
        if (state is ICompositeState composite) {
            if (node.VisitMark == LeafCollectMark) return;
            var prev = node.VisitMark;
            node.VisitMark = LeafCollectMark;
            for (int mi = 0; mi < composite.MemberCount; mi++)
                CollectLeafConstraints(composite.GetMember(mi), result);
            node.VisitMark = prev;
            return;
        }
        if (state is StateRefTo)
            node = node.GetNonReference();
        if (node.State is ConstraintsState)
            result.Add(node);
    }


    public static IEnumerable<TicNode> GetAllLeafTypes(this TicNode node) =>
        node.State switch {
            ICompositeState composite => composite.AllLeafTypes,
            StateRefTo => new[] { node.GetNonReference() },
            _ => new[] { node }
        };

    private static void CollectLeafTypeVariables(TicNode node, List<TicNode> typeVariables, int visitMark) {
        var state = node.State;
        if (state is ICompositeState composite) {
            if (node.VisitMark == visitMark) return;
            node.VisitMark = visitMark;
            for (int mi = 0; mi < composite.MemberCount; mi++)
                CollectLeafTypeVariables(composite.GetMember(mi), typeVariables, visitMark);
            return;
        }
        if (state is StateRefTo)
            node = node.GetNonReference();
        if (node.VisitMark == visitMark) return;
        node.VisitMark = visitMark;
        if (node.Type == TicNodeType.TypeVariable && node.State is ConstraintsState)
            typeVariables.Add(node);
    }

    private static IEnumerable<TicNode> GetAllOutputTypes(this TicNode node) =>
        node.State switch {
            StateFun fun => fun.RetNode.GetAllOutputTypes(),
            StateArray array => array.AllLeafTypes,
            StateStruct @struct => @struct.AllLeafTypes,
            StateOptional opt => opt.AllLeafTypes,
            StateRefTo => node.GetNonReference().GetAllOutputTypes(),
            _ => new[] { node }
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintTrace(IEnumerable<TicNode> nodes) {
#if DEBUG
        if (!TraceLog.IsEnabled)
            return;

        var alreadyPrinted = new HashSet<TicNode>();

        void ReqPrintNode(TicNode node) {
            if (node == null)
                return;
            if (!alreadyPrinted.Add(node))
                return;
            if (node.State is StateArray arr)
                ReqPrintNode(arr.ElementNode);
            node.PrintToConsole();
        }

        foreach (var node in nodes)
            ReqPrintNode(node);
#endif
    }
}
