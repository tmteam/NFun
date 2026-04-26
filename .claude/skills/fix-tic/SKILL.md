---
name: fix-tic
description: Automated TIC bug fixing pipeline. Takes a single failing test or problem description, reproduces at multiple levels, investigates with parallel agents, fixes without hacks, cleans up.
user-invocable: true
argument-hint: <single test name or problem description>
---

# Fix TIC — Automated Bug Fixing Pipeline

Fully automated pipeline for fixing TIC (Type Inference Constraint) solver bugs.
Input: **one bug** — a single failing test name or problem description via `$ARGUMENTS`.
If multiple bugs are given, pick the one with the clearest reproduction and fix it first.

## Project Constants

```
BUILD_CMD=dotnet build src/NFun/NFun.csproj -p:SignAssembly=false -f net6.0
TEST_TIC=dotnet test src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false
TEST_SYNTAX=dotnet test src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false
TEST_UNIT=dotnet test src/Tests/Nfun.UnitTests/Nfun.UnitTests.csproj -p:SignAssembly=false
TEST_API=dotnet test src/Tests/NFun.ApiTests/NFun.ApiTests.csproj -p:SignAssembly=false
```

## Key Source Directories (agents MUST read these before working)

**TIC core** (read the structure, understand the algorithm):
- `src/NFun/Tic/Stages/` — PullConstraintsFunctions.cs, PushConstraintsFunctions.cs, DestructionFunctions.cs, StagesExtension.cs
- `src/NFun/Tic/Algebra/` — StateExtensions (Lca, Gcd, Fit, Convert, Unify, Abstractest, Concretest)
- `src/NFun/Tic/SolvingStates/` — ConstraintsState.cs, StatePrimitive.cs, StateOptional.cs, StateArray.cs, StateFun.cs, StateStruct.cs
- `src/NFun/Tic/GraphBuilder.cs` — builds the constraint graph
- `src/NFun/Tic/SolvingFunctions.cs` — main solving loop
- `src/NFun/Tic/TicNode.cs` — node definition

**TIC specifications** (agents MUST read before proposing fixes):
- `Specs/Tic/TicAlgorithm.md` — Build, Toposort, Pull, Push, PropagatePreferred phases
- `Specs/Tic/TicAlgorithm_Destruction.md` — Destruction, Finalize, resolution strategy
- `Specs/Tic/Algebra.md` — 6 algebraic operators
- `Specs/Tic/TicTypeSystem.md` — type system, row polymorphism, subtyping
- `Specs/Tic/TicGraph.md` — constraint graph structure
- `Specs/Tic/TicPreferred.md` — Preferred system
- `Specs/Tic/TicTechnicalDebt.md` — known limitations and proper fix paths

**Test directories** (agents read for style/patterns):
- `src/Tests/NFun.Tic.Tests/` — TIC-level tests (GraphBuilder + Solve)
- `src/Tests/NFun.Tic.Tests/UnitTests/` — TIC unit tests (algebra, pure functions)
- `src/Tests/NFun.SyntaxTests/` — Integration tests (full expression pipeline)
- `src/Tests/Nfun.UnitTests/` — Unit tests (LCA, parser, converters)

## Execution Plan

Run ALL phases sequentially without pausing for user review.

---

### Phase 0: THEORY

**Before any code work**, understand the bug theoretically.

1. Build the project: `$BUILD_CMD`
2. Run the failing expression/test. Capture the error.
3. Run the expression with `-t` flag (trace) to see the TIC graph.
4. Launch **1 Professor agent** (read-only) with this prompt:

```
You are a type theory professor analyzing a TIC solver bug.

Bug description: {BUG_DESCRIPTION}
Error: {ERROR_OUTPUT}
Trace: {TRACE_OUTPUT}

FIRST: Read ALL TIC specification documents:
- Specs/Tic/TicAlgorithm.md, TicAlgorithm_Destruction.md, Algebra.md
- Specs/Tic/TicTypeSystem.md, TicGraph.md, TicPreferred.md, TicTechnicalDebt.md

THEN: Read relevant implementation:
- src/NFun/Tic/Stages/ (Pull/Push/Destruction)
- src/NFun/Tic/SolvingStates/ (type states)
- src/NFun/Tic/GraphBuilder.cs, SolvingFunctions.cs

YOU ARE READ-ONLY. Do NOT modify files or run commands.

Answer these questions:
1. **Algebraic rule**: What type-theoretic rule governs this case?
   (e.g., struct covariance, generic instantiation, subtyping transitivity)
2. **Expected behavior**: What SHOULD happen according to the algebra?
3. **Root cause hypothesis**: WHERE in the pipeline does it break? (Build/Pull/Push/Destruction/Finalize)
4. **Is this a known limitation?** Check TicTechnicalDebt.md.
5. **Clean fix direction**: What algebraic operation is missing or wrong?
   Express as a rule, NOT as code. (e.g., "struct covariance should apply transitively through lambda params")
6. **Risk assessment**: What other cases could be affected by this fix?
```

5. Read the professor's analysis. If the professor identifies this as a **known limitation** in TicTechnicalDebt.md, follow the documented fix path. If the professor says the fix requires **structural changes** beyond a local fix, STOP and report this to the user before proceeding.

---

### Phase 1: UNDERSTAND

1. Based on professor's analysis, formulate a concrete hypothesis.
2. Determine the level: Syntax (full pipeline) or TIC (solver only) or Unit (algebra/pure function).
3. Print summary: test, error, professor's diagnosis, hypothesis, level.

---

### Phase 2: REPRODUCE

Launch **2 parallel agents** (subagent_type: general-purpose, isolation: worktree):

**Agent A — Same Level, Different Context:**
- Write 3-5 NEW tests that probe the SAME problem in DIFFERENT contexts
- The goal: find the boundary — where does it break, where does it still work?
- Run the tests, report which pass and which fail

**Agent B — One Level Down:**
- If original is Syntax → write TIC-level tests (using GraphBuilder directly)
- If original is TIC → write Unit-level tests (testing algebra/pure functions)
- Write 3-5 tests that isolate the core problem minimally
- Run the tests, report which pass and which fail

**Common context for both agents** (include in their prompts):
```
CRITICAL: You are in a worktree with potentially stale binaries.
You MUST rebuild before running ANY tests:
  dotnet build src/NFun/NFun.csproj -p:SignAssembly=false -f net6.0
  dotnet build src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false
  dotnet build src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false

Professor's analysis: {PROFESSOR_ANALYSIS}
```

After both agents return, verify a sample of results yourself.

---

### Phase 3: FILTER TESTS

For each test written in Phase 2, evaluate:

1. **"Is the expected behavior obviously correct?"** — If ambiguous, DELETE.
2. **"Does this duplicate an existing test?"** — If yes, DELETE.
3. **"Does this add signal beyond the original?"** — If same thing, DELETE.

Keep only invariant tests. Print summary: written, kept, deleted.

---

### Phase 4: INVESTIGATE

Launch **3 parallel agents** (read-only, subagent_type: general-purpose):

Give each: the bug, professor's analysis, all surviving tests, which pass/fail.

**Agent 1 — Data Flow**: Trace Pull/Push/Destruction step by step for the failing graph.
**Agent 2 — Algebra Completeness**: Compare Apply() overloads — is there a missing case?
**Agent 3 — Graph Structure**: Compare graph construction for working vs failing case.

All agents must provide:
- **Practical explanation**: what breaks, file:line
- **Theoretical explanation**: why, per type algebra
- **Proposed fix**: what should change, in text (NOT code)

After all 3 return, **synthesize**: pick the best explanation or combine.

**Gate check**: Does the synthesized fix match the professor's direction from Phase 0?
If not, re-evaluate — the fix might be a workaround, not a clean solution.

---

### Phase 5: FIX

**Step 1: Baseline.** Record test counts for ALL suites.

**Step 2: Implement** the chosen fix.

**Step 3: Verify no regressions.** Run ALL tests. Compare against baseline.

**Step 4: If regressions — PIVOT, don't loop.**
- If ≤3 regressions: analyze each, narrow the fix scope.
- If >3 regressions: the approach is wrong. DO NOT iterate. Instead:
  1. Revert all changes.
  2. Re-read professor's analysis and agent reports.
  3. Choose a DIFFERENT approach (not a narrower version of the same).
  4. If no alternative exists, STOP and report:
     "This bug requires structural change to X. Current architecture cannot support fix Y because Z."
  5. Add to TicTechnicalDebt.md if appropriate.
- **Max 2 fix attempts.** After 2 failed attempts, STOP with a report.

**Step 5: Run ALL tests** including new tests from Phase 2.

---

### Phase 6: CLEANUP

1. Remove debug traces (Console.WriteLine, etc.)
2. Clean new tests: consistent naming, no redundant assertions
3. Clean modified code: no dead code, no commented-out blocks
4. Run all tests one final time
5. Print final summary:
   - What was the problem (1-2 sentences)
   - What was the fix (1-2 sentences)
   - Professor's theoretical justification (1 sentence)
   - Test results (counts per project, delta from baseline)
   - Files changed (list)
   - If NOT fixed: what remains, why, structural change needed
