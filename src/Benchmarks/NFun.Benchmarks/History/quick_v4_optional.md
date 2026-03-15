NFun Quick Benchmark — v1
==========================================================================================
Date: 2026-03-16 00:17:26
Machine: tmteams-MacBook-Pro  OS: Darwin 24.1.0 Darwin Kernel Version 24.1.0: Thu Oct 10 21:06:57 PDT 2024; root:xnu-11215.41.3~3/RELEASE_ARM64_T6041
Runtime: .NET 6.0.36  CPU: 14 cores  Config: Release
Memory: 36864MB available
Git: optional_types @ 966c2990
------------------------------------------------------------------------------------------
Baseline: ArithLoop 80K = 54.7 μs  (CV 1.9%)
1 turtle (t) = 5.5 ns = baseline / 10000. Relative unit; lower = faster.
  Simple=22(8upd)  Medium=13(4upd)  Complex=6(2upd)
Mode: HighPrecision (120s measurement, 131s total)
Rounds: 1000 (100 dropped, 900 used)  Batch: 10ms

Absolute (μs per script):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |  1.99 μs |  8.61 μs |  6.61 μs |  0.04 μs |  0.08 μs |  9.45 μs
Medium     |  6.13 μs |  35.2 μs |  29.1 μs |  0.56 μs |  0.69 μs |  42.1 μs
Complex    |  17.1 μs |   114 μs |  96.6 μs |  2.68 μs |  10.5 μs |   219 μs

Turtles (1t = 5.5ns = baseline/10000, lower is better):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |    364 t |  1,573 t |  1,209 t |    7.9 t |     15 t |  1,727 t
Medium     |  1,121 t |  6,433 t |  5,313 t |    101 t |    126 t |  7,694 t
Complex    |  3,134 t | 20,781 t | 17,647 t |    489 t |  1,921 t | 39,994 t
-----------+----------+----------+----------+----------+----------+----------
Weighted   |    485 t |  2,369 t |  1,884 t |     25 t |     54 t |  2,905 t

Stability (CV%):
           | Parse  | Build  |  Run   | Update
-----------+--------+--------+--------+-------
Simple     |  1.4%  |  1.5%  |  1.1%  |  1.0%
Medium     |  1.4%  |  1.6%  |  1.4%  |  1.2%
Complex    |  1.4%  |  1.6%  |  1.3%  |  1.5%

Memory (KB allocated per script):
           |  Parse   |  Build   |   Run    |  Update
-----------+----------+----------+----------+----------
Simple     |  3.02 KB |  13.1 KB |  0.13 KB |  0.09 KB
Medium     |  8.09 KB |  48.0 KB |  0.95 KB |  1.49 KB
Complex    |  21.4 KB |   153 KB |  4.05 KB |  21.7 KB

Compact (μs/script):  Parse / Build / Run / Update / Bld+10U
  Simple       1.99     8.61     0.04     0.08     9.45
  Medium       6.13     35.2     0.56     0.69     42.1
  Complex      17.1      114     2.68     10.5      219

Batch sizes: Simple.Parse=292, Simple.Build=58, Simple.Run=10555, Simple.Update=16349, Medium.Parse=133, Medium.Build=24, Medium.Run=1548, Medium.Update=4110, Complex.Parse=102, Complex.Build=17, Complex.Run=741, Complex.Update=373
