# Plan: Eliminate `RecBound` from `ConstraintsState`

**Branch**: `fix-mu-identity-userfn`
**Related**: GH #127 (this plan), GH #128 (C/D/E/G symptoms), task #97 (runtime substitution)
**Status**: scoped, not started

## Why

Two problems converge on the same place:

1. **Architectural** (#127): `RecBound` is a vestigial wrapper around `StateStruct`. `RecBoundKind` enum lists 4 forms but only `StructShape` is reachable. `Binders` always empty. `IsClosed` always true. The slot sits on `ConstraintsState` which already carries 6 dimensions — `RecBound` is a 7th that conceptually does not belong (CS = subtype-interval; F-bound = structural predicate, different *kind* of constraint).

2. **Symptomatic** (#128): μ-identity is lost through user-function boundary. `LangTiHelper.ResolveNamedStruct` builds a graph cycle but never stamps `TypeName` on the root `StateStruct`. The `PropagateTypeNamesAcrossSnapshots` post-pass exists (`SolvingFunctions.cs:1195`) but is gated on `anyOptSourced` which is false for declared `:t?` annotations (the flag is set only by `?.` operator). Result: user-fn signatures materialize as finite depth-2 unfolding `{v; next:{v; next:Any}?}` instead of `μX. t`.

Both are addressed by the same conceptual move: **make μ-recursion a property of the graph (true cycle via TicNode.State references), not a slot on CS**.

## Surface map

`StructBound`/`RecursiveBound` references — 145 across 12 files (`grep -c StructBound\|RecBound src/NFun/`):

| File | Role |
|---|---|
| `Tic/SolvingStates/RecBound.cs` | ADT class to delete |
| `Tic/SolvingStates/ConstraintsState.cs` | slot getter/setter (line 46-55), `NoConstrains`, copy semantics |
| `Tic/SolvingStates/StateStruct.cs` | `TypeName` (kept), `MergedTypeName`/`LcaTypeName` (kept) |
| `Tic/SolvingFunctions.cs` | LiftMuTypes, cycle-rescue, Apply, Merge, PropagateTypeNamesAcrossSnapshots |
| `Tic/Stages/PullConstraintsFunctions.cs` | StructBound checks in Pull dispatch |
| `Tic/Stages/PushConstraintsFunctions.cs` | StructBound checks in Push dispatch |
| `Tic/GraphBuilder.cs` | `SignatureHasRecursiveShape` / `NodeHasRecursiveShape` |
| `Tic/TicNode.cs` | IsContractiveCycleHead flag (kept, decouple from CS) |
| `TypeInferenceAdapter/TicSetupVisitor.cs` | reads when materializing constraint state |
| `TypeInferenceAdapter/TicTypesConverter.cs` | `BuildStructBoundFunnyType`, `BuildFieldType` reading bound back-edges |
| `Interpretation/Functions/GenericConstrains.cs` | runtime mirror of the F-bound carrying NamedStructOf |
| `Interpretation/Functions/UserFunctions/GenericUserFunction.cs` | per-call resolution of structurally-bound generic |
| `Interpretation/RuntimeBuilder.cs` | FreezeFunctionSignatureStructs, signature classification |

## End-state (target)

- `RecBound`, `RecBoundKind`, `ConstraintsState.RecursiveBound`, `ConstraintsState.StructBound`, `ConstraintsState.GcdBound`, `ConstraintsState.RewireStructBoundOwnership` — deleted.
- μ-recursion expressed solely as `TicNode A.State = StateStruct{f: ..., g: B}` where `B.State = StateRefTo(A)` (or `B === A`). Cycle is graph-level.
- `StateStruct.TypeName` is the identity carrier. `MergedTypeName`/`LcaTypeName` unchanged.
- Cycle-aware operations (`Equals`, `Lca`, `Concretest`, `Merge`, `Apply`, `Fit`) use visited-pair guards (already present for several — extend uniformly).
- `IsContractiveCycleHead` on `TicNode` stays as fast-path tag.
- `GenericConstrains.StructBound` migrates to `GenericConstrains.NamedTypeBound` (NamedStructOf or just identity).

## Migration phases (each commitable & test-greenable)

### Phase 1 — Plumb `TypeName` through `LangTiHelper.ResolveNamedStruct` root (target: GH #128 partial)

Set `rootStruct.TypeName = typeName` at `LangTiHelper.cs:149`. Note: this alone fails on:
- Generic field types (`type t = {a}` → field `a` is `Any`) — `IsMutable=false` blocks refinement → FU710 in cast at equation boundary.
- Self-recursive user functions (`treeSum(t:tree):int = ... treeSum(t.right!) ...`) — infinite loop somewhere (suspected: every recursive call merges 'tree' with itself, cycle guards don't catch this specific path).

**Decouple `IsMutable` from `TypeName`**: change `StateStruct.IsMutable => TypeName == null` to follow the StateArray/StateOptional pattern (`IsMutable => !IsSolved` or recursive field check). This addresses the generic-field regression.

Then audit the 4 self-recursive paths in `SolvingFunctions.cs` (Apply/Pull/Push/Merge for StateStruct) to confirm visited-pair guards cover the `treeSum` case. Likely fix: add a fresh visited-pair guard to whichever code path enters recursively. Confirm with `RecursiveFunction_DirectFieldAccess_TreeSum_Annotated`.

**Acceptance**: all of TIC + Syntax + Unit + API green; BugC/D/E/G unignored & green; AllTests run completes in normal time.

**Cost**: 6-10h Claude work. The "decouple IsMutable" is the deep audit.

### Phase 2 — Use `StateStruct.TypeName` as the SOLE identity carrier (target: GH #127 prep)

- Audit every read of `ConstraintsState.StructBound`. For each, check: does it need the *full* sub-struct shape, or just the *name*?
  - If just the name → migrate to reading `StateStruct.TypeName` from the descendant/ancestor or from a sibling TicNode.
  - If full shape → migrate to direct `StateStruct` state on the TicNode itself (no CS wrapper).
- `ConstraintsState.StructBound` becomes deprecated but kept temporarily for migration; `RecBoundKind` simplified to just the dispatch tag (still helpful in the deleted-class transition).

**Acceptance**: tests green; `StructBound` setter has zero callers.

**Cost**: 4-8h. Mostly grep/read.

### Phase 3 — Delete `RecBound`, `RecBoundKind`, slot on CS (target: GH #127 main)

- Delete `RecBound.cs`. Remove `using` lines.
- Remove `ConstraintsState.RecursiveBound` and `StructBound`. Remove `GcdBound`, `RewireStructBoundOwnership`.
- Update `NoConstrains` predicate.
- `GenericConstrains.StructBound` → `GenericConstrains.NamedTypeBound` (single TypeName string, or null). The runtime Fit check uses field-by-field NamedStructOf via `NamedTypeFieldRegistry`.

**Acceptance**: clean build, all tests green, RecBound.cs deleted.

**Cost**: 4-6h.

### Phase 4 — Migrate SCC closure to set `node.State = StateStruct{...}` directly

The current SCC closure (`GraphBuilder.ScCClosurePass` calling `LiftMuTypes` / `LiftMuTypesFromSCC`) sets `cs.StructBound = ...` on the cycle-head CS. Replace with: set `node.State = TheStateStruct` directly, where the struct has self-referencing fields pointing back to this TicNode.

Existing infrastructure that should be reused:
- `TryRepairOptSourcedCycle` (already does the opt-wrap + TypeName stamp dance) — extend to non-opt-sourced declared cycles
- `PropagateTypeNamesAcrossSnapshots` — drop the `anyOptSourced` gate; the field-subset match is the gatekeeper

**Acceptance**: full TIC + Syntax pass; M2-B Roadmap tests (CountField/MaxNode/GetLast/AccessChain) green.

**Cost**: 6-10h. Most error-prone phase — touches cycle resolution.

## Total budget

20-32h Claude work, split across 4 commits/PRs. Not weeks, but **multi-session**. Requires the Phase 1 audit before any code change.

## What's known to NOT fix it (anti-knowledge from this session)

- **Set TypeName on root + decouple IsMutable** alone: causes infinite loop in self-recursive user fn `treeSum(t.right!)`. The issue is in the merge/apply recursion, not in IsMutable directly.
- **Set TypeName on root + heuristic "all fields concrete"**: hangs self-recursive user fn for the same reason; heuristic doesn't actually prevent the loop, just narrows which types trigger it.
- **Drop `if (!anyOptSourced) return` gate in PropagateTypeNamesAcrossSnapshots**: zero observable effect on Bug G. The signature structs are probably already past stamping by the time this runs, OR they're not in the toposorted set the call sees.

## What we don't know

- **Without a real debugger** (Rider/VS), tracing TIC state through SetCall → SignatureHasRecursiveShape → CreatePerSiteCloneMap → body-solve is guessing. CLI-only with file-write debug is unreliable (`/tmp/tic-dbg.log` writes during `dotnet run` produced no file — sandbox blocks?).
- **What's the actual graph state** when `f(n)?.next?.next?.v` fails? Is the f signature truly cyclic via `StateRefTo(rootPlaceholder)`, and the failure is in *rendering / FunnyType conversion*, or is the graph itself non-cyclic by that point?
- **Why does `SignatureHasRecursiveShape` not fire** for any user fn call (incl. simple `fn(x)=x+1; out=fn(5)`)? Is `SetCall(StateFun, ids)` actually called? `userFunction != null` path?

## Recommended first action for next session

**Run Roslyn debugger / Rider with a breakpoint at `GraphBuilder.SetCall(StateFun, ids)` and `LangTiHelper.ResolveNamedStruct` for the Bug G script.**

15 minutes of observed truth replaces hours of black-box hypothesis. Start the deep refactor only AFTER confirming the assumed graph topology matches reality.
