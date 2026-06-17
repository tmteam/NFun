# TIC Formal Proofs — Soundness, Termination, Monotonicity, Determinism

> **Status**: v1. Five proof obligations + formal identification of debt #16.
> Parent: [`Algorithm.md`](Algorithm.md), [`Algebra/README.md`](Algebra/README.md), [`TypeSystem.md`](TypeSystem.md), [`Advanced/Preferred.md`](Advanced/Preferred.md).
>
> **Purpose**: state and prove the algebraic / algorithmic properties TIC is supposed to satisfy. Section §3 (P3 Monotonicity) formally identifies the property debt #16 violates, enabling a precise targeted fix.

## 0. Notation

Let `G` be a TIC graph: a set of nodes with TicNode-typed states, plus a set of directed `descendant → ancestor` edges. Each node has a unique identity; nodes can share state via `StateRefTo`.

For a node `n` and a moment in execution (after phase `φ`):
- `n.State_φ` is its state after `φ` completes.
- `n.Anc_φ` and `n.Desc_φ` are its ancestor / descendant edge sets after `φ`.

For a CS node `c`:
- `c.D` = descendant primitive bound (lower).
- `c.A` = ancestor primitive bound (upper).
- `c.P` = preferred (metadata).
- `c.IsOpt` = optional flag.
- `c.IsCmp` = comparable flag.
- `c.SB` = struct bound (F-bound).

The "information content" of a CS node is the 6-tuple `Info(c) = (D, A, P, IsOpt, IsCmp, SB)`.

An ordering `Info(c) ⊑ Info(c')` holds iff:
- `c.D ≤ c'.D` (or both null), `c.A ≥ c'.A` (or both null) — the interval shrinks or stays
- `c.P` is preserved: if `c.P ≠ null` then `c'.P = c.P` (preferred not lost)
- flags are monotone: `c.IsOpt ⇒ c'.IsOpt`, `c.IsCmp ⇒ c'.IsCmp`, `c.SB ⇒ c'.SB`

Informally: `Info(c) ⊑ Info(c')` means `c'` carries at least as much constraint information as `c`.

The phases are: **Build → Toposort → Pull → Push → PropagatePreferred → Destruction → Finalize**.

---

## P1. Soundness

**Statement**. For every input expression `e` and every graph `G_e` built by Build, if Finalize succeeds and assigns type `T` to a node `n`, then `T` satisfies all constraints in `G_e`.

Formally: if `T` is the final type of `n`, then for every edge `n → m` (descendant → ancestor) with final type `T'` of `m`, `T ≤_T T'` holds (the descendant type fits into the ancestor type per `Algebra/BaseOperators.md`).

**Proof strategy**. Soundness reduces to per-cell soundness via composition:
if each Apply cell preserves the constraint relation `T ≤_T T'` on every
edge it touches (or correctly defers), then by induction on the toposort
order, the global graph relation is preserved at Finalize.

**Per-cell soundness — representative proofs**. Three representative cells
demonstrate the pattern; the rest follow analogously by the truth table in
[`ApplyCells.md`](ApplyCells.md).

### 1.1. Representative: `Apply(CS c, Prim p)` in Pull

This is the simplest case — a CS ancestor receiving a concrete primitive descendant.

```csharp
// Pull cell: CS × Prim (ApplyCells.md §1.2)
ApplyAncestorConstrains(ancestorNode, c, p);  // = ancCopy.AddDescendant(p); SimplifyOrNull
```

**Soundness claim**: after this cell, `p ≤ c.D_post` (descendant relation
preserved).

**Proof**.
- `c.AddDescendant(p)` executes `c.D_post ← (c.D_pre = null) ? p.Concretest : c.D_pre.Lca(p)`.
- Case 1 (`c.D_pre = null`): `c.D_post = p.Concretest`. Since `p` is a concrete primitive, `p.Concretest = p`. So `p ≤ c.D_post = p`. Reflexivity gives `p ≤ p`. ✓
- Case 2 (`c.D_pre ≠ null`): `c.D_post = c.D_pre.Lca(p)`. By LCA-as-LUB property (Algebra.md §LCA — наименьшая верхняя грань): `p ≤ Lca(c.D_pre, p) = c.D_post`. ✓

In both cases the descendant relation `p ≤ c.D_post` is preserved. ∎

### 1.2. Representative: `Apply(Arr ancArr, Arr descArr)` in Pull

A same-shape composite cell with element-axis propagation.

```csharp
// Pull cell: Arr × Arr (ApplyCells.md §1.5)
if (descArr.ElementNode != ancArr.ElementNode)
    descArr.ElementNode.AddAncestor(ancArr.ElementNode);
descendantNode.RemoveAncestor(ancestorNode);
```

**Soundness claim**: after this cell, `descArr.Element ≤ ancArr.Element` is
ensured (the constraint relation between element types).

**Proof**.
- The cell adds an edge `descArr.ElementNode → ancArr.ElementNode`.
- This edge is a Pull constraint: the descendant element must fit into the
  ancestor element.
- The element nodes are processed by the rest of Pull. By the recursive
  soundness of element cells (induction on type depth), the constraint
  `descArr.Element ≤ ancArr.Element` holds at Finalize.
- StateArray covariance: `descArr ≤ ancArr ⟺ descArr.Element ≤ ancArr.Element`.
  Therefore the original Pull edge constraint is satisfied. ✓

**RemoveAncestor**: the original descendant→ancestor edge is removed because
its information has been transferred to the element-level edge. No
constraint loss. ∎

### 1.3. Representative: Destruction `Apply(Prim ancestor, CS descendant)` materialization

The Destruction cell that materializes a CS descendant to a concrete
primitive when the ancestor is concrete (most common case).

```csharp
// Destruction cell: Prim × CS (ApplyCells.md §3)
if (!descendant.CanBeConvertedTo(ancestor)) return false;
descendantNode.State = ancestor;  // materialize
ancestorNode.RemoveAncestor — N/A, no edge on this side
```

**Soundness claim**: post-materialization, the descendant's final type
satisfies all its accumulated constraints.

**Proof**.
- Precondition `descendant.CanBeConvertedTo(ancestor)`: by definition of
  `CanBeConvertedTo`, `descendant.D ≤ ancestor` AND `ancestor ≤ descendant.A`
  (the ancestor primitive falls within descendant's [D..A] interval).
- Setting `descendant.State = ancestor` makes the descendant's final type
  equal to `ancestor`.
- For every Pull edge `x → descendant` that was processed: `x.State ≤
  descendant.D ≤ ancestor`. So `x ≤ descendant.Finalized`. ✓
- For every Pull edge `descendant → y`: `descendant.A ≥ ancestor`, so
  `descendant.Finalized = ancestor ≤ descendant.A ≤ y.D ≤ y.Finalized`. ✓

The materialization preserves all constraints. ∎

### 1.4. CompCs cross-Apply pattern

The cells `Apply(CompCs anc, Coll desc)` / `Apply(CompCs anc, Arr desc)` / their
reverse and Map variants use the try-MergeInplace / fallback-to-AddAncestor
structure (see [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md)
§4 element-propagation strategy). Both paths must be sound for P1.

**Path 1 — MergeInplace succeeds**:

```
MergeInplace(anc.elem, desc.elem)
// element nodes unified into one identity; combined CS state via GetMergedStateOrNull
remove the wrapper-level descendant→ancestor edge
```

**Soundness claim**: after the merge, the original element-axis constraint
`desc.elem ≤ anc.elem` is realized as identity (`desc.elem ≡ anc.elem`).
Identity entails the constraint trivially.

**Proof**: identity-sharing soundness (§5, P5) covers this case. The combined
constraint set is the union of pre-merge sets; any final type satisfying the
union satisfies both pre-merge sets. ✓

**Path 2 — MergeInplace fails (`CanMergeStates = false`), AddAncestor fallback**:

```
desc.elem.AddAncestor(anc.elem)            // new Pull edge
PropagatePreferredAcrossFallback(...)       // restores P3a Preferred axis
PullConstraintsForNode(anc.elem)            // eager re-Pull
```

**Soundness claim**: after the fallback, the new edge `desc.elem → anc.elem`
realizes the element-axis constraint as a Pull edge to be processed by the
rest of Pull. Subsequent Pull passes that visit `desc.elem` will fire
`Apply(anc.elem, desc.elem)` per §1.1 / §1.2.

**Proof**: AddAncestor is the standard Pull-edge construction; its soundness
follows by the recursive argument used in §1.2 (element-propagation pattern).
The constraint `desc.elem ≤ anc.elem` is preserved as an edge whose
contribution is realized when that edge is processed by `Apply(CS, CS)` (§1.1)
or `Apply(Composite, Composite)` (§1.2) at the element level. ✓

Soundness of Path 2 does **not** depend on P3 Monotonicity holding for every
dimension — P1 only requires that the final type satisfy the constraints
**that have been processed**. P3 quantifies what information is propagated;
P1 quantifies what relations are preserved. The Descendant axis gap discussed
in §3.6 (P3b conjectured open) does not break soundness because the edge
itself is present — what may be incomplete is the eager pre-population of
`anc.elem.D`, not the constraint relation.

### 1.5. Remaining cells

The other cells in [`ApplyCells.md`](ApplyCells.md) follow one of the four
patterns above:
- **CS-modification pattern** (§1.1): add/strengthen descendant; LCA-as-LUB
  guarantees soundness.
- **Element-propagation pattern** (§1.2): add element-axis edge; recursive
  cell soundness guarantees the wrapper's soundness.
- **Materialization pattern** (§1.3): replace state; precondition ensures
  constraint satisfaction.
- **CompCs cross-Apply pattern** (§1.4): try-merge / fallback-edge dispatch;
  both paths sound as shown above.

Per-cell mapping to pattern is given by the row-class column in
[`ApplyCells.md`](ApplyCells.md) §1–§3. The Optional asymmetry
(`Apply(non-Opt Composite, Opt CS)` — reject in Pull, wrap in Push) is a
precondition check covered by §1.3 (Pull: rejection is a no-op edge removal,
trivially sound) and by P6 (Push wrap, §6.x). The Fun contravariance cell
follows §1.2 with argument-axis edges reversed.

`LcaOrShareIdentity`'s side-effect: post-merge, both element nodes refer to
one identity. This is sound by P5 Identity-sharing Soundness (§5).

P1 holds. ∎

---

## P2. Termination

**Statement**. Pull, Push, Destruction each terminate in time bounded by a
function of `|G|` (number of nodes) and `D(expr)` (formally defined below).

### 2.0. Formal definition of D(expr)

Let `expr` be the input expression. Define `D(expr)` recursively over the AST:

```
D(primitive_literal)              = 0
D(variable)                       = 0
D([e₁, ..., eₙ])                  = 1 + max(D(eᵢ))            // array literal
D(list(e₁, ..., eₙ))              = 1 + max(D(eᵢ))            // list factory
D({f₁: e₁, ..., fₙ: eₙ})          = 1 + max(D(eᵢ))            // struct literal
D(__mkMap(p₁, ..., pₙ))           = 1 + max(D(pᵢ))            // map factory
D(rule(arg) = body)               = 1 + D(body)               // lambda
D(e₁.method(e₂, ..., eₙ))         = 1 + max(D(eᵢ))            // call
D(if c then a else b)             = max(D(c), D(a), D(b))
```

Intuitively `D(expr)` is the **maximum composite nesting depth** in the
input expression's type structure. For a primitive-only expression `D = 0`;
for `[[[[]]]]` (array of array of array of array) `D = 4`; for the
nested-byte-upcast test (`[[0,1],[2,3],[x]].map(rule it.map(rule it+1).sum()).sum()`)
`D = 5` (outer-list → inner-list → map → lambda → body).

Crucially, `D(expr)` is **bounded by `|expr|`** (the source length) since each
nesting level requires at least one syntactic construct. So `D = O(|expr|)`.

### 2.1. Streaming Pull termination

Pull processes nodes in toposorted order, each visited once. For each node, walking its ancestor list and invoking Apply takes O(deg(n)) per node, summing to O(|E|) over all nodes.

**With eager re-Pull**: when an Apply cell calls `PullConstraintsForNode(x)`, it re-fires Pull on `x` and recursively walks `x`'s ancestors / composite members (per the eager re-Pull loop).

Termination invariant: each node has a `VisitMark` set on first visit. `PullRec` returns early if the mark matches the current pass's mark. So within one outer Pull pass, each node is processed at most once even with eager re-Pull.

**Bound**: `O((|V| + |E|) × D(expr))`. The factor `D(expr)` arises because
eager re-Pull cascades through composite members; the cascade depth is at
most `D(expr)` (each recursion step descends one composite level).

### Push termination

Push doesn't add new edges. Per node, Push fires once. Bound: O(|V| + |E|).

### Destruction termination

Destruction recursively destructures composites. Each composite has finite arity; the recursion depth equals the maximum type-tree depth in the graph (bounded statically by the input expression).

`FlattenNestedOptional` fires at most O(D) times total per graph (each flatten reduces nesting depth by 1).

Bound: O(|V| × D).

### Notes on composite states

- `LcaOrShareIdentity`'s MergeInplace side-effect merges identity classes via union-find-like graph mutation. Each MergeInplace reduces the number of equivalence classes by 1, so at most O(|V|) merges total per graph.
- Pair-struct synthesis in `ForwardPullCompCsStateMap` allocates one struct node per Apply call. Bounded by number of CompCs × StateMap cell invocations, ≤ |E|.

P2 holds.

---

## P3. Monotonicity of Pull — split into P3a (closed) + P3b (open)

> **Per-axis decomposition**. The "Pull does not lose information" property
> is stated separately for each of the 6 dimensions of `Info(c)`. The
> Preferred axis is closed (**P3a**, proven below). The Descendant axis
> remains open (**P3b**, debt #16 + debt #10).

**Statement (intuitive)**. Pull does not lose information on a per-dimension
basis. For every node, the post-Pull state's information per dimension is at
least as strong as the pre-Pull state's PLUS the descendant's contribution
along the same dimension.

**Formal statement (full P3)**.

> For every CS node `c` and every Pull edge `d → c` that is processed during
> Pull, after Pull completes:
>
> ```
> ∀ dim ∈ {D, A, P, IsOpt, IsCmp, SB}:
>     Info(c)_post.dim ⊒_dim  Info(c)_pre.dim ⊔_dim Info(d)_processed.dim
> ```
>
> where `⊔_dim` is the algebraic join for that dimension:
> - **D, A** (interval bounds): `Lca` on D (descendant join, narrows), `Gcd` on A (ancestor meet, narrows)
> - **P** (Preferred): bidirectional fill on null — `null ⊔ x = x`
> - **IsOpt, IsCmp** (flags): boolean OR
> - **SB** (StructBound): `GcdBound` (field-set union plus self-RefTo rewire)

Splitting per-dimension:

- **P3a (Preferred axis)** — **CLOSED for direct CS×CS edges and for CompCs cross-Apply fallback edges**.
- **P3b (Descendant axis)** — closed for CS×CS direct edges; for CompCs cross-Apply fallback edges **conjectured open** without a proven TIC-level counterexample (§3.6). Closure path: worklist Pull (debt #10).
- **P3c (Ancestor / IsOpt / IsCmp / SB axes)** — closed by inspection of the per-cell tables in [`ApplyCells.md`](ApplyCells.md) (no known violations).

### 3.1. Proof of P3 for CS × CS direct edges (per-dimension, rigorous)

**Lemma 3.1**: For every CS ancestor `c` and CS descendant `d`, executing
`Apply(CS c, CS d)` in PullConstraintsFunctions preserves P3 on all 6 dimensions.

**Proof**. The Apply cell executes (PullConstraintsFunctions §1.2 row "CS × CS"):

```csharp
var ancCopy = c.GetCopy();
ancCopy.AddDescendant(d.Descendant);                         // (1)
if (ancCopy.Preferred == null && d.Preferred != null)        // (2)
    ancCopy.Preferred = d.Preferred;
if (d.Preferred == null && c.Preferred != null)              // (3)
    d.Preferred = c.Preferred;
if (d.IsOptional) ancCopy.AddDescendant(StatePrimitive.None); // (4)
// StructBound merge via GcdBound                             (5)
ancCopy = ancCopy.SimplifyOrNull();                           // (6)
c.State = ancCopy;
```

Verify per dimension:

**D (Descendant)**: line (1) calls `AddDescendant(d.Descendant)`. By
ConstraintsState.AddDescendant semantics: if `c.D = null`, `ancCopy.D ←
d.D.Concretest`. If `c.D ≠ null`, `ancCopy.D ← c.D.Lca(d.D)`. In both cases
`ancCopy.D ⊒_Lca {c.D, d.D}`. ✓

**A (Ancestor)**: not modified by the cell. `ancCopy.A = c.A`. The contribution
from `d.A` flows through subsequent Push (P6, see §6), not Pull. For Pull-only
monotonicity: `ancCopy.A = c.A ⊒_Gcd c.A` trivially. ✓

**P (Preferred)**: lines (2)+(3) implement the bidirectional copy. Case split:
- Case A: `c.P ≠ null, d.P ≠ null`. Both (2) and (3) guards fail. `ancCopy.P = c.P`. No information loss (we keep our existing Preferred).
- Case B: `c.P = null, d.P ≠ null`. (2) fires: `ancCopy.P ← d.P`. ✓ Preferred propagated up.
- Case C: `c.P ≠ null, d.P = null`. (3) fires: `d.P ← c.P` (downward). `ancCopy.P = c.P`. ✓
- Case D: both null. No-op. ✓ <br>
In all cases `ancCopy.P ⊒_{null-fill} {c.P, d.P}`. ✓

**IsOpt**: line (4) propagates: if `d.IsOptional`, `ancCopy.AddDescendant(None)`,
which (via ConstraintsState.AddDescendant special-case) sets
`ancCopy.IsOptional = true`. Therefore `ancCopy.IsOpt = c.IsOpt OR d.IsOpt`. ✓

**IsCmp**: `AddDescendant` propagates IsComparable via SimplifyOrNull's
type-class refinement. By inspection of ConstraintsState.SimplifyOrNull: if
either side's Descendant or Ancestor narrows to a Comparable-only type,
IsCmp is set. Therefore `ancCopy.IsCmp ⊒_OR {c.IsCmp, d.IsCmp}`. ✓

**SB**: line (5) — `if (d.HasStructBound) ancCopy.StructBound = GcdBound(c.SB,
d.SB, ...)` — F-bound monotone meet. `GcdBound` is associative + commutative
on the field-union lattice (proven in PushReform.md §F-bound algebra). Therefore
`ancCopy.SB ⊒_GcdBound {c.SB, d.SB}`. ✓

Therefore `Info(ancCopy) ⊒ Info(c)_pre ⊔ Info(d)` for ALL 6 dimensions. ∎

### 3.2. Proof of P3 for composite same-shape cells (structural induction)

**Lemma 3.2**: For each same-shape composite cell `Apply(X anc, X desc)`
where X ∈ {Arr, Coll, Map, Fun, Struct, Opt}, P3 holds for the
state-level information of the wrapper, given P3 holds for the element /
component cells.

**Proof** by structural induction on type depth.

**Inductive hypothesis (IH)**: P3 holds for all Apply cells operating on
nodes whose state has type-tree depth `< δ`.

**Base case (δ = 0)**: Primitive Apply cells (P3 trivially holds — primitive
types carry no constraint information beyond the type itself; P1 Soundness
covers this).

**Inductive step (δ ≥ 1)**: For `Apply(Arr ancArr, Arr descArr)` at depth δ:
- The cell adds `descArr.ElementNode.AddAncestor(ancArr.ElementNode)` (ApplyCells.md §1.5 row "Arr × Arr").
- This edge connects nodes at depth `δ - 1`.
- By IH, the per-element Pull cell preserves P3 for the element nodes.
- The wrapper Arr state's information is determined entirely by its element node's information (StateArray has no other dimensions).
- Therefore `Info(ancArr)_post.element ⊒ ...`, and since the wrapper carries
  no additional info, `Info(ancArr)_post ⊒ Info(ancArr)_pre ⊔ Info(descArr)`. ✓

The same argument applies to Coll (one element node), Map (two element nodes,
each handled independently), Fun (arg nodes contravariant, ret node
covariant), Struct (per-field), Opt (one element node).

For Fun contravariant arg edges: `ancFun.ArgNodes[i].AddAncestor(descFun.ArgNodes[i])`
— this is the **reversal direction** (anc → desc on arg). The Apply cell
flips the relationship; the wrapper-state info is still preserved by IH
applied to the arg edges in the flipped direction. ✓

For Struct width propagation: extra fields on descendant are added to
ancestor's mutable state. This strictly enlarges the field set, increasing
info content. Monotone. ✓ ∎

### 3.3. Proof of P3 for composite × CS (transform cells)

**Lemma 3.3**: For each transform cell `Apply(X anc, CS desc)` where the
descendant has the structure required for X (e.g., `desc.HasDescendant of
type X`), P3 holds.

**Proof**. The transform helper (`TransformToArrayOrNull`,
`TransformToCollectionOrNull`, `TransformToMapOrNull`, `TransformToFunOrNull`,
`TransformToStructOrNull`) constructs a state of the target type X using
information drawn from `desc`. By construction:

- The element/component nodes in the result are either:
  - Reused from `desc`'s existing snapshot (TransformTo* perf optimization,
    debt #15) — identity-shared, all info preserved by reference.
  - Freshly allocated with descendant info copied (e.g., the
    `c.AddDescendant(elemTs)`).
- The wrapper's state mutations are applied (AddAncestor on elements after
  guard-check; ancestor node state assignment).

The information transfer from `desc` to the transform result is direct:
- `desc.D` (if it's a composite Descendant matching X's shape) feeds into
  result's element nodes.
- `desc.IsOptional` is wrapped via StateOptional if applicable.
- `desc.A` flows through ApplyAncestorConstrains for the wrapper.

By per-dimension inspection of TransformTo* helpers (matching Lemma 3.1
structure for the CS side), `Info(result) ⊒ Info(desc)` along each dimension
that survives the transform. The wrapper's pre-existing info is also preserved
(the cells do not overwrite ancestor.State.D or .A without LCA/GCD).

Therefore P3 holds for all transform cells. ∎

### 3.4. **VIOLATION at CompCs × Coll (debt #16) — P3 fails along Preferred axis pre-fix, Descendant axis post-fix**

The cell `ForwardPullCompCsSc(CompCs ancestor, Coll sc)`:

```csharp
if (CanMergeStates(ancestor.ElementNode, sc.ElementNode))
    SolvingFunctions.MergeInplace(ancestor.ElementNode, sc.ElementNode);
else
    sc.ElementNode.AddAncestor(ancestor.ElementNode);   // ← fallback
SolvingFunctions.PullConstraintsForNode(ancestor.ElementNode);
```

**Two paths**:
- **Path 1 (MergeInplace succeeds)**: ancestor.ElementNode and sc.ElementNode are merged via `GetMergedStateOrNull`, which propagates all 6 dimensions including Preferred (mirrors CS × CS LCA semantics). **P3 holds**.
- **Path 2 (MergeInplace fails, AddAncestor fallback)**: An edge `sc.ElementNode → ancestor.ElementNode` is added. Then eager re-Pull fires on `ancestor.ElementNode`.

**The eager re-Pull walks `ancestor.ElementNode`'s ancestor edges**, not its descendant edges (per `PullRec` in SolvingFunctions the eager-rePull loop).

So the eager re-Pull processes edges going UP from `ancestor.ElementNode`, but NOT the new edge coming IN from `sc.ElementNode` (which is a descendant edge).

**Consequence**: `sc.ElementNode`'s Preferred metadata never reaches `ancestor.ElementNode` along Path 2.

#### Formal counterexample (the byte→real upcast test)

Consider the lang-mode failing test `LangMirror_NestedByteUpcastMap_RealResult`:

```fun
fun f():
    x:byte = 5
    return [[0,1],[2,3],[x]].map(rule it.map(rule it+1).sum()).sum()
out:real = f()
```

Trace key nodes after Phase 1 + Phase 2 Pull:
- Inner array element node `e₁` with state `CS{D = U24, A = Re, P = I32}` (literals + byte upcast).
- Outer-map lambda input node `T0` with state `CS{D = null, A = null, P = null}` initially (generic, no preferred).
- Edge `e₁ → T0` added via `ForwardPullCompCsSc` Path 2 (CanMergeStates fails because outer map's CompCs element shape conflicts with intermediate function types).
- Eager re-Pull on `T0` (ancestor element) walks T0's ancestors. T0's ancestors are other Pull edges from later in the expression chain; **none of those convey `e₁`'s Preferred**.

After Pull completes:
- `Info(T0)_post = CS{D = null, A = null, P = null}` — unchanged.
- `Info(e₁) = CS{D = U24, A = Re, P = I32}`.
- `Info(T0)_pre ⊔ Info(e₁) = CS{D = U24, A = Re, P = I32}`.
- `Info(T0)_post ⊐ Info(T0)_pre ⊔ Info(e₁)` is **FALSE** (Preferred I32 not propagated).

**P3 is violated for the CompCs × Coll cell, Path 2 (AddAncestor fallback) for the Preferred dimension.**

### Why this manifests as a runtime cast error

At Finalize time, `T0` has no Preferred and an unconstrained interval. SolveCovariant defaults to the ancestor (`Real`). The function body `rule it+1` materializes the addition as `Real + Real`. But the actual runtime values are bytes/ints from the input array. The cast from byte/int to double during result extraction throws `InvalidCastException`.

If P3 held, `T0.P = I32` after Pull, and SolveCovariant would pick `I32` (the preferred), materializing the body as `Int32 + Int32`. The runtime would correctly produce ints, sum them, and return.

### The fix — `PropagatePreferredAcrossFallback`

Applied in the three CompCs cross-Apply cells (`ForwardPullCompCsSc`,
`ForwardCompCsStateArray`, `ReverseCompCsStateArray`). Restores P3
Monotonicity in Path 2 by explicitly propagating Preferred across the
fallback edge:

```csharp
if (CanMergeStates(ancestor.ElementNode, sc.ElementNode)) {
    SolvingFunctions.MergeInplace(ancestor.ElementNode, sc.ElementNode);
} else {
    sc.ElementNode.AddAncestor(ancestor.ElementNode);
    // Restore P3 Monotonicity: preserve Preferred across the fallback edge.
    PropagatePreferredAcrossFallback(sc.ElementNode, ancestor.ElementNode);
}
SolvingFunctions.PullConstraintsForNode(ancestor.ElementNode);

private static void PropagatePreferredAcrossFallback(TicNode source, TicNode target) {
    if (target.GetNonReference().State is ConstraintsState targetCs
        && source.GetNonReference().State is ConstraintsState sourceCs
        && targetCs.Preferred == null && sourceCs.Preferred != null) {
        targetCs.Preferred = sourceCs.Preferred;
    }
}
```

This mirrors the bidirectional Preferred-copy that `Apply(CS, CS)` already does
(see ApplyCells.md §1.2 row "CS × CS"), restoring the rule at the
CompCs cross-Apply boundary.

**Proof rigor**: this section's proof that the fix restores P3 is *informal*
(by inspection of the byte→real case). A rigorous proof would formalize
monotonicity of all 6 CS dimensions under the AddAncestor fallback edge. The
Preferred axis is closed by this fix; the Descendant axis remains open
(`LangMirror_NestedByteUpcastMap_RealResult` test still `[Ignore]`), and
formal closure of all axes requires either worklist Pull (debt #10) or
extended multi-dimension propagation. See TicTechnicalDebt.md #16 for status.

By inspection: this restores `Info(T0)_post = CS{D = null, A = null, P = I32}` for the byte→real case Preferred axis, which satisfies `Info(T0)_pre ⊔ Info(e₁)` modulo dimensions other than P. P3 holds post-fix on the Preferred axis.

### Other cells with the same issue

The same `try-MergeInplace-fallback-to-AddAncestor` pattern exists in:
- `ForwardPullCompCsSc` (CompCs × Coll, Path 2 above)
- `ForwardCompCsStateArray` (CompCs × Arr)
- `ReverseCompCsStateArray` (Arr × CompCs)

All three need the Preferred-propagation fix in their AddAncestor fallback branches.

`ForwardPullCompCsStateMap`, `ReversePullStateMapCompCs`, `ReversePushStateMapCompCs` use unconditional MergeInplace on the synthesized pair-struct (no fallback), so they don't suffer this issue.

### 3.5. **P3a (Preferred axis) — CLOSED**

**Theorem 3.5a**: After applying `PropagatePreferredAcrossFallback` in the
three identified cells (`ForwardPullCompCsSc`, `ForwardCompCsStateArray`,
`ReverseCompCsStateArray`), the Preferred axis of P3 holds for ALL Pull cells.

**Proof**:
- For CS×CS direct edges: by Lemma 3.1, Preferred-axis case split (Cases B+C
  propagate Preferred correctly via lines (2)+(3)). ✓
- For composite same-shape cells: by Lemma 3.2, info preservation includes
  Preferred (carried by element CS nodes). ✓
- For composite × CS transform cells: by Lemma 3.3, Preferred flows through
  the transform helpers (each transform copies the CS state including
  Preferred field). ✓
- For CompCs cross-Apply cells: ✓
  - Path 1 (MergeInplace path): `GetMergedStateOrNull` preserves all 6
    dimensions per CS-LCA semantics in CompCs Layer 1
    (Algebra/CompositeConstraints.md §3.7).
  - Path 2 (AddAncestor fallback path): `PropagatePreferredAcrossFallback`
    explicitly copies Preferred. By inspection of the helper:
    `target.P ← source.P` iff `target.P = null AND source.P ≠ null`. This
    matches Lemma 3.1 Cases A/B. ✓

Therefore P3a holds for every Pull cell. ∎

### 3.6. **P3b (Descendant axis) — CONJECTURED OPEN, no proven TIC counterexample**

**Hypothesis**: at the AddAncestor fallback in CompCs cross-Apply (§3.4 Path 2), the descendant element's `Descendant` bound is not propagated to the ancestor element. The newly added edge points `desc.elem → anc.elem`; eager re-Pull then walks `anc.elem.Ancestors` (upward), not the new incoming edge, so `desc.elem.D` does not flow into `anc.elem.D`.

**Mechanism (why the hypothesis is plausible)**:

For an AddAncestor-fallback edge `desc.elem → anc.elem`:
- The edge is created **without** invoking `Apply(anc, desc)` on the element pair (the wrapping cell already returned).
- The post-fallback eager `PullConstraintsForNode(anc.elem)` iterates `anc.elem.Ancestors` and re-Pulls each `(anc.elem, ancestor_of_anc)` pair. The new incoming edge from `desc.elem` is NOT in this set.
- Therefore the only chance for `desc.elem.D` to reach `anc.elem.D` is a later Pull pass that visits `desc.elem` and processes its ancestor edges in toposort. If toposort already visited `desc.elem`, that pass does not refire.

This is the streaming-toposort gap that worklist Pull (debt #10) is designed to close.

**Why a clean counterexample is hard to construct**:

For P3b to fail observably at the TIC level, the test must:
1. Force the CompCs cross-Apply fallback (`CanMergeStates` returns false because the element shape is an unresolved function/composite at fallback time).
2. Have a descendant element whose `Descendant` is strictly narrower than any other path's contribution to the ancestor element.
3. Reach Finalize with that ancestor's `D` still null, **and** rely on `D` (not `Preferred`, not `Ancestor`) for the final type.

After PropagatePreferredAcrossFallback (§3.5), the Preferred axis carries enough information for every known scenario in the suite: all tests that previously failed in this family now resolve to the correct type at the TIC layer.

**Status of the previously-cited test** (`LangMirror_NestedByteUpcastMap_RealResult`):

This test was treated as the P3b counterexample in earlier revisions. The node-by-node Pull trace (literal `D=U24,A=Re,P=I32` propagation; byte's `D=U8` lifted to `U24` via LCA; inner map's element resolves to `CS{D=U24,A=Re,P=I32}`; outer map's element absorbs Preferred via the fix) shows TIC infers the outer-map element correctly — the Preferred axis is sufficient. The test's residual failure is at runtime: the lambda is materialized once for a heterogeneous-element collection, and per-row execution casts each row's runtime values to the materialized argument type. This is a **runtime materialization** concern (lambda specialization across heterogeneous rows), not a TIC P3 violation.

Tracked as a runtime debt entry, not as P3b's proof obligation.

**What's needed to settle P3b**:

Either a constructed TIC-level test that:
- forces the fallback in (1) above,
- isolates `Descendant` as the sole information carrier (Preferred and Ancestor both null on the relevant path),
- and produces a wrong final type **at Finalize**, before any runtime materialization step;

— or a proof that no such test can exist for the current cell set. We have constructed neither. In every cell that fires the fallback, `desc.elem.D` is either already propagated through a parallel CS×CS edge, or is null at fallback time. We do not have a closed proof that this exhausts all cases.

**Path to closure (architectural)**:

Worklist Pull (debt #10) eliminates the streaming gap by re-firing Pull on `desc.elem` after the fallback edge is added. This routes `desc.elem.D` through the standard CS×CS Pull path with all safety guarantees of Lemma 3.1. The architecture is specified in [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md) §1, with a theorem (§1.5) that worklist Pull restores P3 across all axes including Descendant.

**Status**: **conjectured open** — the mechanism is identifiable but no proven TIC-level counterexample exists post-PropagatePreferredAcrossFallback. The closure path is documented as worklist Pull (debt #10).

### 3.7. **P3c (other axes) — closed by per-cell inspection**

A, IsOpt, IsCmp, SB axes have no known violations across the truth table in
`ApplyCells.md`. Each cell's mutation either:
- Doesn't touch the dimension (preserved trivially)
- Uses the safe CS API (`AddDescendant`, `AddAncestor`, `GcdBound`) which
  preserves monotonicity by construction (Lemma 3.1)
- Or is a documented no-op (e.g., Push doesn't add edges)

No formal proof obligation pending. ∎

---

## P4. Determinism

**Statement**. For a given input expression `e`, running TIC twice produces identical final types for all nodes.

**Proof sketch**.

- Build is deterministic: input AST → graph is a pure function.
- Toposort is deterministic given a fixed tie-breaking rule (`NodeToposort` uses node order from Build).
- Pull / Push / Destruction cells are pure functions of the current graph state, applied in a deterministic toposort order.
- `LcaOrShareIdentity`'s side-effect (MergeInplace) is deterministic given the input pair — it merges into the same equivalence class regardless of which side called.

**Notes on composite states**: pair-struct synthesis in `ForwardPullCompCsStateMap` produces structurally identical struct nodes on each invocation (same field names "key", "value", same KeyNode/ValueNode identities). No nondeterminism.

P4 holds.

---

## P5. Identity-Sharing Soundness

**Statement**. `LcaOrShareIdentity`'s side-effect (MergeInplace on element nodes when pure LCA would widen) does not introduce unsound type derivations.

Formally: let `G` be a graph with two states `a, b` where `LcaOrShareIdentity(a, b)` fires path (2) — identity merge. Let `G'` be `G` after the merge. Then:

> Any final type derivation valid in `G` is valid in `G'`, AND any final type derivation valid in `G'` is valid in `G` modulo the identity equivalence.

**Proof sketch**.

The merge creates an equivalence: `a.ElementNode ≡ b.ElementNode` (post-merge they are the same node). Constraint sets attached to either node before merge become a single combined constraint set post-merge.

- **Soundness preservation**: any final type `T` satisfying the combined constraints satisfies both the pre-merge constraint sets (since merging is a union, satisfying the union satisfies each part). Conversely, any pair of types `(T_a, T_b)` satisfying separate constraints with `T_a == T_b` corresponds to a single `T` satisfying the merged constraints.
- **Completeness preservation**: if a finalization in `G'` succeeds, the merged constraints are consistent. By the equivalence, both pre-merge constraint sets are jointly consistent in `G`.

The uniform invariance discipline relies on this property: "two collections of the same kind with the same element are the same type" is enforceable only when their element nodes share identity.

P5 holds.

---

## P6. Push Monotonicity (mirror of P3)

**Statement (intuitive)**. Push does not lose information. For every node,
the post-Push state's information per dimension is at least as strong as the
pre-Push state's PLUS the ancestor's contribution along the same dimension.

**Formal statement**.

> For every CS descendant node `d` and every Push edge `a → d` (note the
> direction: ancestor → descendant for Push) processed during Push, after
> Push completes:
>
> ```
> ∀ dim ∈ {D, A, P, IsOpt, IsCmp, SB}:
>     Info(d)_post.dim ⊒_dim  Info(d)_pre.dim ⊔_dim Info(a)_processed.dim
> ```
>
> with the same dimension-specific join semantics as P3 (D: Lca, A: Gcd,
> P: null-fill, flags: OR, SB: GcdBound).

### 6.1. Push for CS × CS direct edges

For a CS descendant `d` and CS ancestor `a` with Push edge `a → d`, the Push
cell `Apply(CS a, CS d)` executes (PushConstraintsFunctions §2.x row "CS ×
CS"). Per-dimension verification mirrors Lemma 3.1 with the direction
reversed:
- **A** (Ancestor): propagated downward via `TryAddAncestor(a.Ancestor)`.
- **D** (Descendant): preserved.
- **P** (Preferred): bidirectional copy as in Pull.
- **IsOpt, IsCmp**: OR-propagated.
- **SB**: `GcdBound` meet, same as Pull.

Push doesn't add new edges (unlike Pull which calls AddAncestor). It mutates
the descendant's state in-place via the same `AddAncestor` API on
ConstraintsState. By inspection of PushConstraintsFunctions §2.1 row CS×CS:

```csharp
// Propagate IsComparable downward (D.cmp := D.cmp ∨ A.cmp)
// StructBound merge via Gcd (independent of HasAncestor)
// if HasAncestor: TryAddAncestor(anc.Ancestor)
// SimplifyOrNull
```

All 6 dimensions handled symmetrically to Lemma 3.1. ∎

### 6.2. Push for composite same-shape cells

For Push composite × composite cells (`Apply(Arr, Arr)`, `Apply(Coll,
Coll)`, etc.) in PushConstraintsFunctions §2.2: each cell calls
`PushConstraints(desc.element, anc.element)` recursively. By structural
induction (mirror of Lemma 3.2), P6 holds.

For Fun: `PushFunTypeArgumentsConstraints` handles contravariance correctly
(args push from desc's args to anc's args, return from anc's return to
desc's return).

### 6.3. Push for transform cells

For composite × CS dispatcher (PushConstraintsFunctions §2.1 transform
path): the cell calls `TransformToXOrNull` then `PushConstraints` on the
element. Same monotonicity argument as Lemma 3.3 with direction reversed. ∎

### 6.4. Push for CompCs cross-Apply

`ForwardPushCompCsSc`, `ForwardCompCsStateArray (isPull=false)`,
`ReversePushScCompCs`, `ReversePushStateMapCompCs` — all are
**precondition-checking only**, with no state mutation (per ApplyCells.md
§2.3). Push doesn't fire the AddAncestor fallback. Therefore P3a/P3b's
violation pattern doesn't apply to Push:
- Push doesn't add edges, so no "edge processed without re-Pull" failure mode.
- Element-axis propagation in Push happens via `PushConstraints` recursive
  call on each element pair, which goes through the standard CS×CS path.

**Therefore P6 holds** without the carve-out that affects P3b. ∎

> **Why Push is "easier" than Pull**: Pull adds edges (AddAncestor on
> element nodes during cross-Apply); these new edges create the
> "streaming-toposort-already-visited" problem that needs eager re-Pull and
> introduces the debt #16 failure mode. Push only checks compatibility and
> propagates state through existing edges — no new edges, no re-traversal
> issues.

---

## 6. Summary table

| Property | Status | Verdict | Fix needed? |
|----------|--------|-----------------|-------------|
| P1 Soundness | proven for all enumerated cells (representative proofs in §1) | holds | no |
| P2 Termination | proven with formalized bound (§2) | holds | no |
| **P3a Monotonicity of Pull — Preferred axis** | **proven** (§3.5) | holds via `PropagatePreferredAcrossFallback` | no |
| **P3b Monotonicity of Pull — Descendant axis** | **OPEN** for multi-level nested CompCs chains (§3.6) | partially holds | yes — needs worklist Pull (debt #10) or extended fallback propagation |
| P3c Monotonicity of Pull — A / IsOpt / IsCmp / SB axes | closed by inspection (§3.7) | holds | no |
| P4 Determinism | proven | holds | no |
| P5 Identity-sharing soundness | proven | holds | no |
| P6 Push Monotonicity (symmetric mirror of P3) | see §6 | holds | no — mirror of P3 with dual direction |

P3b is the only outstanding open proof obligation. It is localized to the
AddAncestor fallback in 3 cells, the failure mechanism is documented (§3.6),
and the path to closure is identified (worklist Pull or extended fallback
propagation). For v1 production: acceptable trade-off (only nested numeric
upcast chains affected; specific test `[Ignore]`'d).

---

## 7. Related specs

- [`Algebra/README.md`](Algebra/README.md) — base algebra
- [`Algebra/BaseOperators.md`](Algebra/BaseOperators.md), [`Algebra/BaseOperators.md`](Algebra/BaseOperators.md) — LCA/GCD properties used in P1/P2
- [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md) — CompCs Layer 0/1/2
- [`Algorithm.md`](Algorithm.md) — Pull / Push / phases
- [`Algorithm.md` §Destruction](Algorithm.md#7-phase-6--destruction) — Destruction Lemmata (used in P1)
- [`Advanced/Preferred.md`](Advanced/Preferred.md) — Preferred propagation §3 (the rule P3 enforces)
- [`ApplyCells.md`](ApplyCells.md) — the cells P3 quantifies over
- [`TechnicalDebt.md`](TechnicalDebt.md) — debt #16 (the formal violation above), debt #10 (worklist Pull alternative)
