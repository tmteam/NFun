# Phase 2 Audit — every `StructBound` reader, classified

Companion to `Specs/Tic/RecBound-elimination-plan.md` (task #108, GH #127).
After commit `b5e95344` (drop `RecBound` wrapper) the `StructBound` slot is a
plain `StateStruct` property on `ConstraintsState`. This audit catalogues
every reader to decide the migration target for Phase 3 (delete the slot).

**Status update**:
- Phase 1 done — `d6a16caa` (IsMutable decouple + cascade).
- Phase 3 Step A done — `906a1e95`: all N-category reads migrated from
  `cs.StructBound != null` to `cs.HasStructBound`. The tables below list the
  pre-migration form; the actual code uses `HasStructBound`. The R/S/I
  categories are unchanged.

**Categories**:
- **N** — null-check / null-write only; no field access
- **I** — reference-identity check (`ReferenceEquals(..., bound)`)
- **R** — reads the full struct shape (iterates fields, recurses, compares)
- **S** — setter (writes the slot)
- **runtime** — different layer (`GenericConstrains`/`GenericUserFunction`); mirrors the TIC slot at build time

## TIC layer

### `ConstraintsState.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 52 | N | `NoConstrains` predicate | new TIC representation must offer equivalent "no F-bound" probe |
| 73 | S | `GetCopy()` | copies slot by reference; will copy whatever replacement state holds |
| 83 | R | `CanBeConvertedTo` → `FitStructBound` | needs the bound struct for width+covariant check |
| 129-218 | R | `FitStructBound` / `FitStructBoundInner` | recursive field walk on bound |
| 639 | R | `Equals` → `StructBoundsEqual` | structural compare of bound |
| 683-684 | R | `StructBoundsEqualInner` recursion through nested CS | bound shape access |

**Notes**: the `Equals` deep compare is the same dead-load case as
`StateStruct.Equals` (last session, partially audited): `StructBoundsEqual` may
be unreachable in production but unit tests assert it. Treat as live.

### `SolvingFunctions.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 699 | N | `CollectFBoundHolders` outer fast path | replacement predicate `IsFBoundHolder(state)` |
| 713 | N | `CollectFBoundRec` per-node check | same |
| 736 | N | `TryRedirectDegenerateOptCycle` — fbound has SB | same |
| 750 | N | `TryRedirectDegenerateOptCycle` probe | same |
| 801 | **S** | `TryLiftElement` (LiftMuTypes core) — `cs.StructBound = s` | the lift transforms `elem.State = CS{StructBound=s}` to direct `elem.State = s` (Phase 4 SCC closure move) |
| 980 | N | `StructIsAnyFBound` predicate | trivial |
| 1181 | N | `TraverseStateGraph` PropagateTypeNamesAcrossSnapshots — break on CS{SB} | same |
| 1655 | R | `ThrowIfRecursiveTypeDefinition` — treats CS{SB} as contractive boundary, recurses into Descendant/Ancestor only | bound-aware break logic; in self-ref repr this is "node is a μ-cycle head" |
| 1793 | N | `TryPromoteCSDescendantToStructBound` precondition (already lifted?) | same |
| 1800 | **S** | promotion `cs.StructBound = s` | direct lift to self-ref struct |

**Notes**: 2 setter sites total (line 801 and 1800). Both are the
descendant-to-bound promotion. In the new representation both rewrite
`elem.State = s` where `s` already has self-RefTo through one of its fields.

### `Tic/Stages/PullConstraintsFunctions.cs` (and `Push…` mirror)

The orchestration of Pull/Push between CS instances both carrying SB:

| Lines | Category | Site | Migration target |
|---|---|---|---|
| 43-48 | R + S | CS↔CS pull merges two SBs via `GcdBound` + `RewireStructBoundOwnership` | when both sides are self-ref structs, merge becomes "merge structs with cycle-rewire" |
| 84-86 | R + S | Optional inner descend, rewire SB | rewire-ownership becomes part of the struct's own self-ref edges |
| 95-102 | R + S | ancestor=CS{SB}, desc=struct → MergeStructBound | direct StateStruct merge with cycle awareness |
| 132, 144 | N | rejection: SB vs Array/Fun ancestor | same predicate |
| 177-180 | R + S | descendant becomes struct, merge SB | same as 95-102 |
| 221-242 | S | Optional/RefTo wrapping, SB rewire | same as 84-86 |

**Push** mirror sites 42-47, 71-75 same shape.

**Surface that needs new implementation**:
- `MergeStructBound` (R+S, line 95-102)
- `GcdBound` (R+S, lines 45-46)
- `RewireStructBoundOwnership` (S, multiple sites)

In the new representation, `node.State = StateStruct{s_with_self_ref}`, and
the analogues become regular state merges where the cycle is handled by the
existing cycle guards on StateStruct merge.

### `GraphBuilder.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 460 | N | `NodeHasRecursiveShape` — bound presence | new predicate "state has cycle?" |
| 746 | N | `HasAnyRecursiveCandidate` | same |

### `TicTypesConverter.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 67-68 | I | `BuildFieldType` detecting cycle back-edge through another CS | bound==state check; new repr: cycle detection via reference equality on TicNode/State |
| 77-78 | I | same | same |

**Notes**: this builds a FunnyType representation of the bound by walking and
producing `FunnyType.Generic(i)` at self-edges. After the migration the
"self-edge" detection moves to "RefTo to a node whose state is the bound struct".

### `RuntimeBuilder.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 590-593 | R | `FreezeFunctionSignatureStructs` walks SB struct + freezes + recurses fields | in new repr: just walk the StateStruct that IS the signature state |

## Runtime layer (mirror, separate code path)

### `GenericConstrains.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 47 | declares | `FunnyType StructBound` field | keep — runtime carries an F-bound FunnyType; reading source is TIC |
| 49 | predicate | `HasStructBound` | keep |
| 52, 54 | render | `ToString` | keep |
| 96 | factory | `WithStructBound` | keep |

### `GenericUserFunction.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 83-87 | R+convert | reads `ticGenerics[i].StructBound`, builds FunnyType via `BuildStructBoundFunnyType` | when TIC slot is gone, reader of TIC source becomes "if generic's state is a self-ref struct, use it as the bound" |
| 278-288 | R | runtime Fit dispatch (`Constrains[i].StructBound` — FunnyType) | unchanged — runtime layer |

### `TicSetupVisitor.cs`

| Line | Category | Site | Migration target |
|---|---|---|---|
| 1515 | runtime | reads `GenericConstrains.HasStructBound` | unchanged — runtime layer |

## Summary

**Total reader sites**: ~50 (excluding setter and runtime).
**Distinct categories**:

| Category | Count | Migration complexity |
|---|---|---|
| N (null check) | ~15 | trivial — `state is StateStruct s && IsFBoundHolder(s)` |
| I (identity) | 4 | trivial — substitute `ReferenceEquals(state, bound)` |
| R (full shape) | ~25 | non-trivial — every site needs review of WHICH operation it performs |
| S (setter) | ~12 | replace with `elem.State = s` directly |
| runtime | ~6 | unchanged (TIC→runtime data flow updates) |

**The dangerous category is R (full shape)**: each site has its own semantics
(equals, fit, merge, freeze, traverse-and-break). Migrating those is the
real Phase 4 work — they can't be uniformly mass-substituted.

## Reuse opportunities

Several of the R sites are operations that StateStruct already performs on
itself, just through a different entry:

- `FitStructBoundInner` ≈ "width subtyping check on struct" — could become
  `state.IsSubtypeOf(boundStruct)` if the operation lived on StateStruct.
- `StructBoundsEqualInner` ≈ "structural equality on potentially-cyclic
  struct" — same scope as the previously-deleted `StateStruct.Equals` deep
  path. Putting it back as an explicit `StructuralSame(other, ctx)` method
  (the option B from the previous session) covers both.
- `MergeStructBound`/`GcdBound` ≈ "Gcd of two structs with cycle awareness"
  — `StateStruct.Gcd` already does this with the nominal short-circuit.

**Phase 4 actual work**: refactor the 4-5 above operations onto StateStruct
as explicit methods, then delete CS.StructBound and rewrite the orchestration
in Pull/Push to use direct StateStruct state.

## Risk hotspots (from prior session's "anti-knowledge")

The plan doc lists three things that DO NOT work, all relevant here:
1. Set TypeName on root + decouple IsMutable alone → `treeSum(t.right!)`
   self-recursive user fn enters infinite loop in merge/apply recursion.
2. Set TypeName on root + "all fields concrete" heuristic → same hang.
3. Drop `anyOptSourced` gate in PropagateTypeNamesAcrossSnapshots → zero effect.

Migration must avoid these. Specifically: **decoupling `IsMutable` from
`TypeName==null`** is required for the migration but ALONE causes hangs.
The fix is somewhere in the merge/apply recursion guards. This is the
single largest unknown remaining.
