---
name: bug-hunt
description: Fuzz-test NFun by generating random expressions, predicting results, running via CLI, and collecting mismatches as bug reports. Argument is number of iterations.
user-invocable: true
argument-hint: <number of iterations, e.g. 100>
---

# Bug Hunt — Automated NFun Fuzz Testing

Launch 3 parallel agents that independently generate NFun expressions, predict expected behavior, run them via CLI, and report mismatches.

**Input:** `$ARGUMENTS` — total number of iterations (e.g. 100). Each agent runs N/3 iterations.

## Setup

Before launching agents, build the project:
```bash
dotnet build src/NFun/NFun.csproj -p:SignAssembly=false -f net6.0
```

Calculate iterations per agent: `ITERATIONS_PER_AGENT = ceil($ARGUMENTS / 3)`

## NFun CLI Command

```bash
dotnet run --project src/ConsoleAppExample/ConsoleAppExample.csproj --no-build -p:SignAssembly=false -- -e "EXPRESSION"
```

Multi-line expressions use `\r` as line separator:
```bash
dotnet run ... -- -e "f(x) = x * 2\r y = f(3)"
```

## NFun Language Specification

Agents MUST read ALL specification files before starting:
- `Specs/Basics.md` — script structure, inputs/outputs, execution model
- `Specs/Types.md` — type system, primitives, arrays, structs, optionals, type hierarchy
- `Specs/Operators.md` — all operators, precedence, type constraints
- `Specs/Functions.md` — all built-in functions with signatures
- `Specs/Arrays.md` — array syntax, operations, slicing
- `Specs/Texts.md` — text/string operations
- `Specs/Structs.md` — struct syntax
- `Specs/Rules.md` — anonymous functions (rule keyword)
- `Specs/Optionals.md` — optional types, none, ??, ?., !
- `Specs/Math-Sugar.md` — math syntax sugar

## Agent Launch

Launch **3 parallel agents** (subagent_type: general-purpose). Each agent gets the SAME prompt below, but with a different FOCUS parameter:

**Agent 1 — Focus: SIMPLE_AND_TRICKY**
**Agent 2 — Focus: HELL_AND_NESTED**
**Agent 3 — Focus: EDGE_AND_CREATIVE**

## Agent Prompt Template

```
You are a bug hunter for the NFun expression language.

Your task: generate NFun expressions, PREDICT the expected result BEFORE running,
then run via CLI and compare. Report any mismatches.

FOCUS: {FOCUS}

### Step 0: Read the specification
Read ALL files in the Specs/ directory. You must understand the language thoroughly
before generating expressions. Pay special attention to type inference rules,
operator precedence, and edge cases.

### Step 1: For each iteration (you have {ITERATIONS_PER_AGENT} iterations):

1. **Generate** an NFun expression based on your focus area
2. **Predict** BEFORE running: what should the output be? What type? Should it error?
   Write down your prediction explicitly.
3. **Run** via CLI:
   dotnet run --project src/ConsoleAppExample/ConsoleAppExample.csproj --no-build -p:SignAssembly=false -- -e "EXPRESSION"
4. **Compare** prediction vs actual result
5. If mismatch: record as a potential bug (expression, expected, actual, why you think it's a bug)
   If match: move on

### Focus Areas

**SIMPLE_AND_TRICKY** (Agent 1):
- Start with simple expressions: arithmetic, boolean, text, comparisons
- Then get tricky: edge cases in type inference, implicit conversions, operator precedence
- Integer overflow boundaries, type narrowing/widening
- Mixing types in arrays: [1, 2.5, 3] — what type is inferred?
- Empty arrays, single-element arrays
- Chained comparisons, nested ternaries
- Default values for different types

**HELL_AND_NESTED** (Agent 2):
- Deeply nested structures: structs inside structs inside arrays
- Complex lambda compositions: map(rule filter(rule ...))
- User functions calling user functions calling built-in functions
- fold/reduce with complex accumulators
- Arrays of functions, functions returning arrays of structs
- Multiple outputs depending on each other
- Deeply nested if-else chains (5+ levels)
- Complex struct field access chains

**EDGE_AND_CREATIVE** (Agent 3):
- Optional types in every possible context: arrays of optionals, optional structs, optional arrays
- The none literal in unexpected places
- ?? and ?. chains, mixing with ! operator
- Type annotations that force unusual conversions
- Unusual but valid syntax combinations
- Bitwise operations mixed with arithmetic
- Range expressions with edge values
- Text interpolation with complex expressions
- INVALID expressions that should be rejected: type mismatches, wrong arities, optional where
  non-optional expected, arithmetic on booleans/text, accessing fields on primitives,
  assigning composites to primitives, circular dependencies, duplicate variable names
- Anything creative — valid or invalid — that might break the engine or bypass validation

### Important Rules

- Test BOTH valid AND invalid expressions. A good bug hunter tests both sides:
  - Valid expressions that should produce a result
  - Invalid expressions that SHOULD produce a compile-time or runtime error
- Split your iterations roughly 70/30: 70% valid expressions, 30% invalid ones.

**For valid expressions:**
- If the CLI returns an error but you expected a value — that IS a bug.
- If the CLI returns a WRONG value — that IS definitely a bug.
- If the CLI crashes (unhandled exception, stack overflow) — that IS a bug.

**For invalid expressions (should error):**
- If the CLI returns an error and you expected an error — that is NOT a bug.
- If the CLI returns a value but you expected an error — that IS a bug (missing validation).
- Examples of invalid expressions: type mismatches (`x:int = "hello"`), using non-optional
  where optional required (`x:int = none`), wrong arity (`f(x) = x; f(1,2)`),
  arithmetic on non-numeric types, accessing fields on non-structs, etc.

**General:**
- Double-check the spec before reporting. If unsure, it's not a bug.
- Type inference producing unexpected but technically valid types is worth noting but
  verify against the spec before calling it a bug.

### Output Format

Return a structured report:

TOTAL_ITERATIONS: N
BUGS_FOUND: M

For each bug:
---
BUG #{number}
EXPRESSION: the nfun expression
EXPECTED: what you predicted (value and type, or error)
ACTUAL: what the CLI returned
CATEGORY: [wrong_value | unexpected_error | missing_error | missing_validation | crash | wrong_type]
EXPLANATION: why this is a bug according to the spec
SEVERITY: [critical | moderate | minor]
---

If no bugs found, report: "No bugs found in N iterations."
```

## After All 3 Agents Return

### Collect Results
Gather all bug reports from the 3 agents.

### Filter False Positives
For each reported bug:
1. **Re-run the expression** yourself to confirm it reproduces
2. **Re-read the relevant spec section** — is the agent's expectation actually correct?
3. If the agent misunderstood the spec → discard (false positive)
4. If it's genuinely a bug → keep

### Write Bug Tests
Create file `src/Tests/NFun.SyntaxTests/BugHuntResults.cs` (or append if it exists):

```csharp
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug hunting.
/// Each test is a confirmed bug — expected behavior per specification
/// does not match actual behavior.
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    // Template for value mismatch:
    [Test][Ignore("Bug hunt #{N}: {short description}")]
    public void BugN_ShortDescription() {
        "expression".AssertReturns("y", expectedValue);
    }

    // Template for unexpected error (should work but throws):
    [Test][Ignore("Bug hunt #{N}: {short description}")]
    public void BugN_ShortDescription() {
        "expression".AssertReturns("y", expectedValue);
    }

    // Template for crash:
    [Test][Ignore("Bug hunt #{N}: {short description}")]
    public void BugN_ShortDescription() {
        Assert.DoesNotThrow(() => "expression".Calc());
    }
}
```

Use `[Ignore("...")]` on each test since these are known bugs.
Use the test patterns from existing syntax tests (AssertReturns, Calc, etc.).

### Final Report

Print summary:

```
## Bug Hunt Report

Iterations: {total}
Agent 1 (Simple & Tricky): {iterations} iterations, {bugs} bugs found
Agent 2 (Hell & Nested):   {iterations} iterations, {bugs} bugs found
Agent 3 (Edge & Creative):  {iterations} iterations, {bugs} bugs found

Total bugs found: {N} (before filtering)
False positives removed: {M}
Confirmed bugs: {K}

Confirmed bugs saved to: src/Tests/NFun.SyntaxTests/BugHuntResults.cs

### Bug Summary
1. Bug #1: {short description} [{category}] [{severity}]
2. Bug #2: ...
```
