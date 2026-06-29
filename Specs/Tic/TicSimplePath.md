# TIC Simple Path (SimplePrimitiveSolver)

Fast-path type inference for primitive-only expressions. Replaces full TIC with monotone dataflow on a finite lattice when no composite types are present.

---

## 1. Lattice Definition

### 1.1 The Primitive Type Lattice L

L is a finite lattice of 19 elements with partial order ≤ (subtyping). Element ordinals are fixed:

| Ordinal | Type | Flags |
|---------|------|-------|
| 0 | Any | top |
| 1 | Char | comparable |
| 2 | Bool | - |
| 3 | Ip | - |
| 4 | Real | numeric, comparable |
| 5 | I96 | numeric, comparable, abstract |
| 6 | I64 | numeric, comparable |
| 7 | I48 | numeric, comparable, abstract |
| 8 | I32 | numeric, comparable |
| 9 | I24 | numeric, comparable, abstract |
| 10 | I16 | numeric, comparable |
| 11 | U64 | numeric, comparable |
| 12 | U48 | numeric, comparable, abstract |
| 13 | U32 | numeric, comparable |
| 14 | U24 | numeric, comparable, abstract |
| 15 | U16 | numeric, comparable |
| 16 | U12 | numeric, comparable, abstract |
| 17 | U8 | numeric, comparable |
| 18 | None | bottom (partial) |

Abstract types (I96, I48, I24, U48, U24, U12) exist only as internal constraint bounds; they are never the type of a runtime value.

### 1.2 Hasse Diagram

```
                      Any (0)
                   /   |   \   \
               Real(4) Bool(2) Char(1) Ip(3)
                 |
               I96 (5)
              /     \
           I64(6)   U64(11)
          / |         |  \
       I48(7)\     U48(12) \
        |    |       |      |
      I32(8) |     U32(13)  |
        |    |       |      |
      I24(9) |     U24(14)  |
        |    |       |      |
      I16(10)|     U16(15)  |
        \    |     /        |
         U12(16)--          |
            \    /          |
            U8 (17)---------
              |
           None (18)
```

Covering relations (x -- y means x ≤ y with no z such that x < z < y):

**Signed chain:** U8 ≤ I16 ≤ I24 ≤ I32 ≤ I48 ≤ I64 ≤ I96 ≤ Real ≤ Any
**Unsigned chain:** U8 ≤ U12 ≤ U16 ≤ U32 ≤ U48 ≤ U64 ≤ I96
**Cross links (unsigned to signed):**
- U12 ≤ I16, U12 ≤ I24, U12 ≤ I32, U12 ≤ I48, U12 ≤ I64
- U16 ≤ I24, U16 ≤ I32, U16 ≤ I48, U16 ≤ I64
- U24 ≤ I32, U24 ≤ I48, U24 ≤ I64
- U32 ≤ I48, U32 ≤ I64
- U48 ≤ I64
- U64 ≤ I96
**Non-numeric:** Bool ≤ Any, Char ≤ Any, Ip ≤ Any
**None:** None ≤ Any (only; None is NOT ≤ any other primitive)

Note: None is a partial bottom -- it is below Any but incomparable with all other types.

### 1.3 LCA (Join) and GCD (Meet)

**LCA(a, b)** = least common ancestor = smallest c in L such that a ≤ c and b ≤ c.

**GCD(a, b)** = greatest common descendant = largest c in L such that c ≤ a and c ≤ b. Returns null (denoted OPEN) when no such c exists.

Both are implemented as precomputed `byte[19 x 19]` lookup tables, built from the lattice definition.

**Properties:**
- **Commutativity:** LCA(a,b) = LCA(b,a). GCD(a,b) = GCD(b,a).
- **Associativity:** LCA(a, LCA(b,c)) = LCA(LCA(a,b), c). Same for GCD.
- **Monotonicity:** If a ≤ a', then LCA(a,b) ≤ LCA(a',b). Same for GCD.
- **Idempotence:** LCA(a,a) = a. GCD(a,a) = a.
- **Top/bottom:** LCA(a, Any) = Any. GCD(a, Any) = a.

**Incomparable pairs (GCD = null):** Any pair drawn from distinct non-numeric branches:
- Bool vs Char, Bool vs Ip, Char vs Ip
- Bool vs any numeric, Ip vs any numeric
- Char vs any numeric
- None vs anything except Any and None

Examples: GCD(Bool, I32) = null. GCD(Char, Real) = null. LCA(Bool, I32) = Any. LCA(U16, I32) = I32. GCD(U16, I32) = U16. LCA(U32, I16) = I48. GCD(U32, I16) = U12.

### 1.4 Fits Predicate

Fits(a, b) = "a ≤ b" = a can be implicitly converted to b. Equivalent to LCA(a, b) = b.

Also precomputed as `bool[19 x 19]` table.

Special case: Fits(None, Any) = true. Fits(None, x) = false for x != None, x != Any.

---

## 2. Constraint Language

### 2.1 Variables

V = {g_0, g_1, ..., g_{n-1}} is a set of **groups**. Each AST node and each named variable is assigned to a group. Groups are managed by a union-find data structure.

### 2.2 Group State

Each group g carries an interval and metadata:

- **desc[g]** in L u {OPEN}: lower bound (descendant constraint). OPEN = unconstrained.
- **anc[g]** in L u {OPEN}: upper bound (ancestor constraint). OPEN = unconstrained.
- **pref[g]** in L u {OPEN}: preferred resolution type (soft hint, not a hard constraint).
- **comparable[g]** in {true, false}: whether g must resolve to a comparable type.

The interval [desc[g]..anc[g]] represents the set {t in L : desc[g] ≤ t ≤ anc[g]}.

### 2.3 Constraint Forms

1. **Identity:** g_i ≡ g_j -- modeled by `Unite(i, j)`. Merges two groups into one.
2. **Concrete:** desc[g] = anc[g] = t -- pins a group to an exact type.
3. **Interval:** desc[g] = d, anc[g] = a, pref[g] = p -- sets bounds on a fresh group.
4. **Subtyping edge:** g_i ≤ g_j -- directed constraint, processed during Propagate.
5. **Generic constraint:** applies desc, anc, and comparable from a function signature's generic parameter bounds to a group.
6. **Preferred:** pref[g] = p -- soft hint; does not affect satisfiability.

### 2.4 Satisfiability

A group g is **satisfiable** iff:
- desc[g] = OPEN, or anc[g] = OPEN, or Fits(desc[g], anc[g]).

A constraint system is satisfiable iff all groups are satisfiable and all subtyping edges are satisfiable after propagation.

---

## 3. Gate (Precondition)

SPS correctness depends on the expression containing ONLY primitive types. The gate is a two-level filter:

### 3.1 Level 1: AST Structure Gate (`IsSimpleBody`)

Determined during the AST numbering pass (`SetNodeNumberVisitor`). Sets `IsSimpleBody = false` on first non-primitive node. Zero overhead for non-simple expressions (SPS is never entered).

**Accepted node types (exhaustive):**
- `GenericIntSyntaxNode` -- integer literals
- `IpAddressConstantSyntaxNode` -- IP address literals
- `NamedIdSyntaxNode` -- variable references and constants
- `BinOperatorSyntaxNode` -- binary operators
- `UnaryOperatorSyntaxNode` -- unary operators
- `FunCallSyntaxNode` -- function calls
- `IfThenElseSyntaxNode` -- conditional expressions
- `IfCaseSyntaxNode` -- if-else branch (child of IfThenElseSyntaxNode)
- `ComparisonChainSyntaxNode` -- chained comparisons (a < b < c)
- `EquationSyntaxNode` -- variable definitions (y = expr)
- `VarDefinitionSyntaxNode` -- type-annotated variable declarations
- `SyntaxTree` -- root node
- `ConstantSyntaxNode` -- ONLY if output type is numeric, Bool, Real, Char, or Ip

**Rejected (any one causes IsSimpleBody = false):**
- `ArraySyntaxNode`, `StructInitSyntaxNode`, `AnonymFunctionSyntaxNode`, `SuperAnonymFunctionSyntaxNode`, `StructFieldAccessSyntaxNode`, `DefaultValueSyntaxNode`, `TypeDeclarationSyntaxNode`, `SafeFieldAccessSyntaxNode`, `SafeGetElementSyntaxNode`, `CoalesceSyntaxNode`, `PipeOperatorSyntaxNode`, and any other node type not in the accepted list
- `ConstantSyntaxNode` with non-primitive output type (e.g., string literals, array constants)

### 3.2 Level 2: Semantic Gate (in `SolveOrNull`)

Even when IsSimpleBody = true, SPS performs additional checks that cause it to return null:

1. **Apriori types:** All externally-provided variable types must be primitive (numeric, Bool, Char, Ip, Any, or Empty).
2. **Type annotations:** Variable definition type syntax must be either empty or a named primitive type. Recognition uses a case-insensitive set: {int8, int16, int, int32, int64, byte, uint8, uint16, uint, uint32, uint64, real, bool, char, ip, any}.
3. **Function signatures:** Each function used in the expression must be one of:
   - `PureGenericFunctionBase` with exactly 1 generic constraint (single type parameter T with bounds).
   - `IConcreteFunction` with all argument and return types being primitive.
4. **Top-level constructs:** `UserFunctionDefinitionSyntaxNode` and `TypeDeclarationSyntaxNode` cause immediate rejection.
5. **Unknown node types:** The `default` branch in `WalkAndCheck` returns false for any node type not explicitly handled.

### 3.3 Gate Exhaustiveness Argument

The gate is a NECESSARY condition for SPS correctness. SPS produces incorrect results if any composite type (array, struct, optional, function) enters the constraint system, because:
- Composite types require **variance** (covariant arrays, contravariant function args) which SPS does not model.
- Composite types require **structural decomposition** (array element types, struct fields) which SPS does not model.
- The union-find merge semantics (bidirectional equality) is only correct for primitive types; composite types need directional subtyping with structural recursion.

Level 1 catches all composite-producing AST nodes. Level 2 catches composite types entering via function signatures or external type annotations. Together, they ensure no composite type enters the constraint system.

---

## 4. Build Phase

Single fused AST pass: `WalkAndCheck` performs gate checking and constraint generation simultaneously. On first non-primitive node, returns null immediately (no wasted work).

### 4.1 Operations

**NewGroup() -> g:** Allocates a fresh group with desc=OPEN, anc=OPEN, pref=OPEN, comparable=false.

**GetOrCreateNodeGroup(nodeId) -> g:** Returns the group for AST node `nodeId`, creating one if needed. Each AST node maps to at most one group.

**GetOrCreateVarGroup(name) -> g:** Returns the group for variable `name`, creating one if needed. Case-insensitive lookup.

**SetConcrete(g, t):** Pins group g to exact type t.
- Let r = Find(g) (union-find root).
- **Validation:** If desc[r] != OPEN and NOT Fits(desc[r], t): fail. If anc[r] != OPEN and NOT Fits(t, anc[r]): fail.
- Set desc[r] = t, anc[r] = t.
- Note: does NOT update pref. A concrete type is its own resolution.

**SetInterval(g, d, a, p):** Sets interval bounds on group g.
- Let r = Find(g).
- Set desc[r] = d, anc[r] = a.
- If pref[r] = OPEN: set pref[r] = p.
- **Precondition:** Called only on groups that have not had prior SetConcrete or SetInterval calls (fresh groups from literal processing). No cross-validation performed.

**Unite(a, b):** Union-find merge.
- Let ra = Find(a), rb = Find(b). If ra = rb: no-op.
- Merge by rank: the higher-rank root becomes the new root. On tie, increment rank.
- **MergeInto(root, other):**
  - desc[root] = OPEN if both OPEN; otherwise LCA(desc[root], desc[other]) treating OPEN as identity.
  - anc[root] = OPEN if both OPEN; otherwise GCD(anc[root], anc[other]) treating OPEN as identity. If GCD returns null (incompatible ancestors): **fail**.
  - After merge: if desc[root] != OPEN and anc[root] != OPEN and NOT Fits(desc[root], anc[root]): **fail**.
  - comparable[root] = comparable[root] OR comparable[other].
  - pref[root] = pref[root] if not OPEN, else pref[other].

**ApplyGenericConstraint(g, c):** Applies generic function bounds to group g.
- Let r = Find(g).
- If c has descendant d: desc[r] = LCA(desc[r], OrdOf(d)), treating OPEN as identity.
- If c has ancestor a: anc[r] = GCD(anc[r], OrdOf(a)), treating OPEN as identity.
- If c.IsComparable: comparable[r] = true.

**AddEdge(from, to):** Records a directed subtyping edge from -> to, processed in Propagate.

### 4.2 Constraint Generation by Node Type

**GenericIntSyntaxNode (integer literals):**

Integer literals generate interval constraints based on value range and notation.

*Hex/binary notation (`IsHexOrBin = true`):*
- Ancestor is always I96 (not Real -- hex/bin literals are integer-only).
- Preferred is I32 (for values up to int.MaxValue) or I64 (for larger values).
- Descendant is the narrowest type that can hold the value.
- Negative values: desc = I16 (>= short.MinValue), I32 (>= int.MinValue), or concrete I64.
- Positive values: desc = U8 (≤ 255), U12 (≤ 32767), U16 (≤ 65535), U24 (≤ int.MaxValue), U32 (≤ uint.MaxValue), U48 (≤ long.MaxValue), or concrete U64.

*Decimal notation (`IsHexOrBin = false`):*
- Ancestor is always Real (decimals can promote to floating-point).
- Preferred is the dialect's `IntegerPreferredType` (typically I32).
- Descendant computed identically to hex/binary positive case.
- Negative values: desc = I16 (>= short.MinValue), I32 (>= int.MinValue), or I64.

Operation: `SetInterval(nodeGroup, desc, anc, pref)`.

**IpAddressConstantSyntaxNode:**
Operation: `SetConcrete(nodeGroup, Ip)`.

**ConstantSyntaxNode:**
Gate: node already validated as primitive by Level 1.
Operation: `SetConcrete(nodeGroup, ToPrimitiveOrd(outputType))`.

**NamedIdSyntaxNode (variable references and constants):**
1. If the name is already known as a variable: `Unite(varGroup, nodeGroup)`. Mark as Variable.
2. Else if the name matches a constant in the constant registry: `SetConcrete(nodeGroup, constantType)`. Mark as Constant.
3. Else (new variable): `Unite(varGroup, nodeGroup)`. Mark as Variable.

**BinOperatorSyntaxNode:**
Gate: signature must pass IsSimpleSignature.
Children walked first (recursive WalkAndCheck).

- *PureGenericFunctionBase (single constraint c):*
  - **Pow special case:** If operator is Pow AND `IsPowForcedReal(rightOperand)`: set `SetInterval(resultGroup, Real, Real, OPEN)`, then `Unite(leftGroup, resultGroup)`, `Unite(rightGroup, resultGroup)`. Record generic group.
  - **Normal case:** `ApplyGenericConstraint(resultGroup, c)`, `Unite(leftGroup, resultGroup)`, `Unite(rightGroup, resultGroup)`. Record generic group.
- *IConcreteFunction:* `SetConcrete(leftGroup, argType[0])`, `SetConcrete(rightGroup, argType[1])`, `SetConcrete(resultGroup, returnType)`.

**UnaryOperatorSyntaxNode:**
Gate: signature must pass IsSimpleSignature.
Child walked first.

- *PureGenericFunctionBase:* `ApplyGenericConstraint(resultGroup, c)`, `Unite(operandGroup, resultGroup)`. Record generic group.
- *IConcreteFunction:* `SetConcrete(operandGroup, argType[0])`, `SetConcrete(resultGroup, returnType)`.

**FunCallSyntaxNode:**
Gate: signature must pass IsSimpleSignature.
All args walked first.

- *PureGenericFunctionBase:*
  - **Pow special case (function-call form):** same as binary Pow -- force Real on all args + result.
  - **Normal case:** `ApplyGenericConstraint(resultGroup, c)`, then `Unite(argGroup[i], resultGroup)` for each arg. Record generic group.
- *IConcreteFunction:* `SetConcrete(argGroup[i], argType[i])` for each arg, `SetConcrete(resultGroup, returnType)`.

**IfThenElseSyntaxNode:**
All conditions and branch expressions walked first.
- Each condition: `SetConcrete(conditionGroup, Bool)`.
- Each branch expression: `AddEdge(branchGroup, resultGroup)`.
- Else expression: `AddEdge(elseGroup, resultGroup)`.

**ComparisonChainSyntaxNode (a < b < c, a == b != c, etc.):**
All operands walked first.
For each operator at position i:
- Determine constraint: equality operators (==, !=) use `GenericConstrains.Any` (no comparable). Comparison operators (<, >, <=, >=) use `GenericConstrains.Comparable`.
- Apply comparable flag to operand[i]'s group if needed.
- `Unite(operandGroup[i], operandGroup[i+1])`.
- Note: transitive chaining through union-find -- in `a < b < c`, all three operands end up in the same group.
- Result: `SetConcrete(resultGroup, Bool)`.

**EquationSyntaxNode (y = expr):**
Gate: type annotation (if present) must be simple type syntax.
Expression walked first.
1. If type annotation present: resolve to primitive type, `SetConcrete(varGroup, resolvedType)`.
2. **Preferred propagation (SetDef):** If expr group is concrete (desc = anc), propagate as preferred to var group: if pref[varRoot] = OPEN, set pref[varRoot] = desc[exprRoot].
3. `AddEdge(exprGroup, varGroup)`.

**VarDefinitionSyntaxNode:**
Gate: type syntax must be simple.
Resolve to primitive type, `SetConcrete(varGroup, resolvedType)`.

**UserFunctionDefinitionSyntaxNode, TypeDeclarationSyntaxNode:**
Immediate rejection (return null).

---

## 5. Propagate Phase

Fixed-point iteration over directed edges. Computes the tightest intervals consistent with all subtyping edges.

### 5.1 Transfer Function

For each edge (from, to) where from and to are resolved to their union-find roots:

If from and to are the same root: skip (edge is within a unified group).

Otherwise, apply all six transfer rules:

**T1. Push desc (from -> to):** If desc[from] != OPEN:
- to.desc <- LCA(to.desc, from.desc), treating OPEN as identity.

**T2. Pull anc (to -> from):** If anc[to] != OPEN:
- from.anc <- GCD(from.anc, anc[to]), treating OPEN as identity. If GCD = null: no change (deferred to Resolve for failure detection).

**T3. Concrete desc pull (to -> from):** If to.desc != OPEN AND to.desc = to.anc (to is concrete):
- from.desc <- LCA(from.desc, to.desc), treating OPEN as identity.
- Rationale: if the target is pinned to type t, the source must be at least t (since from ≤ to and to = t means from ≤ t, and since from's value flows into to, from must produce a type that is ≤ t; but also t ≤ from because to being concrete means from must be at least t for the edge to be satisfiable with from's desc). This mirrors TIC's Destruction(StatePrimitive ancestor, ConstraintsState descendant).

**T4. Propagate anc (from -> to):** If anc[from] != OPEN:
- to.anc <- GCD(to.anc, anc[from]), treating OPEN as identity. If GCD = null: no change.
- Note: This rule has no direct TIC counterpart. It collapses what would be separate Push + Destruction phases in TIC. Correct for primitives because primitive types have no variance (no structural components to recurse into).

**T5. Comparable (bidirectional):** If comparable[from] AND NOT comparable[to]: set comparable[to] = true. Symmetric.

**T6. Preferred (bidirectional):** If pref[from] != OPEN AND pref[to] = OPEN: set pref[to] = pref[from]. Symmetric.

### 5.2 Loop Invariant

For each group g across iterations:
- desc[g] is **monotonically non-decreasing** in L (only replaced by LCA, which is ≥ both inputs).
- anc[g] is **monotonically non-increasing** in L (only replaced by GCD, which is ≤ both inputs).
- comparable[g] is monotonically non-decreasing (false -> true, never true -> false).
- pref[g] is write-once (OPEN -> value, never changed after).

Intervals only shrink: [desc..anc] can only narrow or remain the same.

### 5.3 Termination

**Potential function:** Phi = Sum over all groups g of (rank(anc[g]) - rank(desc[g])), where rank(OPEN_anc) = 18 (maximal), rank(OPEN_desc) = 0 (minimal).

- Each productive iteration (changed = true) strictly decreases Phi by at least 1.
- Phi >= 0 (or the system has failed, i.e., desc > anc for some group).
- **Upper bound:** Phi_initial <= |G| * 18 where |G| is the number of groups.
- **Per-iteration bound:** Each iteration processes |E| edges, each capable of decreasing Phi.
- **Maximum iterations:** bounded by |E| * h where h = lattice height (7 for the longest chain U8 -> I16 -> ... -> Real -> Any). Engineering safety cap: 20.

### 5.4 Note on Satisfiability

Propagate does NOT check desc ≤ anc after each transfer step. Unsatisfiable intermediate states are harmless because:
1. LCA on an unsatisfiable pair still produces a valid ordinal (the table is total on L).
2. GCD on an incompatible pair returns OPEN (null), and the transfer rule performs no change.
3. Resolve (Phase 3) catches all unsatisfiable intervals and returns null.

This avoids branching in the inner loop.

---

## 6. Resolve Phase

After propagation reaches a fixed point, each group is resolved to a single concrete type.

### 6.1 Decision Procedure

For each group g, let d = desc[g], a = anc[g], p = pref[g], c = comparable[g].

The first matching rule wins:

| # | Condition | Result | Rationale |
|---|-----------|--------|-----------|
| 1 | d != OPEN, a != OPEN, d = a | d | Exact: interval is a singleton. |
| 2 | d != OPEN, a != OPEN, NOT Fits(d, a) | **null** (fail) | Unsatisfiable interval. |
| 3 | d != OPEN, a != OPEN, p != OPEN, Fits(d, p), Fits(p, a) | p | Preferred fits within satisfiable interval. |
| 4 | d != OPEN, a != OPEN | a | Covariant resolution: widen to ancestor. |
| 5 | a != OPEN, p != OPEN, Fits(p, a) | p | No desc; preferred fits under ancestor. |
| 6 | a != OPEN | a | No desc, no fitting preferred: use ancestor. |
| 7 | d != OPEN, p != OPEN, Fits(d, p) | p | No anc; preferred fits above desc. |
| 8 | d != OPEN | d | No anc, no fitting preferred: use desc. |
| 9 | c = true | Real | Comparable with no other constraint: Real is the default comparable type. |
| 10 | (otherwise) | Any | Fully unconstrained. |

### 6.2 Covariant Bias

When both desc and anc are present and the interval has multiple inhabitants, resolution picks the **ancestor** (widest type), not the descendant. This matches TIC's `SolveCovariant` semantics: output variables resolve to the widest type the consumer can accept.

Exception: preferred type overrides ancestor when it fits in the interval (rule 3).

### 6.3 Output Construction

For each AST node and each named variable, the resolved type is looked up via its group. If any group fails to resolve (rule 2), the entire result is null and the caller falls through to full TIC (which produces a proper error message).

For generic function calls, the resolved type of the generic group is recorded as the generic argument, packaged as a `StateRefTo` pointing to a flyweight `TicNode` singleton.

---

## 7. Unite Lemma (Variable References)

### 7.1 Statement

**Lemma.** For primitive-only expressions without variable shadowing, `Unite(var, ref)` produces the same type assignment as TIC's `SetVarRef(var, ref)` (which creates bidirectional edges + later Destruction unification).

### 7.2 Proof Sketch

1. In TIC, a variable read `x_ref` creates a `RefTo` edge. After Pull and Push, this establishes the bidirectional constraint: x_var ≤ x_ref (from Push desc) and x_ref ≤ x_var (from Pull anc + Destruction).
2. For primitive types without variance, bidirectional ≤ is equality: if a ≤ b and b ≤ a, then a = b.
3. Unite(var, ref) eagerly establishes equality (same group), which is the unique fixed point of the bidirectional constraint.
4. Therefore Unite and TIC's deferred propagation produce the same type assignment for all variable references.

### 7.3 Preconditions

- **No composite types:** Composite types have structural components where variance matters. Array(A) ≤ Array(B) requires A ≤ B (covariant), not A = B.
- **No variable shadowing:** Each variable name maps to exactly one group.
- **No mutation:** All variable uses are reads (writes go through SetDef, which uses edges, not Unite).

---

## 8. Safety Invariant

### 8.1 Statement

For all expressions e accepted by the gate:

    SPS(e) != null  implies  forall node n in e: SPS(e).type(n) = TIC(e).type(n)

That is, when SPS produces a result, it agrees with full TIC on every node's type.

### 8.2 Proof Obligations

1. **Gate completeness:** Every expression accepted by the gate is truly primitive-only. No composite type can enter the constraint system. (Argued in Section 3.3.)

2. **Build equivalence:** The constraints generated by SPS Build (Section 4) are equivalent to the constraints generated by TIC's `TicSetupVisitor` restricted to L. Specifically:
   - SPS's `Unite(var, ref)` = TIC's `SetVarRef` for primitives (Lemma, Section 7).
   - SPS's `AddEdge(expr, var)` = TIC's `SetDef` edge.
   - SPS's `SetConcrete` = TIC's `SetConst`.
   - SPS's `SetInterval` = TIC's `SetIntConst` restricted to primitive bounds.
   - SPS's `ApplyGenericConstraint + Unite` = TIC's `SetCall(PureGenericFunction)` for single-constraint generics.
   - SPS's concrete function setup = TIC's `SetCall(ConcreteFunction)`.

3. **Propagate fixed-point = TIC Pull+Push+Destruction for L:** On the primitive sublattice (no composite types), TIC's three-phase propagation (Pull constraints, Push constraints, Destruction) collapses to the transfer function in Section 5.1. T1 = Push desc. T2 = Pull anc. T3 = Destruction of concrete ancestor into constraint descendant. T4 = forward ancestor propagation (Push anc, which is redundant in full TIC for composites but valid for primitives).

4. **Resolve = TIC SolveCovariant for L:** The decision procedure in Section 6 produces the same result as `ConstraintsState.SolveCovariant()` restricted to primitive types.

### 8.3 Empirical Verification

37 differential tests: for each test, run both SPS and full TIC on the same expression and assert identical type assignments for all nodes and variables.

---

## 9. Comparison Chain Specifics

Comparison chains (`a < b == c >= d`) are syntactic sugar for multiple pairwise comparisons.

### 9.1 Operator Classification

- **Equality operators (==, !=):** Use `GenericConstrains.Any` -- no descendant, no ancestor, no comparable flag. All types can be compared for equality.
- **Comparison operators (<, >, <=, >=):** Use `GenericConstrains.Comparable` -- no descendant, no ancestor, comparable = true. Operands must resolve to a comparable type.

### 9.2 Constraint Generation

For a chain with operators [op_0, ..., op_{k-1}] and operands [o_0, ..., o_k]:

```
for i in 0..k-1:
    c = Comparable if op_i is relational, else Any
    if c.IsComparable: set comparable on operandGroup[i]
    Unite(operandGroup[i], operandGroup[i+1])
SetConcrete(resultGroup, Bool)
```

Because Unite is transitive through union-find, all operands in a chain end up in the same group. The comparable flag from any relational operator propagates to all operands.

### 9.3 Result

The chain result is always Bool (concrete). Operand type is determined by interval resolution of the unified group.

---

## 10. Pow Special Case

### 10.1 Predicate: IsPowForcedReal

`IsPowForcedReal(exponent)` = true iff the exponent is NOT a `GenericIntSyntaxNode` with a non-negative integer value.

Specifically, forced Real when:
- Exponent is not an integer literal, OR
- Exponent is an integer literal with a negative value (long < 0).

When the exponent is a non-negative integer literal, `x ** 3` is integer-preserving (repeated multiplication), so the normal PureGeneric path applies (T * T * ... * T with arithmetical constraints).

### 10.2 Forced Real Behavior

When `IsPowForcedReal` is true:
- `SetInterval(resultGroup, Real, Real, OPEN)` -- pins to exactly Real.
- `Unite(leftGroup, resultGroup)`, `Unite(rightGroup, resultGroup)` -- all operands share the Real group.
- Record generic group for result output.

This applies to both binary operator syntax (`x ** y`) and function call syntax (`pow(x, y)`).

### 10.3 Normal Path

When `IsPowForcedReal` is false: standard PureGeneric handling with the function's constraint (typically `[U24..Real]`, i.e., Arithmetical).

---

## 11. Performance Model

### 11.1 Complexity

- **Build:** O(N) where N = AST node count. Single pass, each node processed once. Union-find operations are effectively O(alpha(N)) ~ O(1) with path compression.
- **Propagate:** O(E * k) where E = edge count, k = number of iterations. k <= min(20, E * h) where h = 7 (lattice height). In practice k is typically 1-3 for simple expressions.
- **Resolve:** O(G) where G = group count. One O(1) lookup per group.
- **Total:** O(N + E * k + G). For typical expressions, E << N and G <= N, so effectively O(N).

### 11.2 Memory

- **Groups array:** G * 12 bytes (Group struct: 1 + 1 + 1 + 4 + 1 + 1 bytes, padded to 12).
- **Node-to-group mapping:** N * 4 bytes (int array).
- **Call generic groups:** N * 4 bytes (int array, sparse).
- **Variable-to-group dictionary:** O(V) where V = distinct variable count.
- **Edges array:** E * 8 bytes (two ints per edge). Initial capacity 16, doubled on overflow.

### 11.3 Cache Behavior

- LCA table: 19 * 19 = 361 bytes. GCD table: 361 bytes. Fits table: 361 bytes. Total: ~1 KB -- fits entirely in L1 cache.
- Group struct at 12 bytes means ~5 groups per cache line. Typical simple expressions have 5-20 groups, fitting in 1-4 cache lines.
- Flyweight singletons for output TicNodes: 19 syntax node singletons + 19 named node singletons + 19 RefTo arrays. Allocated once statically, reused across all SPS invocations. Zero per-call allocation for output construction.
