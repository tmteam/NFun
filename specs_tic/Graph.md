# Graph

> **Scope**: the constraint graph data structure that TIC operates on.

## 1. Nodes and states

A TIC node represents a sub-expression or a type variable. Each node has a **state**.

State types are documented in detail in [`TypeSystem.md`](TypeSystem.md). Briefly:

| State | Solved? | Purpose |
|---|---|---|
| `StatePrimitive` | yes | a concrete primitive type (I32, Bool, …) |
| `StateArray`, `StateOptional`, `StateFun`, `StateStruct` | yes | classical concrete composite |
| `StateCollection`, `StateMap` | yes | lang-mode concrete composites |
| `ConstraintsState` (CS) | no | primitive interval `[D..A]` + 4 dimension flags |
| `StateCompositeConstraints` (CompCs) | no | composite interval over ConstructorKind |
| `StateRefTo` | n/a | transparent alias to another node |

### Node properties

In addition to state, each node carries metadata:

- `Name` — human-readable label (variable name or synthetic).
- `Ancestors` — list of nodes that this node fits into (descendant → ancestor edges).
- `IsSignatureParam` — flag on function-signature parameter slots; prevents shape-changing transformations (e.g., Optional-wrapping).
- `IsOptionalElement` — flag marking nodes that are the element of a StateOptional.
- `Registered` — tracking flag for cycle detection.
- `VisitMark` — per-pass mark used by Toposort and recursive Pull to detect already-visited nodes.


## 2. Edges

Two kinds.

### 2.1. Constraint edges (descendant → ancestor)

Encode the typing relation `descendant ≤ ancestor`. Added during Build (most edges) and during Pull/Push (transform cells, cross-Apply fallback).

When `node A` has `B` in its Ancestors list, this means "A must fit into B" — `A.State ≤ B.State` at Finalize.

### 2.2. Component edges (structural)

Implicit — encoded by composite states' internal references. Examples:

- `StateArray.ElementNode`.
- `StateOptional.ElementNode`.
- `StateFun.ArgNodes` and `StateFun.RetNode`.
- `StateStruct.Fields` dictionary.
- `StateMap.KeyNode` and `StateMap.ValueNode`.

Component edges are not added to ancestor lists — they are part of the wrapping state's structure.

## 3. StateRefTo — transparent aliasing

```
StateRefTo { Node: TicNode }
```

When a node has `StateRefTo(T)`, every operator dereferences to `T.State` before applying. This makes `RefTo` invisible at the algebraic level.

Used for:
- Generic-parameter instantiation at function call sites.
- Merged equivalence classes after `MergeInplace`.
- Recursive type back-references (named types).

`node.GetNonReference()` walks the RefTo chain to the canonical owner. `MergeInplace(a, b)` makes one of them a RefTo to the other.

## 4. Cycles

Cycles arise from:
- Recursive function definitions.
- Recursive named types (`type t = {next: t? = none}`).
- Mutually recursive constructs.

Handled by:
- **SCC merge** during Toposort (Tarjan): nodes in a cycle are merged; their states union via Layer-1 commit.
- **F-bounded polymorphism**: named recursive types are detected and rewritten as F-bounds (`T <: τ(T)`). See [`Advanced/PushReform.md`](Advanced/PushReform.md).
- **Cycle guards** (mark constants, visited-pair caches) in algebraic operators prevent infinite recursion.

## 5. Grammar (BNF)

```
Node       := name : State
State      := Primitive | Composite | ConstraintsState | CompCs | RefTo
Primitive  := Bool | Char | Ip | I16 | I32 | … | None | Any
Composite  := Array(Node) | Optional(Node) | Fun(Node*, Node)
            | Struct({name → Node}*) | Collection(K, Node) | Map(Node, Node)
CompCs     := { Anc?: K, Desc?: K, e: Node, IsOpt: bool }
RefTo      := → Node
ConstraintsState
           := [ Desc?: State, Anc?: Primitive, IsOpt, IsCmp,
                StructBound?: Struct, Preferred?: Primitive ]
K          := List | MutableArray | FixedArray | Set | Map | Enumerable | Any
```

## See also

- Type system: [`TypeSystem.md`](TypeSystem.md)
- Algorithm: [`Algorithm.md`](Algorithm.md)
- Algebra: [`Algebra/README.md`](Algebra/README.md)
