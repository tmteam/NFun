# master (pre-LCA) — baseline

- **Date**: 2026-03-05
- **Branch**: master, commit eb7d16f2
- **Machine**: MacBook Pro, Apple M4 Max
- **OS**: macOS 15.1 (Darwin arm64)
- **.NET SDK**: 9.0.303, target net6.0
- **Build**: Debug, SignAssembly=false
- **Mode**: Quick3 (median of 3 runs)

```
Baseline: Sort+Scan int[1000] = 7.89 μs

           |  Parse  |  Build  | TIC+Asm |   Run   | Update
-----------+---------+---------+---------+---------+--------
Simple     |   2.8 μs |  12.1 μs |   9.2 μs |  0.04 μs |  0.12 μs
           |   0.4x  |   1.5x  |   1.2x  |   0.0x  |   0.0x
Medium     |  36.3 μs | 131.3 μs |  95.0 μs |   3.3 μs |    —
           |   4.6x  |  16.6x  |  12.0x  |   0.4x  |    —
Complex    |  68.6 μs | 306.4 μs | 237.7 μs |   9.7 μs |    —
           |   8.7x  |  38.8x  |  30.1x  |   1.2x  |    —
```

Raw runs:
- Run 1: Baseline=7.72  Simple.Build=12.0  Medium.Build=129.4  Complex.Build=301.7
- Run 2: Baseline=7.89  Simple.Build=12.1  Medium.Build=131.3  Complex.Build=306.4
- Run 3: Baseline=7.96  Simple.Build=12.1  Medium.Build=132.3  Complex.Build=308.9
