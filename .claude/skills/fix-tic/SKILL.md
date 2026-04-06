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

**Test directories** (agents read for style/patterns):
- `src/Tests/NFun.Tic.Tests/` — TIC-level tests (GraphBuilder + Solve)
- `src/Tests/NFun.Tic.Tests/UnitTests/` — TIC unit tests (algebra, pure functions)
- `src/Tests/NFun.SyntaxTests/` — Integration tests (full expression pipeline)
- `src/Tests/Nfun.UnitTests/` — Unit tests (LCA, parser, converters)

## Execution Plan

Run ALL phases sequentially without pausing for user review.

---

### Phase 1: UNDERSTAND

1. Build the project: `$BUILD_CMD`
2. Run the specified failing test(s). Parse the error output.
3. Determine the level: Syntax (full pipeline) or TIC (solver only) or Unit (algebra/pure function).
4. Formulate a hypothesis: what subsystem is likely broken and why.
5. Print a short summary: test name, error, hypothesis, level.

---

### Phase 2: REPRODUCE

Launch **2 parallel agents** (subagent_type: general-purpose, isolation: worktree):

**Agent A — Same Level, Different Context:**
- Read the failing test and understand what it tests
- Read other tests in the same file/directory for style
- Write 3-5 NEW tests that probe the SAME problem in DIFFERENT contexts
  - Different types, different nesting, different combinations
  - The goal: find the boundary of the bug — where does it break, where does it still work?
- Run the tests, report which pass and which fail

**Agent B — One Level Down:**
- If original is Syntax → write TIC-level tests (using GraphBuilder directly)
- If original is TIC → write Unit-level tests (testing algebra/pure functions)
- Read existing tests at the target level for style
- Write 3-5 tests that isolate the core problem with minimal nodes/complexity
- Then try to write an even simpler test — the MINIMAL reproduction
- Run the tests, report which pass and which fail

**Common context for both agents** (include in their prompts):
```
CRITICAL: You are in a worktree with potentially stale binaries.
You MUST rebuild before running ANY tests:
  dotnet build src/NFun/NFun.csproj -p:SignAssembly=false -f net6.0
  dotnet build src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false
  dotnet build src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false
  dotnet build src/Tests/Nfun.UnitTests/Nfun.UnitTests.csproj -p:SignAssembly=false

Test TIC: dotnet test src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false --no-build --filter "FullyQualifiedName~TestName"
Test Syntax: dotnet test src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false --no-build --filter "FullyQualifiedName~TestName"

Key directories to READ before writing tests:
- src/NFun/Tic/Stages/ (understand Pull/Push/Destruction)
- src/NFun/Tic/SolvingStates/ (understand type states)
- src/NFun/Tic/GraphBuilder.cs (understand how to build test graphs)
- src/Tests/NFun.Tic.Tests/ (existing test patterns)
- src/Tests/NFun.SyntaxTests/ (existing test patterns)
```

After both agents return, collect all new tests (both passing and failing).
**Verify agent results**: re-run a sample of reported tests yourself to confirm pass/fail claims.

---

### Phase 3: FILTER TESTS

For each test written in Phase 2, evaluate:

1. **"Is the expected behavior obviously correct for TIC?"**
   — If the correct behavior is ambiguous or debatable, DELETE the test.

2. **"Could this expected behavior legitimately change in the future?"**
   — If yes (e.g., it depends on a design decision not yet made), DELETE the test.

3. **"Does this duplicate an existing simpler test?"**
   — Check existing tests. If a simpler test already covers the same invariant, DELETE.

4. **"Does this test add signal beyond the original failing test?"**
   — If it tests exactly the same thing in the same way, DELETE.

Keep only tests that are **invariant** — they MUST always pass in any correct TIC implementation.

Remove deleted tests from the code. Keep the surviving tests (both passing and failing).
Print summary: how many tests written, how many kept, how many deleted and why.

---

### Phase 4: INVESTIGATE

Launch **3 parallel agents** (subagent_type: general-purpose). Give each:
- The original failing test + error
- All surviving minimal reproduction tests from Phase 3
- Which tests pass and which fail

**Common context for all three** (include in their prompts):
```
Read these directories thoroughly before analysis:
- src/NFun/Tic/Stages/ — PullConstraintsFunctions.cs, PushConstraintsFunctions.cs, DestructionFunctions.cs, StagesExtension.cs
- src/NFun/Tic/Algebra/ — all StateExtensions files
- src/NFun/Tic/SolvingStates/ — ConstraintsState.cs and all State*.cs
- src/NFun/Tic/GraphBuilder.cs
- src/NFun/Tic/SolvingFunctions.cs
- src/NFun/Tic/TicNode.cs

IMPORTANT: TIC is a precise algorithm. Solutions must be theoretically clean.
No hacks, no special-case if-statements for specific types.
The fix must follow from the algebra of the type system.

YOU ARE READ-ONLY. THIS IS A HARD CONSTRAINT, NOT A SUGGESTION.
- You MUST NOT modify any .cs file. Not logic, not Console.WriteLine, nothing.
- You MUST NOT run dotnet build or dotnet test.
- You MUST NOT create, delete, or edit any file.
- Your ONLY tools: Read files. Think. Write your analysis as text output.
- If you catch yourself about to edit a file or run a command — STOP.

Analyze by READING code, not by running it. Trace execution mentally.
Focus 100% on understanding WHY the bug happens.

Your output is a TEXT REPORT — not code, not a PR, not a fix.
```

**Agent 1 — Data Flow Focus** (start with Stages/):
"Trace MENTALLY how constraints propagate through Pull, Push, and Destruction for the failing test. Read DestructionFunctions.cs, PullConstraintsFunctions.cs, PushConstraintsFunctions.cs. Walk through each Apply() call step by step for the failing graph. Where does the wrong state get set? What should have happened?"

**Agent 2 — Algebra Completeness Focus** (start with Algebra/ and SolvingStates/):
"The TIC solver has multiple Apply() overloads in DestructionFunctions for different type combinations: (Prim,Prim), (Prim,CS), (CS,Prim), (CS,CS), (CS,Composite), (Composite,Prim), (Composite,CS), (Optional,Optional), (Array,Array), etc. A common root cause is INCOMPLETE ALGEBRA: one overload handles a case correctly but the analogous overload for a different type combination does NOT. Compare how the same semantic operation is handled across different Apply() overloads. Is there an overload that handles the failing case's types but lacks logic that a sibling overload has? Specifically: find the overload that WORKS for a similar case and the overload that FAILS for this case. What does the working one do that the failing one doesn't?"

**Agent 3 — Graph Structure Focus** (start with GraphBuilder.cs and TicNode.cs):
"Read SetCall, SetIfElse, SetDef, SetSoftArrayInit to understand how the graph is constructed for the failing test. Draw the node graph: which nodes exist, their types, their ancestor edges. Compare with a WORKING case that is structurally similar. What is different?"

Each agent must provide:
- **Practical explanation**: what exactly breaks in the code, file:line
- **Theoretical explanation**: why this should or shouldn't work in the type system
- **Proposed fix description**: what should change and where, described in text (NOT code). Must follow from TIC algebra — no hacks, no special-casing for specific types

After all 3 return, **synthesize**: pick the best explanation, or combine insights from multiple agents.

---

### Phase 5: FIX

**Step 1: Baseline.** Record current test counts (pass/fail) for ALL suites BEFORE making changes:
```
dotnet test src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false
dotnet test src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false
dotnet test src/Tests/Nfun.UnitTests/Nfun.UnitTests.csproj -p:SignAssembly=false
dotnet test src/Tests/NFun.ApiTests/NFun.ApiTests.csproj -p:SignAssembly=false
```

**Step 2: Implement** the chosen fix.

**Step 3: Verify no regressions.** Run ALL existing tests (without the new tests from Phase 2).
Compare pass/fail counts against baseline. If any previously-passing test now fails — the fix has regressions.

**Step 4: If regressions** — do NOT iterate blindly. Analyze:
- Which specific test broke?
- Trace the fix through that test's scenario — WHY does it break?
- Narrow the fix (add conditions, restrict scope) to avoid that case.
- If the fix fundamentally conflicts with the broken test's invariant, STOP.
  Report: "This bug requires a structural change to X. The simple fix breaks Y because Z."

**Step 5: Run ALL tests** including new tests from Phase 2:
```
dotnet test src/Tests/NFun.Tic.Tests/NFun.Tic.Tests.csproj -p:SignAssembly=false
dotnet test src/Tests/NFun.SyntaxTests/NFun.SyntaxTests.csproj -p:SignAssembly=false
dotnet test src/Tests/Nfun.UnitTests/Nfun.UnitTests.csproj -p:SignAssembly=false
dotnet test src/Tests/NFun.ApiTests/NFun.ApiTests.csproj -p:SignAssembly=false
```

---

### Phase 6: CLEANUP

1. **Remove debug traces**: any `Console.WriteLine`, `TraceLog` calls, `Debug.Assert` that were added during investigation.
2. **Clean new tests**: consistent naming, no redundant assertions, clear arrange/act/assert.
3. **Clean modified code**: no dead code, no commented-out blocks, no TODO comments that should be resolved.
4. **Look at surrounding code**: if the fix naturally suggests an obvious generalization or simplification of nearby code, do it. But ONLY if obvious — don't refactor for the sake of refactoring.
5. Run all tests one final time to confirm everything still passes.
6. Print final summary:
   - What was the problem (1-2 sentences)
   - What was the fix (1-2 sentences)
   - Test results (counts per project, delta from baseline)
   - Files changed (list)
   - If the bug was NOT fully fixed: what remains, why, and what structural change is needed
