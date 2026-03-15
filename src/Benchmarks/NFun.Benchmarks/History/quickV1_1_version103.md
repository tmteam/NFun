NFun Quick Benchmark — v1
==========================================================================================
Date: 2026-03-15 23:41:31
Machine: tmteams-MacBook-Pro  OS: Darwin 24.1.0 Darwin Kernel Version 24.1.0: Thu Oct 10 21:06:57 PDT 2024; root:xnu-11215.41.3~3/RELEASE_ARM64_T6041
Runtime: .NET 6.0.36  CPU: 14 cores  Config: Release
Memory: 36864MB available
Git: HEAD @ 8c6211be (dirty)
------------------------------------------------------------------------------------------
Baseline: ArithLoop 80K = 54.6 μs  (CV 1.2%)
1 turtle (t) = 5.5 ns = baseline / 10000. Relative unit; lower = faster.
  Simple=22(8upd)  Medium=13(4upd)  Complex=6(2upd)
Mode: HighPrecision (120s measurement, 130s total)
Rounds: 1000 (100 dropped, 900 used)  Batch: 10ms

Absolute (μs per script):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |  1.88 μs |  7.70 μs |  5.82 μs |  0.05 μs |  0.09 μs |  8.57 μs
Medium     |  5.77 μs |  31.4 μs |  25.6 μs |  0.50 μs |  0.70 μs |  38.4 μs
Complex    |  16.1 μs |   103 μs |  87.1 μs |  2.74 μs |  11.0 μs |   214 μs

Turtles (1t = 5.5ns = baseline/10000, lower is better):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |    345 t |  1,411 t |  1,066 t |    8.9 t |     16 t |  1,570 t
Medium     |  1,058 t |  5,759 t |  4,701 t |     92 t |    129 t |  7,048 t
Complex    |  2,943 t | 18,915 t | 15,973 t |    503 t |  2,022 t | 39,135 t
-----------+----------+----------+----------+----------+----------+----------
Weighted   |    459 t |  2,128 t |  1,669 t |     25 t |     56 t |  2,685 t

Stability (CV%):
           | Parse  | Build  |  Run   | Update
-----------+--------+--------+--------+-------
Simple     |  1.1%  |  1.2%  |  1.1%  |  1.6%
Medium     |  1.2%  |  1.3%  |  1.1%  |  1.0%
Complex    |  1.3%  |  1.7%  |  1.2%  |  1.1%

Memory (KB allocated per script):
           |  Parse   |  Build   |   Run    |  Update
-----------+----------+----------+----------+----------
Simple     |  2.91 KB |  11.8 KB |  0.09 KB |  0.20 KB
Medium     |  7.80 KB |  42.0 KB |  0.82 KB |  1.48 KB
Complex    |  20.6 KB |   137 KB |  4.00 KB |  21.7 KB

Compact (μs/script):  Parse / Build / Run / Update / Bld+10U
  Simple       1.88     7.70     0.05     0.09     8.57
  Medium       5.77     31.4     0.50     0.70     38.4
  Complex      16.1      103     2.74     11.0      214

Batch sizes: Simple.Parse=292, Simple.Build=62, Simple.Run=8922, Simple.Update=15687, Medium.Parse=150, Medium.Build=27, Medium.Run=1720, Medium.Update=3907, Complex.Parse=108, Complex.Build=17, Complex.Run=680, Complex.Update=451
