# Stage 2 — Implementation Plan v2: `List<T>` end-to-end (read-only)

> Parent spec: [`/Specs/Collections.md`](Collections.md)
> Prerequisite (done): Stage 1 — lattice scaffolding + unified `StateCollection`.
> v2 supersedes v1 based on two-reviewer feedback. Major changes called out at the top.
>
> **⚠ STATUS BANNER (post-ship): Stage 2 shipped in a trimmed form** — see the
> "Implementation status" table at the top of `Specs/Collections.md`. The key
> divergences from this plan:
> - 2.3's **`list<T>` / `array<T>` / `fixedArray<T>` / `enumerable<T>` type-annotation
>   syntax was skipped** (user direction). The factory `list(...)` still ships; type
>   annotations stay on `int[]` until a future stage.
> - 2.4's **`GenericArrayLiteralSyntaxNode` + `ConstraintsState.{EnumerableArgNode,
>   ConstructorBound}` fields (D1) were not introduced**. `[1,2,3]` resolves directly
>   to `StateCollection(List)` in lang-mode via the `DialectSettings.IsLangMode` flag.
>   Bare `[]` still requires annotation — deferred-inference postponed again.
> - 2.5's **LINQ migration to `Enumerable<T>` was replaced by a single TIC subtyping
>   rule `list<T> ≤ T[]`** (`Apply(StateArray, StateCollection)` in Pull/Push/Destruction
>   plus a `StateCollection(List) → StateArray` LCA arm). Existing `T[]`-keyed LINQ
>   accepts lists transparently. No per-function rewrite, no typeclass plumbing yet.
> - 2.5's **cross-mode user-function call diagnostic (D3) was not built**.
> - 2.5's **iteration via `IFunnyEnumerable`** materialised as a switch in
>   `ForExpressionNode.Calc` over `IFunnyArray` / `IFunnyList` / `IEnumerable`.
>
> The sections below describe the **original plan**. They are retained for historical
> context and as the spec for the deferred work (annotation syntax, generic literal,
> mode-leak, full Enumerable<T> tier).
>
> **⚠ NAMING NOTE (mid-Stage 2.1b refactor):** This document was written when Stage 1
> shipped three separate state classes (`StateList`, `StateFixedArray`, `StateMutableArray`).
> During Stage 2.1b they were collapsed into a single data-driven `StateCollection` class
> discriminated by `ConstructorKind`. References below to the three old class names should
> be read as "`StateCollection` with the corresponding `ConstructorKind`". E.g.
> "`StateList × StateList`" means "`StateCollection(List) × StateCollection(List)`",
> "`MergeOrNull(StateList, StateMutableArray)`" means "MergeOrNull of two StateCollections
> with different `ConstructorKind`". The plan's structural reasoning is unaffected.

## v2 changes from v1

1. **Enumerable<T> representation = full structural (TicNode inner).** v1's predicate-only (a) was rejected as a structural ceiling for user-defined generic functions. Spec the field on `ConstraintsState` upfront.
2. **`[1,2,3]` is a generic-constant (parametric literal).** v1 made the parser produce a concrete state at parse time; that creates self-contradicting cases like `a:int[] = [1,2,3]`. Instead: literal is generic, TIC resolves to annotation context. Same pattern as `42` (generic int, binds to I32/I64/Real per context).
3. **2.4 and 2.5 merge.** Independent shippability of v1's 2.4-then-2.5 is impossible — between them, `[1,2,3].count()` fails resolution. Merge.
4. **2.1 splits into 2.1a (algebra) + 2.1b (constraint stages).**
5. **Mode-leak through global function registry is addressed** — mode is captured at function-registration time; cross-mode calls produce a clean diagnostic.
6. **Effort estimate bumped to 8–11 weeks** (was 4–5).
7. **Added artifacts:** Apply-table spec, error codes, CI perf gate, doc updates, release plan, CLR round-trip path.

## Goal (unchanged from v1)

Ship `list<T>` as a first-class type with read-only API. Users can:
- Annotate variables/parameters with `list<int>`.
- Construct via `list(1, 2, 3)` factory OR (in lang-mode) bare `[1, 2, 3]` literal.
- Read by index `a[i]`.
- Iterate `for x in mylist:`.
- All existing LINQ (`count`, `contains`, `map`, `filter`, `fold`, `first`, …) works via `Enumerable<T>`.
- Convert: `T[].toList()`, `list<T>.toArray()`, `list<T>.toFixedArray()`.
- Compare by value: `[1,2,3] == list(1,2,3) → true`.

**NOT in Stage 2:** mutation (`a[i]=v`, `add`, `remove`, `clear` — Stage 3); `Set` (Stage 4); deferred empty-literal inference (Stage 5+).

## Resolved design decisions

### D1 — Enumerable<T> = full structural

`ConstraintsState` gets two new fields:

```csharp
// non-null when the constraint requires the resolved type to satisfy
// Enumerable<T>. Argument may be RefTo / Constraints / concrete.
public TicNode EnumerableArgNode { get; set; }

// Lattice upper bound for the constructor: Enumerable (loosest),
// FixedArray, Array (default for `array<T>`), List (default for `list<T>`).
public ConstructorKind? ConstructorBound { get; set; }
```

Merge rule: `MergeOrNull` of two constraints with both `EnumerableArgNode` set requires the inner nodes to merge (via existing `MergeInplace`). `ConstructorBound` intersects via `ConstructorLattice.Gcd`.

User-defined generic over Enumerable now works:
```
fun sumIt(xs:enumerable<int>): xs.fold(rule a,b = a+b)   # xs.T flows through fold's lambda signature
```

### D2 — Generic literal `[1,2,3]`

Parser emits a `GenericArrayLiteralSyntaxNode` (new node type). TIC sees:
- Element type: standard generic-integer constant (already exists).
- Constructor: starts as `ConstructorBound = Enumerable`, preferred `List` (lang-mode) / `Array` (ee-mode). 
- Resolves at annotation context:
  - `a:int[] = [1,2,3]` lang-mode → constructor narrows to `Array` (= `StateMutableArray`).
  - `a:list<int> = [1,2,3]` → narrows to `List`.
  - `a:fixedArray<int> = [1,2,3]` → narrows to `FixedArray`.
  - no annotation lang-mode → preferred `List` (resolution defaults to preferred).
  - no annotation ee-mode → preferred `Array` (existing `StateArray` covariant immutable — ee path unchanged).
- Empty literal `[]` — same shape but element node has no constraints. Without annotation context → finalize fails → clean parse error.

Parser does **not** decide between `StateList` / `StateMutableArray` / `StateFixedArray` / ee `StateArray`. TIC resolves.

### D3 — Mode-leak resolution

User functions carry a `Mode` tag at registration time. Cross-mode function calls produce a diagnostic at TIC time, NOT silent type error.

Concretely: `UserFunctionDefinitionSyntaxNode` gets a `Mode` field (`Expression | Lang`). `IFunctionRegistry.GetOrNull(name, arity)` returns the function with mode metadata. TIC's `SetCall(...)` checks compatibility:
- ee → ee, lang → lang: trivially OK.
- ee caller → lang fn: OK only if all parameters and return are mode-mode-agnostic (primitives, options, structs). Composite-collection args/returns reject with `FU8##: Cannot call lang-mode function from expression-mode (mutability semantics differ)`.
- lang caller → ee fn: same rule, reverse.

The simpler version for Stage 2: **forbid cross-mode user-function calls entirely** until Stage 5+ matures the policy. Clean diagnostic, no soundness hole.

### D4 — Iteration via `Enumerable<T>`

New runtime interface `IFunnyEnumerable` satisfied by `IFunnyArray`, `IFunnyList<T>` (and Stage 4 `IFunnySet<T>`). `for x in coll:` runtime extracts iterator via `IFunnyEnumerable`. TIC: collection child must satisfy `EnumerableArgNode` constraint.

### D5 — `list<T>` value equality

Per Stage 0 spec: element-wise + length match. Implement in `FunnyEnumerableEquality` static class — applies to all enumerable types. Cross-construct (`list<int> == array<int>`) uses the same helper.

### D6 — Lang-mode `int[]` semantics in Stage 2

Type alias: lang-mode `int[]` = `array<int>` = `StateMutableArray<I32>`. Stage 2 ships READ-ONLY for this state — no `a[i] = v` parsing yet (Stage 3). Indexed read `a[i]` works. `a:int[] = [1,2,3]` works because literal is generic (D2).

This is consistent: same state class, just one capability (mutation) deferred to Stage 3.

## Sub-stages

| # | Title | Effort | User-visible after this stage |
|---|---|---|---|
| 2.1a | TIC algebra: MergeOrNull / LCA / GCD for new states | 5–7 days | nothing |
| 2.1b | TIC constraint stages: Pull / Push / Destruction / Finalize Apply table | 7–10 days | nothing |
| 2.2 | BaseFunnyType.List + runtime IFunnyList<T> + IFunnyEnumerable + visitor | 7–10 days | nothing (CLR types exist; no syntax yet) |
| 2.3 | Parser: `list<T>` / `array<T>` / `fixedArray<T>` / `enumerable<T>` syntax + factories | 7–10 days | `list(1,2,3)`, `list<int>` annotations work |
| 2.4 | Generic literal `[1,2,3]` + ConstraintsState fields D1 + mode-leak D3 | 7–10 days | bare `[1,2,3]` infers correctly; cross-mode rejected |
| 2.5 | LINQ migration to Enumerable + iteration + conversions + equality | 10–14 days | full feature; all `Stage2_*` contract tests green |

**Total: 8–11 weeks** (realistic, post-review).

### 2.1a — TIC algebra (5–7 days)

**Files:**
- `src/NFun/Tic/SolvingFunctions.cs` — `MergeOrNull` cases for `StateList × StateList`, `StateFixedArray × StateFixedArray`, `StateMutableArray × StateMutableArray`. Cross-class returns null per uniform-invariance rule (existing GetLastCommonAncestorOrNull already does Any; merge fails to null).
- `src/NFun/Tic/SolvingFunctions.cs` `GetMergedStateOrNull` — add cycle-guarded recursion through new composites.
- `Specs/Tic/CompositeApplyTable.md` (new) — full N × M table of state-pair operations.

**Tests:**
- `NFun.Tic.Tests/Collections/MergeOrNullTest.cs` — ~12 cases per new state class.

**Acceptance:** 14088 baseline tests green + new unit tests pass. **No constraint stages touched yet.**

### 2.1b — TIC constraint stages (7–10 days)

**Files:**
- `src/NFun/Tic/Stages/PullConstraintsFunctions.cs` — Apply overloads for `(NewState × Primitive)`, `(NewState × ConstraintsState)`, `(NewState × RefTo)`, `(NewState × NewState)`, `(NewState × Optional)`. Estimated ~30 new overloads.
- `src/NFun/Tic/Stages/PushConstraintsFunctions.cs` — same coverage.
- `src/NFun/Tic/Stages/DestructionFunctions.cs` — destruction recursion.
- `src/NFun/Tic/Stages/StagesExtension.cs` — if cross-state coercion rules need extension.
- `src/NFun/TypeInferenceAdapter/TicTypesConverter.cs` — `ToConcrete(StateList | StateFixedArray | StateMutableArray)`.

**Tests:**
- `NFun.Tic.Tests/Collections/PullPushTest.cs` — Pull-then-Push round-trip on graphs with new states.
- `NFun.Tic.Tests/Collections/DestructionTest.cs` — destruction of `StateList<ConstraintsState>` nests.

**Acceptance:** TIC unit tests cover the Apply table. Existing tests green.

### 2.2 — Runtime types (7–10 days)

**Files:**
- `src/NFun/Types/BaseFunnyType.cs` — add `List = 22` (next ordinal). Keep ee-mode untouched (`ArrayOf` remains separate).
- `src/NFun/Types/FunnyType.cs` — `FunnyType.List(elementType)` factory + `IsList` + `ListTypeSpecification`.
- `src/NFun/Types/BaseFunnyTypeExtensions.cs` (new) — `IsCollection()`, `IsMutableCollection()` predicates. Future-proofs external switches.
- `src/NFun/Types/IBaseFunnyTypeVisitor.cs` (new) — visitor for exhaustive type dispatch. Encouraged for new external code.
- `src/NFun/Runtime/Lists/IFunnyList.cs` — read-only interface.
- `src/NFun/Runtime/Lists/IFunnyEnumerable.cs` — common iteration interface for arrays + lists + future sets.
- `src/NFun/Runtime/Lists/MutableFunnyList.cs` — `System.Collections.Generic.List<T>` backing.
- `src/NFun/Runtime/Lists/FunnyEnumerableEquality.cs` — by-value helper.
- `src/NFun/TypeInferenceAdapter/TicTypesConverter.cs` — `ToConcrete(List)`.
- `src/NFun/Types/FunnyConverter.cs` (and siblings) — CLR `List<T>` ↔ `MutableFunnyList`. Both directions.
- **External switch sweep** — find every `switch (baseType)` site, add `case List` branch (initially `throw NotSupportedException` for sites not yet migrated). Estimate: 63 sites per pragmatist's grep.

**Sonica integration gate:** before merging 2.2, run Sonica's test suite. Expect `BaseFunnyType.List` to throw `NotSupportedException` in Sonica's `SonicaFunnyHelper.cs` — add `IsCollection()` migration to Sonica side. Document in release notes.

**Tests:**
- `NFun.SyntaxTests/Lang/Collections/RuntimeListTest.cs` — construct, iterate, equals, count via CLR.
- Switch-sweep verification: build with `-warnaserror CS8524` (exhaustive switch warnings).

**Acceptance:** all 63+ switch sites updated. Sonica suite confirmed compatible.

### 2.3 — Parser surface (7–10 days)

**Files:**
- `src/NFun/Tokenization/TokFlow.ReadTypeSyntax` — recognise `list<T>`, `array<T>`, `fixedArray<T>`, `enumerable<T>`. Reserve as type-only keywords; `list` as identifier (factory) is still accepted but lang-mode rejects it in non-factory positions.
- `src/NFun/SyntaxParsing/TypeSyntax.cs` — extend AST.
- `src/NFun/SyntaxParsing/TypeSyntaxResolver.cs` — resolve to `FunnyType.List(...)` / `FunnyType.MutableArray(...)` etc.
- `src/NFun/BaseFunctions.cs` — register `ListFactoryFunction`, `FixedArrayFactoryFunction`. `array(...)` factory also for explicit mutable-array construction in lang.
- `src/NFun/Functions/Collections/ListFactoryFunction.cs`
- `src/NFun/Functions/Collections/FixedArrayFactoryFunction.cs`
- `src/NFun/Functions/Collections/ArrayFactoryFunction.cs` (lang mutable-array factory)
- `src/NFun/Interpretation/Nodes/ListFactoryNode.cs` (runtime).
- `src/NFun/ParseErrors/Errors.*.cs` — new error codes:
  - `FU8##`: empty literal `[]` requires type annotation.
  - `FU8##`: cross-mode user function call rejected.

**Identifier clash check:** grep `list\(`, `fixedArray\(`, `array\(`, `enumerable\(` in test corpus and examples. Update any user code that uses these as identifiers.

**Tests:**
- `NFun.SyntaxTests/Lang/Collections/ListFactoryTest.cs` — 20+ syntax tests.
- `Stage2_ListFactory_ProducesList`, `Stage2_FixedArrayFactory_ProducesFixedArray`, `Stage2_ListAnnotation_BindsLiteralAsList` un-ignored.

**Acceptance:** new syntax tests green. Existing tests green.

### 2.4 — Generic literal + ConstraintsState fields + mode-leak (7–10 days)

**Files:**
- `src/NFun/SyntaxParsing/SyntaxNodes/GenericArrayLiteralSyntaxNode.cs` (new). Replaces `ArrayInitSyntaxNode` for bare `[1,2,3]` literals.
- `src/NFun/Tic/SolvingStates/ConstraintsState.cs` — add `EnumerableArgNode` + `ConstructorBound` fields per D1. Update `MergeOrNull` to intersect these.
- `src/NFun/TypeInferenceAdapter/TicSetupVisitor.cs` `Visit(GenericArrayLiteralSyntaxNode)` — set up `ConstructorBound = Enumerable` + `EnumerableArgNode = elementNode` + Preferred = `List` (lang) / `Array` (ee).
- `src/NFun/SyntaxParsing/SyntaxNodes/UserFunctionDefinitionSyntaxNode.cs` — add `Mode` field. Set during parse.
- `src/NFun/Interpretation/Functions/IFunctionRegistry.cs` — function lookup returns mode metadata.
- TIC `SetCall(...)` — enforce mode-compatibility per D3.

**Backward-compat behavior:** during the literal-default migration, the lang-mode test corpus needs auditing. Per pragmatist's grep: 33 hits on `BuildLang.*\[` across 19+ test files. Each must be re-asserted: explicit type assertions checked, type-sensitive expressions (narrowing tests) checked.

**Tests:**
- `Stage2_LangMode_ListLiteralWithoutAnnotation_IsList` un-ignored.
- `Stage2_MixedNumericElements_PromotesToReal` un-ignored.
- `Stage2_EmptyLiteral_WithListAnnotation_ZeroLength` un-ignored.
- `Stage2_EmptyLiteral_WithoutAnnotation_ParseError` un-ignored.
- Cross-mode rejection tests (3-4 new).

**Acceptance:** literal default works, mode-leak diagnostic clean, ~33 lang-mode tests updated and reasoned about.

### 2.5 — LINQ migration + iteration + conversions (10–14 days)

**Files:**
- `src/NFun/Functions/ArrayGenericFunctions.cs` — migrate signatures from `T[]` to `Enumerable<T>`. ~17 functions in this file alone.
- `src/NFun/Functions/Collections/` — new conversion functions: `ToListFunction`, `ToArrayFunction`, `ToFixedArrayFunction`.
- For each migrated function: runtime now dispatches via `IFunnyEnumerable`.
- Iteration: `ForExpressionNode` extracts iterator via `IFunnyEnumerable`.
- `src/NFun/Functions/Operators/EqualityFunction.cs` — extend to compare via `FunnyEnumerableEquality`.

**Commit strategy:** one function family per commit. Family = small group (e.g. `count`/`contains`/`isEmpty` together; `map`/`filter`/`fold` together). Each commit:
1. Migrate signatures.
2. Add new tests for both Array AND List paths.
3. All existing tests still green.

Estimated 8-10 commits within 2.5.

**Tests:**
- ALL remaining `Stage2_*` contract tests un-ignored (18 total → 0 ignored).
- ~80 new acceptance tests per family.

**Acceptance gate Stage 2 overall:**
- All 18 `Stage2_*` contract tests un-ignored and green.
- 14088 baseline tests stay green.
- ~230 new tests added.
- **QuickBench Simple Build delta ≤ +5% measured AND committed** (gate enforced via CI script — see Risks).
- `Specs/Collections.md` updated. `Specs/Arrays.md`, `Specs/Statements.md`, `Specs/Functions.md` updated.
- `CLAUDE.md` "Accepted Design" entries for D1, D3.

## Open questions remaining (Stage 2 ships even if these are not fully decided)

1. **User-defined polymorphic functions over Enumerable.** D1's structural representation allows `fun mySumIt(xs:enumerable<int>) = xs.fold(...)`. But what about `fun myCount(xs)` (no annotation)? Should inference default to `Enumerable<T>` (most general) or `T[]` (backward-compat)? Recommendation: `Enumerable<T>` (per pragmatist). Backward-compat preserved at usage sites — call to `myCount(array_input)` works because `array<int>` ≤ `enumerable<int>` per lattice.

2. **list(...)` factory in ee-mode.** Open ambiguity since Stage 0 (`Ambiguity_EeMode_ListFactory_Rejected` contract test). Recommendation for Stage 2: reject (clean error). Stage 5+ can relax if user demand emerges.

3. **Cross-class `==` semantics.** Specced as element-wise + length match via `FunnyEnumerableEquality`. Edge case: comparing `list<int>` to `set<int>` (Stage 4) — different element semantics (order vs no-order). Defer to Stage 4 spec.

## Risks (Stage 2-specific, updated)

1. **Test churn in 2.4.** ~33 lang-mode tests need updating. Mitigation: pre-flight `BuildLang.*\[` grep at start of 2.4; budget 1 day for test audit.

2. **External Sonica break in 2.2.** New `BaseFunnyType.List` value. Mitigation: `BaseFunnyTypeExtensions.IsCollection()` + visitor pattern. Sonica suite run as 2.2 acceptance gate.

3. **Confluence preservation.** New Apply table. Mitigation: full table spec in `Specs/Tic/CompositeApplyTable.md` written BEFORE 2.1b coding starts.

4. **Performance regression.** Mitigation: **CI gate** — add `dotnet test src/Benchmarks/QuickBench/...` to CI pipeline; hard fail at >+5% Simple Build delta. Owner: each sub-stage PR.

5. **Cross-mode user-function call complexity.** Mitigation: D3 ships with simple "forbid cross-mode" diagnostic. Refine policy in Stage 5+ if user demand emerges.

6. **LINQ function discovery.** Plan assumes `ArrayGenericFunctions.cs` is the canonical site. Verify: grep for all `: GenericFunctionBase` in src/NFun/Functions. Pragmatist confirmed 17 in that file; sibling files may host more.

## Release plan

- 2.1a / 2.1b / 2.2 / 2.3 land as separate commits on the `mutable-collections` branch.
- 2.4 lands as one commit (single-PR feature change).
- 2.5 lands as 8–10 commits (per LINQ family).
- After 2.5 + acceptance gates → merge `mutable-collections` to `master`.
- Release: **NuGet 1.2.0** with breaking-change note for lang-mode literal defaults. ee-mode unchanged. CHANGELOG entry. CLI examples updated.

## Documentation deliverables

- `Specs/Collections.md` — update factory examples, equality section, iteration semantics.
- `Specs/Arrays.md` — note lang-mode mutability split + new factory syntax.
- `Specs/Statements.md` — update iteration examples.
- `Specs/Functions.md` — note Enumerable-constrained LINQ signatures.
- `Specs/Tic/CompositeApplyTable.md` — new file, full table.
- `CLAUDE.md` — Accepted Design entries for D1, D3, D6.
- README: brief Stage 2 mention.
- ConsoleAppExample: 3–5 new lang-mode examples using lists.

## Test budget (revised)

- TIC algebra (2.1a): ~30 new unit tests.
- TIC stages (2.1b): ~40 new unit tests.
- Runtime (2.2): ~25 new tests.
- Parser (2.3): ~30 new tests.
- Generic literal + mode-leak (2.4): ~25 new tests + ~33 updates.
- LINQ + conversions (2.5): ~80 new tests + 18 contract un-ignored.

**Total: ~230 new + ~33 updated + 18 un-ignored ≈ 280 test deltas.**

## Acceptance gate for Stage 2 overall

- All 14088 Stage 1 tests stay green.
- All 18 `Stage2_*` contract tests un-ignored and green.
- ~230 new tests added across sub-stages.
- QuickBench Simple Build delta ≤ +5% (CI-enforced).
- All Spec docs updated.
- CLAUDE.md "Accepted Design" entries for D1, D3, D6.
- Sonica integration suite passes with new `BaseFunnyType.List` handling.
- No new `// WORKAROUND:` markers (per CLAUDE.md anti-debt policy).
