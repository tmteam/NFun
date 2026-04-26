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
- **Current workarounds**: 1 (DescendantHasOptionalLift — stale Pull snapshots). See TicTechnicalDebt.md #5.

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

*(No current items — all resolved.)*

### Accepted Design (not debt)

- **`IsOptionalElement` flag** — compensates for lack of parent references on TicNode. Used in 4 decision points. Alternative (parent tracking) adds memory/complexity for minimal benefit.
- **`IsSignatureParam` flag** — marks composite param nodes from function signatures as shape-rigid. Prevents Optional wrapping (LCA widening) of function params: `Opt(T[]) ≤ T[]` is invalid because it would change the function contract. 1 decision point (WrapAncestorInOptional), set in 2 places (SetCallArgument, SetCall(StateFun)). Analogous to rigid/skolem type variables in HM — shape is given, components are flexible.
- **`FlattenNestedOptional`** — reactive flatten of `opt(opt(T))` in State setter + Destruction + Finalize. Correct approach for deferred constraint resolution (element state changes asynchronously through propagation).
- **`SetCoalesce` TIC special form** — `??` operator uses `SetCoalesce(left, right, result)` instead of a generic function. Creates fresh node U, constrains `left ≤ opt(U)`, `U →c result`, `right →c result`. Result = LCA(U, right). Supports optional right operand: `int? ?? int?` = `int?`. Same pattern as `?.` and `?[` — operators with Optional-specific semantics get TIC special forms rather than generic function signatures.
- **FU711 `ValidateGenericResolution`** — in ExpressionBuilderVisitor, rejects generic T=Any when T appears at different structural depths in input args (e.g., `'h' in 'hello'` where T must be both `char[]` and `char`). Lives in ExpressionBuilder (not TIC) because TIC constraints don't carry structural depth info — the check requires function signature metadata. TIC-level fix attempted and rejected: breaks 21 legitimate tests (heterogeneous arrays, optional LCA).
