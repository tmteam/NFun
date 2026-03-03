# TIC Algorithm — Type Inference Constraint Solver

## Part 1: Formal Description

### 1.1 Type Universe

Types form a lattice:

```
T ::= P                    -- primitive (Bool, Char, U8, U16, ..., I16, I32, ..., Real, Ip, Any)
    | Array(T)             -- array with element type T
    | Fun(T₁...Tₙ → T)    -- function with arg types and return type
    | Struct{f₁:T₁...fₙ:Tₙ}  -- struct with named fields
    | C[desc, anc, cmp]    -- constraint (unsolved type variable)
    | Ref(node)            -- reference to another node (alias)
```

#### Subtyping relation ≤

For primitives: pre-defined partial order (U8 ≤ U16 ≤ U32 ≤ ... ≤ Real ≤ Any, etc.)

For composites:
- `Array(A) ≤ Array(B)` iff `A ≤ B` (covariant)
- `Fun(A₁...Aₙ→R₁) ≤ Fun(B₁...Bₙ→R₂)` iff `Bᵢ ≤ Aᵢ` and `R₁ ≤ R₂` (contra/covariant)
- `Struct{...f:A...} ≤ Struct{f:B}` iff `A ≤ B` for each field f in B (covariant, width+depth subtyping)
- `T ≤ Any` for all T

### 1.2 LCA and FCD

**LCA(A, B)** — Least Common Ancestor — smallest type C such that A ≤ C and B ≤ C:
- For primitives: pre-computed matrix
- `LCA(Array(A), Array(B)) = Array(LCA(A, B))`
- `LCA(Fun(A→R₁), Fun(B→R₂)) = Fun(FCD(A,B) → LCA(R₁,R₂))` — or Any if FCD fails
- `LCA(Struct{fᵢ:Aᵢ}, Struct{fⱼ:Bⱼ}) = Struct{fₖ:LCA(Aₖ,Bₖ)}` where fₖ ∈ fields(A) ∩ fields(B)
- `LCA(A, B) = Any` when A and B are different composite kinds

**FCD(A, B)** — First Common Descendant — largest type C such that C ≤ A and C ≤ B.

### 1.3 Constraint State

A constraint `C[desc, anc, cmp]` represents an unsolved type variable with:
- `desc` (descendant/lower bound): most concrete type that satisfies constraints so far
- `anc` (ancestor/upper bound): most abstract type — always a primitive or null
- `cmp` (comparable flag): if true, type must support comparison operators
- `pref` (preferred): hint for resolution (e.g., I32 for integer constants)

**Invariant**: if both desc and anc are set, then `desc ≤ anc`.

### 1.4 Constraint Graph

A directed graph G = (N, E) where:
- **N**: set of typed nodes. Each node has a mutable `state ∈ T`
- **E**: ancestor edges. `(A, B) ∈ E` means "A is constrained by B" (A ≤ B)

Node types:
- **Named**: input/output variables of the expression
- **SyntaxNode**: intermediate expression nodes
- **TypeVariable**: generated during graph building (array elements, function args, struct fields)

### 1.5 Solving Algorithm

```
SOLVE(G):
    1. TOPOSORT(G)        -- topological sort, merge cycles
    2. PULL(sorted)       -- bottom-up constraint propagation
    3. PUSH(sorted)       -- top-down constraint propagation
    4. DESTRUCT(sorted)   -- resolve remaining constraints
    5. if generics remain:
       FINALIZE(sorted)   -- resolve output/input generics
```

#### Stage 1: TOPOSORT

Topologically sort nodes by ancestor edges. If cycle detected (A ≤ B ≤ ... ≤ A),
merge all nodes in cycle into one via `MergeGroup`.

**Post-condition**: sorted array is acyclic, no Ref nodes.

#### Stage 2: PULL (bottom-up)

For each node in toposort order, for each ancestor:
```
PULL(ancestor, descendant):
    -- descendant's constraints tighten ancestor's lower bound
    ancestor.desc = LCA(ancestor.desc, descendant.desc)
```

Dispatched by state types of both nodes (3×3 matrix: Primitive, Constrains, Composite).

#### Stage 3: PUSH (top-down)

For each node in **reverse** toposort order, for each ancestor:
```
PUSH(ancestor, descendant):
    -- ancestor's constraints refine descendant's upper bound
    descendant.anc = FCD(descendant.anc, ancestor.anc)
```

#### Stage 4: DESTRUCT

For each unsolved node pair (ancestor, descendant):
- Merge constraints, resolve to concrete types where possible
- `ConstrainsState.MergeOrNull` combines two constraint intervals

#### Stage 5: FINALIZE

Resolve remaining generics:
- Output types: solve covariantly (prefer ancestor/most generic)
- Input types (contravariant): solve contravariantly (prefer descendant/most concrete)
- Other: solve covariantly

### 1.6 Dispatch Matrix

Each stage (Pull, Push, Destruct) dispatches on `(ancestor.state, descendant.state)`:

```
              │ Primitive    Constrains    Composite
──────────────┼──────────────────────────────────────
Primitive     │ check compat  add bound    check compat
Constrains    │ add bound     merge        transform/check
Composite     │ fail          transform    merge members
```

For Composite×Composite, further dispatch on (Array×Array, Fun×Fun, Struct×Struct).
Mixed composites (Array×Struct) → fail.

---

## Part 2: Human Description

### What TIC does

TIC answers: "Given an expression with no type annotations, what type does every subexpression have?"

It builds a graph where each subexpression is a node, and type constraints are edges between nodes. Then it solves this graph in 4 passes.

### The Key Idea

Every node starts as "I don't know my type yet" (empty ConstrainsState). As constraints arrive, the node narrows down: "I must be at least X" (descendant), "I must be at most Y" (ancestor). Eventually it resolves to a concrete type.

### The Ancestor Edge

If node A has ancestor B, it means: "A's type must be convertible to B's type." This is the only constraint mechanism. Everything else — if-else, arrays, function calls — is expressed through ancestor edges.

Example: `y = if(cond) a else b`
- Both `a` and `b` get ancestor edge to `y`
- Meaning: both `a` and `b` must be convertible to `y`'s type
- `y`'s type becomes LCA(type_a, type_b) — the most specific type that covers both

### The Four Passes

**Pass 1 — Toposort**: Order nodes so that "simpler" nodes come first. If there's a cycle (A depends on B which depends on A), merge them into one node — they must be the same type.

**Pass 2 — Pull (bottom-up)**: Walk from leaves to root. Each node "pulls" information from what it already knows. If I know my descendant must be an integer, my ancestor gets that info too. This propagates lower bounds upward.

**Pass 3 — Push (top-down)**: Walk from root to leaves. Each node "pushes" information downward. If my ancestor must be Real, I learn my upper bound is Real. This propagates upper bounds downward.

**Pass 4 — Destruct**: For each remaining unsolved node, try to pick a concrete type from its constraint interval [desc..anc]. Use preferred type if possible (e.g., integer constants prefer I32).

### What Goes Wrong With Structs

For primitives, the constraint interval `[U8..Real]` is a simple linear range. For structs, the "interval" is multi-dimensional: each field has its own constraint. The code handles this with a big dispatch matrix (Primitive×Primitive, Primitive×Constrains, ..., Struct×Struct), but **struct-specific cases are incomplete**:

- `ConstrainsState + Struct` in Push — doesn't propagate field constraints
- `MergeOrNull` for two ConstrainsState both containing Struct descendant — didn't resolve
- `FitsInto` and `CanBeFitConverted` for structs — used invariant field checks instead of covariant

---

## Part 3: Invariants (What We're Certain Of)

### Proven by tests (1120+ unit tests pass):

1. **Primitive LCA is correct and symmetric**: LCA(A,B) = LCA(B,A). Pre-computed 18×18 matrix.
2. **Primitive FCD is correct**: FCD(A,B) finds the most abstract common descendant.
3. **Array LCA is covariant**: LCA(A[], B[]) = LCA(A,B)[]. Works recursively.
4. **Function LCA**: covariant in return, contravariant in args via FCD.
5. **Struct LCA**: intersection of field names, covariant in field types (LCA per field). *(newly established)*
6. **Toposort detects and merges cycles**: no infinite loops possible.
7. **Constraint interval [desc..anc] is always valid**: if both set, desc ≤ anc.

### Proven by recent work:

8. **Struct fields are covariant**: {age:int} ≤ {age:real} because int ≤ real. Sound because structs are immutable.
9. **ConstrainsState with solved StateStruct descendant resolves via MergeOrNull**: `C[{age:I32}, null] + C[{age:I32}, null] → {age:I32}`.
10. **Nested struct field access works for 1-3 levels**: `a.b`, `a.b.c`, `a.b.c.d` — all resolve correctly.
11. **Struct merge (GetMergedStateOrNull) handles Struct+ConstrainsState**: ConstrainsState with struct descendant can merge with StateStruct.

### Known gaps:

12. **Array Struct LCA doesn't finalize in Destruction**: When array element is `C[desc=Struct{...}]`, Destruction's `Apply(ICompositeState, ConstrainsState)` picks one ancestor via `ref(ancestor)` instead of the LCA result. The LCA is computed correctly in Pull but lost in Destruction.

13. **Push ConstrainsState→ICompositeState is a no-op for most cases**: `Apply(ConstrainsState, ICompositeState)` just checks ancestor compatibility but doesn't propagate struct field constraints downward. Partially fixed for struct fields.

14. **`CanBeFitConverted(StateStruct, StateStruct)` uses invariant field checks**: checks `desc.field → to.field` instead of `to.field → desc.field` (covariant direction). This blocks Destruction from recognizing that `{age:I32}` fits into `C[desc={age:Real}]`.

---

## Part 4: Architectural Observations

### What's missing: a unified "constraint algebra" for composite types

For primitives, the constraint system is elegant:
```
Merge([A..B], [C..D]) = [LCA(A,C) .. FCD(B,D)]
```

This is a single operation that works uniformly. For composites (Array, Fun, Struct), there is no such algebra. Instead, there's a 9-entry dispatch matrix with hand-coded logic per combination.

**The ideal**: a single `Merge(T, T) → T` operation that works for all types, including composite types with nested constraints. This would replace the separate Pull/Push/Destruct dispatch tables with one consistent algebra.

### What's missing: covariance-aware FitsInto

`FitsInto` is the fundamental question: "can type A be used where type B is expected?"

For primitives it delegates to `CanBePessimisticConvertedTo` — a simple lookup.

For structs, it checks field-by-field. But the check direction (A.field → B.field vs B.field → A.field) depends on whether fields are covariant or invariant. Currently the code has **both directions** in different methods, inconsistently. A single, covariance-aware `FitsInto` for struct fields would resolve several bugs at once.

### What's missing: distinction between Identity and Subtyping edges

The graph has two kinds of edges, but the code doesn't distinguish them clearly:

1. **Ancestor edge** (A ≤ B): "A is convertible to B". Created by `AddAncestor`. Semantics: Pull computes LCA of all descendants, Push narrows descendants.

2. **Identity edge** (A ≡ B): "A and B are the same type". Created by `BecomeReferenceFor` / `MergeInplace`. One becomes `StateRefTo` of the other.

For primitives, identity works: if two int nodes merge, they're both int. No information loss.

For structs, identity is **too strong**: `MergeInplace({age:I32}, {age:Real, size:I32})` creates `{age:I32, size:I32}` — a union. But the intended semantics for arrays is LCA = `{age:Real}` — an intersection with covariant field types.

`SetStrictArrayInit` uses identity edges (`BecomeReferenceFor`), but the correct semantics for struct elements is subtyping (`AddAncestor`). This is the root cause of array struct LCA failures.

**Proposal**: For composite types in array init, use ancestor edges instead of identity. This lets the existing Pull/LCA mechanism compute the correct element type automatically, without special cases in Destruction.

### What's excessive: the 3×3 dispatch matrix per stage

Each stage (Pull, Push, Destruct) has 9+ methods for different type combinations:
```
Apply(Primitive, Primitive)    Apply(Primitive, Constrains)    Apply(Primitive, Composite)
Apply(Constrains, Primitive)   Apply(Constrains, Constrains)   Apply(Constrains, Composite)
Apply(Composite, Primitive)    Apply(Composite, Constrains)    Apply(Composite, Composite)
```

Plus Composite×Composite further dispatches to Array×Array, Fun×Fun, Struct×Struct.

Most of these do very simple things. The complexity comes from **missing abstractions**: if there were a unified `Merge` and `FitsInto`, most of these methods would collapse.

---

## Part 5: How Composite Members Are Decomposed — The Key Asymmetry

### How Array and Fun work (correctly)

When Pull encounters `Array(descElem) ≤ Array(ancElem)`:

```csharp
// PullConstraintsFunctions.Apply(StateArray, StateArray):
descendant.ElementNode.AddAncestor(ancestor.ElementNode);  // elem ≤ elem
descendantNode.RemoveAncestor(ancestorNode);                // composite edge → member edges
```

**What this does**: removes the composite-level edge and replaces it with member-level edges. After this, `descElem` and `ancElem` are **independent nodes in the graph** with an ancestor edge between them. Pull/Push/Destruction handle them in subsequent passes **automatically**.

Same for Fun (return is covariant, args are contravariant):
```csharp
descendant.RetNode.AddAncestor(ancestor.RetNode);
ancestor.ArgNodes[i].AddAncestor(descendant.ArgNodes[i]);  // reversed
descendantNode.RemoveAncestor(ancestorNode);
```

**Key property**: member nodes (ElementNode, RetNode, ArgNodes) are created via `CreateVarType()`, registered in `_typeVariables`, and included in toposort. They are **first-class graph citizens**.

### How Struct works (broken)

When Pull encounters `Struct{f:descF} ≤ Struct{f:ancF}`:

```csharp
// PullConstraintsFunctions.Apply(StateStruct, StateStruct):
MergeInplace(ancField.Value, descField);   // f ≡ f  (IDENTITY, not subtyping)
// NO RemoveAncestor                        // composite edge stays
```

**Two problems**:

1. **Identity instead of subtyping**: MergeInplace makes both field nodes the same object. This means `{age:I32}` and `{age:Real}` can't coexist — one must become the other. For covariant fields, we need `descF ≤ ancF` (ancestor edge), not `descF ≡ ancF` (identity).

2. **No decomposition**: the composite edge `Struct ≤ Struct` is NOT removed. Field edges are NOT added to the graph. So Pull/Push can't process field constraints in later passes.

### Why naive fix failed

Replacing `MergeInplace` with `AddAncestor` and `RemoveAncestor` — exactly matching Array/Fun pattern — caused regressions because:

**Struct field nodes are NOT always registered in the graph.**

- `SetStructInit` creates a struct using **existing expression nodes** as field values. These nodes ARE in the graph (they're syntax nodes).
- `SetFieldAccess` creates **new TypeVariable nodes** for fields via `CreateVarType()`. These ARE registered in `_typeVariables`.
- But `StateStruct.Of(...)` (used in tests via `SetVarType`) creates field nodes via `TicNode.CreateTypeVariableNode()` — these are NOT registered in `_typeVariables` and NOT in toposort.
- `StateStruct.With(...)` (used in Pull to add missing fields) also creates new nodes that may not be registered.
- LCA creates frozen structs with `TicNode.CreateInvisibleNode()` — explicitly NOT registered.

When `AddAncestor` creates an edge between a registered node and an unregistered node, the unregistered node is **never visited** by Pull/Push/Destruction. The edge exists but is never processed.

### What needs to happen (plan)

To make struct fields work like Array elements and Fun args:

1. **All struct field nodes must be registered in the graph.** Every time a StateStruct is created or modified, its field nodes must be in `_typeVariables` (or toposort). This includes:
   - `StateStruct.Of(...)` factory methods
   - `StateStruct.With(...)` when adding new fields
   - LCA results (frozen structs with invisible nodes)
   - `TransformToStructOrNull` results

2. **Pull Struct≤Struct should decompose into field edges**, same as Array:
   ```
   descField.AddAncestor(ancField);
   descendantNode.RemoveAncestor(ancestorNode);
   ```

3. **Push Struct≤Struct should push field constraints**, same as Array:
   ```
   PushConstraints(descField, ancField);
   ```

4. **Destruction Struct≤Struct should destruct fields**, same as Array:
   ```
   Destruction(descField, ancField);
   ```

### How to get there incrementally

**Step 0** (current state): Struct fields use MergeInplace. Works for identity cases, fails for covariance.

**Step 1**: Ensure all struct field nodes are registered. Add a `RegisterStructFields(StateStruct)` method to GraphBuilder that walks field nodes and adds unregistered ones to `_typeVariables`. Call it whenever a StateStruct is set as a node's state.

**Step 2**: In PullConstraintsFunctions, replace MergeInplace with AddAncestor for struct fields. Add RemoveAncestor for the composite edge. This decomposes struct constraints into field constraints.

**Step 3**: Simplify PushConstraintsFunctions for Struct×Struct to match Array×Array pattern.

**Step 4**: Remove struct-specific hacks in Destruction, ConstrainsState.MergeOrNull, etc. — they become unnecessary because fields are handled as regular graph nodes.

**Step 1 is the prerequisite.** Without it, Steps 2-4 will cause the same regression we saw.

### Update after implementation attempt

Step 1 turned out to be **unnecessary**. `PullConstraintsRecursive` and `PushConstraintsRecursive` already recurse into composite members:

```csharp
if (node.State is ICompositeState composite)
    foreach (var member in composite.Members)
        PullConstraintsRecursive(member);
```

So struct field nodes ARE processed even without explicit registration.

**Step 2 was partially done** — `Apply(StateStruct, StateStruct)` now uses `AddAncestor` + `RemoveAncestor`. This fixed crash bugs and nested struct access.

**The remaining problem** is `Apply(ICompositeState, ConstrainsState)` for structs — specifically the `Struct ancestor + ConstrainsState descendant` case in if-else. Here the flow is:

1. Pull: node 2 `{age:[U8..Re]}` and node 3 `{age:Real}` both ancestors of node 4
2. Pull processes `(node 2, node 4)` and `(node 3, node 4)` via `Apply(ICompositeState, ConstrainsState)`
3. LCA computed in `AddDescendant`: `{age:Real}` (correct)
4. `TransformToStructOrNull` returns the LCA frozen struct with **new invisible field nodes**
5. These new field nodes are **disconnected** from original field nodes (node 1 = generic const [U8..Re])
6. No ancestor edge exists between node 1 and LCA's age field node

**Root cause**: `Apply(ICompositeState, ConstrainsState)` for structs does NOT decompose into field-level edges (unlike Array which does: `result.ElementNode.AddAncestor(ancArray.ElementNode)`). It can't, because it processes one ancestor at a time, and field decomposition needs to connect to ALL ancestors' field nodes.

**Possible solutions**:

A. **Field decomposition in Apply(ICompositeState, ConstrainsState)**: After TransformToStructOrNull, add ancestor edges from result field nodes to ancestor field nodes. Problem: only sees one ancestor per call. Partially works — helps for the case where TransformToStructOrNull returns ancestorStruct itself (NoConstrains case).

B. **Push-based field propagation** (current workaround): In `Apply(ConstrainsState, ICompositeState)`, if ConstrainsState has a struct descendant, MergeInplace the LCA's field types into the descendant struct's field types. Works for concrete LCA fields (Real), crashes for Any.

C. **Restructure graph building**: Instead of creating ConstrainsState node for if-else result, create a struct node with new field type variables, and add ancestor edges from EACH branch's field nodes. This is the cleanest solution but requires changes to how `SetIfElse` works for struct types.

Currently using solution B with a guard against Any fields.
