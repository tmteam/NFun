# V2 Benchmark — HighPrecision — optional_types (optimized)

## Context
- **Benchmark set**: V2 = V1 standard scripts + optional/struct-LCA additions
- **Scripts**: Simple=28(9upd), Medium=17(5upd), Complex=8(2upd)
- **Branch**: `optional_types` @ 83e88196 (dirty)
- **Optimizations applied**: Members (ICompositeState), AllLeafTypes (iterator elimination), Array (_syntaxNodes List→TicNode[])
- **Date**: 2026-03-16

## Environment
- Machine: tmteams-MacBook-Pro, 14 cores, 36864MB
- Runtime: .NET 6.0.36, Config: Release
- Baseline: ArithLoop 80K = 54.9 μs (CV 2.4%)
- 1 turtle (t) = 5.5 ns = baseline / 10000
- Mode: HighPrecision (120s measurement, 127s total)
- Rounds: 1000 (100 dropped, 900 used), Batch: 10ms

## Absolute (μs per script)

|           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U  |
|-----------|----------|----------|----------|----------|----------|----------|
| Simple    |  2.43 μs |  10.3 μs |  7.91 μs |  0.06 μs |  0.09 μs |  11.2 μs |
| Medium    |  6.46 μs |  35.5 μs |  29.1 μs |  0.61 μs |  0.64 μs |  41.9 μs |
| Complex   |  17.2 μs |   112 μs |  94.5 μs |  2.44 μs |  10.5 μs |   217 μs |

## Turtles (1t = 5.5ns, lower is better)

|          | Parse   | Build    | TIC+Asm  | Run   | Update  | Bld+10U  |
|----------|---------|----------|----------|-------|---------|----------|
| Simple   | 443 t   | 1,884 t  | 1,441 t  | 10 t  | 16 t    | 2,048 t  |
| Medium   | 1,176 t | 6,469 t  | 5,293 t  | 110 t | 117 t   | 7,638 t  |
| Complex  | 3,132 t | 20,336 t | 17,204 t | 444 t | 1,920 t | 39,532 t |
| Weighted | 560 t   | 2,640 t  | 2,079 t  | 27 t  | 53 t    | 3,174 t  |

## Stability (CV%)

|           | Parse  | Build  |  Run   | Update |
|-----------|--------|--------|--------|--------|
| Simple    |  1.1%  |  1.1%  |  1.0%  |  0.8%  |
| Medium    |  1.1%  |  1.3%  |  1.0%  |  0.8%  |
| Complex   |  1.1%  |  1.2%  |  1.0%  |  1.2%  |

## Memory (KB allocated per script)

|           |  Parse   |  Build   |   Run    |  Update  |
|-----------|----------|----------|----------|----------|
| Simple    |  3.50 KB |  14.6 KB |  0.14 KB |  0.10 KB |
| Medium    |  8.26 KB |  43.0 KB |  0.95 KB |  1.27 KB |
| Complex   |  21.2 KB |   133 KB |  3.41 KB |  21.7 KB |

## Comparison with V1 standard (HighPrecision, same machine)

V1 standard (22+13+6 scripts) measured at v7 (same optimizations):

|           | V1 Build | V2 Build | V2/V1   | V1 TIC  | V2 TIC  | V2/V1   | V1 Mem   | V2 Mem   |
|-----------|----------|----------|---------|---------|---------|---------|----------|----------|
| Simple    | 8.35 μs  | 10.3 μs  | +23.4%  | 6.35 μs | 7.91 μs | +24.6%  | 12.2 KB  | 14.6 KB  |
| Medium    | 32.2 μs  | 35.5 μs  | +10.2%  | 26.0 μs | 29.1 μs | +11.9%  | 40.0 KB  | 43.0 KB  |
| Complex   |  103 μs  |  112 μs  | +8.7%   | 85.5 μs | 94.5 μs | +10.5%  |  125 KB  |  133 KB  |

Optional/struct-LCA scripts are heavier than V1 standard per-script on average.
The cost is concentrated in type inference (TIC): optional wrapping, safe-access
chains, struct LCA — all add constraint nodes and solver work.
