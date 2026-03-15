NFun Quick Benchmark — v1
==========================================================================================
Date: 2026-03-15 23:47:48
Machine: tmteams-MacBook-Pro  OS: Darwin 24.1.0 Darwin Kernel Version 24.1.0: Thu Oct 10 21:06:57 PDT 2024; root:xnu-11215.41.3~3/RELEASE_ARM64_T6041
Runtime: .NET 6.0.36  CPU: 14 cores  Config: Release
Memory: 36864MB available
Git: HEAD @ a20a1b8d (dirty)
------------------------------------------------------------------------------------------
Baseline: ArithLoop 80K = 57.7 μs  (CV 1.7%)
1 turtle (t) = 5.8 ns = baseline / 10000. Relative unit; lower = faster.
  Simple=22(8upd)  Medium=13(4upd)  Complex=6(2upd)
Mode: HighPrecision (120s measurement, 118s total)
Rounds: 1000 (100 dropped, 900 used)  Batch: 10ms

Absolute (μs per script):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |  2.01 μs |  8.74 μs |  6.73 μs |  0.04 μs |  0.08 μs |  9.58 μs
Medium     |  6.18 μs |  35.8 μs |  29.6 μs |  0.56 μs |  0.69 μs |  42.7 μs
Complex    |  17.2 μs |   115 μs |  98.2 μs |  2.71 μs |  10.8 μs |   223 μs

Turtles (1t = 5.8ns = baseline/10000, lower is better):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |    349 t |  1,515 t |  1,166 t |    7.5 t |     15 t |  1,661 t
Medium     |  1,071 t |  6,211 t |  5,140 t |     97 t |    120 t |  7,408 t
Complex    |  2,978 t | 19,999 t | 17,022 t |    470 t |  1,865 t | 38,648 t
-----------+----------+----------+----------+----------+----------+----------
Weighted   |    464 t |  2,283 t |  1,819 t |     24 t |     51 t |  2,797 t

Stability (CV%):
           | Parse  | Build  |  Run   | Update
-----------+--------+--------+--------+-------
Simple     |  1.1%  |  1.1%  |  1.0%  |  0.8%
Medium     |  1.0%  |  1.1%  |  0.9%  |  0.9%
Complex    |  0.9%  |  1.2%  |  0.9%  |  0.9%

Memory (KB allocated per script):
           |  Parse   |  Build   |   Run    |  Update
-----------+----------+----------+----------+----------
Simple     |  3.02 KB |  13.1 KB |  0.09 KB |  0.09 KB
Medium     |  8.09 KB |  48.0 KB |  0.95 KB |  1.49 KB
Complex    |  21.4 KB |   153 KB |  4.05 KB |  21.7 KB

Compact (μs/script):  Parse / Build / Run / Update / Bld+10U
  Simple       2.01     8.74     0.04     0.08     9.58
  Medium       6.18     35.8     0.56     0.69     42.7
  Complex      17.2      115     2.71     10.8      223

Batch sizes: Simple.Parse=266, Simple.Build=54, Simple.Run=9967, Simple.Update=15305, Medium.Parse=125, Medium.Build=23, Medium.Run=1488, Medium.Update=1668, Complex.Parse=99, Complex.Build=16, Complex.Run=535, Complex.Update=467
