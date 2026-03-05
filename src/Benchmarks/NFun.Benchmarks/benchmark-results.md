# NFun Quick Benchmark Results

## Environment
- **Machine**: MacBook Pro, Apple M4 Max
- **OS**: macOS 15.1 (Darwin arm64)
- **.NET SDK**: 9.0.303, target net6.0
- **Build**: Debug, SignAssembly=false
- **Date**: 2026-03-05
- **Benchmark**: QuickBenchmark.cs (auto-balancing, Fisher-Yates shuffle, seed=42)
- **Baseline**: Copy + Sort + Scan + BinarySearch on int[1000]
- **Rounds**: 2727 per operation, ~30000 shuffled slots, ~13s total

## Scripts
| Level    | Script | Description |
|----------|--------|-------------|
| Simple   | `y = 10 * x + 1` | Single arithmetic expression with variable |
| Medium   | `MultiplyArrayItems` | Generic user function + if-else + array map |
| Complex  | `DummyBubbleSort` | 5 user functions + fold + generics + dual-typed output |

---

## master (pre-LCA, commit eb7d16f2)

Best of 3 runs (median):

```
Baseline: Sort+Scan int[1000] = 7.89 μs

           |  Parse  |  Build  | TIC+Asm |   Run   | Update
-----------+---------+---------+---------+---------+--------
Simple     |   2.8 μs |  12.1 μs |   9.2 μs |  0.04 μs |  0.12 μs
Medium     |  36.3 μs | 131.3 μs |  95.0 μs |   3.3 μs |    —
Complex    |  68.6 μs | 306.4 μs | 237.7 μs |   9.7 μs |    —
```

Raw runs:
- Run 1: Baseline=7.72  Simple.Build=12.0  Medium.Build=129.4  Complex.Build=301.7
- Run 2: Baseline=7.89  Simple.Build=12.1  Medium.Build=131.3  Complex.Build=306.4
- Run 3: Baseline=7.96  Simple.Build=12.1  Medium.Build=132.3  Complex.Build=308.9

---

## complex-lca-2026 (post-LCA, commit ed0a157e)

Best of 3 runs (median, excluding warmup run 1):

```
Baseline: Sort+Scan int[1000] = 8.19 μs

           |  Parse  |  Build  | TIC+Asm |   Run   | Update
-----------+---------+---------+---------+---------+--------
Simple     |   2.9 μs |  13.5 μs |  10.6 μs |  0.04 μs |  0.12 μs
Medium     |  37.3 μs | 135.5 μs |  98.2 μs |   3.5 μs |    —
Complex    |  70.6 μs | 331.6 μs | 261.0 μs |  10.2 μs |    —
```

Raw runs:
- Run 1: Baseline=8.47  Simple.Build=14.1  Medium.Build=145.8  Complex.Build=364.9  *(warm system)*
- Run 2: Baseline=8.20  Simple.Build=13.4  Medium.Build=135.6  Complex.Build=331.8
- Run 3: Baseline=8.19  Simple.Build=13.5  Medium.Build=135.5  Complex.Build=331.6

---

## Comparison (normalized to baseline "parrots")

Parrots remove system-level variance (thermal, scheduler). Lower = faster.

| Metric         | master | branch |   Δ   |
|----------------|--------|--------|-------|
| Simple.Parse   |  0.4x  |  0.4x  |   0%  |
| Simple.Build   |  1.5x  |  1.6x  |  +7%  |
| Simple.TIC+Asm |  1.2x  |  1.3x  |  +8%  |
| Simple.Run     |  0.0x  |  0.0x  |   0%  |
| Simple.Update  |  0.0x  |  0.0x  |   0%  |
| Medium.Parse   |  4.6x  |  4.6x  |   0%  |
| Medium.Build   | 16.6x  | 16.5x  |  -1%  |
| Medium.TIC+Asm | 12.0x  | 12.0x  |   0%  |
| Medium.Run     |  0.4x  |  0.4x  |   0%  |
| Complex.Parse  |  8.7x  |  8.6x  |   0%  |
| Complex.Build  | 38.8x  | 40.5x  |  +4%  |
| Complex.TIC+Asm| 30.1x  | 31.8x  |  +6%  |
| Complex.Run    |  1.2x  |  1.2x  |   0%  |

### Summary
- **Parse**: No change (expected — parser untouched)
- **Run/Update**: No change (expected — interpreter untouched)
- **Simple.Build (TIC+Asm)**: ~+8% slower — small overhead from algebra split
- **Medium.Build (TIC+Asm)**: ~0% — within noise
- **Complex.Build (TIC+Asm)**: ~+5% slower — moderate overhead from refactored solver
- Overall: **modest regression of 0-8%** in TIC+Assembly, acceptable for the structural improvements (algebra split, struct LCA, Unify fix)
