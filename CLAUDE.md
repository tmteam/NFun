# NFun — Claude Code Instructions

## TIC Solver: Design Principles

TIC (Type Inference Constraint) is the core algorithm of NFun. It must meet the same standard as Red-Black Trees or Rust's Borrow Checker: **formally describable, provably correct, no special cases.**

### 1. Formalism First

Every TIC operation must follow from the type algebra. Before writing code, answer:
- What is the algebraic rule? (e.g., `T ≤ opt(T)`, `LCA(A,B) = C`)
- Does it hold for ALL type combinations, not just the failing test case?
- Can the rule be stated in one sentence without referencing implementation details?

If you can't state the rule — you don't understand the fix. Stop and think.

### 2. No Workarounds

- No `if (specificType) then specialBehavior` — every Apply/Merge overload must handle ALL inputs of its declared types
- No flags that exist to patch one scenario (`IsOptionalElement` is a known debt — track it, don't add more)
- No post-hoc fixups in expression builder for what TIC should have resolved
- If a workaround is ever introduced, it MUST have a `// WORKAROUND:` comment explaining the root cause and the proper fix. Track in `Specs/Tic/TicTechnicalDebt.md`.
- **Current workarounds**:
  1. **DescendantHasOptionalLift** — stale Pull snapshots. TicTechnicalDebt.md #5.
  2. **`list ↔ array` asymmetric runtime cast** vs one-way TIC subtyping. TicTechnicalDebt.md #11.
  3. **Composite-type default values asymmetric** across `List`/`Optional`/`Struct`/`Custom`. TicTechnicalDebt.md #12. Blocked on Stage 3 mutable-struct design.
  4. **`TestHelper.AreSame` permissive cross-kind comparison** masks container-kind regressions. TicTechnicalDebt.md #13.
  5. **`Transform*OrNull` element-node reuse + identity guards in `Apply(ICompositeState, CS)`** — descendant collection's element aliased ancestor's during chained `[]` over lang collections. Guards mirror existing line-430 pattern; proper fix is fresh-node allocation in Transform*. TicTechnicalDebt.md #15.
  6. **Try-merge-fallback-to-AddAncestor in CompCs cross-Apply** — Stage 5 widened LINQ `map` to `Enumerable<T0>` so Map<K,V> can be passed directly. MergeInplace fails on unresolved composite element shapes (function types); fallback path loses preferred-type propagation precision. Affects 4 ee-mode tests (closure-in-array, all `[Ignore]`) AND 1 lang-mode test (nested byte→real upcast through `.map().sum()`). TicTechnicalDebt.md #16.
  7. **`Visit(BlockSyntaxNode)` skip-block-bind for assignment statements** — `TicSetupVisitor:2046-2069` detects `IndexedAssignmentSyntaxNode` / `FieldAssignmentSyntaxNode` (and `EquationSyntaxNode` wrapping them) and does NOT add them as block ancestor. These are void/side-effecting statements (NFun parser rejects expression-position assignment); binding them grafts implicit-None V0 onto the assignment's target via the back-edge `targetRef.AddAncestor(resultNode)`, producing a self-Optional cycle in Destruction (was the root of #131 `Pair_ClearThenWrite`).
  8. **Amadio-Cardelli contractivity guard in `DestructionFunctions.cs:78`** — mirrors the existing Pull/Push guard at `StagesExtension:185-186`. Skip wrap if `descendantNode.GetNonReference() == ancestorNode` (the lift `T ≤ opt(T)` is coinductively trivial). Belt-and-suspenders against latent μX.opt(X) wraps in Destruction-time Optional lifts.

### 3. Performance Matters

- QuickBench on MacBook gives stable measurements — use it for before/after comparison
- Simple expressions must not regress. Named types overhead must be near-zero for code that doesn't use them
- Avoid O(n²) patterns in constraint propagation. TIC processes thousands of nodes per expression
- Prefer mutation over allocation in hot paths (reuse nodes, avoid LINQ in solving loops)

### 4. Test Pyramid

- **Unit tests** for pure algebra: `IntersectIntervals`, `IntervalIsNonEmpty`, `SolveCovariant`, `SolveContravariant`, `MergeOrNull`, `Concretest`, `LCA`, `GCD`
- **TIC-level tests** for constraint graphs: `GraphBuilder` → `Solve` → assert node types
- **Syntax-level tests** for end-to-end: expression string → expected output value
- Every bug fix must include a test at the LOWEST possible level

### 5. Debugging Methodology

1. Определи примерную причину
2. Простейший тест на TIC builder
3. Если это алгебра или Pull/Push — unit test на TIC
4. Фиксим тесты снизу вверх (от простейшего к сложному)

## Build & Test

```bash
# Build
dotnet build src/NFun/NFun.csproj -p:SignAssembly=false -f net6.0

# Test (SNK key missing — always use SignAssembly=false)
dotnet test src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false
dotnet test src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false
dotnet test src/Tests/Nfun.UnitTests/Nfun.UnitTests.csproj -p:SignAssembly=false
dotnet test src/Tests/NFun.ApiTests/NFun.ApiTests.csproj -p:SignAssembly=false

# Quick expression check
dotnet run --project src/ConsoleAppExample/ConsoleAppExample.csproj -p:SignAssembly=false -- -e "expression"

# Performance benchmark (paired ratio, drift-resistant)
# src/Benchmarks/QuickBench/ — compare HEAD vs master
```

## Known Technical Debt

Track here. Each item must have: what's wrong, why it exists, what the clean fix is.

- **Stage 2 dispatch path for `Enumerable<T>` typeclass.** Stage 1 added the `ConstructorLattice` but did not decide how `count<T>(xs: Enumerable<T>): int` is registered in `IFunctionRegistry` when multiple concrete impls (Array, List, Set, …) satisfy the constraint. Current registry keys by `(name, arity)`; no runtime-type dispatch exists. Stage 2 must pick: N concrete overloads with constraint-discriminator OR add a per-arg dispatch table. Pinned via `MutableCollectionsContractTest` ambiguity markers. Spec: `Specs/Collections.md` §LINQ via typeclasses.
- **Liskov direction at parameter position for new collections.** With uniform invariance + lattice subtype (`List ⊆ Array ⊆ FixedArray ⊆ Enumerable`), passing `list<int>` where `array<int>` is expected is contested. Spec hierarchy says yes (Liskov upcast through subtype edges); invariance suggests no. Stage 2 decision required. Pinned via `Ambiguity_ListPassedWhereArrayExpected_Accepted` and `Ambiguity_ArrayPassedWhereListExpected_Rejected` in `MutableCollectionsContractTest.cs`.

### Accepted Design (not debt)

- **`IsOptionalElement` flag** — compensates for lack of parent references on TicNode. Used in 4 decision points. Alternative (parent tracking) adds memory/complexity for minimal benefit.
- **`IsSignatureParam` flag** — marks composite param nodes from function signatures as shape-rigid. Prevents Optional wrapping (LCA widening) of function params: `Opt(T[]) ≤ T[]` is invalid because it would change the function contract. 1 decision point (WrapAncestorInOptional), set in 2 places (SetCallArgument, SetCall(StateFun)). Analogous to rigid/skolem type variables in HM — shape is given, components are flexible.
- **`FlattenNestedOptional`** — reactive flatten of `opt(opt(T))` in State setter + Destruction + Finalize. Correct approach for deferred constraint resolution (element state changes asynchronously through propagation).
- **`SetCoalesce` TIC special form** — `??` operator uses `SetCoalesce(left, right, result)` instead of a generic function. Creates fresh node U, constrains `left ≤ opt(U)`, `U →c result`, `right →c result`. Result = LCA(U, right). Supports optional right operand: `int? ?? int?` = `int?`. Same pattern as `?.` and `?[` — operators with Optional-specific semantics get TIC special forms rather than generic function signatures.
- **FU711 `ValidateGenericResolution`** — in ExpressionBuilderVisitor, rejects generic T=Any when T appears at different structural depths in input args (e.g., `'h' in 'hello'` where T must be both `char[]` and `char`). Lives in ExpressionBuilder (not TIC) because TIC constraints don't carry structural depth info — the check requires function signature metadata. TIC-level fix attempted and rejected: breaks 21 legitimate tests (heterogeneous arrays, optional LCA).
- **`ConstructorLattice` + unified `StateCollection`** — Stage 1 of mutable collections introduces `StateComposite` (abstract base) + `ConstructorLattice` (LCA/GCD on ConstructorKind). Single-arg collections (List, FixedArray, Array, Set, future Queue/Stack) all share the unified `StateCollection` class with `ConstructorKind` carried as DATA — not separate C# subclasses. Two-arg collections (Map) will get their own future class because their shape differs structurally. Legacy `StateArray` (ee-mode covariant immutable), `StateFun`, `StateStruct` deliberately stay outside the new infrastructure — their internals (covariant element / arg+ret split / named-field dict) don't benefit from the uniform single-arg invariant shape. Cross-kind LCA collapses to `Any` by uniform-invariance rule. See `Specs/Tic/ConstructorLattice.md` and `Specs/Collections.md` §Scope of the refactor.
- **Default value `[]` for declared collections** — `a:list<int>` (and future `array<int>` / `set<int>` / `map<K,V>`) without initializer defaults to the empty collection of its constructor. Already implemented for `List` in `IFunnyVar.GetDefaultValueOrNullFor`. Same rule applies to future composites — implemented per-constructor until debt #12 (unified default-value protocol) lands.
- **Iteration mutation = runtime error** — `for x in a: a.add(y)` throws `FunnyRuntimeException("collection modified during iteration")`. Backing `System.Collections.Generic.List<T>` already enforces this; we surface it as a NFun-typed exception. Snapshot semantics rejected: hides bugs and costs memory. Explicit `for x in a.toList(): a.add(x)` is the documented escape hatch.
- **Immutable typeclass for Set/Map keys** (renamed from Hashable to capture the fundamental property — hashing follows from immutability). **Initial scope: primitives only** (`bool`, `U8..I64`, `Real`, `char`, `text`, `IPAddress`); check happens at the call site of `set(...)` / `__mkMap(...)` in `ImmutableTypePredicate.RequireImmutable` and surfaces as FU580. **Recursive extension** to `FixedArray<T>` (iff T Immutable), `Fun(...)` (identity hash), `Optional<T>` (iff T Immutable), future frozen-struct — tracked in [issue #129](https://github.com/tmteam/NFun/issues/129). Mutable composites (List, Array-lang, Set, Map, mutable struct) are NEVER Immutable regardless of inner. Convert path: `.toFixedArray()` (when recursive predicate lands).
- **Struct and indexed assignment stay separate** — `s.field = v` (statically-resolved field name) and `a[i] = v` (generic int index) keep dedicated parser + runtime nodes (`FieldAssignExpressionNode` / `IndexedAssignExpressionNode`). No unified `IndexedMutable<K,V>` typeclass spanning both — fields are compile-time strings, indices are runtime ints; conflating them muddies the type system. The collection-only `IndexedMutable<K,V>` typeclass arrives in Stage 4+ and covers Array/List (and Map's internal helpers); Struct does not participate.
- **Map is method-only, no `[...]` syntax** — neither `m[k]` read nor `m[k] = v` write. Map operations via `.get(k)`, `.getOrOops(k)`, `.set(k, v)`, `.remove(k)`, `.contains(k)`, `.keys()`, `.values()`. Rationale: keys can be any Hashable type (text, named-struct, …); methods read more clearly than brackets; parser stays simple (`[...]` is unambiguously array/list indexing).
