# complex-lca-2026 + Constant-to-Ref optimization

- **Date**: 2026-03-05
- **Branch**: complex-lca-2026
- **Machine**: MacBook Pro, Apple M4 Max
- **OS**: macOS 15.1 (Darwin arm64)
- **.NET SDK**: 9.0.303, target net6.0
- **Build**: Debug, SignAssembly=false
- **Mode**: Quick5 (median of 5 runs), 3 independent sessions

## Changes vs 003

- **Constant-to-Ref in SetCallArgument**: when a fresh constraint node (e.g. integer
  constant `2`) is an argument to a PureGenericFunctionBase (arithmetic, compare),
  and its constraint range is subsumed by the generic's range, convert it to
  `StateRefTo(generic)` instead of adding an ancestor edge.
  - Eliminates one ancestor edge and one constraint resolution per constant argument
  - Transfers `Preferred` from constant to generic node to preserve type resolution
  - Hot path kept small (`[AggressiveInlining]`) with slow path `[NoInlining]`
    to preserve JIT inlining of `SetCallArgument`

## Raw runs (Quick5, baseline = 8.21 μs)

```
           |  Parse  |  Build  | TIC+Asm |   Run   | Update
-----------+---------+---------+---------+---------+--------
Simple     |   2.9 μs |  11.8 μs |   8.9 μs |  0.04 μs |  0.12 μs
           |   0.4x  |   1.4x  |   1.1x  |   0.0x  |   0.0x
Medium     |  37.5 μs | 134.5 μs |  97.0 μs |   3.6 μs |    —
           |   4.6x  |  16.4x  |  11.8x  |   0.4x  |    —
Complex    |  70.9 μs | 326.4 μs | 255.6 μs |  10.3 μs |    —
           |   8.6x  |  39.7x  |  31.1x  |   1.3x  |    —
```

Stability across 3 Quick5 sessions:
- Simple.TIC+Asm: 1.1x, 1.1x, 1.1x (rock-stable)
- Medium.TIC+Asm: 11.8x, 11.9x, 11.8x (median 11.8x)
- Complex.TIC+Asm: 31.1x, 31.3x, 31.1x (median 31.1x)

## Comparison with 003 (parrots)

| Metric         |  003  |  004  |   Δ   |
|----------------|-------|-------|-------|
| Simple.TIC+Asm |  1.3x |  1.1x | **-15%** |
| Medium.TIC+Asm | 11.8x | 11.8x |   0%  |
| Complex.TIC+Asm| 31.2x | 31.1x |   0%  |

## Comparison with master (parrots, from 001)

| Metric         | master | 004   |   Δ   |
|----------------|--------|-------|-------|
| Simple.TIC+Asm |  1.2x  |  1.1x |  **-8%** |
| Medium.TIC+Asm | 12.0x  | 11.8x |  -2%  |
| Complex.TIC+Asm| 30.1x  | 31.1x |  +3%  |

## All perf fixes applied (cumulative)

| # | Fix | Δ |
|---|-----|---|
| 2 | StateStruct LINQ → foreach | -1% |
| 3 | GCD Union → foreach | 0% |
| 4 | MergeGroup HashSet | 0% |
| 5 | With() in-place | 0% |
| 6 | SmallList\<T\> for ancestors | ~-1% |
| 7 | FieldMap for struct fields | ~-1% |
| 8 | Constant-to-Ref in SetCallArgument | **-15% Simple** |
| **Total vs master** | | **Simple -8%, Medium -2%, Complex +3%** |
