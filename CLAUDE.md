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
- **Current debt registry**: `Specs/Tic/TicTechnicalDebt.md` — algorithm-layer legacy (#5-#10) + open findings of the 2026-07 TIC review (#22, #25-#29, #32-#34). Algebra items #12-#18, #20, #30-#31, spec-drift #21 and safety items #23/#24 are closed; #19 is partially closed — the ↓-part is done (resolution arms extracted to `ConcretestSnapshot`, ↓ₛ), the ⊓ solved-composite-collapse remainder is tracked (#19/#22); #25 is narrowed — the assert attempt REFUTED Лемма 1's corollary on live scripts (⊓-null on non-optional constraint edges is reachable; TraceLog signal, real fix = Pull-contribution completeness). Closed entries are removed from the file (history lives in git).

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

Full registry: `Specs/Tic/TicTechnicalDebt.md`. Each item must have: what's wrong,
why it exists, what the clean fix is. Headline status (2026-07 algebra review):

- **Two-layer target architecture** for TIC is normative (`Specs/Tic/Algebra.md`):
  pure/total ALGEBRA (LCA, GCD, Merge, Fit, ≤c, Concretest, Abstractest, Simplify)
  vs RESOLUTION (Solve*, `ConcretestSnapshot` ↓ₛ, Preferred policy, materialization).
  Algebra items #12-#18 are closed; #19 is partially closed (↓ is a pure projection,
  resolution arms live in ↓ₛ; remainder — the Sat-changing solved-composite collapse
  inside ⊓, tracked with #22). Per-file "Отклонения" sections in Algebra*-specs record
  the closures.
- **Code-level defects found by the review — all fixed**: GCD non-commutativity on the
  C fragment (#17), Fit comparable-cell unsoundness (#15), three diverging ⊓
  implementations (#13), non-commutative Preferred tie-breaks (#14).
- **Safety pass 2026-07-10**: #23 closed (DEBUG panic on SCC-fixpoint cap), #24 closed
  (Push fun-args flipped to contravariant; COMPENSATED verdict refuted — the cell IS
  reachable via if-else LCA of lambdas + call; plus `NoEffectiveConstrains`: an Any upper
  bound is vacuous, `Sat([∅..Any]) = Sat([∅..∅])`, applied in all Transform*OrNull),
  #20 closed (↓/↑ monotonicity: verified on T, REFUTED-pinned on C under ≤=Fit),
  #25 narrowed (assert attempt REFUTED Лемма 1's corollary on 6 live scripts —
  ⊓-null on non-optional constraint edges is reachable; TraceLog signal for now).
- **Open**: algorithm-layer legacy #5-#10, #22, #25-#29 and #32-#34 (see registry).

### Accepted Design (not debt)

- **`IsOptionalElement` flag** — compensates for lack of parent references on TicNode. Used in 4 decision points. Alternative (parent tracking) adds memory/complexity for minimal benefit.
- **`IsSignatureParam` flag** — marks composite param nodes from function signatures as shape-rigid. Prevents Optional wrapping (LCA widening) of function params: `Opt(T[]) ≤ T[]` is invalid because it would change the function contract. 1 decision point (WrapAncestorInOptional), set in 2 places (SetCallArgument, SetCall(StateFun)). Analogous to rigid/skolem type variables in HM — shape is given, components are flexible.
- **`FlattenNestedOptional`** — reactive flatten of `opt(opt(T))` in State setter + Destruction + Finalize. Correct approach for deferred constraint resolution (element state changes asynchronously through propagation).
- **`SetCoalesce` TIC special form** — `??` operator uses `SetCoalesce(left, right, result)` instead of a generic function. Creates fresh node U, constrains `left ≤ opt(U)`, `U →c result`, `right →c result`. Result = LCA(U, right). Supports optional right operand: `int? ?? int?` = `int?`. Same pattern as `?.` and `?[` — operators with Optional-specific semantics get TIC special forms rather than generic function signatures.
- **FU711 `ValidateGenericResolution`** — in ExpressionBuilderVisitor, rejects generic T=Any when T appears at different structural depths in input args (e.g., `'h' in 'hello'` where T must be both `char[]` and `char`). Lives in ExpressionBuilder (not TIC) because TIC constraints don't carry structural depth info — the check requires function signature metadata. TIC-level fix attempted and rejected: breaks 21 legitimate tests (heterogeneous arrays, optional LCA).
- **Floats-constraint dialect pin at TIC setup** — in `TicSetupVisitor.FinishBinOp` / `Visit(FunCallSyntaxNode)`, when a `PureGenericFunctionBase` has `Constrains[0] = [F32..Real]` (Floats) and the dialect has `FloatFamilySupport = None`, the fresh generic T is initialized as `[Real..Real]` instead of the full interval. Same if-branch pattern as real-literal dispatch (`TypeBehaviour.RealLiteralIsGeneric`). Under F32F64 mode the full [F32..Real] survives, and TicTypesConverter picks Real via ancestor. Restores pre-Phase-5 concrete-Real semantics for `/`, `sqrt`, `sin`, etc. — required because outputs bound to a linked CS node aren't resolved by SolveUselessGenerics (they're skipped as polymorphic signatures).
