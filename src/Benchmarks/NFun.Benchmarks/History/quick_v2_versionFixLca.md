NFun Quick Benchmark — v1
==========================================================================================
Date: 2026-03-15 23:44:34
Machine: tmteams-MacBook-Pro  OS: Darwin 24.1.0 Darwin Kernel Version 24.1.0: Thu Oct 10 21:06:57 PDT 2024; root:xnu-11215.41.3~3/RELEASE_ARM64_T6041
Runtime: .NET 6.0.36  CPU: 14 cores  Config: Release
Memory: 36864MB available
Git: HEAD @ d19e8e15 (dirty)
------------------------------------------------------------------------------------------
Baseline: ArithLoop 80K = 60.2 μs  (CV 0.6%)
1 turtle (t) = 6.0 ns = baseline / 10000. Relative unit; lower = faster.
  Simple=22(8upd)  Medium=13(4upd)  Complex=6(2upd)
Mode: HighPrecision (120s measurement, 124s total)
Rounds: 1000 (100 dropped, 900 used)  Batch: 10ms

Absolute (μs per script):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |  1.88 μs |  8.36 μs |  6.47 μs |  0.05 μs |  0.09 μs |  9.21 μs
Medium     |  5.78 μs |  34.6 μs |  28.8 μs |  0.56 μs |  0.72 μs |  41.8 μs
Complex    |  16.0 μs |   112 μs |  95.7 μs |  2.78 μs |  10.9 μs |   221 μs

Turtles (1t = 6.0ns = baseline/10000, lower is better):
           |  Parse   |  Build   | TIC+Asm  |   Run    |  Update  | Bld+10U
-----------+----------+----------+----------+----------+----------+----------
Simple     |    313 t |  1,388 t |  1,075 t |    8.0 t |     14 t |  1,529 t
Medium     |    959 t |  5,742 t |  4,783 t |     93 t |    119 t |  6,934 t
Complex    |  2,661 t | 18,546 t | 15,885 t |    461 t |  1,811 t | 36,661 t
-----------+----------+----------+----------+----------+----------+----------
Weighted   |    416 t |  2,100 t |  1,684 t |     24 t |     50 t |  2,603 t

Stability (CV%):
           | Parse  | Build  |  Run   | Update
-----------+--------+--------+--------+-------
Simple     |  1.0%  |  1.2%  |  0.9%  |  0.8%
Medium     |  1.0%  |  1.1%  |  1.1%  |  0.9%
Complex    |  1.0%  |  1.2%  |  1.0%  |  1.3%

Memory (KB allocated per script):
           |  Parse   |  Build   |   Run    |  Update
-----------+----------+----------+----------+----------
Simple     |  2.91 KB |  12.9 KB |  0.09 KB |  0.09 KB
Medium     |  7.80 KB |  47.3 KB |  0.94 KB |  1.48 KB
Complex    |  20.6 KB |   151 KB |  4.00 KB |  21.7 KB

Compact (μs/script):  Parse / Build / Run / Update / Bld+10U
  Simple       1.88     8.36     0.05     0.09     9.21
  Medium       5.78     34.6     0.56     0.72     41.8
  Complex      16.0      112     2.78     10.9      221

Batch sizes: Simple.Parse=278, Simple.Build=54, Simple.Run=9383, Simple.Update=15095, Medium.Parse=126, Medium.Build=23, Medium.Run=1479, Medium.Update=3983, Complex.Parse=104, Complex.Build=16, Complex.Run=527, Complex.Update=463
