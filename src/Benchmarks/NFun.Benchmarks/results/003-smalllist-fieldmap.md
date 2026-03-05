# complex-lca-2026 + SmallList + FieldMap

- **Date**: 2026-03-05
- **Branch**: complex-lca-2026
- **Machine**: MacBook Pro, Apple M4 Max
- **OS**: macOS 15.1 (Darwin arm64)
- **.NET SDK**: 9.0.303, target net6.0
- **Build**: Debug, SignAssembly=false
- **Mode**: Quick5 (median of 5 runs), 3 independent sessions
- **Note**: System thermally throttled (baseline ~12.6μs vs normal ~8μs). Parrots normalize this.

## Changes vs 002

- **SmallList\<T\>** replacing `List<TicNode>` in `TicNode._ancestors`
  - 0 items: no array allocation
  - 1 item: inline field, no T[4] allocation
  - 2+ items: raw T[], no List wrapper
  - Struct Enumerator for non-boxing foreach
- **FieldMap** replacing `Dictionary<string, TicNode>` in `StateStruct._nodes`
  - 0-2 entries: inline key/value fields, no Dictionary
  - 3+ entries: falls back to Dictionary
  - Struct enumerators for Values and KeyValuePair iteration

## Raw runs (Quick5, baseline = 12.64 μs)

```
           |  Parse  |  Build  | TIC+Asm |   Run   | Update
-----------+---------+---------+---------+---------+--------
Simple     |   4.4 μs |  20.4 μs |  16.0 μs |  0.06 μs |  0.19 μs
           |   0.4x  |   1.6x  |   1.3x  |   0.0x  |   0.0x
Medium     |  57.5 μs | 206.4 μs | 148.9 μs |   5.3 μs |    —
           |   4.5x  |  16.3x  |  11.8x  |   0.4x  |    —
Complex    | 108.5 μs | 502.8 μs | 394.2 μs |  15.7 μs |    —
           |   8.6x  |  39.8x  |  31.2x  |   1.2x  |    —
```

Stability across 3 Quick5 sessions:
- Medium.TIC+Asm: 11.8x, 11.8x, 11.8x (rock-stable)
- Complex.TIC+Asm: 31.0x, 31.6x, 31.2x (median 31.2x)

## Comparison with 002 (parrots)

| Metric         |  002  |  003  |   Δ   |
|----------------|-------|-------|-------|
| Simple.TIC+Asm |  1.3x |  1.3x |   0%  |
| Medium.TIC+Asm | 12.0x | 11.8x |  -2%  |
| Complex.TIC+Asm| 31.6x | 31.2x |  -1%  |

## Comparison with master (parrots, from 001)

| Metric         | master | 003   |   Δ   |
|----------------|--------|-------|-------|
| Simple.TIC+Asm |  1.2x  |  1.3x |  +8%  |
| Medium.TIC+Asm | 12.0x  | 11.8x |  -2%  |
| Complex.TIC+Asm| 30.1x  | 31.2x |  +4%  |

## All perf fixes applied (cumulative)

| # | Fix | Δ |
|---|-----|---|
| 2 | StateStruct LINQ → foreach | -1% |
| 3 | GCD Union → foreach | 0% |
| 4 | MergeGroup HashSet | 0% |
| 5 | With() in-place | 0% |
| 6 | SmallList\<T\> for ancestors | ~-1% |
| 7 | FieldMap for struct fields | ~-1% |
| **Total vs 002** | | **~-2%** |
