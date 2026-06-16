# Algorithm

> **Scope**: the TIC solver — building the constraint graph, propagating constraints, materializing concrete types.
>
> **Phases**: Build → Toposort → Pull → Push → PropagatePreferred → Destruction → Finalize.

## 1. Overview

TIC consumes an AST (output of NFun's parser) and produces a typed graph: every expression node has an inferred concrete type. The algorithm runs in 7 phases applied sequentially.

```
Input:   AST + dialect settings + apriori types
Phase 1: Build         — translate AST to constraint graph
Phase 2: Toposort      — order nodes for streaming propagation
Phase 3: Pull          — propagate constraints from descendants to ancestors
Phase 4: Push          — propagate constraints from ancestors to descendants
Phase 5: PropagatePref — broadcast Preferred metadata
Phase 6: Destruction   — materialize concrete types from intervals
Phase 7: Finalize      — sanity checks, generic resolution
Output:  TicResults    — type per node + generic instantiations
```

Phases 3–6 may iterate internally (eager re-Pull, fixed-point Push).

## 2. Phase 1 — Build

The Build phase translates each AST construct into a `TicNode` with appropriate state. See [`Graph.md`](Graph.md) for the node/state taxonomy.

### Construct-to-graph mapping (representative)

| Construct | Created nodes and edges |
|---|---|
| Integer literal `1` | node with state `ConstraintsState{D=U8, A=Real, P=I32}` |
| Real literal `1.0` | node with state `StatePrimitive.Real` |
| Variable reference `x` | node aliased to `x`'s definition node via `StateRefTo` |
| Array literal `[a,b,c]` | array node + edges from each element node (covariant element) |
| Lambda `rule(x) = body` | `StateFun` with argument and return slots |
| Function call `f(a,b)` | call result node + slots typed per `f`'s signature + edges from `a, b` to slots |
| Conditional `if c then a else b` | edges from `a, b` to result node; edge from `c` to a Bool slot |
| Struct literal `{x:1, y:true}` | `StateStruct` (frozen) with field nodes |
| Field access `s.x` | open `StateStruct` (`{x: ?}`) with edge from `s` |

For each function call, TIC creates fresh generic-parameter slots per call site (Damas-Milner let-polymorphism instantiation).

### Output

A graph with:
- Every AST node has a corresponding TIC node.
- Type constraints encoded as edges (descendant → ancestor: "this descendant fits in this ancestor").
- Generic function call sites have per-site instantiated slots.

## 3. Phase 2 — Toposort

Sort TIC nodes such that descendants precede ancestors (within Pull's processing direction). Cycles are handled by **strongly-connected-component (SCC) merge**: nodes in a cycle are merged into one (their states union via Layer-1 commit).

Output: a topological order over the (cycle-collapsed) graph for streaming Pull.

Implementation: `NodeToposort` (Tarjan SCC + topological sort).

## 4. Phase 3 — Pull

**Goal**: for every node `c`, ensure `c.State` reflects all information from c's descendants.

**Two-phase Pull**:
- **Phase 3a (None-only)**: for every descendant edge with `descendant.State = None`, set `c.IsOptional = true` flag. None must be absorbed FIRST because the IsOptional flag affects LCA semantics in Phase 3b.
- **Phase 3b (main)**: walk nodes in toposort order. For each node, walk its ancestor list and invoke `Apply(ancestor, descendant)` per the [`ApplyCells.md`](ApplyCells.md) truth table.

### Streaming vs worklist

Current implementation: **streaming**. Each node visited once per phase. When an Apply cell adds a new edge (`AddAncestor`) after the source was already visited, the new edge would not propagate. Mitigated by:
- **Eager re-Pull** (`PullConstraintsForNode`) after specific cross-Apply cells.
- **Identity guards** in `TransformTo*OrNull` helpers.
- `PropagatePreferredAcrossFallback` at CompCs AddAncestor fallback boundaries.

The principled fix is **worklist Pull** (see [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md)) — spec-closed, implementation pending.

### Apply cells

See [`ApplyCells.md`](ApplyCells.md) for the canonical truth table.

Pattern summary by state-pair:

| Ancestor | Descendant | Outcome |
|---|---|---|
| Prim | Prim | compatibility check |
| CS | Prim | `ApplyAncestorConstrains` (LCA-narrow descendant) |
| CS | CS | bidirectional merge per [`TypeSystem.md`](TypeSystem.md) §5 |
| Composite | Composite (same shape) | element-axis edge propagation |
| Composite | CS | `TransformToXOrNull` lifts CS to a snapshot composite |
| CompCs | Coll/Arr/Map | element merge or AddAncestor fallback (see [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md) §4) |

### Optional guard rule

`Apply(non-Opt composite ancestor, IsOptional CS descendant)` returns **false** in Pull. This rejects the unsound implicit unwrap `opt(T) ≤ T`. In Push, the same case **wraps the descendant in Optional** — Push permits the wrap (sound in that direction).

## 5. Phase 4 — Push

**Goal**: for every node `d`, ensure `d.State` reflects all information from `d`'s ancestors.

**Symmetric to Pull**, but propagates DOWNWARD. Per node, walk descendant list, invoke `Apply(ancestor, descendant)` per Push semantics.

**Push doesn't add edges**: only validates compatibility and updates state. This makes Push immune to the "streaming toposort" issue affecting Pull.

See [`ApplyCells.md`](ApplyCells.md) §2 for Push cells.

## 6. Phase 5 — PropagatePreferred

**Goal**: broadcast `Preferred` metadata across CS nodes.

Preferred is not a constraint — it is a **hint for resolution**. The propagation step ensures that any CS node connected to a node with a Preferred receives it (if compatible).

Implementation: a single forward pass over toposorted CS nodes; for each node with `Preferred = null`, check if any descendant has a compatible Preferred and copy it. See [`Advanced/Preferred.md`](Advanced/Preferred.md).

## 7. Phase 6 — Destruction

**Goal**: pick a concrete type from each constraint interval.

For each node in reverse toposort order:
1. If the node already has a solved state (`StatePrimitive`, concrete composite), no-op.
2. Otherwise, apply per-state Destruction rules to materialize:
   - `ConstraintsState [D..A]`: pick by `SolveCovariant` (use D), `SolveContravariant` (use A), or Preferred default.
   - `StateOptional`: recursively destruct element, wrap in Optional.
   - `StateCollection`, `StateMap`, etc.: recursively destruct components.
   - `CompCs`: collapse via `TryCollapseToPoint`, then destruct the resulting concrete state.

### Sub-phases

- **MaterializeOptionalFlags**: convert CS with `IsOptional=true` to actual `StateOptional`.
- **FlattenNestedOptional**: collapse `opt(opt(T)) → opt(T)`.
- **WrapAncestorInOptional / WrapDescendantInOptional**: adjust cross-pairs that need Optional wrapping.

See [`ApplyCells.md`](ApplyCells.md) §3 for Destruction cells.

### Resolution strategy

Pick the most specific type that satisfies all constraints:

- **Covariant position** (e.g., function return): use `Concretest` (smallest D, fall back to Preferred, fall back to type-class default).
- **Contravariant position** (e.g., function argument): use `Abstractest` (largest A, fall back to Any).
- **Invariant position** (collection element): both bounds must converge; if not, error.

## 8. Phase 7 — Finalize

- Verify every node has a concrete final type.
- For generic functions, record per-call-site generic instantiations in `TicResultsWithGenerics`.
- Detect remaining unsolved nodes (would be a TIC bug; error out).

## 9. Termination

Bound: `O((|V| + |E|) × D(expr))` for Pull, `O(|V| + |E|)` for Push, `O(|V| × D(expr))` for Destruction. Where `D(expr)` = max composite-nesting depth of the input expression (≤ `|expr|`).

Full bound + proof: [`Proofs.md`](Proofs.md) §P2.

## 10. Determinism

Same input expression ⇒ same output types. Toposort tie-breaking is deterministic (insertion order from Build). Apply cells are pure functions of their input state. `LcaOrShareIdentity` side-effect is deterministic (union-find merge).

Full argument: [`Proofs.md`](Proofs.md) §P4.

## See also

- Canonical Apply truth tables: [`ApplyCells.md`](ApplyCells.md)
- Formal proofs of soundness, termination, monotonicity: [`Proofs.md`](Proofs.md)
- Graph structure: [`Graph.md`](Graph.md)
- Preferred propagation details: [`Advanced/Preferred.md`](Advanced/Preferred.md)
- F-bounded polymorphism (named recursive types): [`Advanced/PushReform.md`](Advanced/PushReform.md)
- SimplePrimitiveSolver fast-path: [`Advanced/SimplePath.md`](Advanced/SimplePath.md)
