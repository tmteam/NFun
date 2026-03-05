# complex-lca-2026 + perf fixes

- **Date**: 2026-03-05
- **Branch**: complex-lca-2026, commit 853d4342+
- **Machine**: MacBook Pro, Apple M4 Max
- **OS**: macOS 15.1 (Darwin arm64)
- **.NET SDK**: 9.0.303, target net6.0
- **Build**: Debug, SignAssembly=false
- **Mode**: Quick5 (median of 5 runs)

## Changes vs master
- Algebra split (Lca, Gcd, Concretest, Abstractest)
- Struct LCA support
- Unify fix (abstract primitive types in ToConcrete)
- Perf fixes: StateStruct LINQ→foreach, GCD struct union, MergeGroup HashSet

```
Baseline: Sort+Scan int[1000] = 8.17 μs

           |  Parse  |  Build  | TIC+Asm |   Run   | Update
-----------+---------+---------+---------+---------+--------
Simple     |   2.9 μs |  13.4 μs |  10.6 μs |  0.04 μs |  0.12 μs
           |   0.4x  |   1.6x  |   1.3x  |   0.0x  |   0.0x
Medium     |  37.2 μs | 135.1 μs |  97.9 μs |   3.4 μs |    —
           |   4.6x  |  16.5x  |  12.0x  |   0.4x  |    —
Complex    |  70.2 μs | 328.6 μs | 258.3 μs |  10.2 μs |    —
           |   8.6x  |  40.2x  |  31.6x  |   1.2x  |    —
```

## Comparison with master (parrots)

| Metric         | master | branch |   Δ   |
|----------------|--------|--------|-------|
| Simple.Parse   |  0.4x  |  0.4x  |   0%  |
| Simple.TIC+Asm |  1.2x  |  1.3x  |  +8%  |
| Simple.Run     |  0.0x  |  0.0x  |   0%  |
| Medium.Parse   |  4.6x  |  4.6x  |   0%  |
| Medium.TIC+Asm | 12.0x  | 12.0x  |   0%  |
| Medium.Run     |  0.4x  |  0.4x  |   0%  |
| Complex.Parse  |  8.7x  |  8.6x  |   0%  |
| Complex.TIC+Asm| 30.1x  | 31.6x  |  +5%  |
| Complex.Run    |  1.2x  |  1.2x  |   0%  |

## Perf optimization attempts

| Fix | Description | Result | Decision |
|-----|-------------|--------|----------|
| 1. `new[]` → `yield return` in GetAllLeafTypes/GetAllOutputTypes | State machine overhead worse than 1-element array | +3% slower | Reverted |
| 2. StateStruct LINQ → foreach (IsSolved, HasAnyReferenceMember, Members, GetNonReferenced) | Removed closure allocations | -1% | Kept |
| 3. GCD struct `.Select().Union()` → two foreach loops | Zero-allocation key iteration | 0% (cleaner code) | Kept |
| 4. MergeGroup `.Where().Distinct().ToList()` → HashSet + manual loop | O(1) Contains instead of O(n) on lazy IEnumerable | 0% (correct complexity) | Kept |
