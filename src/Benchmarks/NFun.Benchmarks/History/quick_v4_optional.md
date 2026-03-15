NFun Quick Benchmark — v1
==========================================================================================
Date: 2026-03-16 01:38:03
Machine: tmteams-MacBook-Pro  OS: Darwin 24.1.0 Darwin Kernel Version 24.1.0: Thu Oct 10 21:06:57 PDT 2024; root:xnu-11215.41.3~3/RELEASE_ARM64_T6041
Runtime: .NET 6.0.36  CPU: 14 cores  Config: Release
Memory: 36864MB available
Git: optional_types @ 5b1c9f20 (dirty)
------------------------------------------------------------------------------------------
Baseline: ArithLoop 80K = 58.4 μs  (CV 2.6%)
1 turtle (t) = 5.8 ns = baseline / 10000. Relative unit; lower = faster.
  Simple=22(8upd)  Medium=13(4upd)  Complex=6(2upd)
Mode: HighPrecision (120s measurement, 124s total)
Rounds: 1000 (100 dropped, 900 used)  Batch: 10ms

Absolute (μs per script):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |  2.01 μs |  8.50 μs |  6.50 μs |  0.05 μs |  0.09 μs |  9.36 μs
Medium     |  6.21 μs |  32.9 μs |  26.7 μs |  0.57 μs |  0.72 μs |  40.1 μs
Complex    |  17.3 μs |   105 μs |  88.1 μs |  2.73 μs |  10.7 μs |   212 μs

Turtles (1t = 5.8ns = baseline/10000, lower is better):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |    343 t |  1,456 t |  1,113 t |    7.7 t |     15 t |  1,603 t
Medium     |  1,063 t |  5,629 t |  4,565 t |     97 t |    123 t |  6,860 t
Complex    |  2,964 t | 18,045 t | 15,081 t |    467 t |  1,830 t | 36,348 t
-----------+----------+----------+----------+----------+----------+----------
Weighted   |    458 t |  2,140 t |  1,682 t |     24 t |     51 t |  2,655 t

Stability (CV%):
           | Parse  | Build  |  Run   | Update
-----------+--------+--------+--------+-------
Simple     |  1.0%  |  1.1%  |  0.9%  |  0.9%
Medium     |  1.1%  |  1.2%  |  1.0%  |  0.9%
Complex    |  1.0%  |  1.2%  |  0.9%  |  1.1%

Memory (KB allocated per script):
           |  Parse   |  Build   |   Run    |  Update
-----------+----------+----------+----------+----------
Simple     |  3.02 KB |  12.5 KB |  0.09 KB |  0.09 KB
Medium     |  8.09 KB |  41.4 KB |  0.95 KB |  1.49 KB
Complex    |  21.4 KB |   131 KB |  4.05 KB |  21.7 KB

Compact (μs/script):  Parse / Build / Run / Update / Bld+10U
  Simple       2.01     8.50     0.05     0.09     9.36
  Medium       6.21     32.9     0.57     0.72     40.1
  Complex      17.3      105     2.73     10.7      212

Batch sizes: Simple.Parse=263, Simple.Build=56, Simple.Run=8677, Simple.Update=2388, Medium.Parse=135, Medium.Build=25, Medium.Run=1444, Medium.Update=4109, Complex.Parse=100, Complex.Build=17, Complex.Run=535, Complex.Update=453

---
Optimizations applied:
1. MemberCount/GetMember(int) indexed access on ICompositeState
   - Eliminated new[] allocations from Members property on 9 hot-path call sites
   - StateArray, StateOptional: MemberCount=1, GetMember(0)=ElementNode
   - StateFun: MemberCount=Args+1, GetMember(i)=ArgNodes[i] or RetNode
   - StateStruct: indexed via FieldMap.GetValueAt(int)
2. AllLeafTypes → CollectLeafTypeVariables recursive method
   - Eliminated yield-return iterator chain allocations in Finalize phase
   - CollectNotSolvedContravariantLeafs replaces LINQ SelectMany+Where

Comparison vs history (weighted μs from absolutes):
  Build:   v1 1.0.3 11.66 → v2 fixLca 12.66 (+8.6%) → v3 optional 13.16 (+3.9%) → v5 opt 12.50 (-1.3%)
  TIC+Asm: v1 1.0.3  9.09 → v2 fixLca 10.14 (+11.6%) → v3 optional 10.49 (+3.5%) → v5 opt  9.83 (-3.1%)
  Memory:  v1 1.0.3 16.76 → v2 fixLca 18.56 (+10.7%) → v5 opt 17.29 (-6.8%)

vs v1 1.0.3: Build +7.2%, TIC+Asm +8.1%, Memory +3.2%
vs v2 fixLca: Build -1.3%, TIC+Asm -3.1%, Memory -6.8%
  (Optional regression fully compensated vs fixLca; remaining gap is fixLca's own regression)
