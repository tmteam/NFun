NFun Quick Benchmark — v1
==========================================================================================
Date: 2026-03-16 02:05:19
Machine: tmteams-MacBook-Pro  OS: Darwin 24.1.0 Darwin Kernel Version 24.1.0: Thu Oct 10 21:06:57 PDT 2024; root:xnu-11215.41.3~3/RELEASE_ARM64_T6041
Runtime: .NET 6.0.36  CPU: 14 cores  Config: Release
Memory: 36864MB available
Git: optional_types @ 3d05dd1b (dirty)
------------------------------------------------------------------------------------------
Baseline: ArithLoop 80K = 58.7 μs  (CV 1.5%)
1 turtle (t) = 5.9 ns = baseline / 10000. Relative unit; lower = faster.
  Simple=22(8upd)  Medium=13(4upd)  Complex=6(2upd)
Mode: HighPrecision (120s measurement, 125s total)
Rounds: 1000 (100 dropped, 900 used)  Batch: 10ms

Absolute (μs per script):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |  2.01 μs |  8.35 μs |  6.35 μs |  0.04 μs |  0.09 μs |  9.21 μs
Medium     |  6.16 μs |  32.2 μs |  26.0 μs |  0.55 μs |  0.69 μs |  39.0 μs
Complex    |  17.2 μs |   103 μs |  85.5 μs |  2.72 μs |  10.4 μs |   207 μs

Turtles (1t = 5.9ns = baseline/10000, lower is better):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |    341 t |  1,422 t |  1,081 t |    7.5 t |     15 t |  1,569 t
Medium     |  1,050 t |  5,478 t |  4,429 t |     94 t |    117 t |  6,649 t
Complex    |  2,930 t | 17,497 t | 14,567 t |    463 t |  1,767 t | 35,163 t
-----------+----------+----------+----------+----------+----------+----------
Weighted   |    455 t |  2,087 t |  1,633 t |     23 t |     50 t |  2,586 t

Stability (CV%):
           | Parse  | Build  |  Run   | Update
-----------+--------+--------+--------+-------
Simple     |  1.1%  |  1.1%  |  1.0%  |  0.7%
Medium     |  1.2%  |  1.2%  |  1.1%  |  0.9%
Complex    |  1.0%  |  1.2%  |  1.0%  |  1.3%

Memory (KB allocated per script):
           |  Parse   |  Build   |   Run    |  Update
-----------+----------+----------+----------+----------
Simple     |  3.02 KB |  12.2 KB |  0.09 KB |  0.09 KB
Medium     |  8.09 KB |  40.0 KB |  0.95 KB |  1.49 KB
Complex    |  21.4 KB |   125 KB |  4.05 KB |  21.7 KB

Compact (μs/script):  Parse / Build / Run / Update / Bld+10U
  Simple       2.01     8.35     0.04     0.09     9.21
  Medium       6.16     32.2     0.55     0.69     39.0
  Complex      17.2      103     2.72     10.4      207

Batch sizes: Simple.Parse=257, Simple.Build=55, Simple.Run=9464, Simple.Update=14742, Medium.Parse=130, Medium.Build=25, Medium.Run=1362, Medium.Update=4256, Complex.Parse=98, Complex.Build=17, Complex.Run=679, Complex.Update=456

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
3. GraphBuilder._syntaxNodes: List<TicNode> → TicNode[]
   - Eliminated GetOrEnlarge while-loop (5 call sites) → direct array access
   - ITicResults: IReadOnlyList<TicNode> → TicNode[] (no virtual dispatch)
   - GetSyntaxNodeOrNull: uint cast bounds check (JIT eliminates redundant check)

Comparison vs history (weighted μs from absolutes):
  Build:   v1 1.0.3 11.66 → v2 fixLca 12.66 (+8.6%) → v3 optional 13.16 (+3.9%) → v5 opt 12.26 (-3.2%)
  TIC+Asm: v1 1.0.3  9.09 → v2 fixLca 10.14 (+11.6%) → v3 optional 10.49 (+3.5%) → v5 opt  9.59 (-5.4%)
  Memory:  v1 1.0.3 16.76 → v2 fixLca 18.56 (+10.7%) → v5 opt 16.79 (-9.5%)

vs v1 1.0.3: Build +5.1%, TIC+Asm +5.5%, Memory +0.2%
vs v2 fixLca: Build -3.2%, TIC+Asm -5.4%, Memory -9.5%
  Complex Build = v1 level (103 vs 103). Complex TIC faster than v1 (85.5 vs 87.1).
  Memory returned to v1 level (16.79 vs 16.76 KB).
  Remaining +5% regression is Simple Build (StateOptional type-checks in StagesExtension).
